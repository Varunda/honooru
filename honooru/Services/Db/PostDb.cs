﻿using honooru.Code.ExtensionMethods;
using honooru.Models.Api;
using honooru.Models.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Db {

    public class PostDb {

        private readonly ILogger<PostDb> _Logger;
        private readonly IDataReader<Post> _Reader;
        private readonly IDbHelper _DbHelper;

        public PostDb(ILogger<PostDb> logger,
            IDataReader<Post> reader, IDbHelper dbHelper) {

            _Logger = logger;
            _Reader = reader;
            _DbHelper = dbHelper;
        }

        public async Task<List<Post>> Search(SearchQuery query) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post
                    WHERE id = @ID;
            ");

            //cmd.AddParameter("ID", ID);

            await cmd.PrepareAsync();

            Post? post = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return new List<Post>();
        }

        /// <summary>
        ///     get a single <see cref="Post"/> by its <see cref="Post.ID"/>
        /// </summary>
        /// <param name="ID">ID of the <see cref="Post"/> to get</param>
        /// <returns>
        ///     the <see cref="Post"/> with <see cref="Post.ID"/> of <paramref name="ID"/>,
        ///     or <c>null</c> if it does not exist
        /// </returns>
        public async Task<Post?> GetByID(ulong ID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", ID);

            await cmd.PrepareAsync();

            Post? post = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return post;
        }

        /// <summary>
        ///     get a single <see cref="Post"/> by its <see cref="Post.MD5"/>
        /// </summary>
        /// <param name="md5">lower case md5 string</param>
        /// <returns>
        ///     the <see cref="Post"/> with <see cref="Post.MD5"/> of <paramref name="md5"/>,
        ///     or <c>null</c> if it does not exist
        /// </returns>
        public async Task<Post?> GetByMD5(string md5) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post
                    WHERE md5 = @MD5;
            ");

            cmd.AddParameter("MD5", md5.ToLower());

            await cmd.PrepareAsync();

            Post? post = await _Reader.ReadSingle(cmd);
            await conn.CloseAsync();

            return post;
        }

        /// <summary>
        ///     insert a new <see cref="Post"/>, returning the ID of the newly created DB entry
        /// </summary>
        /// <param name="post">parameters used to insert</param>
        /// <returns>
        ///     the <see cref="Post.ID"/> of the DB entry that was just inserted
        /// </returns>
        public async Task<ulong> Insert(Post post) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO post (
                    poster_user_id, timestamp, title, description, last_editor_user_id, md5, rating, file_name, source, file_location, file_size_bytes
                ) VALUES (
                    @PosterUserID, @Timestamp, @Title, @Description, 0, @MD5, @Rating, @FileName, @Source, @FileLocation, @FileSizeBytes
                ) RETURNING id;
            ");

            cmd.AddParameter("PosterUserID", post.PosterUserID);
            cmd.AddParameter("Timestamp", post.Timestamp);
            cmd.AddParameter("Title", post.Title);
            cmd.AddParameter("Description", post.Description);
            cmd.AddParameter("MD5", post.MD5);
            cmd.AddParameter("Rating", (short) post.Rating);
            cmd.AddParameter("FileName", post.FileName);
            cmd.AddParameter("Source", post.Source);
            cmd.AddParameter("FileLocation", post.FileLocation);
            cmd.AddParameter("FileSizeBytes", post.FileSizeBytes);
            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);

            await conn.CloseAsync();

            return id;
        }


    }
}
