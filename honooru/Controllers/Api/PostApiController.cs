﻿using honooru.Code.Constants;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.Config;
using honooru.Models.Db;
using honooru.Models.Queues;
using honooru.Models.Search;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Parsing;
using honooru.Services.Queues;
using honooru.Services.Repositories;
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
        private readonly PostDb _PostDb;
        private readonly MediaAssetDb _MediaAssetDb;
        private readonly TagRepository _TagRepository;
        private readonly TagTypeDb _TagTypeDb;
        private readonly PostTagDb _PostTagDb;

        private readonly BaseQueue<ThumbnailCreationQueueEntry> _ThumbnailCreationQueue;

        private readonly SearchQueryRepository _SearchQueryRepository;
        private readonly SearchQueryParser _SearchQueryParser;

        public PostApiController(ILogger<PostApiController> logger,
            IOptions<StorageOptions> storageOptions,
            PostDb postDb, AppCurrentAccount currentAccount,
            MediaAssetDb mediaAssetDb, TagDb tagDb,
            TagTypeDb tagTypeDb, PostTagDb postTagDb,
            SearchQueryRepository searchQueryRepository, SearchQueryParser searchQueryParser,
            BaseQueue<ThumbnailCreationQueueEntry> thumbnailCreationQueue, TagRepository tagRepository) {

            _Logger = logger;

            _StorageOptions = storageOptions;

            _PostDb = postDb;
            _CurrentAccount = currentAccount;
            _MediaAssetDb = mediaAssetDb;
            _TagTypeDb = tagTypeDb;
            _PostTagDb = postTagDb;
            _TagRepository = tagRepository;

            _SearchQueryRepository = searchQueryRepository;
            _SearchQueryParser = searchQueryParser;
            _ThumbnailCreationQueue = thumbnailCreationQueue;
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
        public async Task<ApiResponse<Post>> GetByID(ulong postID) {
            Post? post = await _PostDb.GetByID(postID);

            if (post == null) {
                return ApiNoContent<Post>();
            }

            return ApiOk(post);
        }

        /// <summary>
        ///     search for posts
        /// </summary>
        /// <param name="q"></param>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        [HttpGet("search")]
        public async Task<ApiResponse<SearchResults>> Search(
            [FromQuery] string q,
            [FromQuery] uint offset = 0,
            [FromQuery] uint limit = 100
            ) {

            if (limit > 500) {
                return ApiBadRequest<SearchResults>($"{nameof(limit)} cannot be higher than 500");
            }

            Stopwatch timer = Stopwatch.StartNew();
            Ast searchAst = _SearchQueryParser.Parse(q);
            long parseMs = timer.ElapsedMilliseconds; timer.Restart();

            SearchQuery query = new(searchAst);
            query.Offset = offset;
            query.Limit = limit;

            timer.Start();
            List<Post> posts = await _PostDb.Search(query);
            long dbMs = timer.ElapsedMilliseconds; timer.Restart();

            SearchResults results = new(query);
            results.Results = posts;

            results.Timings.Add($"parseMs={parseMs}");
            results.Timings.Add($"dbMs={dbMs}");

            return ApiOk(results);
        }

        [HttpPost("{assetID}")]
        //[Authorize]
        public async Task<ApiResponse<Post>> Create(Guid assetID, 
            [FromQuery] string tags,
            [FromQuery] string rating,
            [FromQuery] string? title = null,
            [FromQuery] string? description = null,
            [FromQuery] string source = ""
            ) {

            Post post = new();
            post.PosterUserID = 0;
            post.Timestamp = DateTime.UtcNow;
            post.Title = title;
            post.Description = description;
            post.Source = source;

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

            MediaAsset? asset = await _MediaAssetDb.GetByID(assetID);
            if (asset == null) {
                return ApiNotFound<Post>($"{nameof(MediaAsset)} {assetID}");
            }

            if (asset.Status != MediaAssetStatus.DONE) {
                return ApiBadRequest<Post>($"{nameof(MediaAsset)} {assetID} is still processing, try again later");
            }

            post.MD5 = asset.MD5;
            post.FileName = asset.FileName;
            post.FileExtension = asset.FileExtension;
            post.FileSizeBytes = asset.FileSizeBytes;

            // tag parsing!
            List<string> tag = tags.ToLower().Split(" ").ToList();
            HashSet<ulong> tagIds = new(); // list of tag IDs to insert
            foreach (string t in tag) {
                string iter = t.ToLower();
                string tagType = "General";

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

                // get the tag type
                TagType? tagTypeObj = await _TagTypeDb.GetByName(tagType);
                if (tagTypeObj == null) {
                    // check the alias
                    _Logger.LogDebug($"failed to find tag type, using alias next [tagType={tagType}]");
                    tagTypeObj = await _TagTypeDb.GetByAlias(tagType);
                }

                // if still no tag object, fail
                if (tagTypeObj == null) {
                    return ApiBadRequest<Post>($"bad tag type {tagType} from tag {t}");
                }

                // get the tag, create it if it doesn't exist
                Tag? tagObj = await _TagRepository.GetByName(iter);
                if (tagObj == null) { // tag doesn't exist, make it
                    _Logger.LogDebug($"creating new tag [tag={iter}] [typeID={tagTypeObj.ID}]");
                    tagObj = new Tag() {
                        Name = iter,
                        TypeID = tagTypeObj.ID
                    };

                    tagObj.ID = await _TagRepository.Insert(tagObj);
                    _Logger.LogDebug($"created new tag [tag={tagObj.Name}] [tagID={tagObj.ID}]");
                } else {
                    // check if the type has changed, for example if the tag varunda has a type of general,
                    //      inputting play:varunda would change the type to player
                    _Logger.LogTrace($"checking if tag type update is needed [tag={tagObj.ID}/{tagObj.Name}] [tagType={tagTypeObj.ID}/{tagTypeObj.Name}]");
                    if (tagObj.TypeID != tagTypeObj.ID) {
                        _Logger.LogDebug($"updating tag type [tag={tagObj.ID}/{tagObj.Name}] [tagType={tagTypeObj.ID}/{tagTypeObj.Name}]");
                        tagObj.TypeID = tagTypeObj.ID;
                        await _TagRepository.Update(tagObj);
                    }
                }

                if (tagObj.ID == 0) {
                    throw new Exception($"missing tag ID from {t}");
                }

                tagIds.Add(tagObj.ID);
            }

            post.ID = await _PostDb.Insert(post);

            ThumbnailCreationQueueEntry entry = new() {
                FileName = post.MD5 + post.FileExtension
            };
            _ThumbnailCreationQueue.Queue(entry);

            _Logger.LogDebug($"inserting tags for new post [postID={post.ID}] [tag count={tagIds.Count}]");
            foreach (ulong tagID in tagIds) {
                PostTag postTag = new();
                postTag.PostID = post.ID;
                postTag.TagID = tagID;
                await _PostTagDb.Insert(postTag);
            }

            return ApiOk(post);
        }

    }
}
