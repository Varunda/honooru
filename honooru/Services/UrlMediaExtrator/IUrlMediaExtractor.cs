using honooru.Models.App;
using honooru.Models.Config;
using System;
using System.Threading.Tasks;

namespace honooru.Services.UrlMediaExtrator {

    public interface IUrlMediaExtractor {

        string Name { get; }

        bool NeedsQueue { get; }

        bool CanHandle(Uri url);

        Task Handle(Uri url, StorageOptions options, MediaAsset asset, Action<decimal> progress);

    }
}
