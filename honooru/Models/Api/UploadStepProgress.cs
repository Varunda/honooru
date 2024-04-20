using honooru.Models.App.MediaUploadStep;
using System;
using System.Collections.Generic;

namespace honooru.Models.Api {

    /// <summary>
    ///     represents the state of a single <see cref="IUploadStep"/>
    /// </summary>
    public class UploadStepProgress {

        /// <summary>
        ///     name of the <see cref="IUploadStep"/>
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     in what order this <see cref="IUploadStep"/> will be performed
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        ///     what percent of this step is completed
        /// </summary>
        public decimal Percent { get; set; } = 0m;

        /// <summary>
        ///     if this step is done or not
        /// </summary>
        public bool Finished { get; set; } = false;

        /// <summary>
        ///     when this step was started
        /// </summary>
        public DateTime StartedAt { get; set; }

    }

    /// <summary>
    ///     represents the progress of a single <see cref="UploadSteps"/>
    /// </summary>
    public class UploadStepEntry {

        /// <summary>
        ///     ID of the asset this progress is for
        /// </summary>
        public Guid MediaAssetID { get; set; } = Guid.Empty;

        /// <summary>
        ///     what step is currently being processed
        /// </summary>
        public UploadStepProgress? Current { get; set; } = null;

        /// <summary>
        ///     the progress on each step
        /// </summary>
        public Dictionary<string, UploadStepProgress> Progress { get; set; } = new();

        /// <summary>
        ///     error that occured while uploading
        /// </summary>
        public ExceptionInfo? Error { get; set; }

    }

}
