using Google.Protobuf.Reflection;
using honooru.Code.Constants;
using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.Db;
using honooru.Models.Search;
using honooru.Services.Db;
using honooru.Services.Parsing;
using honooru.Services.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class SearchQueryRepository {

        private readonly ILogger<SearchQueryRepository> _Logger;

        private readonly TagRepository _TagRepository;
        private readonly UserSettingRepository _UserSettingRepository;
        private readonly AppAccountRepository _AccountRepository;
        private readonly FileExtensionService _ExtensionUtil;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_SEARCH = "SearchQuery.{0}.{1}"; // {0} => user ID, {1} => hash key

        public SearchQueryRepository(ILogger<SearchQueryRepository> logger,
            UserSettingRepository userSettingRepository, TagRepository tagRepository,
            FileExtensionService extensionUtil, IMemoryCache cache,
            AppAccountRepository accountRepository) {

            _Logger = logger;

            _TagRepository = tagRepository;
            _UserSettingRepository = userSettingRepository;
            _ExtensionUtil = extensionUtil;
            _Cache = cache;
            _AccountRepository = accountRepository;
        }

        /// <summary>
        ///     compile a search query into an SQL statement
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<NpgsqlCommand> Compile(SearchQuery ast, AppAccount user) {
            string cacheKey = string.Format(CACHE_KEY_SEARCH, user.ID, ast.HashKey);
            _Logger.LogTrace($"checking if query was already compiled [cacheKey={cacheKey}]");
            if (_Cache.TryGetValue(cacheKey, out NpgsqlCommand? sqlCmd) == true && sqlCmd != null) {
                return sqlCmd;
            }

            sqlCmd = new NpgsqlCommand();
            sqlCmd.CommandType = System.Data.CommandType.Text;

            string cmd = @$"
                SELECT 
                    *
                FROM 
                    post p
                WHERE 1 = 1 AND 
            ";

            QuerySetup query = new();

            cmd += await _Compile(ast.QueryAst.Root, query);

            // if a query did not explicitly say what ratings to include, use the users settings to determine it
            if (query.SetRating == false) {
                List<UserSetting> settings = await _UserSettingRepository.GetByAccountID(user.ID);
                bool unsafeHide = settings.FirstOrDefault(iter => iter.Name == "postings.unsafe.behavior")?.Value == "hidden";
                bool explicitHide = settings.FirstOrDefault(iter => iter.Name == "postings.explicit.behavior")?.Value == "hidden";

                _Logger.LogTrace($"user did not specify rating, excluding based on user options [user.ID={user.ID}] [unsafeHide={unsafeHide}] [explicitHide={explicitHide}]");

                // at this part in the query, additions to the WHERE clause are still possible
                if (unsafeHide == true) {
                    cmd += " AND p.rating != 2\n";
                }

                if (explicitHide == true) {
                    cmd += " AND p.rating != 3\n";
                }
            }

            // if the query didn't set a status, assume the status is OK
            if (query.SetStatus == false) {
                cmd += " AND p.status = 1\n";
            }

            cmd += $" ORDER BY {(query.OrderBy ?? "id desc")}\n";
            sqlCmd.CommandText = cmd;

            foreach (object? param in query.Parameters) {
                sqlCmd.AddParameter(param);
            }

            return sqlCmd;
        }

        private async Task<string?> _Compile(Node node, QuerySetup query) {

            string? cmd = null;

            if (node.Type == NodeType.AND) {
                cmd = " ( ";
                List<string> childCmds = new();

                foreach (Node child in node.Children) {
                    string? cc = await _Compile(child, query);
                    if (cc != null) {
                        childCmds.Add(cc);
                    }
                }

                if (childCmds.Count == 0) {
                    childCmds.Add(" 1 = 1 ");
                }

                cmd += string.Join(" AND ", childCmds);
                cmd += ")\n";
            } else if (node.Type == NodeType.OR) {
                cmd = " (";
                List<string> childCmds = new();

                foreach (Node child in node.Children) {
                    string? cc = await _Compile(child, query);
                    if (cc != null) {
                        childCmds.Add(cc);
                    }
                }

                cmd += string.Join(" OR ", childCmds);
                cmd += ")\n";
            } else if (node.Type == NodeType.NOT) {
                string normalizedTag = node.Token.Value.ToLower();

                Tag? tag = await _TagRepository.GetByName(normalizedTag);
                if (tag != null) {
                    cmd += $" p.ID NOT IN (select distinct(post_id) AS post_id from post_tag WHERE tag_id = ${query.Parameters.Count + 1} )\n";
                    query.Parameters.Add(tag.ID);
                } else {
                    cmd += " 1 = 1 \n"; // if the tag does not exist, then no post includes this tag, so the term is effectively a noop
                }
            } else if (node.Type == NodeType.TAG) {
                string normalizedTag = node.Token.Value.ToLower();

                Tag? tag = await _TagRepository.GetByName(normalizedTag);
                if (tag != null) {
                    cmd = $" p.ID IN (select distinct(post_id) AS post_id from post_tag WHERE tag_id = ${query.Parameters.Count + 1} )\n";
                    query.Parameters.Add(tag.ID);
                } else {
                    cmd = " 0 = 1 \n"; // if the tag does not exist, a search is not valid, as no post contains the tag
                }
            } else if (node.Type == NodeType.META) {
                Node field = node.Children[0];
                Node op = node.Children[1];
                Node value = node.Children[2];

                // select posts based on who posted it
                // user:name OR id
                if (field.Token.Value == "user") {

                    // first, get the account by name
                    AppAccount? account = await _AccountRepository.GetByName(value.Token.Value, CancellationToken.None);
                    if (account == null) {
                        // if the account doesn't exist, next try by ID
                        if (ulong.TryParse(value.Token.Value, out ulong userID) == false) {
                            throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                        }
                        account = await _AccountRepository.GetByID(userID, CancellationToken.None);
                    }

                    if (account != null) {
                        cmd = $" p.poster_user_id " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                        query.Parameters.Add(account.ID);
                    } else {
                        cmd = $" 1 = 0\n"; // the account doesn't exist, there are no posts from this user
                    }
                }

                // select posts based on rating
                // rating:general, rating:unsafe, rating:explicit
                else if (field.Token.Value == "rating") {
                    PostRating rating;
                    if (value.Token.Value == "general") {
                        rating = PostRating.GENERAL;
                    } else if (value.Token.Value == "unsafe") {
                        rating = PostRating.UNSAFE;
                    } else if (value.Token.Value == "explicit") {
                        rating = PostRating.EXPLICIT;
                    } else {
                        throw new Exception($"invalid rating string '{value.Token.Value}', valid values are: general, unsafe, explicit");
                    }

                    query.SetRating = true;

                    cmd = $" p.rating = " + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add((short)rating);
                }

                // select posts based on status
                // status:ok, status:deleted
                else if (field.Token.Value == "status") {
                    PostStatus status;

                    if (value.Token.Value == "ok") {
                        status = PostStatus.OK;
                    } else if (value.Token.Value == "deleted") {
                        status = PostStatus.DELETED;
                    } else {
                        throw new Exception($"invalid status string '{value.Token.Value}', valid values are: ok, deleted");
                    }

                    query.SetStatus = true;

                    cmd = $" p.status = ${query.Parameters.Count + 1}\n";
                    query.Parameters.Add((short)status);
                }

                // select by MD5
                // md5:$string
                else if (field.Token.Value == "md5") { // select by md5
                    cmd = $" p.md5 " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(value.Token.Value);
                }

                // select posts with this post ID as a parent
                // parent:103
                else if (field.Token.Value == "parent") { // select posts with this post ID as a parent
                    if (ulong.TryParse(value.Token.Value, out ulong postID) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.id IN (SELECT distinct(child_post_id) FROM post_child WHERE parent_post_id = ${query.Parameters.Count + 1})\n";
                    query.Parameters.Add(postID);
                }

                // select posts with this post ID as a child
                // child:103
                else if (field.Token.Value == "child") { // select posts with this post ID as a child
                    if (ulong.TryParse(value.Token.Value, out ulong postID) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.id IN (SELECT distinct(parent_post_id) FROM post_child WHERE child_post_id = ${query.Parameters.Count + 1})\n";
                    query.Parameters.Add(postID);
                }

                // select based on file type of the post
                // type:image
                //      will select all posts with a file type of image
                // type:video
                else if (field.Token.Value == "type") {
                    string type;
                    if (value.Token.Value == "image") {
                        type = "image";
                    } else if (value.Token.Value == "video") {
                        type = "video";
                    } else {
                        throw new Exception($"unchecked value '{field.Token.Value}'. valid values are: 'image', 'video'");
                    }

                    cmd = $" p.file_type = ${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(type);
                }

                // select based on the file extension
                // extension:png
                //      will select all posts with the "png" file extension
                // extension:.jpg
                //      bad example! file extensions are not stored with a leading dot
                else if (field.Token.Value == "extension") {
                    cmd = $" p.file_extension " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(value.Token.Value);
                }

                // select based on the title containing the input string
                // title:gelos
                else if (field.Token.Value == "title") {
                    cmd = $" LOWER(p.title) LIKE '%' || ${query.Parameters.Count + 1} || '%'\n";
                    query.Parameters.Add(value.Token.Value.ToLower());
                }

                // select based on the description containing the input string
                // description:gelos
                else if (field.Token.Value == "description") {
                    cmd = $" LOWER(p.description) LIKE '%' || ${query.Parameters.Count + 1} || '%'\n";
                    query.Parameters.Add(value.Token.Value.ToLower());
                }

                // select based on the context containing the input string
                // context:gelos
                else if (field.Token.Value == "context") {
                    cmd = $" LOWER(p.context) LIKE '%' || ${query.Parameters.Count + 1} || '%'\n";
                    query.Parameters.Add(value.Token.Value.ToLower());
                }

                // select based on the title, description or context containing the input string
                // contains:gelos
                //      will select all posts that contain the string "gelos" in the title, description or context fields
                else if (field.Token.Value == "contains") {
                    cmd = $" (" +
                        $"LOWER(p.title) LIKE '%' || ${query.Parameters.Count + 1} || '%' " +
                        $"OR LOWER(p.description) LIKE '%' || ${query.Parameters.Count + 2} || '%'" +
                        $"OR LOWER(p.context) LIKE '%' || ${query.Parameters.Count + 3} || '%')\n";
                    query.Parameters.Add(value.Token.Value.ToLower());
                    query.Parameters.Add(value.Token.Value.ToLower());
                    query.Parameters.Add(value.Token.Value.ToLower());
                }

                // select based on posts within a post pool
                // pool:103
                //      will select all posts within a pool. no check is performed to ensure that the post pool with that ID exists
                else if (field.Token.Value == "pool") {
                    if (ulong.TryParse(value.Token.Value, out ulong poolID) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.id IN (select distinct(post_id) FROM post_pool_entry WHERE pool_id = ${query.Parameters.Count + 1})\n";
                    query.Parameters.Add(poolID);
                }

                // select posts based on duration
                // duration:>3m4s
                //      will select posts with a duration of greater than 3 minutes and 4 seconds
                // duration:10s
                //      will select posts with a duration of example 10 seconds
                // duration:<10m
                //      will select posts with a duration less than 10 minutes
                else if (field.Token.Value == "duration") {
                    if (DurationStringUtil.TryParse(value.Token.Value, out TimeSpan span) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid duration");
                    }

                    cmd = $" p.duration " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(span.TotalSeconds);

                // select posts based on width
                // width:>100
                //      will select posts with a width of 100 or greater
                } else if (field.Token.Value == "width") {
                    if (ulong.TryParse(value.Token.Value, out ulong v) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.width " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(v);
                }

                // select posts based on height
                // height:>100
                //      will select posts with a height of 100 or greater
                else if (field.Token.Value == "height") {
                    if (ulong.TryParse(value.Token.Value, out ulong v) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.height " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(v);
                }

                // select posts based on how many tags it has
                // tag_count:>25
                //      will select posts with more than 25 tags
                else if (field.Token.Value == "tag_count") {
                    if (ulong.TryParse(value.Token.Value, out ulong v) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.id in (select distinct(post_id) from post_tag GROUP BY post_id HAVING COUNT(*) " + parseOperation(op) + $"${query.Parameters.Count + 1})\n";
                    query.Parameters.Add(v);
                }

                // handle sort 
                // sort:width_desc
                else if (field.Token.Value == "sort") {
                    query.OrderBy = parseSort(value);
                }

                // otherwise we don't know how to handle this token
                else {
                    throw new Exception($"invalid search field: {field} ({field.Token.Value} is not a valid search term)");
                }
            }

            return cmd;
        }

        /// <summary>
        ///     parse the operator node of a token into the SQL value
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static string parseOperation(Node node) {
            return node.Token.Value switch {
                "=" => "=",
                ">" => ">",
                "<" => ">",
                "!" => "!=",
                _ => throw new ArgumentException($"invalid field operator: {node.Token.Value}"),
            };
        }

        /// <summary>
        ///     parse a sort field into a SQL statement. adding _asc or _desc on the end will respect that value
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static string parseSort(Node node) {

            string[] parts = node.Token.Value.Split("_");

            string field = parts[0] switch {
                "id" => "id",
                "size" => "file_size_bytes",
                "duration" => "duration_seconds",
                "width" => "width",
                "height" => "height",
                _ => throw new Exception($"invalid sort field: {parts[0]}")
            };

            if (parts.Length == 2) {
                if (parts[1] == "desc") {
                    field += " DESC ";
                } else if (parts[1] == "asc") {
                    field += " ASC ";
                } else {
                    throw new Exception($"invalid sort order: {parts[1]}");
                }
            }

            return field;
        }

        private class QuerySetup {

            public List<object?> Parameters { get; set; } = new();

            public string OrderBy { get; set; } = "id desc";

            public uint Offset { get; set; }

            public uint Limit { get; set; }

            public bool SetRating { get; set; } = false;

            public bool SetStatus { get; set; } = false;

        }

    }
}
