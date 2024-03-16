﻿using Npgsql;
using System.Threading.Tasks;

namespace honooru.Services.Db.Patches {

    public class Patch01AddAccount : IDbPatch {
        public int MinVersion => 1;
        public string Name => "add account table";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection();
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS app_account (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    name varchar NOT NULL,
                    discord_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL,
                    deleted_on timestamptz NULL,
                    deleted_by bigint NULL
                );

                CREATE TABLE IF NOT EXISTS app_group (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    name varchar NOT NULL,
                    hex_color varchar NOT NULL,
                    implies varchar NOT NULL
                );

                INSERT INTO app_group (
                    id, name, hex_color, implies
                ) OVERRIDING SYSTEM VALUE
                VALUES (
                    1, 'admin', 'ff00ff', ''
                ) ON CONFLICT (id) DO NOTHING;

                CREATE TABLE IF NOT EXISTS app_account_access_logs (
                    id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
                    timestamp timestamptz NOT NULL,
                    success boolean NOT NULL,
                    account_id bigint NOT NULL,

                    CONSTRAINT fk_app_account_access_logs_account_id FOREIGN KEY (account_id) REFERENCES app_account(id) 
                );

                CREATE TABLE IF NOT EXISTS app_group_permission (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    group_id bigint NOT NULL,
                    permission varchar NOT NULL,
                    timestamp timestamptz NOT NULL,
                    granted_by_id bigint NOT NULL,

                    CONSTRAINT fk_app_group_permission FOREIGN KEY (group_id) REFERENCES app_group(id)
                );
            
                CREATE INDEX IF NOT EXISTS idx_app_group_permission_account_id ON app_group_permission(group_id);

                CREATE INDEX IF NOT EXISTS idx_app_group_permission_permisison ON app_group_permission(permission);

                CREATE TABLE IF NOT EXISTS app_permission (
                    id VARCHAR NOT NULL PRIMARY KEY,
                    description VARCHAR NOT NULL
                );

                CREATE TABLE IF NOT EXISTS app_account_group_membership (
                    id bigint NOT NULL PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                    account_id bigint NOT NULL,
                    group_id bigint NOT NULL,
                    timestamp timestamptz NOT NULL,
                    granted_by_account_id bigint NOT NULL,

                    UNIQUE (account_id, group_id),
                    CONSTRAINT fk_app_account_group_membership_account_id FOREIGN KEY (account_id) REFERENCES app_account(id),
                    CONSTRAINT fk_app_account_group_membership_group_id FOREIGN KEY (group_id) REFERENCES app_group(id)
                );

                CREATE INDEX IF NOT EXISTS idx_app_account_group_membership_account_id ON app_account_group_membership (account_id);

                CREATE INDEX IF NOT EXISTS idx_app_account_group_membership_group_id ON app_account_group_membership (group_id);
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
