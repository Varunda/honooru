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

            short statusID = reader.GetInt16("status");
            if (Enum.IsDefined((PostStatus) statusID)) {
                post.Status = (PostStatus)statusID;
            } else {
                throw new InvalidCastException($"{statusID} is not a valid {nameof(PostStatus)}");
            }

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
            post.FileExtension = reader.GetString("file_extension");
            post.IqdbHash = reader.GetString("iqdb_hash");
            post.FileSizeBytes = reader.GetInt64("file_size_bytes");
            post.FileType = reader.GetString("file_type");
            post.DurationSeconds = reader.GetInt64("duration_seconds");
            post.Width = reader.GetInt64("width");
            post.Height = reader.GetInt64("height");

            return post;
        }

    }
}
