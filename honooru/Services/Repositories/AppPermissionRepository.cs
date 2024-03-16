using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using honooru.Models;
using honooru.Services.Db;
using honooru.Models.Internal;
using System.Linq;

namespace honooru.Services.Repositories {

    /// <summary>
    ///     Repository to interact with <see cref="AppGroupPermission"/>s
    /// </summary>
    public class AppPermissionRepository {

        private readonly ILogger<AppPermissionRepository> _Logger;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY = "App.Permission.{0}"; // {0} => groupd ID

        private readonly AppGroupPermissionDbStore _PermissionDb;
        private readonly AppAccountGroupMembershipDbStore _MembershipDb;

        public AppPermissionRepository(ILogger<AppPermissionRepository> logger, IMemoryCache cache,
            AppGroupPermissionDbStore permissionDb, AppAccountGroupMembershipDbStore membershipDb) {

            _Logger = logger;
            _Cache = cache;

            _PermissionDb = permissionDb;
            _MembershipDb = membershipDb;
        }

        /// <summary>
        ///     Get a specific <see cref="AppGroupPermission"/> by its ID, or null if it doens't exist
        /// </summary>
        public Task<AppGroupPermission?> GetByID(long ID) {
            return _PermissionDb.GetByID(ID);
        }

        public async Task<List<AppGroupPermission>> GetByAccountID(ulong accountID) {
            Dictionary<string, AppGroupPermission> perms = new();

            List<AppAccountGroupMembership> groups = await _MembershipDb.GetByAccountID(accountID);

            foreach (AppAccountGroupMembership member in groups) {
                List<AppGroupPermission> groupPerms = await GetByGroupID(member.GroupID);

                foreach (AppGroupPermission p in groupPerms) {
                    if (perms.ContainsKey(p.Permission) == false) {
                        perms.Add(p.Permission, p);
                    }
                }
            }

            return perms.Values.ToList();
        }

        /// <summary>
        ///     Get the <see cref="AppGroupPermission"/>s of a group
        /// </summary>
        /// <param name="groupID">ID of the group</param>
        public async Task<List<AppGroupPermission>> GetByGroupID(ulong groupID) {
            string cacheKey = string.Format(CACHE_KEY, groupID);

            if (_Cache.TryGetValue(cacheKey, out List<AppGroupPermission>? perms) == false || perms == null) {
                perms = await _PermissionDb.GetByGroupID(groupID);

                _Cache.Set(cacheKey, perms, new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }

            return perms;
        }

        /// <summary>
        ///     Insert a new <see cref="AppGroupPermission"/>, returning the ID it has after being inserted
        /// </summary>
        public Task<ulong> Insert(AppGroupPermission perm) {
            string cacheKey = string.Format(CACHE_KEY, perm.GroupID);
            _Cache.Remove(cacheKey);

            return _PermissionDb.Insert(perm);
        }

        /// <summary>
        ///     Delete a <see cref="AppGroupPermission"/>
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task DeleteByID(long ID) {
            AppGroupPermission? perm = await GetByID(ID);
            if (perm != null) {
                string cacheKey = string.Format(CACHE_KEY, perm.GroupID);
                _Cache.Remove(cacheKey);
            }

            await _PermissionDb.DeleteByID(ID);
        }

    }

    /// <summary>
    ///     Useful extensions method for a <see cref="AppPermissionRepository"/>
    /// </summary>
    public static class AppGroupPermissionRepositoryExtensionMethods {

        /// <summary>
        ///     Get the <see cref="AppGroupPermission"/> for a <see cref="AppAccount"/> based on a list of permissions
        /// </summary>
        /// <param name="repo">Extension instance</param>
        /// <param name="group">group to get the permission of</param>
        /// <param name="permissions">Permissions to return</param>
        /// <returns>
        ///     The first <see cref="AppGroupPermission"/> that the account <paramref name="group"/> has that matches
        ///     one of the permission keys in <paramref name="permissions"/>.
        ///     Or <c>null</c> if the user does not have any of those permissions
        /// </returns>
        public static Task<AppGroupPermission?> GetPermissionByGroup(this AppPermissionRepository repo, AppGroup group, params string[] permissions) {
            return repo.GetPermissionByGroupID(group.ID, permissions);
        }

        /// <summary>
        ///     Get the <see cref="AppGroupPermission"/> for a <see cref="AppAccount"/> based on a list of permissions
        /// </summary>
        /// <param name="repo">Extension instance</param>
        /// <param name="groupID">ID of the group to get the permission of</param>
        /// <param name="permissions">Permissions to return</param>
        /// <returns>
        ///     The first <see cref="AppGroupPermission"/> that the account with <see cref="AppAccount.ID"/>
        ///     of <paramref name="groupID"/> has that matches one of the permission keys in <paramref name="permissions"/>.
        ///     Or <c>null</c> if the user does not have any of those permissions
        /// </returns>
        public static async Task<AppGroupPermission?> GetPermissionByGroupID(this AppPermissionRepository repo, ulong groupID, params string[] permissions) {
            List<AppGroupPermission> perms = await repo.GetByGroupID(groupID);

            foreach (AppGroupPermission perm in perms) {
                foreach (string testPerm in permissions) {
                    if (perm.Permission.ToLower() == testPerm.ToLower()) {
                        return perm;
                    }
                }
            }

            return null;
        }

    }


}
