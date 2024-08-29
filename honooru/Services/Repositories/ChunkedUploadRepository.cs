using Microsoft.Extensions.Logging;
using Npgsql.Replication.PgOutput.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace honooru.Services.Repositories {
    public class ChunkedUploadRepository {

        private readonly ILogger<ChunkedUploadRepository> _Logger;

        private Dictionary<Guid, Stream> _Uploads = new();

        public ChunkedUploadRepository(ILogger<ChunkedUploadRepository> logger) {
            _Logger = logger;
        }

        public void Create(Guid uploadId) {
            _Uploads.Add(uploadId, new MemoryStream());
        }

        public async Task Append(Guid uploadId, Stream stream) {
            Stream? existingStream = _Uploads.GetValueOrDefault(uploadId)
                ?? throw new Exception($"cannot append to chunked upload stream, uploadId does not exist [uploadId={uploadId}]");

            await stream.CopyToAsync(existingStream, 1024 * 1024 * 80); // 80MB buffer
        }

        public Stream? Get(Guid uploadId) {
            Stream? s = _Uploads.GetValueOrDefault(uploadId);
            if (s == null) {
                return null;
            }

            s.Seek(0, SeekOrigin.Begin);

            return _Uploads.GetValueOrDefault(uploadId);
        }

        public void Close(Guid uploadId) {
            Stream? existingStream = _Uploads.GetValueOrDefault(uploadId);
            existingStream?.Dispose();

            _Uploads.Remove(uploadId);

        }

    }
}
