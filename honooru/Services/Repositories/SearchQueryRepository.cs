﻿using Google.Protobuf.Reflection;
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
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class SearchQueryRepository {

        private readonly ILogger<SearchQueryRepository> _Logger;

        private readonly TagRepository _TagRepository;
        private readonly UserSettingRepository _UserSettingRepository;
        private readonly FileExtensionService _ExtensionUtil;

        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_SEARCH = "SearchQuery.{0}.{1}"; // {0} => user ID, {1} => hash key

        public SearchQueryRepository(ILogger<SearchQueryRepository> logger,
            UserSettingRepository userSettingRepository, TagRepository tagRepository,
            FileExtensionService extensionUtil, IMemoryCache cache) {

            _Logger = logger;

            _TagRepository = tagRepository;
            _UserSettingRepository = userSettingRepository;
            _ExtensionUtil = extensionUtil;
            _Cache = cache;
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

            List<UserSetting> settings = await _UserSettingRepository.GetByAccountID(user.ID);

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
            //cmd += $" OFFSET {ast.Offset}\n";
            //cmd += $" LIMIT {ast.Limit}\n";
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

                //
                // the following meta operations can be performed:
                //      user:{NAME}
                //          search for posts uploaded by this user
                //      
                //      rating:"general"|"unsafe"|"explicit"
                //          search for posts with this rating
                //
                //      status:"ok"|"deleted"
                //          search for posts with this status
                //      
                //      md5:{string}
                //          search for posts with this md5
                //
                //      parent:{ID}
                //          search for posts with a parent of this post ID
                //  
                //      child:{ID}
                //          search for posts that are a child of this post ID
                //
                //      type:"image" | "video
                //          search for image or video posts
                //
                //      extension:{string}
                //          search for posts with this extension type
                //
                //      pool:{ID}
                //          search for posts that are in a pool
                //
                //      sort
                //          sort returned posts by this value
                //

                if (field.Token.Value == "user") {
                    if (ulong.TryParse(value.Token.Value, out ulong userID) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.poster_user_id " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(userID);
                } else if (field.Token.Value == "rating") {
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
                } else if (field.Token.Value == "status") {
                    PostStatus status;

                    if (value.Token.Value == "ok") {
                        status = PostStatus.OK;
                    } else if (value.Token.Value == "deleted") {
                        status = PostStatus.DELETED;
                    } else {
                        throw new Exception($"invalid status string '{value.Token.Value}', valid values are: ok, deleted");
                    }

                    query.SetStatus = true;

                    cmd = $" p.status = " + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add((short)status);
                } else if (field.Token.Value == "md5") { // select by md5
                    cmd = $" p.md5 " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(value.Token.Value);
                } else if (field.Token.Value == "parent") { // select posts with this post ID as a parent
                    if (ulong.TryParse(value.Token.Value, out ulong postID) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.id IN (SELECT distinct(child_post_id) FROM post_child WHERE parent_post_id = ${query.Parameters.Count + 1})\n";
                    query.Parameters.Add(postID);
                } else if (field.Token.Value == "child") { // select posts with this post ID as a child
                    if (ulong.TryParse(value.Token.Value, out ulong postID) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.id IN (SELECT distinct(parent_post_id) FROM post_child WHERE child_post_id = ${query.Parameters.Count + 1})\n";
                    query.Parameters.Add(postID);
                } else if (field.Token.Value == "type") {
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
                } else if (field.Token.Value == "extension") {
                    cmd = $" p.file_extension " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(value.Token.Value);
                } else if (field.Token.Value == "title") {
                    cmd = $"LOWER(p.title) LIKE '%' || ${query.Parameters.Count + 1} || '%'\n";
                    query.Parameters.Add(value.Token.Value.ToLower());
                } else if (field.Token.Value == "description") {
                    cmd = $"LOWER(p.description) LIKE '%' || ${query.Parameters.Count + 1} || '%'\n";
                    query.Parameters.Add(value.Token.Value.ToLower());
                } else if (field.Token.Value == "context") {
                    cmd = $"LOWER(p.context) LIKE '%' || ${query.Parameters.Count + 1} || '%'\n";
                    query.Parameters.Add(value.Token.Value.ToLower());
                } else if (field.Token.Value == "contains") {
                    cmd = $" (" +
                        $"LOWER(p.title) LIKE '%' || ${query.Parameters.Count + 1} || '%' " +
                        $"OR LOWER(p.description) LIKE '%' || ${query.Parameters.Count + 2} || '%'" +
                        $"OR LOWER(p.context) LIKE '%' || ${query.Parameters.Count + 3} || '%')\n";
                    query.Parameters.Add(value.Token.Value.ToLower());
                    query.Parameters.Add(value.Token.Value.ToLower());
                    query.Parameters.Add(value.Token.Value.ToLower());
                } else if (field.Token.Value == "pool") {
                    if (ulong.TryParse(value.Token.Value, out ulong poolID) == false) {
                        throw new Exception($"failed to parse {value.Token.Value} to a valid ulong");
                    }

                    cmd = $" p.id IN (select distinct(post_id) FROM post_pool_entry WHERE pool_id = ${query.Parameters.Count + 1})\n";
                    query.Parameters.Add(poolID);
                } else if (field.Token.Value == "sort") {
                    query.OrderBy = parseSort(value);
                } else {
                    throw new Exception($"invalid search field: {field} ({field.Token.Value} is not a valid search term)");
                }
            }

            return cmd;
        }

        public string parseOperation(Node node) {
            return node.Token.Value switch {
                "=" => "=",
                ">" => ">",
                "<" => ">",
                "!" => "!=",
                _ => throw new ArgumentException($"invalid field operator: {node.Token.Value}"),
            };
        }

        private string parseSort(Node node) {
            return node.Token.Value switch {
                "id" => "id",
                "id_desc" => "id DESC",
                "size" => "file_size_bytes",
                "size_desc" => "file_size_bytes DESC",
                "duration" => "duration_seconds",
                "duration_desc" => "duration_seconds DESC",
                "width" => "width",
                "width_desc" => "width DESC",
                "height" => "height",
                "height_desc" => "height DESC",
                _ => throw new Exception($"invalid sort field: {node.Token.Value}")
            };
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
