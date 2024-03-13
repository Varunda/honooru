using honooru.Models.Api;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace honooru.Services.UploadStepHandler {

    public class UploadStepProgressRepository {

        private readonly ILogger<UploadStepProgressRepository> _Logger;

        private readonly Dictionary<Guid, UploadStepEntry> _Entries = new();

        public UploadStepProgressRepository(ILogger<UploadStepProgressRepository> logger) {
            _Logger = logger;
        }

        public void Add(UploadStepEntry entry) {
            lock (_Entries) {
                if (_Entries.ContainsKey(entry.MediaAssetID) == true) {
                    _Logger.LogWarning($"not adding duplicate {nameof(UploadStepEntry)} [MediaAssetID={entry.MediaAssetID}]");
                    return;
                }

                _Entries.Add(entry.MediaAssetID, entry);
            }
        }

        public bool Remove(Guid guid) {
            lock (_Entries) {
                return _Entries.Remove(guid);
            }
        }

        public UploadStepEntry? GetByMediaAssetID(Guid guid) {
            lock (_Entries) {
                return _Entries.GetValueOrDefault(guid);
            }
        }

        public List<UploadStepEntry> GetAll() {
            lock (_Entries) {
                return new List<UploadStepEntry>(_Entries.Values);
            }
        }

    }
}
