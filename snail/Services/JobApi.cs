using honooru_common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using snail.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace snail.Services {

    public class JobApi {

        private readonly ILogger<JobApi> _Logger;
        private readonly IOptions<HostOptions> _HostOptions;

        private static HttpClient _HttpClient;

        static JobApi() {
            HttpClientHandler httpHandler = new();
            // i'm too lazy to fix this, but if the api-key auth fails, then it tries Discord auth next,
            // which will redirect to the discord login page. we can assume if there is a redirect,
            // that the auth using the api key failed
            httpHandler.AllowAutoRedirect = false;

            _HttpClient = new HttpClient(httpHandler);
        }

        public JobApi(ILogger<JobApi> logger,
            IOptions<HostOptions> hostOptions) {

            _Logger = logger;

            _HostOptions = hostOptions;

            _Logger.LogDebug($"job api setup [HostUrl={_HostOptions.Value.HostUrl}]");
        }

        /// <summary>
        ///     get a list of all unclaimed <see cref="DistributedJob"/>s that need to be worked on
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<DistributedJob>> GetPendingJobs() {
            HttpResponseMessage? response = await _S(_R("/api/jobs/unclaimed"));
            if (response == null) {
                return [];
            }

            string body = await response.Content.ReadAsStringAsync();

            List<DistributedJob>? jobs = JsonSerializer.Deserialize<List<DistributedJob>>(body, new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true
            });

            return jobs ?? throw new Exception($"failed to parse json: {body}");
        }

        public async Task<List<DistributedJob>> GetClaimed() {
            HttpResponseMessage? response = await _S(_R("/api/jobs/claimed/mine"));
            if (response == null) {
                return [];
            }

            string body = await response.Content.ReadAsStringAsync();

            List<DistributedJob>? jobs = JsonSerializer.Deserialize<List<DistributedJob>>(body, new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true
            });

            return jobs ?? throw new Exception($"failed to parse json: {body}");
        }

        /// <summary>
        ///     tell honooru that this client has claimed the job, and is working on it
        /// </summary>
        /// <param name="ID">ID of the job to be claimed</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task ClaimJob(Guid ID) {
            HttpResponseMessage? response = await _S(_R($"/api/jobs/claim/{ID}", HttpMethod.Post))
                ?? throw new Exception($"failed to claim job {ID}");

            _Logger.LogInformation($"claimed job [ID={ID}]");
        }

        public async Task Upload(DistributedJob job) {
            string[] files = Directory.GetFiles($"./downloads/{job.ID}");
            if (files.Length == 0) {
                throw new Exception($"failed to find any files in ./downloads/{job.ID}");
            }

            _Logger.LogDebug($"found file to upload [file={files[0]}]");
            FileStreamOptions opt = new() {
                BufferSize = 1024 * 1024 * 128, // 128 MB buffer instead of default 4'096
                Mode = FileMode.Open,
                Access = FileAccess.Read
            };
            using FileStream file = File.Open(files[0], opt);
            await Upload(job.ID, file);
        }

        /// <summary>
        ///     upload the result of a <see cref="DistributedJob"/>
        /// </summary>
        /// <param name="ID">ID of the <see cref="DistributedJob"/> that was finished</param>
        /// <param name="file">the file being uploaded</param>
        /// <returns></returns>
        public async Task Upload(Guid ID, FileStream file) {
            _Logger.LogDebug($"uploading distributed job result to host [ID={ID}]");

            byte[] backingBuffer = new byte[1024 * 1024 * 80]; // 80MB buffer
            Memory<byte> buffer = new(backingBuffer, 0, backingBuffer.Length);
            int bytesRead = 0;
            do {
                bytesRead = await file.ReadAsync(buffer);
                if (bytesRead == 0) {
                    break;
                }

                // |file.Name| contains the full path, we only want the name of the file
                string fileName = file.Name.Split(Path.DirectorySeparatorChar)[^1];
                _Logger.LogDebug($"read chunk of file to upload [fileName={fileName}] [bytesRead={bytesRead}]");
                // because the buffer is 0 filled, we don't want a bunch of junk zeros at the end
                // so we only slice to the amount read
                await _UploadChunk(ID, buffer.Slice(0, bytesRead), fileName);
            } while (bytesRead > 0);

            await _FinalizeUpload(ID);
        }

        /// <summary>
        ///     upload a chunk of the video to honooru
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="chunk"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task _UploadChunk(Guid ID, Memory<byte> chunk, string name) {
            HttpRequestMessage request = _R($"/api/jobs/upload/part?ID={ID}", HttpMethod.Post);
            request.Content = new MultipartFormDataContent() {
                { new ReadOnlyMemoryContent(chunk), "data", name }
            };

            HttpResponseMessage response = await _HttpClient.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                throw new Exception($"failed to upload chunk (expected 200 OK): {response.StatusCode} {body}");
            }

            _Logger.LogDebug($"successfully uploaded chunk [ID={ID}] [size={chunk.Length}] [name={name}]");
        }

        /// <summary>
        ///     mark the job as done and fully uploaded
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task _FinalizeUpload(Guid ID) {
            HttpResponseMessage response = await _HttpClient.SendAsync(_R($"/api/jobs/upload/finish?ID={ID}", HttpMethod.Post));
            if (response.StatusCode == System.Net.HttpStatusCode.Redirect) {
                throw new Exception($"invalid api key");
            }

            string body = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                throw new Exception($"failed to claim job (expected 200 OK): {response.StatusCode} {body}");
            }
        }

        private HttpRequestMessage _R(string url, HttpMethod? method = null) {
            HttpRequestMessage request = new();
            request.RequestUri = new Uri(_HostOptions.Value.HostUrl + url);
            request.Method = method ?? HttpMethod.Get;
            // the api key is not passed in a standard way
            if (request.Headers.TryAddWithoutValidation("Authorization", _HostOptions.Value.ApiKey) == false) {
                throw new Exception($"failed to add Authorization header to request");
            }

            _Logger.LogTrace($"sending HTTP request [url={request.RequestUri.ToString()}]");

            return request;
        }

        private async Task<HttpResponseMessage?> _S(HttpRequestMessage request) {
            HttpResponseMessage response;
            try {
                response = await _HttpClient.SendAsync(request);
            } catch (HttpRequestException rex) {
                if (rex.Message.StartsWith("No connection could be made because the target machine actively refused it")) {
                    _Logger.LogWarning($"timeout to host");
                    return null;
                }
                throw rex;
            } catch (Exception ex) {
                throw;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Redirect) {
                throw new Exception($"invalid api key");
            }

            string body = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                throw new Exception($"failed to claim job (expected 200 OK): {response.StatusCode} {body}");
            }

            return response;
        }

    }
}
