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

        public SearchQueryRepository(ILogger<SearchQueryRepository> logger,
            UserSettingRepository userSettingRepository, TagRepository tagRepository) {

            _Logger = logger;

            _TagRepository = tagRepository;
            _UserSettingRepository = userSettingRepository;
        }

        public async Task<NpgsqlCommand> Compile(Ast ast, AppAccount user) {
            List<UserSetting> settings = await _UserSettingRepository.GetByAccountID(user.ID);

            NpgsqlCommand sqlCmd = new();
            sqlCmd.CommandType = System.Data.CommandType.Text;

            string cmd = @$"
                SELECT 
                    *
                FROM 
                    post p
                WHERE 1 = 1 AND 
            ";

            QuerySetup query = new();

            cmd += await _Compile(ast.Root, query);

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

            cmd += $"ORDER BY {(query.OrderBy ?? "id desc")}";
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
                } else if (field.Token.Value == "md5") {
                    cmd = $" p.md5 " + parseOperation(op) + $"${query.Parameters.Count + 1}\n";
                    query.Parameters.Add(value.Token.Value);

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
                _ => throw new Exception($"invalid sort field: {node.Token.Value}")
            };
        }

        private class QuerySetup {

            //public string Cmd { get; set; } = "";

            public List<object?> Parameters { get; set; } = new();

            public string OrderBy { get; set; } = "id desc";

            public uint Offset { get; set; }

            public uint Limit { get; set; }

            public bool SetRating { get; set; } = false;

            public bool SetStatus { get; set; } = false;

        }

    }
}
