using honooru.Code.ExtensionMethods;
using honooru.Models.App;
using Npgsql;
using System;
using System.Data;

namespace honooru.Services.Db.Readers {

    public class UserSettingReader : IDataReader<UserSetting> {

        public override UserSetting? ReadEntry(NpgsqlDataReader reader) {
            UserSetting setting = new();

            setting.AccountID = reader.GetUInt64("account_id");
            setting.Name = reader.GetString("name");
            setting.Type = Enum.Parse<UserSettingType>(reader.GetString("type"));
            setting.Value = reader.GetString("value");

            return setting;
        }

    }
}
