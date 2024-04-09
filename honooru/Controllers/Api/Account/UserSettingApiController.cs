using honooru.Code;
using honooru.Models;
using honooru.Models.App;
using honooru.Models.Internal;
using honooru.Services;
using honooru.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace honooru.Controllers.Api.Account {

    [ApiController]
    [Route("/api/user-setting")]
    public class UserSettingApiController : ApiControllerBase {

        private readonly ILogger<UserSettingApiController> _Logger;

        private readonly AppCurrentAccount _CurrentUser;

        private readonly UserSettingRepository _UserSettingRepository;

        public UserSettingApiController(ILogger<UserSettingApiController> logger,
            AppCurrentAccount currentUser, UserSettingRepository userSettingRepository) {

            _Logger = logger;
            _CurrentUser = currentUser;
            _UserSettingRepository = userSettingRepository;
        }

        /// <summary>
        ///     get the <see cref="UserSetting"/>s of the current user
        /// </summary>
        /// <response code="200">
        ///     the response will contain a list of <see cref="UserSetting"/> with
        ///     <see cref="UserSetting.AccountID"/> of the user making the request
        /// </response>
        [HttpGet]
        public async Task<ApiResponse<List<UserSetting>>> GetByCurrentUser() {
            AppAccount? currentUser = await _CurrentUser.Get();
            if (currentUser == null) {
                return ApiAuthorize<List<UserSetting>>();
            }

            List<UserSetting> settings = await _UserSettingRepository.GetByAccountID(currentUser.ID);

            return ApiOk(settings);
        }

        /// <summary>
        ///     get the <see cref="UserSetting"/>s of another user
        /// </summary>
        /// <param name="accountID">ID of the user to get the settings of</param>
        /// <response code="200">
        ///     a list of <see cref="UserSetting"/>s with <see cref="UserSetting.AccountID"/> of <paramref name="accountID"/>
        /// </response>
        [HttpGet("{accountID}")]
        [PermissionNeeded(AppPermission.APP_ACCOUNT_ADMIN)]
        public async Task<ApiResponse<List<UserSetting>>> GetOtherByAccountID(ulong accountID) {
            List<UserSetting> settings = await _UserSettingRepository.GetByAccountID(accountID);

            return ApiOk(settings);
        }

        /// <summary>
        ///     update a <see cref="UserSetting"/> of the user making the request
        /// </summary>
        /// <param name="name">name of the <see cref="UserSetting"/> to update, or one will be created if it does not exist</param>
        /// <param name="value">value of the <see cref="UserSetting"/></param>
        /// <response code="200">
        ///     the <see cref="UserSetting"/> with <see cref="UserSetting.AccountID"/> of the account making the request,
        ///     and <see cref="UserSetting.Name"/> of <paramref name="name"/>,
        ///     had it's <see cref="UserSetting.Value"/> updated to <paramref name="value"/>
        /// </response>
        /// <exception cref="System.Exception"></exception>
        [HttpPost("{name}")]
        public async Task<ApiResponse> Update(string name, [FromQuery] string value) {
            AppAccount? currentUser = await _CurrentUser.Get();
            if (currentUser == null) {
                return ApiAuthorize();
            }

            UserSetting? setting = await _UserSettingRepository.GetByAccountIDAndName(currentUser.ID, name);

            if (setting == null) {
                _Logger.LogDebug($"creating new setting for user [currentUser.ID={currentUser.ID}] [name={name}] [value={value}]");
                setting = new UserSetting();
                setting.AccountID = currentUser.ID;
                setting.Name = name.ToLower();
                setting.Type = _UserSettingRepository.GetTypeMapping(name) ?? throw new System.Exception($"unmapped type from name '{name}'");
                _Logger.LogDebug($"type is {setting.Type}");
                setting.Value = value;
            } else {
                setting.Value = value;
            }

            await _UserSettingRepository.Upsert(setting);

            return ApiOk();
        }


    }
}
