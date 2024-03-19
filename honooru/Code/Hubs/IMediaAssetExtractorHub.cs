using honooru.Models.App;
using System.Threading.Tasks;

namespace honooru.Code.Hubs {

    public interface IMediaAssetExtractorHub {

        Task UpdateProgress(decimal percent);

        Task Finish(MediaAsset asset);

        Task Info(string msg);

    }
}
