using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Models.Api;
using honooru.Models.App;

namespace honooru.Code.Hubs {

    public interface IMediaAssetUploadHub {

        Task UpdateProgress(UploadStepEntry entry);

        Task Finish(MediaAsset asset);

        Task Info(string msg);

    }

}
