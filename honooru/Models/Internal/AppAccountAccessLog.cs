using System;

namespace honooru.Models {

    public class AppAccountAccessLog {

        public long ID { get; set; }

        public DateTime Timestamp { get; set; }

        public bool Success { get; set; }

        public long? AccountID { get; set; }

        public string? Email { get; set; }

    }
}
