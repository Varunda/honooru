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

                CREATE TABLE IF NOT EXISTS post_rating (
                    id smallint NOT NULL PRIMARY KEY,
                    name varchar NOT NULL
                );

                INSERT INTO post_rating (id, name) VALUES 
                    (1, 'general'),
                    (2, 'unsafe'),
                    (3, 'explicit')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS post_status (
                    id smallint NOT NULL PRIMARY KEY,
                    name varchar NOT NULL
                );

                INSERT INTO post_status (id, name) VALUES 
                    (1, 'ok'),
                    (2, 'deleted')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS post (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY (START WITH 1),
                    poster_user_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL,
                    status smallint NOT NULL,
                    title varchar NULL,
                    description varchar NULL,
                    last_editor_user_id bigint NOT NULL DEFAULT 0,
                    last_edited timestamptz NULL,
                    md5 varchar NOT NULL,
                    rating smallint NOT NULL,
                    file_name varchar NOT NULL,
                    file_extension varchar NOT NULL,
                    source varchar NOT NULL,
                    file_size_bytes bigint NOT NULL,
                    duration_seconds bigint NOT NULL,
                    width bigint NOT NULL,
                    height bigint NOT NULL,

                    UNIQUE(md5),

                    CONSTRAINT fk_post_rating FOREIGN KEY (rating) REFERENCES post_rating(id),
                    CONSTRAINT fk_post_status FOREIGN KEY (status) REFERENCES post_status(id)
                );

                CREATE INDEX IF NOT EXISTS idx_post_md5 ON post(md5);
    
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
                    (1, 'General', '009be6', 'gen'),
                    (2, 'Artist', 'ff8a8b', 'art'),
                    (3, 'Player', '35c64a', 'play'),
                    (4, 'Group', '0000ff', 'grp'),
                    (5, 'Base', 'ff00ff', 'base'),
                    (6, 'Meta', 'ead084', 'meta'),
                    (7, 'Source', 'c797ff', 'source')
                ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS tag (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY (START WITH 1),
                    name varchar NOT NULL,
                    type_id bigint NOT NULL,

                    UNIQUE(name),
                    CONSTRAINT fk_tag_type_type_id FOREIGN KEY(type_id) REFERENCES tag_type(id)
                );

                CREATE INDEX IF NOT EXISTS idx_tag_name ON tag USING gin (name gin_trgm_ops);

                CREATE TABLE IF NOT EXISTS tag_implication (
                    tag_a BIGINT NOT NULL,
                    tag_b BIGINT NOT NULL,
                
                    UNIQUE(tag_a, tag_b),
                    CONSTRAINT fk_tag_implication_tag_a FOREIGN KEY(tag_a) REFERENCES tag(id),
                    CONSTRAINT fk_tag_implication_tag_b FOREIGN KEY(tag_b) REFERENCES tag(id)
                );

                CREATE INDEX IF NOT EXISTS idx_tag_implication_tag_a ON tag_implication(tag_a);
                CREATE INDEX IF NOT EXISTS idx_tag_implication_tag_b ON tag_implication(tag_b);

                CREATE TABLE IF NOT EXISTS post_tag (
                    post_id bigint NOT NULL,
                    tag_id bigint NOT NULL,

                    PRIMARY KEY (post_id, tag_id),
                    CONSTRAINT fk_post_tag_tag_id FOREIGN KEY (tag_id) REFERENCES tag(id),
                    CONSTRAINT fk_post_tag_post_id FOREIGN KEY (post_id) REFERENCES post(id)
                );

                CREATE INDEX IF NOT EXISTS idx_post_tag_post_id ON post_tag (post_id);
                CREATE INDEX IF NOT EXISTS idx_post_tag_tag_id ON post_tag(tag_id);

                CREATE TABLE IF NOT EXISTS media_asset (
                    id UUID NOT NULL PRIMARY KEY,
                    post_id bigint NULL,
                    md5 varchar NOT NULL,
                    status int NOT NULL,
                    file_name varchar NOT NULL,
                    file_extension varchar NOT NULL,
                    timestamp timestamptz NOT NULL,
                    file_size_bytes bigint NOT NULL,
                    source varchar NOT NULL,
                    additional_tags varchar NOT NULL,
                    title varchar NOT NULL,
                    descriptiont varchar NOT NULL,

                    UNIQUE (md5)
                );

                CREATE INDEX IF NOT EXISTS idx_media_asset_md5 ON media_asset(md5);

                CREATE TABLE IF NOT EXISTS tag_info (
                    id bigint NOT NULL PRIMARY KEY,
                    uses bigint NOT NULL,
                    description varchar NULL,

                    CONSTRAINT fk_tag_info_id FOREIGN KEY(id) REFERENCES tag(id)
                );

                CREATE TABLE IF NOT EXISTS tag_alias (
                    alias varchar NOT NULL PRIMARY KEY,
                    tag_id bigint NOT NULL,

                    UNIQUE(alias),
                    CONSTRAINT fk_tag_alias_tag_id FOREIGN KEY (tag_id) REFERENCES tag(id)
                );

                CREATE INDEX IF NOT EXISTS idx_tag_alias_tag_id;

            ");

        }

    }
}
