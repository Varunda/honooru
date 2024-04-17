using System;

namespace honooru.Models.App {

    public class PostPool {

        public ulong ID { get; set; }

        public string Name { get; set; } = "";

        public ulong CreatedByID { get; set; }

        public DateTime Timestamp { get; set; }

    }
}
