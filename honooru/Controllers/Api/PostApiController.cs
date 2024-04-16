using FFMpegCore;
using honooru.Code;
using honooru.Code.Constants;
using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.App.Iqdb;
using honooru.Models.Config;
using honooru.Models.Db;
using honooru.Models.Internal;
using honooru.Models.Queues;
using honooru.Models.Search;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Parsing;
using honooru.Services.Queues;
using honooru.Services.Repositories;
using honooru.Services.Util;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [Route("/api/post")]
    [ApiController]
    public class PostApiController : ApiControllerBase {

        private readonly ILogger<PostApiController> _Logger;

        private readonly IOptions<StorageOptions> _StorageOptions;

        private readonly AppCurrentAccount _CurrentAccount;
        private readonly PostRepository _PostRepository;
        private readonly MediaAssetRepository _MediaAssetRepository;
        private readonly TagRepository _TagRepository;
        private readonly TagTypeRepository _TagTypeRepository;
        private readonly PostTagRepository _PostTagRepository;
        private readonly FileExtensionService _FileExtensionHelper;
        private readonly TagImplicationRepository _TagImplicationRepository;
        private readonly TagInfoRepository _TagInfoRepository;
        private readonly IqdbClient _IqdbClient;

        private readonly BaseQueue<ThumbnailCreationQueueEntry> _ThumbnailCreationQueue;
        private readonly BaseQueue<TagInfoUpdateQueueEntry> _TagInfoUpdateQueue;

        private readonly SearchQueryRepository _SearchQueryRepository;
        private readonly SearchQueryParser _SearchQueryParser;

        private static readonly string[] TAG_SEPARATOR = [" ", "\n", "\r"];

        public PostApiController(ILogger<PostApiController> logger,
            IOptions<StorageOptions> storageOptions,
            PostRepository postRepo, AppCurrentAccount currentAccount,
            MediaAssetRepository mediaAssetDb, TagDb tagDb,
            TagTypeRepository tagTypeRepository, PostTagRepository postTagDb,
            SearchQueryRepository searchQueryRepository, SearchQueryParser searchQueryParser,
            BaseQueue<ThumbnailCreationQueueEntry> thumbnailCreationQueue, TagRepository tagRepository,
            BaseQueue<TagInfoUpdateQueueEntry> tagInfoUpdateQueue, FileExtensionService fileExtensionHelper,
            TagImplicationRepository tagImplicationRepository, IqdbClient iqdbClient,
            TagInfoRepository tagInfoRepository) {

            _Logger = logger;

            _StorageOptions = storageOptions;

            _PostRepository = postRepo;
            _CurrentAccount = currentAccount;
            _MediaAssetRepository = mediaAssetDb;
            _TagTypeRepository = tagTypeRepository;
            _PostTagRepository = postTagDb;
            _TagRepository = tagRepository;

            _SearchQueryRepository = searchQueryRepository;
            _SearchQueryParser = searchQueryParser;
            _ThumbnailCreationQueue = thumbnailCreationQueue;
            _TagInfoUpdateQueue = tagInfoUpdateQueue;
            _FileExtensionHelper = fileExtensionHelper;
            _TagImplicationRepository = tagImplicationRepository;
            _IqdbClient = iqdbClient;
            _TagInfoRepository = tagInfoRepository;
        }

        /// <summary>
        ///     get a <see cref="Post"/> by its <see cref="Post.ID"/>
        /// </summary>
        /// <param name="postID">ID of the <see cref="Post"/> to get</param>
        /// <response code="200">
        ///     the response will contain the <see cref="Post"/> with <see cref="Post.ID"/>
        ///     of <paramref name="postID"/>
        /// </response>
        /// <response code="204">
        ///     no <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/> exists
        /// </response>
        [HttpGet("{postID}")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<Post>> GetByID(ulong postID) {
            Post? post = await _PostRepository.GetByID(postID);

            if (post == null) {
                return ApiNoContent<Post>();
            }

            return ApiOk(post);
        }

        /// <summary>
        ///     search for posts
        /// </summary>
        /// <param name="q">input search query</param>
        /// <param name="offset">offset into the search</param>
        /// <param name="limit">how many search results to return, max 500</param>
        /// <param name="includeTags">if <see cref="SearchResults.Tags"/> will be populated or not</param>
        /// <response code="200">
        ///     the response will contain a <see cref="SearchResults"/> for the search performed
        /// </response>
        [HttpGet("search")]
        [PermissionNeeded(AppPermission.APP_VIEW)]
        public async Task<ApiResponse<SearchResults>> Search(
            [FromQuery] string q = "",
            [FromQuery] uint offset = 0,
            [FromQuery] uint limit = 100,
            [FromQuery] bool includeTags = true
            ) {

            AppAccount? currentUser = await _CurrentAccount.Get();
            if (currentUser == null) {
                return ApiAuthorize<SearchResults>();
            }

            if (limit > 500) {
                return ApiBadRequest<SearchResults>($"{nameof(limit)} cannot be higher than 500");
            }

            // set a default search of the most recent posts if one is not given
            if (string.IsNullOrWhiteSpace(q)) {
                q = "sort:id_desc";
            }

            // parse the query into an AST that can be compiled into an SQL query
            Stopwatch timer = Stopwatch.StartNew();
            Ast searchAst = _SearchQueryParser.Parse(q);
            long parseMs = timer.ElapsedMilliseconds; timer.Restart();

            SearchQuery query = new(searchAst);
            query.Offset = offset;
            query.Limit = limit * 10; // get 10 pages
            _Logger.LogDebug($"query parsed [offset={offset}] [limit={limit}]");

            // with the parsed AST, perform the search, passing the current user to include user settings
            timer.Start();
            List<Post> posts = await _PostRepository.Search(query, currentUser);
            long dbMs = timer.ElapsedMilliseconds; timer.Restart();

            SearchResults results = new(query);
            results.Results = posts[..(int)Math.Min(limit, posts.Count)]; // this is probably fine ????
            results.PageCount = (int)Math.Ceiling(posts.Count / (decimal)limit);
            _Logger.LogDebug($"search done [limit={limit}] [posts.Count={posts.Count}] [PageCount={results.PageCount}]");

            // if the request wants the tags as well, get those here
            if (includeTags == true) {
                HashSet<ulong> tagIDs = new();
                foreach (Post post in results.Results) {
                    List<PostTag> postTags = await _PostTagRepository.GetByPostID(post.ID);
                    tagIDs.AddRange(postTags.Select(iter => iter.TagID));
                }

                List<Tag> tags = await _TagRepository.GetByIDs(tagIDs);
                results.Tags = await _TagRepository.CreateExtended(tags);
                long tagMs = timer.ElapsedMilliseconds; timer.Restart();
                results.Timings.Add($"tag={tagMs}ms");
            }

            results.Timings.Add($"parse={parseMs}ms");
            results.Timings.Add($"db={dbMs}ms");

            return ApiOk(results);
        }

        /// <summary>
        ///     get similar images to the input IQDB hash
        /// </summary>
        /// <param name="iqdb">IQDB hash</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="IqdbQueryResult"/>s
        /// </response>
        [HttpGet("similar/{iqdb}")]
        public async Task<ApiResponse<List<IqdbQueryResult>>> SearchByIqdbHash(string iqdb) {
            if (string.IsNullOrWhiteSpace(iqdb)) {
                return ApiBadRequest<List<IqdbQueryResult>>($"{nameof(iqdb)} cannot be empty");
            }

            List<IqdbQueryResult> results = await _IqdbClient.GetSimilar(iqdb);

            foreach (IqdbQueryResult r in results) {
                string[] parts = r.PostID.Split("-");
                string md5 = parts[0];

                r.Post = await _PostRepository.GetByMD5(md5);
                if (r.Post == null) {
                    r.MediaAsset = await _MediaAssetRepository.GetByMD5(md5);
                }

                if (r.Post == null && r.MediaAsset == null) {
                    _Logger.LogWarning($"missing both post and media asset for IQDB entry [PostID/MD5={r.PostID}/{md5}]");
                }
            }

            return ApiOk(results);
        }

        /// <summary>
        ///     create a new <see cref="Post"/> from a <see cref="MediaAsset"/> that is ready to be used
        /// </summary>
        /// <remarks>
        ///     if any <see cref="Tag"/>s from <paramref name="tags"/> do not exist, then they are created.
        ///     tags are space separated, and can contain a colon before the tag to change what <see cref="TagType"/>
        ///     the tag has, for both inserting and updating.
        ///     <br/>
        ///     <br/>
        ///     examples:<br/>
        ///         <code>
        /// player:varunda base:indar_excavation_site montage youtube 
        ///         </code>
        ///         will create each tag if needed, and will set the type of 'varunda' to player,
        ///         and the type of indar_excavation_site to base, if the type is already set.
        ///         the types of montage and youtube will not be changed, or if the tags do not exist, 
        ///         will be set to general
        /// </remarks>
        /// <param name="assetID">The <see cref="MediaAsset.Guid"/> of the <see cref="MediaAsset"/> to use</param>
        /// <param name="tags">a string containing all the <see cref="Tag"/>s that will be added to this post</param>
        /// <param name="rating">what the <see cref="Post.Rating"/> will be. valid values are: 'g', 'u' and 'e' (or any string that starts with those)</param>
        /// <param name="title">optional <see cref="Post.Title"/></param>
        /// <param name="description">optional <see cref="Post.Description"/></param>
        /// <param name="source">where the <see cref="Post"/> comes from</param>
        /// 
        /// <response code="200">
        ///     the response will contain the newly created <see cref="Post"/> using
        ///     the parameters passed
        /// </response>
        /// 
        /// <response code="400">
        ///     one of the following validation errors occured:
        ///     <ul>
        ///         <li>
        ///             an invalid <paramref name="rating"/> was passed.
        ///             valid values are: 'g', 'u', and 'e' (or any string that starts with those letters)
        ///         </li>
        ///         <li>
        ///             the <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="assetID"/>
        ///             was not fully processed (according to <see cref="MediaAsset.Status"/>, which is was not <see cref="MediaAssetStatus.DONE"/>)
        ///         </li>
        ///         <li>
        ///             one of the space deliminated tags from <paramref name="tags"/> was invalid.
        ///             a tag can be invalid if one of the following conditions is met:
        ///             <ul>
        ///                 <li>the tag contained more than one ':' (which are used to change the <see cref="TagType"/> of a tag)</li>
        ///                 <li>the tag contained invalid characters</li>
        ///                 <li>when creating a new tag, and a <see cref="TagType"/> is specified by using a ':', the <see cref="TagType"/> could not be found</li>
        ///             </ul>
        ///         </li>
        ///     </ul>
        /// </response>
        /// 
        /// <response code="404">
        ///     no <see cref="MediaAsset"/> with <see cref="MediaAsset.Guid"/> of <paramref name="assetID"/> exists
        /// </response>
        /// 
        [HttpPost("{assetID:guid}")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse<Post>> Create(Guid assetID, 
            [FromQuery] string tags,
            [FromQuery] string rating,
            [FromQuery] string? title = null,
            [FromQuery] string? description = null,
            [FromQuery] string? source = ""
            ) {

            AppAccount? currentUser = await _CurrentAccount.Get();
            if (currentUser == null) {
                return ApiAuthorize<Post>();
            }

            Post post = new();
            post.PosterUserID = currentUser.ID;
            post.Timestamp = DateTime.UtcNow;
            post.Title = title;
            post.Description = description;
            post.Source = source ?? ""; 

            if (string.IsNullOrEmpty(source)) {
                tags += " missing_source";
            }

            if (rating.StartsWith("g")) {
                post.Rating = PostRating.GENERAL;
            } else if (rating.StartsWith("u")) {
                post.Rating = PostRating.UNSAFE;
            } else if (rating.StartsWith("e")) {
                post.Rating = PostRating.EXPLICIT;
            } else {
                return ApiBadRequest<Post>($"invalid rating '{rating}'");
            }

            MediaAsset? asset = await _MediaAssetRepository.GetByID(assetID);
            if (asset == null) {
                return ApiNotFound<Post>($"{nameof(MediaAsset)} {assetID}");
            }

            if (asset.Status != MediaAssetStatus.DONE) {
                return ApiBadRequest<Post>($"{nameof(MediaAsset)} {assetID} is still processing, try again later");
            }

            if (asset.IqdbHash == null) {
                return ApiBadRequest<Post>($"{nameof(MediaAsset)} {assetID} does not have an IQDB hash set!");
            }

            post.MD5 = asset.MD5;
            post.FileName = asset.FileName;
            post.FileExtension = asset.FileExtension;
            post.FileSizeBytes = asset.FileSizeBytes;
            post.FileType = asset.FileType;
            post.Status = PostStatus.OK;
            post.IqdbHash = asset.IqdbHash;

            // parse additional tags and add metadata such as width//height and duration (if video)
            try {
                string? fileType = _FileExtensionHelper.GetFileType(post.FileExtension);
                string p = Path.Combine(_StorageOptions.Value.RootDirectory, "original", post.FileLocation);

                string newTags = "";

                if (fileType == "image") {
                    _Logger.LogDebug($"analyze upload as an image [path={p}]");
                    using FileStream imageFile = System.IO.File.Open(p, FileMode.Open);

                    MagickImage mImage = new(imageFile);
                    int height = mImage.BaseHeight;
                    int width = mImage.BaseWidth;

                    post.Width = width;
                    post.Height = height;
                    post.DurationSeconds = 0;

                    if (height >= 10_000 || width >= 10_000) { newTags += " incredibly_absurdres"; }
                    if (height >= 3200 || width >= 3200) { newTags += " absurdres";  }
                    if (height >= 1600 || width >= 1600) { newTags += " highres"; }
                    if (height <= 500 && width <= 500) { newTags += " lowres"; }
                    if (width >= 1024 && (width * 4 > height)) { newTags += " wide_image"; }
                    if (height >= 1024 && (height * 4 > width)) { newTags += " tall_image"; }

                } else if (fileType == "video") {
                    _Logger.LogDebug($"analyzing upload as a video [path={p}]");
                    tags += " video animated";

                    IMediaAnalysis analysis = await FFProbe.AnalyseAsync(p);
                    if (analysis.PrimaryAudioStream != null) { newTags += " sound"; }
                    if (analysis.Duration >= TimeSpan.FromMinutes(10)) { newTags += " long_video"; }

                    post.DurationSeconds = (long)analysis.Duration.TotalSeconds;

                    if (analysis.PrimaryVideoStream != null) {
                        post.Width = analysis.PrimaryVideoStream.Width;
                        post.Height = analysis.PrimaryVideoStream.Height;
                    } else {
                        _Logger.LogWarning($"missing primary video stream, cannot set width and height [MediaAssetID={assetID}]");
                    }
                } else {
                    _Logger.LogWarning($"unchecked filetype when automatically adding tags [FileExtension={post.FileExtension}] [fileType={fileType}]");
                }

                if (newTags.Length > 0) {
                    tags += newTags;
                    _Logger.LogInformation($"adding automatic tags to post [newTags={newTags}]");
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to auto generated tags for post [MD5={post.MD5}] [FileExtension={post.FileExtension}]");
            }

            if (asset.AdditionalTags.Length > 0) {
                tags += asset.AdditionalTags;
                _Logger.LogInformation($"adding additional tags from uploading [additionalTags={asset.AdditionalTags}]");
            }

            // tag parsing!
            List<string> tag = _SplitTags(tags);
            _Logger.LogDebug($"tags found [tags=[{string.Join(" ", tag)}]]");
            HashSet<ulong> tagIds = new(); // list of tag IDs to insert
            foreach (string t in tag) {
                string iter = t.ToLower().Trim();
                if (iter.Length == 0) {
                    continue;
                }

                string? tagType = null;

                // for inputs like art:varunda_(artist), create the tag as a certain type
                int colonCount = t.Count(iter => iter == ':');
                if (colonCount == 1) {
                    string[] parts = t.Split(":");
                    if (parts.Length != 2) {
                        throw new Exception($"failed to split {t} into two parts from ':'");
                    }

                    tagType = parts[0];
                    iter = parts[1];
                } else if (colonCount > 1) {
                    return ApiBadRequest<Post>($"tag {t} has more than one colon in it");
                }

                if (Tag.Validate(iter) == false) {
                    return ApiBadRequest<Post>($"invalid tag '{iter}'");
                }

                TagType? tagTypeObj = (tagType == null) ? null : await _TagTypeRepository.GetByNameOrAlias(tagType);
                Tag tagObj = await _TagRepository.GetOrCreateByName(iter, tagTypeObj);

                if (tagObj.ID == 0) {
                    throw new Exception($"missing tag ID from {t}");
                }

                List<TagImplication> implications = await _TagImplicationRepository.GetBySourceTagID(tagObj.ID);
                tagIds.AddRange(implications.Select(iter => iter.TagB));

                tagIds.Add(tagObj.ID);
            }

            post.ID = await _PostRepository.Insert(post);

            // queue the thumbnail creation
            ThumbnailCreationQueueEntry entry = new() {
                MD5 = post.MD5,
                FileExtension = post.FileExtension
            };
            _ThumbnailCreationQueue.Queue(entry);

            _Logger.LogDebug($"inserting tags for new post [postID={post.ID}] [tag count={tagIds.Count}]");
            foreach (ulong tagID in tagIds) {
                PostTag postTag = new();
                postTag.PostID = post.ID;
                postTag.TagID = tagID;
                await _PostTagRepository.Insert(postTag);

                // queue the uses update now that it's been changed
                _TagInfoUpdateQueue.Queue(new TagInfoUpdateQueueEntry() {
                    TagID = tagID
                });
            }

            await _MediaAssetRepository.Delete(assetID);

            return ApiOk(post);
        }

        /// <summary>
        ///     update an existing <see cref="Post"/> with new information
        /// </summary>
        /// <param name="postID">ID of the <see cref="Post"/> to update</param>
        /// <param name="tags">optional, leave null to not change. what tags to update the post with</param>
        /// <param name="rating">optional, leave null to not chagne. what rating to update the post with</param>
        /// <param name="title">optional, leave null to not change. what title to update the post with</param>
        /// <param name="description">optional, leave null to not change. what description to update the post with</param>
        /// <param name="source">optional, leave null to not change. what source to update the post with</param>
        /// <exception cref="Exception"></exception>
        /// <response code="200">
        ///     the <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/> was successfully
        ///     updated with all the non-null parameters. any parameter left <c>null</c> will result in that
        ///     field not being updated
        /// </response>
        /// <response code="400">
        ///     one of the following validation errors occurred:
        ///     <ul>
        ///         <li><paramref name="rating"/> was not a valid valid</li>
        ///     </ul>
        /// </response>
        [HttpPost("{postID}")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse> Update(ulong postID,
            [FromQuery] string? tags = null,
            [FromQuery] string? rating = null,
            [FromQuery] string? title = null,
            [FromQuery] string? description = null,
            [FromQuery] string? source = null
            ) {

            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound($"{nameof(Post)} {postID}");
            }

            AppAccount? currentUser = await _CurrentAccount.Get();
            if (currentUser == null) {
                return ApiAuthorize();
            }

            bool doSave = false;

            if (rating != null) {
                if (rating.StartsWith("g")) {
                    doSave = doSave || post.Rating != PostRating.GENERAL;
                    post.Rating = PostRating.GENERAL;
                } else if (rating.StartsWith("u")) {
                    doSave = doSave || post.Rating != PostRating.UNSAFE;
                    post.Rating = PostRating.UNSAFE;
                } else if (rating.StartsWith("e")) {
                    doSave = doSave || post.Rating != PostRating.EXPLICIT;
                    post.Rating = PostRating.EXPLICIT;
                } else {
                    return ApiBadRequest($"invalid rating '{rating}'");
                }
            }

            if (title != null) {
                doSave = doSave || post.Title != title;
                post.Title = title;
            }

            if (description != null) {
                doSave = doSave || post.Description != description;
                post.Description = description;
            }

            if (source != null) {
                doSave = doSave || post.Source != source;
                post.Source = source;
            }

            if (doSave == true) {
                post.LastEditorUserID = currentUser.ID;
                post.LastEdited = DateTime.UtcNow;
                _Logger.LogDebug($"post updated from query, performing DB update [postID={postID}]");
                await _PostRepository.Update(postID, post);
            }

            if (tags != null) {
                List<PostTag> postTags = await _PostTagRepository.GetByPostID(postID);
                List<Tag> currentTags = await _TagRepository.GetByIDs(postTags.Select(iter => iter.TagID));

                // save current tags, removed as we find them in |tags|
                // this represents the set of tags to remove from a post AFTER all tags have been processed
                HashSet<ulong> tagsToRemove = new(postTags.Select(iter => iter.TagID));
                HashSet<ulong> tagsToAdd = new();
                HashSet<ulong> tagsToKeep = new();

                List<string> tagsInput = _SplitTags(tags);

                foreach (string tag in tagsInput) {
                    string iter = tag.ToLower().Trim();
                    if (iter.Length == 0) {
                        continue;
                    }

                    string? tagType = null;

                    // for inputs like art:varunda_(artist), create the tag as a certain type
                    int colonCount = tag.Count(iter => iter == ':');
                    if (colonCount == 1) {
                        string[] parts = tag.Split(":");
                        if (parts.Length != 2) {
                            throw new Exception($"failed to split {tag} into two parts from ':'");
                        }

                        tagType = parts[0];
                        iter = parts[1];
                    } else if (colonCount > 1) {
                        return ApiBadRequest($"tag {tag} has more than one colon in it");
                    }

                    if (Tag.Validate(iter) == false) {
                        return ApiBadRequest($"invalid tag '{iter}'");
                    }

                    TagType? tagTypeObj = (tagType == null) ? null : await _TagTypeRepository.GetByNameOrAlias(tagType);
                    Tag tagObj = await _TagRepository.GetOrCreateByName(iter, tagTypeObj);

                    if (tagObj.ID == 0) {
                        throw new Exception($"missing tag ID from {tag}");
                    }

                    // if the tag is in the set of tags to remove, that tag still present for the post, and isn't removed
                    // (yes this could be shorter, but it's more clear to me like this)
                    if (tagsToRemove.Contains(tagObj.ID) == true) {
                        tagsToRemove.Remove(tagObj.ID);
                        tagsToKeep.Add(tagObj.ID);
                    } else {
                        // otherwise, the tag isn't already part of the post, so we need to create a new one
                        tagsToAdd.Add(tagObj.ID);

                        List<TagImplication> implications = await _TagImplicationRepository.GetBySourceTagID(tagObj.ID);
                        tagsToAdd.AddRange(implications.Select(iter => iter.TagB));
                    }
                }

                _Logger.LogDebug($"processed tags for update [postID={postID}] [tagsToRemove={string.Join(" ", tagsToRemove)}] "
                    + $"[tagsToAdd={string.Join(" ", tagsToAdd)}] [tagsToKeep={string.Join(" ", tagsToKeep)}]");

                foreach (ulong tagID in tagsToRemove) {
                    _Logger.LogTrace($"removing tag from post [tagID={tagID}] [postID={postID}]");
                    await _PostTagRepository.Delete(postID, tagID);

                    // queue the uses update now that it's been changed
                    _TagInfoUpdateQueue.Queue(new TagInfoUpdateQueueEntry() {
                        TagID = tagID
                    });
                }

                foreach (ulong tagID in tagsToAdd) {
                    _Logger.LogTrace($"adding new tag to post [tagID={tagID}] [postID={postID}]");
                    await _PostTagRepository.Insert(new PostTag() { PostID = postID, TagID = tagID });

                    // queue the uses update now that it's been changed
                    _TagInfoUpdateQueue.Queue(new TagInfoUpdateQueueEntry() {
                        TagID = tagID
                    });
                }
            }

            return ApiOk();
        }

        /// <summary>
        ///     enter a <see cref="Post"/> into the thumbnail creation queue for a forced-remake
        /// </summary>
        /// <param name="postID">ID of the <see cref="Post"/> to update</param>
        /// <response code="200">
        ///     the <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/>
        ///     successfully had it's thumbnail queued for recreation.
        ///     NOTE: this DOES NOT MEAN the thumbnail was recreated, just that is was queued for recreation
        /// </response>
        /// <response code="404">
        ///     no <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/> exists
        /// </response>
        [HttpPost("{postID}/remake-thumbnail")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse> RemakeThumbnail(ulong postID) {
            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound($"{nameof(Post)} {postID}");
            }

            ThumbnailCreationQueueEntry entry = new() {
                MD5 = post.MD5,
                FileExtension = post.FileExtension,
                RecreateIfNeeded = true
            };
            _ThumbnailCreationQueue.Queue(entry);

            return ApiOk();
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="postID"></param>
        /// <returns></returns>
        [HttpPost("{postID}/regenerate-iqdb")]
        public async Task<ApiResponse<IqdbEntry>> RegenerateIqdbHash(ulong postID) {
            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound<IqdbEntry>($"{nameof(Post)} {postID}");
            }

            ServiceHealthEntry health = await _IqdbClient.CheckHealth();
            if (health.Enabled == false) {
                return ApiInternalError<IqdbEntry>($"IQDB client cannot handle requests currently: {health.Message}");
            }

            string path = Path.Combine(_StorageOptions.Value.RootDirectory, "original", post.FileLocation);
            _Logger.LogDebug($"regenerated IQDB hash [postID={postID}] [path={path}]");

            await _IqdbClient.RemoveByMD5(post.MD5);
            IqdbEntry? entry = await _IqdbClient.Create(path, post.MD5, post.FileExtension);

            if (entry == null) {
                return ApiInternalError<IqdbEntry>($"failed to generate IQDB hash, please check console logs");
            }

            _Logger.LogInformation($"regenerated hash for media asset [postID={postID}] [hash={entry.Hash}]");

            post.IqdbHash = entry.Hash;
            await _PostRepository.Update(post.ID, post);

            return ApiOk(entry);
        }

        /// <summary>
        ///     mark a <see cref="Post"/> as deleted, which does not remove the file itself.
        ///     does not remove the IQDB entry either
        /// </summary>
        /// <param name="postID">ID of the <see cref="Post"/> to delete</param>
        /// <response code="200">
        ///     the <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/>
        ///     was successfully marked as deleted
        /// </response>
        [HttpDelete("{postID}")]
        [PermissionNeeded(AppPermission.APP_POST_DELETE)]
        public async Task<ApiResponse> Delete(ulong postID) {
            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound($"{nameof(Post)} {postID}");
            }

            if (post.Status == PostStatus.DELETED) {
                return ApiBadRequest($"{nameof(Post)} {postID} is already deleted");
            }

            await _PostRepository.Delete(postID);

            _Logger.LogInformation($"marked post as deleted [postID={postID}]");
            return ApiOk();
        }

        /// <summary>
        ///     restore a deleted post
        /// </summary>
        /// <param name="postID">ID of the <see cref="Post"/> to restore</param>
        /// <response code="200">
        ///     the <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/>
        ///     successfully had its <see cref="Post.Status"/> updated to <see cref="PostStatus.OK"/>
        /// </response>
        /// <response code="400">
        ///     the <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/>
        ///     does not have a <see cref="Post.Status"/> of <see cref="PostStatus.DELETED"/>.
        ///     only deleted posts can be restored
        /// </response>
        /// <response code="404">
        ///     no <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="postID"/> exists
        /// </response>
        [HttpPost("{postID}/restore")]
        public async Task<ApiResponse> Restore(ulong postID) {
            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound($"{nameof(Post)} {postID}");
            }

            if (post.Status != PostStatus.DELETED) {
                return ApiBadRequest($"{nameof(Post)} {postID} is not deleted");
            }

            await _PostRepository.Restore(postID);

            return ApiOk();
        }

        /// <summary>
        ///     completely erase a <see cref="Post"/>, deleting the IQDB entries and all files with it
        /// </summary>
        /// <param name="postID">ID of the post to erase</param>
        /// <returns></returns>
        [HttpDelete("{postID}/erase")]
        [PermissionNeeded(AppPermission.APP_POST_ERASE)]
        public async Task<ApiResponse> Erase(ulong postID) {
            Post? post = await _PostRepository.GetByID(postID);
            if (post == null) {
                return ApiNotFound($"{nameof(Post)} {postID}");
            }

            if (post.Status == PostStatus.DELETED) {
                return ApiBadRequest($"{nameof(Post)} {postID} is already deleted");
            }

            await _PostRepository.Erase(postID);

            _Logger.LogInformation($"marked post as deleted [postID={postID}]");
            return ApiOk();
        }

        internal List<string> _SplitTags(string tags) {
            // making the tag distinct ensures that multiple of the same tag on upload doesn't throw an error
            return [.. tags.Trim().ToLower().Split(TAG_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct()];
        }

    }
}
