using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch02AddTagTables : IDbPatch {
        public int MinVersion => 2;
        public string Name => "add tag tables";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE EXTENSION IF NOT EXISTS pg_trgm;

                CREATE TABLE IF NOT EXISTS post (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY (START WITH 1),
                    poster_user_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL,
                    title varchar NULL,
                    description varchar NULL,
                    last_editor_user_id bigint NOT NULL DEFAULT 0,
                    last_edited timestamptz NULL,
                    md5 varchar NOT NULL,
                    rating smallint NOT NULL,
                    file_name varchar NOT NULL,
                    file_location varchar NOT NULL,
                    source varchar NOT NULL,
                    file_size_bytes bigint NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_post_md5 ON post(md5);

                CREATE TABLE IF NOT EXISTS tag (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY (START WITH 1),
                    name varchar NOT NULL,
                    type_id bigint NOT NULL,

                    UNIQUE(name)
                );

                CREATE INDEX IF NOT EXISTS idx_tag_name ON tag USING gin (name gin_trgm_ops);
    
                CREATE TABLE IF NOT EXISTS tag_type (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    name varchar NOT NULL,
                    hex_color varchar NOT NULL,
                    alias varchar NOT NULL,

                    UNIQUE(name),
                    UNIQUE(alias)
                );
    
                INSERT INTO tag_type (
                    id, name, hex_color, alias
                ) OVERRIDING SYSTEM VALUE VALUES
                    (1, 'General', '00ff00', 'gen'),
                    (2, 'Artist', 'f0f0f0', 'art'),
                    (3, 'Player', 'ff0000', 'play'),
                    (4, 'Outfit', '0000ff', 'out'),
                    (5, 'Base', 'ff00ff', 'base'),
                    (6, 'Meta', '00ffff', 'meta'),
                    (7, 'Source', 'ffff00', 'source')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS post_tag (
                    post_id bigint NOT NULL,
                    tag_id bigint NOT NULL,

                    PRIMARY KEY (post_id, tag_id)
                );

                CREATE INDEX IF NOT EXISTS idx_post_tag_post_id ON post_tag (post_id);
    
                CREATE INDEX IF NOT EXISTS idx_post_tag_tag_id ON post_tag(tag_id);

                CREATE TABLE IF NOT EXISTS post_rating (
                    id smallint NOT NULL PRIMARY KEY,
                    name varchar NOT NULL
                );

                INSERT INTO post_rating (id, name) VALUES 
                    (1, 'general'),
                    (2, 'unsafe'),
                    (3, 'explicit')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS media_asset (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY (START WITH 1),
                    md5 varchar NOT NULL,
                    file_name varchar NOT NULL,
                    file_location varchar NOT NULL,
                    timestamp timestamptz NOT NULL,
                    file_size_bytes bigint NOT NULL,

                    UNIQUE (md5)
                );

                CREATE INDEX IF NOT EXISTS idx_media_asset_md5 ON media_asset(md5);
            ");

        }

    }
}
