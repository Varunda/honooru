namespace honooru.Models.Api {

    public class ExtendedTag {

        public ulong ID { get; set; }

        public string Name { get; set; } = "";

        public ulong TypeID { get; set; }

        public string TypeName { get; set; } = "";

        public short TypeOrder { get; set; }

        public string HexColor { get; set; } = "";

        public ulong Uses { get; set; }

        public string? Description { get; set; } = null;

    }
}
