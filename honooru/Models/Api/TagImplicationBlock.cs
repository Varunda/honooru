using honooru.Models.App;
using System.Collections.Generic;

namespace honooru.Models.Api {

    public class TagImplicationBlock {

        public ulong TagID { get; set; }

        public List<TagImplication> Sources { get; set; } = new();

        public List<TagImplication> Targets { get; set; } = new();

        public List<Tag> Tags { get; set; } = new();

    }
}
