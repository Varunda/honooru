using FFMpegCore.Enums;
using honooru.Models.App;
using honooru.Models.Config;
using System.Collections.Generic;

namespace honooru.Models.App.MediaUploadStep {

    public class UploadSteps {

        public readonly MediaAsset Asset;

        public readonly StorageOptions StorageOptions;

        public readonly List<IUploadStep> Steps = new();

        public UploadSteps(MediaAsset asset, StorageOptions options) {
            Asset = asset;
            StorageOptions = options;
        }

        public UploadSteps AddReencodeStep(Codec videoCodec, Codec audioCoded) {
            Steps.Add(new ReencodeUploadStep.Order(Asset, StorageOptions, videoCodec, audioCoded));
            return this;
        }

        public UploadSteps AddFinalMoveStep() {
            Steps.Add(new MoveUploadStep.Order(Asset, StorageOptions));
            return this;
        }

        public UploadSteps AddExtractStep(string url) {
            Steps.Add(new ExtractStep.Order(Asset, StorageOptions, url));
            return this;
        }

        public UploadSteps AddImageHashStep() {
            Steps.Add(new GenerateImageHashUploadStep.Order(Asset, StorageOptions));
            return this;
        }

    }

}
