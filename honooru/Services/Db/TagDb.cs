﻿using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class TagDb {

        private readonly ILogger<TagDb> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<Tag> _Reader;

        public TagDb(ILogger<TagDb> logger,
            IDbHelper dbHelper, IDataReader<Tag> reader) {

            _Logger = logger;
            _DbHelper = dbHelper;
            _Reader = reader;
        }

        /// <summary>
        ///     get all <see cref="Tag"/>s in the DB
        /// </summary>
        /// <returns></returns>
        public async Task<List<Tag>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag;
            ");

            await cmd.PrepareAsync(cancel);

            List<Tag> tags = await _Reader.ReadList(cmd, cancel);
            await conn.CloseAsync();

            return tags;
        }

        public Task<List<Tag>> GetAll() {
            return GetAll(CancellationToken.None);
        }

        /// <summary>
        ///     get a single <see cref="Tag"/> by its <see cref="Tag.ID"/>
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task<Tag?> GetByID(ulong ID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);

            await cmd.PrepareAsync();

            Tag? tag = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return tag;
        }

        /// <summary>
        ///     get a <see cref="Tag"/> by its <see cref="Tag.Name"/>, which is unique
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Tag?> GetByName(string name) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM tag
                    WHERE name = @Name;
            ");

            cmd.AddParameter("Name", name.ToLower());

            await cmd.PrepareAsync();

            Tag? tag = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return tag;
        }

        /// <summary>
        ///     Insert a new <see cref="Tag"/>
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<ulong> Insert(Tag tag) {
            if (Tag.Validate(tag.Name) == false) {
                throw new ArgumentException($"not upserting tag with invalid name '{tag.Name}'");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO tag (
                    name, type_id 
                ) VALUES (
                    @Name, @TypeID
                ) RETURNING id;
            ");

            cmd.AddParameter("Name", tag.Name.ToLower());
            cmd.AddParameter("TypeID", tag.TypeID);

            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);

            await conn.CloseAsync();

            return id;
        }

        /// <summary>
        ///     update an existing <see cref="Tag"/>
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Tag> Update(Tag tag) {
            if (Tag.Validate(tag.Name) == false) {
                throw new ArgumentException($"not upserting tag with invalid name '{tag.Name}'");
            }

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE tag
                    SET name = @Name,
                        type_id = @TypeID
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", tag.ID);
            cmd.AddParameter("Name", tag.Name.ToLower());
            cmd.AddParameter("TypeID", tag.TypeID);

            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();

            await conn.CloseAsync();

            return tag;
        }

    }
}
