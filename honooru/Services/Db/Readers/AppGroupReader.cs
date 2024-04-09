using honooru.Code.ExtensionMethods;
using honooru.Models.Internal;
using Npgsql;
using System.Data;
using System.Linq;

namespace honooru.Services.Db.Readers {

    public class AppGroupReader : IDataReader<AppGroup> {
        
        public override AppGroup? ReadEntry(NpgsqlDataReader reader) {
            AppGroup group = new();

            group.ID = reader.GetUInt64("id");
            group.Name = reader.GetString("name");
            group.HexColor = reader.GetString("hex_color");
            group.Implies = reader.GetString("implies").Split(" ").Where(iter => iter != "").Select(iter => ulong.Parse(iter)).ToList();

            return group;
        }

    }
}
