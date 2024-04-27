using System;

namespace honooru.Models.App {

    public class ExtractorAuthorMapping {

        public string Site { get; set; } = "";

        public string Author { get; set; } = "";

        public ulong TagID { get; set; }

        public DateTime Timestamp { get; set; }

    }
}
