using honooru.Code.Constants;
using honooru.Code.ExtensionMethods;
using honooru.Models.Db;
using Npgsql;
using System;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class PostReader : IDataReader<Post> {

        public override Post? ReadEntry(NpgsqlDataReader reader) {
            Post post = new();

            post.ID = reader.GetUInt64("id");
            post.PosterUserID = reader.GetUInt64("poster_user_id");
            post.Timestamp = reader.GetDateTime("timestamp");
            post.Title = reader.GetNullableString("title");
            post.Description = reader.GetNullableString("description");
            post.LastEditorUserID = reader.GetUInt64("last_editor_user_id");
            post.LastEdited = reader.GetNullableDateTime("last_edited");
            post.MD5 = reader.GetString("md5");

            short ratingID = reader.GetInt16("rating");
            if (Enum.IsDefined((PostRating) ratingID)) {
                post.Rating = (PostRating)ratingID;
            } else {
                throw new InvalidCastException($"{ratingID} is not a valid {nameof(PostRating)}");
            }

            post.FileName = reader.GetString("file_name");
            post.Source = reader.GetString("source");
            post.FileLocation = reader.GetString("file_location");
            post.FileSizeBytes = reader.GetInt64("file_size_bytes");

            return post;
        }

    }
}
