using honooru.Models.App;
using honooru.Models.Config;
using honooru.Services.Repositories;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace honooru.Services.UrlMediaExtrator {

    public class UrlMediaExtractorHandler {

        private readonly ILogger<UrlMediaExtractorHandler> _Logger;
        private readonly IOptions<StorageOptions> _Options;
        private readonly IServiceProvider _Services;

        private readonly MediaAssetRepository _MediaAssetRepository;

        public UrlMediaExtractorHandler(ILogger<UrlMediaExtractorHandler> logger,
            IOptions<StorageOptions> options, IServiceProvider services,
            MediaAssetRepository mediaAssetRepository) {

            _Logger = logger;
            _Options = options;
            _Services = services;

            _MediaAssetRepository = mediaAssetRepository;
        }

        public bool CanHandle(string url) {
            return _GetExtractor(url) != null;
        }

        public bool NeedsQueue(string url) {
            IUrlMediaExtractor? extractor = _GetExtractor(url);

            if (extractor == null) {
                return false;
            }

            return extractor.NeedsQueue;
        }

        public async Task<MediaAsset> HandleUrl(MediaAsset asset, string url, Action<decimal> progress) {
            IUrlMediaExtractor? extractor = _GetExtractor(url);
            if (extractor == null) {
                throw new Exception($"cannot handle url: '{url}'");
            }

            asset.Source = url;

            Uri uri = new Uri(url);
            Stopwatch timer = Stopwatch.StartNew();
            await extractor.Handle(uri, _Options.Value, asset, progress);
            _Logger.LogDebug($"download complete [timer={timer.ElapsedMilliseconds}ms] [url={url}] [asset.Guid={asset.Guid}]");
            timer.Restart();

            if (asset.FileExtension == "") {
                _Logger.LogWarning($"file extractor did not update file extension! [extractor={extractor.Name}] [url={url}]");
            }

            string path = Path.Combine(_Options.Value.RootDirectory, "upload", asset.Guid + "." + asset.FileExtension);
            _Logger.LogDebug($"checking output file exists [path={path}]");

            if (File.Exists(path) == false) {
                throw new Exception($"failed to find file {path} after extractor ran");
            }

            // now that the file has been downloaded and placed into the upload folder,
            // compute the md5 hash
            _Logger.LogDebug($"starting to compute hash of file [path={path}]");
            using FileStream file = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024 * 128);
            byte[] md5 = await MD5.Create().ComputeHashAsync(file);
            file.Close(); // IMPORTANT: if this is not closed, the handle is left open, and the file cannot be moved
            string md5Str = string.Join("", md5.Select(iter => iter.ToString("x2"))); // turn that md5 into a string
            _Logger.LogInformation($"updating md5 of extracted media asset [timer={timer.ElapsedMilliseconds}ms] [md5={md5Str}] [path={path}]");
            asset.MD5 = md5Str;

            MediaAsset? existingAsset = await _MediaAssetRepository.GetByMD5(asset.MD5);
            if (existingAsset != null) {
                _Logger.LogWarning($"media asset with MD5 already uploaded, deleting existing asset [guid={asset.Guid}] [md5={md5Str}]");
                await _MediaAssetRepository.Delete(asset.Guid);
                return existingAsset;
            }

            asset.Status = MediaAssetStatus.DONE;

            string targetFile = Path.Combine(_Options.Value.RootDirectory, "work", asset.MD5 + "." + asset.FileExtension);
            _Logger.LogInformation($"moving extracted file to work dir [path={path}] [targetFile={targetFile}]");
            File.Move(path, targetFile); // this is why |file| must be .Close()ed above

            await _MediaAssetRepository.Upsert(asset);

            return asset;
        }

        private IUrlMediaExtractor? _GetExtractor(string url) {
            List<Type> extractors = _GetExtractors();

            Uri uri = new(url);

            foreach (Type type in extractors) {
                using IServiceScope scope = _Services.CreateScope();

                IUrlMediaExtractor? extractor = null;
                try {
                    extractor = (IUrlMediaExtractor)scope.ServiceProvider.GetRequiredService(type);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to convert {type.FullName} into a {nameof(IUrlMediaExtractor)}");
                    continue;
                }

                _Logger.LogDebug($"checking if extractor can handle url [extractor={extractor.Name}] [url={url}]");

                if (extractor.CanHandle(uri) == false) {
                    continue;
                }

                _Logger.LogInformation($"found extractor to handle URL [extractor={extractor.Name}] [url={url}]");
                return extractor;
            }

            return null;
        }

        private List<Type> _GetExtractors() {
            List<Type> types = new();

            Stopwatch timer = Stopwatch.StartNew();

            AppDomain.CurrentDomain.GetAssemblies()
                .ToList().ForEach(asm => {
                    asm.GetTypes().Where(type => {
                        if ((type.FullName ?? "").StartsWith("honooru") == false) {
                            return false;
                        }
                        return type.IsClass
                            && !type.IsAbstract && !type.IsInterface
                            && typeof(IUrlMediaExtractor).IsAssignableFrom(type);
                    }).ToList().ForEach(extractor => {
                        _Logger.LogDebug($"adding extractor [extractor={extractor.FullName}]");
                        types.Add(extractor);
                    });
                });

            _Logger.LogInformation($"loaded extractors [timer={timer.ElapsedMilliseconds}ms] [count={types.Count}]");
            timer.Stop();

            return types;
        }

    }
}
