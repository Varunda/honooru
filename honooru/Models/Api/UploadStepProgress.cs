using honooru.Models.App.MediaUploadStep;
using System.Collections.Generic;

namespace honooru.Models.Api {

    public class UploadStepProgress {

        public string Name { get; set; } = "";

        public int Order { get; set; } = 0;

        public decimal Percent { get; set; } = 0m;

        public bool Finished { get; set; } = false;

    }

    public class UploadStepEntry {

        public UploadStepProgress? Current { get; set; } = null;

        public Dictionary<string, UploadStepProgress> Progress { get; set; } = new();

    }

}
