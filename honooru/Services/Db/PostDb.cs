using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.Db;
using honooru.Services.Parsing;
using honooru.Services.Repositories;
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

        private readonly SearchQueryRepository _SearchQueryRepository;

        public PostDb(ILogger<PostDb> logger,
            IDataReader<Post> reader, IDbHelper dbHelper,
            SearchQueryRepository searchQueryRepository) {

            _Logger = logger;
            _Reader = reader;
            _DbHelper = dbHelper;

            _SearchQueryRepository = searchQueryRepository;
        }

        /// <summary>
        ///     get all <see cref="Post"/>s
        /// </summary>
        /// <returns>a list of <see cref="Post"/>s</returns>
        public async Task<List<Post>> GetAll() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM post;
            ");

            await cmd.PrepareAsync();

            List<Post> posts = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return posts;
        }

        /// <summary>
        ///     perform a search
        /// </summary>
        /// <param name="query">query that will be performed. use a <see cref="SearchQueryParser"/> to obtain this from a string</param>
        /// <param name="user">
        ///     account of the user making the request. used for filtering unsafe/explicit content if the user does not want to see it
        /// </param>
        /// <returns>
        ///     a list of <see cref="Post"/>s that fulfill the search parameters from <paramref name="query"/>
        /// </returns>
        public async Task<List<Post>> Search(SearchQuery query, AppAccount user) {
            using NpgsqlConnection conn = _DbHelper.Connection();

            NpgsqlCommand cmd = await _SearchQueryRepository.Compile(query, user);
            cmd.Connection = conn;
            await cmd.Connection.OpenAsync();

            _Logger.LogDebug(cmd.Print());

            List<Post> post = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return post;
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
                    poster_user_id, timestamp, status, title, description, context, last_editor_user_id, iqdb_hash,
                    md5, rating, file_name, source, file_extension, file_size_bytes, duration_seconds, width, height, file_type
                ) VALUES (
                    @PosterUserID, @Timestamp, @Status, @Title, @Description, @Context, 0, @IqdbHash,
                    @MD5, @Rating, @FileName, @Source, @FileExtension, @FileSizeBytes, @DurationSeconds, @Width, @Height, @FileType
                ) RETURNING id;
            ");

            cmd.AddParameter("PosterUserID", post.PosterUserID);
            cmd.AddParameter("Timestamp", post.Timestamp);
            cmd.AddParameter("Status", (short) post.Status);
            cmd.AddParameter("Title", post.Title);
            cmd.AddParameter("Description", post.Description);
            cmd.AddParameter("Context", post.Context);
            cmd.AddParameter("IqdbHash", post.IqdbHash);
            cmd.AddParameter("MD5", post.MD5);
            cmd.AddParameter("Rating", (short) post.Rating);
            cmd.AddParameter("FileName", post.FileName);
            cmd.AddParameter("Source", post.Source);
            cmd.AddParameter("FileExtension", post.FileExtension);
            cmd.AddParameter("FileSizeBytes", post.FileSizeBytes);
            cmd.AddParameter("DurationSeconds", post.DurationSeconds);
            cmd.AddParameter("Width", post.Width);
            cmd.AddParameter("Height", post.Height);
            cmd.AddParameter("FileType", post.FileType);
            await cmd.PrepareAsync();

            ulong id = await cmd.ExecuteUInt64(CancellationToken.None);

            await conn.CloseAsync();

            return id;
        }

        /// <summary>
        ///     update an existing <see cref="Post"/> with new information
        /// </summary>
        /// <param name="postID"></param>
        /// <param name="post"></param>
        /// <returns></returns>
        public async Task Update(ulong postID, Post post) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE 
                    post
                SET title = @Title,
                    description = @Description,
                    context = @Context,
                    rating = @Rating,
                    source = @Source,
                    last_editor_user_id = @LastUserEditorID,
                    last_edited = @LastEdited,
                    file_type = @FileType,
                    iqdb_hash = @IqdbHash
                WHERE
                    id = @ID;
            ");

            cmd.AddParameter("ID", postID);
            cmd.AddParameter("Title", post.Title);
            cmd.AddParameter("Description", post.Description);
            cmd.AddParameter("Context", post.Context);
            cmd.AddParameter("Rating", (short) post.Rating);
            cmd.AddParameter("Source", post.Source);
            cmd.AddParameter("LastUserEditorID", post.LastEditorUserID);
            cmd.AddParameter("LastEdited", post.LastEdited);
            cmd.AddParameter("FileType", post.FileType);
            cmd.AddParameter("IqdbHash", post.IqdbHash);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        /// <summary>
        ///     update the <see cref="Post.Status"/> of a <see cref="Post"/> based on ID
        /// </summary>
        /// <param name="postID">ID of the <see cref="Post"/> to update</param>
        /// <param name="status">the new <see cref="PostStatus"/> to set <see cref="Post.Status"/> to</param>
        /// <returns>a Task for the async operation</returns>
        public async Task UpdateStatus(ulong postID, PostStatus status) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE 
                    post
                SET
                    status = @Status
                WHERE
                    id = @ID;
            ");

            cmd.AddParameter("ID", postID);
            cmd.AddParameter("Status", (short) status);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        /// <summary>
        ///     delete a <see cref="Post"/> from the DB. if you want to mark a post as deleted,
        ///     instead call <see cref="UpdateStatus(ulong, PostStatus)"/> with <see cref="PostStatus.DELETED"/>
        /// </summary>
        /// <param name="postID">ID of the <see cref="Post"/> to delete</param>
        /// <returns></returns>
        public async Task Delete(ulong postID) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                DELETE from post
                    WHERE id = @ID;
            ");

            cmd.AddParameter("ID", postID);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
