using honooru.Code;
using honooru.Controllers.Api.Upload;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.Config;
using honooru.Models.Internal;
using honooru.Services;
using honooru.Services.Db;
using honooru.Services.Repositories;
using honooru.Services.Util;
using honooru_common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Controllers.Api {

    [ApiController]
    [Route("/api/jobs")]
    public class DistributedJobApiController : FileUploadBaseController {

        private readonly DistributedJobDb _JobDb;

        private static SemaphoreSlim _Semaphore = new(1, 1); // start at 1, not 0, so there is something to be released for 1 job

        public DistributedJobApiController(ILoggerFactory loggerFactory,
            DistributedJobDb jobDb, AppCurrentAccount currentUser,
            MediaAssetRepository mediaAssetRepository, FileExtensionService fileExtensionHelper,
            IOptions<StorageOptions> storageOptions, PostDb postDb)
        : base (loggerFactory, currentUser, mediaAssetRepository, fileExtensionHelper, storageOptions, postDb) {

            _JobDb = jobDb;
        }

        /// <summary>
        ///     get a list of all unclaimed <see cref="DistributedJob"/>s that can be worked on
        /// </summary>
        /// <response code="200">
        ///     the response will contain a list of all <see cref="DistributedJob"/>s
        ///     with a <see cref="DistributedJob.ClaimedAt"/> of <c>null</c>
        /// </response>
        [HttpGet("unclaimed")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse<List<DistributedJob>>> GetUnclaimedJobs() {
            List<DistributedJob> jobs = await _JobDb.GetUnclaimed();

            return ApiOk(jobs);
        }

        /// <summary>
        ///     get a list of all <see cref="DistributedJob"/>s claimed by a user
        /// </summary>
        /// <response code="200">
        ///     returns a list of all <see cref="DistributedJob"/>s with a
        ///     <see cref="DistributedJob.ClaimedByUserID"/> of the current user
        /// </response>
        [HttpGet("claimed/mine")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse<List<DistributedJob>>> GetClaimedByCurrent() {

            AppAccount currentUser = await _CurrentUser.GetRequired();

            List<DistributedJob> jobs = (await _JobDb.GetAll())
                .Where(iter => iter.ClaimedByUserID == currentUser.ID).ToList();

            return ApiOk(jobs);
        }

        /// <summary>
        ///     mark a <see cref="DistributedJob"/> as claimed. this is done by worker clients (snails)
        ///     for distributed jobs (such as downloading a youtube video)
        /// </summary>
        /// <param name="ID">ID of the <see cref="DistributedJob"/> being claimed</param>
        /// <response code="200">
        ///     the <see cref="DistributedJob"/> with <see cref="DistributedJob.ID"/> of <paramref name="ID"/>
        ///     was successfully claimed by the user making the request
        /// </response>
        /// <response code="400">
        ///     the <see cref="DistributedJob"/> was already claimed, and cannot be claimed again
        /// </response>
        /// <response code="404">
        ///     no <see cref="DistributedJob"/> with <see cref="DistributedJob.ID"/>
        ///     of <paramref name="ID"/> exists
        /// </response>
        /// <response code="500">
        ///     the semaphore used to ensure only one job is being claimed at a time failed
        ///     to release within 15 seconds. this likely means something else broke
        /// </response>
        [HttpPost("claim/{ID}")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        public async Task<ApiResponse> Claim(Guid ID) {
            // only allow one of these requests to be processed at a time
            // this is to prevent a race conditions where 2 worker clients could think they own a job
            try {
                if (await _Semaphore.WaitAsync(TimeSpan.FromSeconds(15)) == false) {
                    return ApiInternalError("failed to aquire lock within 15s");
                }
            } catch (Exception ex) {
                _Logger.LogError(ex, "mutex failed to release with timeout of 15s");
                return ApiInternalError(ex);
            }

            try {
                DistributedJob? job = await _JobDb.GetByID(ID);
                if (job == null) {
                    return ApiNotFound($"{nameof(DistributedJob)} {ID}");
                }

                if (job.ClaimedAt != null) {
                    return ApiBadRequest($"{nameof(DistributedJob)} {ID} is already claimed");
                }

                if (job.Done == true) {
                    return ApiBadRequest($"{nameof(DistributedJob)} {ID} has been marked as done");
                }

                AppAccount currentUser = await _CurrentUser.GetRequired();
                job.ClaimedAt = DateTime.UtcNow;
                job.ClaimedByUserID = currentUser.ID;

                await _JobDb.Upsert(job);
                _Logger.LogInformation($"job claimed by user [job.ID={job.ID}] [user={currentUser.ID}/{currentUser.Name}]");

                return ApiOk();
            } finally {
                _Semaphore.Release();
            }
        }

        /// <summary>
        ///     called by a job worker (snail) to upload part of a file
        /// </summary>
        /// <param name="ID">ID of the <see cref="DistributedJob"/> this upload is a part of</param>
        /// <returns></returns>
        [HttpPost("upload/part")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        [DisableFormValueModelBinding]
        public async Task<ApiResponse<MediaAsset>> UploadChunk([FromQuery] Guid ID) {
            DistributedJob? job = await _JobDb.GetByID(ID);
            if (job == null) {
                return ApiNotFound<MediaAsset>($"{nameof(DistributedJob)} {ID}");
            }

            if (job.Done == true) {
                return ApiBadRequest<MediaAsset>($"{nameof(DistributedJob)} {ID} has been marked as done");
            }

            AppAccount currentUser = await _CurrentUser.GetRequired();
            if (currentUser.ID != job.ClaimedByUserID) {
                return ApiBadRequest<MediaAsset>($"{nameof(DistributedJob)} {ID} is claimed by a different user");
            }

            ApiResponse<MediaAsset> asset = await ReadSection(ID, MediaAssetStatus.PROCESSING);
            return asset;
        }

        /// <summary>
        ///     mark a job as done uploading
        /// </summary>
        /// <param name="ID">ID of the job that has been finished uploading</param>
        /// <returns></returns>
        [HttpPost("upload/finish")]
        [PermissionNeeded(AppPermission.APP_UPLOAD)]
        [DisableFormValueModelBinding]
        public async Task<ApiResponse<MediaAsset>> FinishJob([FromQuery] Guid ID) {
            DistributedJob? job = await _JobDb.GetByID(ID);
            if (job == null) {
                return ApiNotFound<MediaAsset>($"{nameof(DistributedJob)} {ID}");
            }

            if (job.Done == true) {
                return ApiBadRequest<MediaAsset>($"{nameof(DistributedJob)} {ID} has been marked as done");
            }

            AppAccount currentUser = await _CurrentUser.GetRequired();
            if (currentUser.ID != job.ClaimedByUserID) {
                return ApiBadRequest<MediaAsset>($"cannot finish job: {nameof(DistributedJob)} {ID} is claimed by a different user");
            }

            MediaAsset? asset = await _MediaAssetRepository.GetByID(ID);
            if (asset == null) {
                return ApiNotFound<MediaAsset>($"{nameof(MediaAsset)} {ID}");
            }

            if (asset.Status != MediaAssetStatus.PROCESSING) {
                return ApiBadRequest<MediaAsset>($"expected {nameof(MediaAsset)} {ID} to be {MediaAssetStatus.PROCESSING}, but it was {asset.Status} instead");
            }

            // do not move the asset to the /work folder, as the extract expects it to still be in /upload
            ApiResponse<MediaAsset> response = await _HandleAsset(asset, moveAsset: false);
            if (response.Status != 200) {
                return response;
            }

            job.Done = true;
            await _JobDb.Upsert(job);

            return response;
        }

    }
}
