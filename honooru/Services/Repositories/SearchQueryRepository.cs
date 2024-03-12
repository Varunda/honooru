using honooru.Code.ExtensionMethods;
using honooru.Models.Api;
using honooru.Models.App;
using honooru.Models.Search;
using honooru.Services.Db;
using honooru.Services.Parsing;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {

    public class SearchQueryRepository {

        private readonly ILogger<SearchQueryRepository> _Logger;

        private readonly TagDb _TagDb;

        public SearchQueryRepository(ILogger<SearchQueryRepository> logger,
            TagDb tagDb) {

            _Logger = logger;

            _TagDb = tagDb;
        }

        public async Task<NpgsqlCommand> Compile(Ast ast) {

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

                Tag? tag = await _TagDb.GetByName(normalizedTag);
                if (tag != null) {
                    cmd += $" p.ID NOT IN (select distinct(post_id) AS post_id from post_tag WHERE tag_id = ${query.Parameters.Count + 1} )\n";
                    query.Parameters.Add(tag.ID);
                } else {
                    cmd += " 1 = 1 \n";
                }
            } else if (node.Type == NodeType.TAG) {
                string normalizedTag = node.Token.Value.ToLower();

                Tag? tag = await _TagDb.GetByName(normalizedTag);
                if (tag != null) {
                    cmd = $" p.ID IN (select distinct(post_id) AS post_id from post_tag WHERE tag_id = ${query.Parameters.Count + 1} )\n";
                    query.Parameters.Add(tag.ID);
                } else {
                    cmd = " 1 = 1 \n";
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
                } else if (field.Token.Value == "sort") {
                    query.OrderBy = parseSort(value);
                } else {
                    throw new Exception($"invalid search field: {field}");
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

        }

    }
}
