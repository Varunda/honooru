using FFMpegCore;
using Google.Protobuf;
using honooru.Code.ExtensionMethods;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.App.Iqdb;
using honooru.Models.Config;
using honooru.Services.Util;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace honooru.Services {

    public class IqdbClient {

        private const string SERVICE_NAME = "iqdb-client";

        private readonly ILogger<IqdbClient> _Logger;

        private readonly ServiceHealthMonitor _ServiceHealthMonitor;
        private readonly FileExtensionService _FileExtensionUtility;

        private readonly IOptions<IqdbOptions> _Options;
        private readonly IOptions<StorageOptions> _StorageOptions;

        private static readonly HttpClient _HttpClient = new HttpClient();

        private static readonly JsonSerializerOptions _SerializingOptions = new(JsonSerializerOptions.Default);
        static IqdbClient() {
            _SerializingOptions.WriteIndented = false;
        }

        public IqdbClient(ILogger<IqdbClient> logger,
            IOptions<IqdbOptions> options, IOptions<StorageOptions> storageOptions,
            ServiceHealthMonitor serviceHealthMonitor, FileExtensionService fileExtensionUtility) {

            _Logger = logger;

            _ServiceHealthMonitor = serviceHealthMonitor;

            _Options = options;
            _StorageOptions = storageOptions;

            if (string.IsNullOrWhiteSpace(_Options.Value.Host)) {
                _Logger.LogWarning($"not using IQDB service, Host is not set");
                _GetServiceHealth().Enabled = false;
            } else {
                _Logger.LogInformation($"setting up IQDB client [host={_Options.Value.Host}] [timeout={_Options.Value.TimeoutMs}ms]");
            }
            _FileExtensionUtility = fileExtensionUtility;
        }

        private ServiceHealthEntry _GetServiceHealth() {
            return _ServiceHealthMonitor.GetOrCreate(SERVICE_NAME);
        }

        /// <summary>
        ///     perform a health update of the IQDB service
        /// </summary>
        /// <returns></returns>
        public async Task<ServiceHealthEntry> UpdateHealth() {
            ServiceHealthEntry entry = await CheckHealth();
            _ServiceHealthMonitor.Set(SERVICE_NAME, entry);

            return entry;
        }

        /// <summary>
        ///     check the health of the external IQDB service
        /// </summary>
        /// <returns>
        ///     a <see cref="ServiceHealthEntry"/> for the service
        /// </returns>
        public async Task<ServiceHealthEntry> CheckHealth() {
            ServiceHealthEntry health = _GetServiceHealth();
            _Logger.LogDebug($"checking health of {SERVICE_NAME} [LastRan={health.LastRan:u}]");

            if (DateTime.UtcNow - health.LastRan < TimeSpan.FromSeconds(5)) {
                _Logger.LogInformation($"health of {SERVICE_NAME} was checked too recently, skipping [LastRan={health.LastRan:u}]");
                return health;
            }

            health.LastRan = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(_Options.Value.Host)) {
                health.Message = $"IQDB server Host is not set";
                health.Enabled = false;
                _Logger.LogWarning(health.Message);
                return health;
            }

            string url = $"{_Options.Value.Host}/status";
            _Logger.LogDebug($"sending status request to IQDB server [url={url}]");
            try {
                HttpResponseMessage response = await _HttpClient.GetAsync(url);

                if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                    health.Enabled = false;
                    health.Message = $"got non-200 OK response from IQDB [url={url}] [StatusCode={response.StatusCode}]";
                    _Logger.LogWarning(health.Message);
                    return health;
                }

                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                JsonNode? elem = JsonSerializer.Deserialize<JsonNode>(bytes, _SerializingOptions);
                if (elem == null) {
                    health.Enabled = false;
                    health.Message = $"parsed bytes into JsonNode returned null";
                    _Logger.LogWarning(health.Message);
                    return health;
                }

                _Logger.LogDebug($"status response from server [elem={elem.ToJsonString(_SerializingOptions)}]");

                JsonNode? versionNode = elem["version"];
                string? version = versionNode?.GetValue<string>();
                if (version != "honooru") {
                    health.Enabled = false;
                    health.Message = $"expected version string of 'honooru', got '{version}' instead";
                    _Logger.LogWarning(health.Message);
                    return health;
                }
            } catch (Exception ex) {
                if (ex is HttpRequestException hex && hex.Message.StartsWith("No connection could be made because the target machine actively refused it")) {
                    _Logger.LogWarning($"caught timeout to {url}: {hex.Message}");
                } else {
                    _Logger.LogError(ex, $"failed to check health of IQDB service: {ex.Message}");
                }

                health.Enabled = false;
                health.Message = $"exception while checking health: {ex.Message}";
                return health;
            }

            health.Enabled = true;

            return health;
        }

        /// <summary>
        ///     get a <see cref="IqdbEntry"/> from the IQDB service
        /// </summary>
        /// <param name="postID">ID of the post to get from the IQDB service, usually an md5 hash</param>
        /// <returns>
        ///     the <see cref="IqdbEntry"/> with <see cref="IqdbEntry.PostID"/> of <paramref name="postID"/>,
        ///     or <c>null</c> if the IQDB service does not have it
        /// </returns>
        /// <exception cref="Exception">if the response from the IQDB service did not have the correct fields</exception>
        public async Task<IqdbEntry?> GetByPostID(string postID) {
            ServiceHealthEntry healthEntry = _GetServiceHealth();
            if (healthEntry.Enabled == false) {
                _Logger.LogWarning($"service is not enabled, cannot get IQDB entry [LastRan={healthEntry.LastRan:u}] [message={healthEntry.Message}]");
                return null;
            }

            string url = $"{_Options.Value.Host}/images/{postID}";

            HttpResponseMessage response = await _HttpClient.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound) {
                return null;
            }

            string res = await response.Content.ReadAsStringAsync();

            JsonNode? elem;
            try {
                elem = JsonSerializer.Deserialize<JsonNode>(res, _SerializingOptions)
                    ?? throw new Exception($"JsonNode being parse was null?");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to convert IQDB servicer response to a valid JSON string: {res}");
                throw;
            }

            IqdbEntry entry = new();
            entry.PostID = elem["post_id"]?.GetValue<string>() ?? throw new Exception($"missing 'post_id' from {elem}");
            entry.Hash = elem["hash"]?.GetValue<string>() ?? throw new Exception($"missing 'hash' from {elem}");

            return entry;
        }

        /// <summary>
        ///     using an IQDB image hash (which is a HAAR signarture), get similar images to the input hash
        /// </summary>
        /// <param name="iqdb">hash. must starts with the string "iqdb_"</param>
        /// <returns>
        ///     a list of <see cref="IqdbQueryResult"/> from the IQDB service, in descending or (Z->A)
        ///     based on <see cref="IqdbQueryResult.Score"/>. all values in <see cref="IqdbQueryResult.Post"/> are null.
        ///     <br></br>
        ///     an empty list is returned if the IQDB service is not available
        /// </returns>
        /// <exception cref="Exception">
        ///     if <paramref name="iqdb"/> is not valid
        /// </exception>
        public async Task<List<IqdbQueryResult>> GetSimilar(string iqdb) {
            if (iqdb.StartsWith("iqdb_") == false) {
                throw new ArgumentException($"invalid IQDB hash: {nameof(iqdb)} needs to start with \"iqdb_\"");
            }

            ServiceHealthEntry entry = _GetServiceHealth();
            if (entry.Enabled == false) {
                _Logger.LogWarning($"service is not enabled, cannot query IQDB similarity [LastRan={entry.LastRan:u}] [message={entry.Message}]");
                return new List<IqdbQueryResult>();
            }

            string url = $"{_Options.Value.Host}/query?hash={iqdb}";

            _Logger.LogInformation($"searching for IQDB similar values [url={url}]");

            HttpResponseMessage response = await _HttpClient.PostAsync(url, null);
            string res = await response.Content.ReadAsStringAsync();

            _Logger.LogDebug($"IQDB get similar response [response={response.Content}] [res={res}]");
            JsonNode? elem;
            try {
                elem = JsonSerializer.Deserialize<JsonNode>(res, _SerializingOptions)
                    ?? throw new Exception($"JsonNode being parse was null?");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to convert IQDB servicer response to a valid JSON string: {res}");
                throw;
            }

            Dictionary<string, IqdbQueryResult> finds = new();

            List<IqdbQueryResult> results = new();
            foreach (JsonNode? iter in elem.AsArray()) {
                if (iter == null) {
                    continue;
                }

                IqdbQueryResult r = new();
                r.PostID = iter["post_id"]?.GetValue<string>() ?? throw new Exception($"missing 'post_id' from {iter}");

                // for video frame uploads, only include the post once
                string[] parts = r.PostID.Split("-");
                string md5 = parts[0];
                r.MD5 = md5;

                r.Hash = iter["hash"]?.GetValue<string>() ?? throw new Exception($"missing 'hash' from {iter}");
                r.Score = iter["score"]?.GetValue<float>() ?? throw new Exception($"missing 'score' from {iter}");

                if (((finds.GetValueOrDefault(md5)?.Score) ?? -100f) < r.Score) {
                    finds[md5] = r;
                }
            }

            results = finds.Values.OrderByDescending(iter => iter.Score).ToList();

            //_Logger.LogDebug($"got iqdb similar query results [res={res}]");

            return results;
        }

        /// <summary>
        ///     create a new <see cref="IqdbEntry"/>
        /// </summary>
        /// <param name="path">path that the file is read from</param>
        /// <param name="md5">md5 of the upload. usually the MD5 of the post, but it can be appended with -frame-# for video uploads</param>
        /// <param name="fileExt">extension of the file</param>
        /// <returns>
        ///     a <see cref="IqdbEntry"/> that was created, or <c>null</c> if the IQDB service is not reachable
        /// </returns>
        public async Task<IqdbEntry?> Create(string path, string md5, string fileExt) {
            ServiceHealthEntry entry = _GetServiceHealth();
            if (entry.Enabled == false) {
                _Logger.LogWarning($"service is not enabled, not creating IQDB entry [LastRan={entry.LastRan:u}] [message={entry.Message}]");
                return null;
            }

            _Logger.LogDebug($"opening file for upload [md5={md5}] [path={path}]");

            string? fileType = _FileExtensionUtility.GetFileType(fileExt);
            if (fileType == "image") {
                byte[] bytes;
                if (fileExt == "jpg" || fileExt == "jpeg") {
                    bytes = await File.ReadAllBytesAsync(path);
                } else {
                    _Logger.LogDebug($"converting non-jpg image to jpg for IQDB (in memory) [fileExt={fileExt}] [md5={md5}] [path={path}]");
                    // if it's not a jpg, convert it
                    using MemoryStream jpgStream = new();
                    using MagickImage mImage = new(path);
                    mImage.Strip();
                    mImage.Format = MagickFormat.Jpg;
                    await mImage.WriteAsync(jpgStream);
                    bytes = jpgStream.ToArray();
                }

                return await Insert(bytes, md5, md5);
            } else if (fileType == "video") {
                IqdbEntry? ret = null;

                IMediaAnalysis videoInfo = FFProbe.Analyse(path);

                if (videoInfo.Duration < TimeSpan.FromSeconds(1)) {
                    _Logger.LogDebug($"generating iqdb frame for video [path={path}]");

                    string output = Path.Combine(_StorageOptions.Value.RootDirectory, "temp", md5 + ".png");

                    if (File.Exists(output) == false) {
                        await FFMpeg.SnapshotAsync(path, output, new System.Drawing.Size() {
                            Width = 180,
                            Height = 0 // 0 means calculate it from the aspect ratio, which is what we want!
                        });
                    }

                    // if it's not a jpg, convert it
                    using MemoryStream jpgStream = new();
                    using MagickImage mImage = new(output);
                    mImage.Strip();
                    mImage.Format = MagickFormat.Jpg;
                    await mImage.WriteAsync(jpgStream);
                    byte[] bytes = jpgStream.ToArray();
                    ret = await Insert(bytes, md5, md5);

                    _Logger.LogDebug($"deleting temp file [output={output}]");
                    File.Delete(output);
                } else {
                    int i = 0;
                    for (TimeSpan frame = TimeSpan.FromSeconds(1); frame < videoInfo.Duration; frame *= 2) {
                        _Logger.LogDebug($"generating iqdb frame for video [i={i}] [frame={frame}] [path={path}]");

                        string output = Path.Combine(_StorageOptions.Value.RootDirectory, "temp", md5 + $"-frame-{i}.png");

                        if (File.Exists(output) == false) {
                            await FFMpeg.SnapshotAsync(path, output, new System.Drawing.Size() {
                                Width = 180,
                                Height = 0 // 0 means calculate it from the aspect ratio, which is what we want!
                            }, frame);
                        }

                        // if it's not a jpg, convert it
                        using MemoryStream jpgStream = new();
                        using MagickImage mImage = new(output);
                        mImage.Strip();
                        mImage.Format = MagickFormat.Jpg;
                        await mImage.WriteAsync(jpgStream);
                        byte[] bytes = jpgStream.ToArray();
                        ret = await Insert(bytes, md5 + $"-frame-{i}", md5);

                        ++i;

                        _Logger.LogDebug($"deleting temp file [output={output}]");
                        File.Delete(output);
                    }
                }

                if (ret == null) {
                    throw new Exception($"ret is not supposed to be null now");
                }

                return ret;
            } else {
                _Logger.LogError($"cannot create IQDB entry from given file extension [fileExt={fileExt}]");
                return null;
            }
        }

        /// <summary>
        ///     insert a new entry, used as video hashing creates multiple entries
        /// </summary>
        /// <param name="bytes">bytes of the jpg file</param>
        /// <param name="name"></param>
        /// <param name="md5"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<IqdbEntry?> Insert(byte[] bytes, string name, string md5) {
            string url = $"{_Options.Value.Host}/images/{name}/{md5}";

            if (bytes.Length > 250 * 1024) {
                _Logger.LogWarning($"uploading large file to IQDB [size={bytes.Length}]");
            }

            MultipartFormDataContent content = new();
            ByteArrayContent imageData = new(bytes);
            content.Add(imageData, "file");

            HttpResponseMessage response = await _HttpClient.PostAsync(url, content);
            string res = await response.Content.ReadAsStringAsync();

            _Logger.LogTrace($"IQDB upload response [response={response.Content}] [res={res}]");
            JsonNode? elem;
            try {
                elem = JsonSerializer.Deserialize<JsonNode>(res, _SerializingOptions)
                    ?? throw new Exception($"JsonNode being parse was null?");
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to convert IQDB servicer response to a valid JSON string: {res}");
                throw;
            }

            IqdbEntry ret = new();
            ret.PostID = name;
            ret.Hash = elem["hash"]?.GetValue<string>() ?? throw new Exception($"missing 'hash' from {elem}");
            ret.MD5 = elem["md5"]?.GetValue<string>() ?? throw new Exception($"missing 'md5' from {elem}");

            _Logger.LogInformation($"generated image hash from IQDB service [name={name}] [hash={ret.Hash}]");

            return ret;
        }

        /// <summary>
        ///     remove an IQDB entry from the IQDB service
        /// </summary>
        /// <param name="postID"></param>
        /// <returns></returns>
        public async Task<bool> Remove(string postID) {
            ServiceHealthEntry healthEntry = _GetServiceHealth();
            if (healthEntry.Enabled == false) {
                _Logger.LogWarning($"service is not enabled, cannot delete IQDB entry [LastRan={healthEntry.LastRan:u}] [message={healthEntry.Message}]");
                return false;
            }

            string url = $"{_Options.Value.Host}/images?post_id={postID}";

            _Logger.LogInformation($"deleting IQDB entry [postID={postID}] [url={url}]");

            HttpResponseMessage response = await _HttpClient.DeleteAsync(url);

            return response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<bool> RemoveByMD5(string md5) {
            ServiceHealthEntry healthEntry = _GetServiceHealth();
            if (healthEntry.Enabled == false) {
                _Logger.LogWarning($"service is not enabled, cannot delete IQDB entry [LastRan={healthEntry.LastRan:u}] [message={healthEntry.Message}]");
                return false;
            }

            string url = $"{_Options.Value.Host}/images?md5={md5}";

            _Logger.LogInformation($"deleting IQDB entry by MD5 [md5={md5}] [url={url}]");

            HttpResponseMessage response = await _HttpClient.DeleteAsync(url);

            return response.StatusCode == HttpStatusCode.OK;
        }


    }
}
