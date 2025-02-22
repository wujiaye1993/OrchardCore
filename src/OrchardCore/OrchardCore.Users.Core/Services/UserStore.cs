using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OrchardCore.Modules;
using OrchardCore.Security.Services;
using OrchardCore.Users.Handlers;
using OrchardCore.Users.Indexes;
using OrchardCore.Users.Models;
using YesSql;

namespace OrchardCore.Users.Services
{
    public class UserStore :
        IUserClaimStore<IUser>,
        IUserRoleStore<IUser>,
        IUserPasswordStore<IUser>,
        IUserEmailStore<IUser>,
        IUserSecurityStampStore<IUser>,
        IUserLoginStore<IUser>
    {
        private readonly ISession _session;
        private readonly IRoleService _roleService;
        private readonly ILookupNormalizer _keyNormalizer;
        private readonly ILogger _logger;

        public UserStore(ISession session,
            IRoleService roleService,
            ILookupNormalizer keyNormalizer,
            ILogger<UserStore> logger,
            IEnumerable<IUserCreatedEventHandler> handlers)
        {
            _session = session;
            _roleService = roleService;
            _keyNormalizer = keyNormalizer;
            _logger = logger;
            Handlers = handlers;
        }
        public IEnumerable<IUserCreatedEventHandler> Handlers { get; private set; }

        public void Dispose()
        {
        }

        public string NormalizeKey(string key)
        {
            return _keyNormalizer == null ? key : _keyNormalizer.Normalize(key);
        }

        #region IUserStore<IUser>
        public async Task<IdentityResult> CreateAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _session.Save(user);

            try
            {
                await _session.CommitAsync();

                var context = new CreateUserContext(user);
                await Handlers.InvokeAsync(handler => handler.CreatedAsync(context), _logger);
            }
            catch
            {
                return IdentityResult.Failed();
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _session.Delete(user);

            try
            {
                await _session.CommitAsync();
            }
            catch
            {
                return IdentityResult.Failed();
            }

            return IdentityResult.Success;
        }

        public async Task<IUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            int id;
            if (!int.TryParse(userId, out id))
            {
                return null;
            }

            return await _session.GetAsync<User>(id);
        }

        public async Task<IUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _session.Query<User, UserIndex>(u => u.NormalizedUserName == normalizedUserName).FirstOrDefaultAsync();
        }

        public Task<string> GetNormalizedUserNameAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).NormalizedUserName);
        }

        public Task<string> GetUserIdAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public Task<string> GetUserNameAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).UserName);
        }

        public Task SetNormalizedUserNameAsync(IUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ((User)user).NormalizedUserName = normalizedName;

            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(IUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ((User)user).UserName = userName;

            return Task.CompletedTask;
        }

        public Task<IdentityResult> UpdateAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            _session.Save(user);

            return Task.FromResult(IdentityResult.Success);
        }

        #endregion

        #region IUserPasswordStore<IUser>
        public Task<string> GetPasswordHashAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).PasswordHash);
        }

        public Task SetPasswordHashAsync(IUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ((User)user).PasswordHash = passwordHash;

            return Task.CompletedTask;
        }

        public Task<bool> HasPasswordAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).PasswordHash != null);
        }

        #endregion

        #region ISecurityStampValidator<IUser>
        public Task SetSecurityStampAsync(IUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ((User)user).SecurityStamp = stamp;

            return Task.CompletedTask;
        }

        public Task<string> GetSecurityStampAsync(IUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).SecurityStamp);
        }
        #endregion

        #region IUserEmailStore<IUser>
        public Task SetEmailAsync(IUser user, string email, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ((User)user).Email = email;

            return Task.CompletedTask;
        }

        public Task<string> GetEmailAsync(IUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).Email);
        }

        public Task<bool> GetEmailConfirmedAsync(IUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(IUser user, bool confirmed, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ((User)user).EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public async Task<IUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return await _session.Query<User, UserIndex>(u => u.NormalizedEmail == normalizedEmail).FirstOrDefaultAsync();
        }

        public Task<string> GetNormalizedEmailAsync(IUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(((User)user).NormalizedEmail);
        }

        public Task SetNormalizedEmailAsync(IUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ((User)user).NormalizedEmail = normalizedEmail;

            return Task.CompletedTask;
        }

        #endregion

        #region IUserRoleStore<IUser>
        public async Task AddToRoleAsync(IUser user, string normalizedRoleName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roleNames = await _roleService.GetRoleNamesAsync();
            var roleName = roleNames?.FirstOrDefault(r => NormalizeKey(r) == normalizedRoleName);

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new InvalidOperationException($"Role {normalizedRoleName} does not exist.");
            }

            ((User)user).RoleNames.Add(roleName);
        }

        public async Task RemoveFromRoleAsync(IUser user, string normalizedRoleName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roleNames = await _roleService.GetRoleNamesAsync();
            var roleName = roleNames?.FirstOrDefault(r => NormalizeKey(r) == normalizedRoleName);

            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new InvalidOperationException($"Role {normalizedRoleName} does not exist.");
            }

            ((User)user).RoleNames.Remove(roleName);
        }

        public Task<IList<string>> GetRolesAsync(IUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult<IList<string>>(((User)user).RoleNames);
        }

        public Task<bool> IsInRoleAsync(IUser user, string normalizedRoleName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(normalizedRoleName));
            }

            return Task.FromResult(((User)user).RoleNames.Contains(normalizedRoleName, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<IList<IUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(normalizedRoleName))
            {
                throw new ArgumentNullException(nameof(normalizedRoleName));
            }

            var users = await _session.Query<User, UserByRoleNameIndex>(u => u.RoleName == normalizedRoleName).ListAsync();
            return users == null ? new List<IUser>() : users.ToList<IUser>();
        }
        #endregion

        #region IUserLoginStore<IUser>
        public Task AddLoginAsync(IUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            if (((User)user).LoginInfos.Any(i => i.LoginProvider == login.LoginProvider))
                throw new InvalidOperationException($"Provider {login.LoginProvider} is already linked for {user.UserName}");

            ((User)user).LoginInfos.Add(login);

            return Task.CompletedTask;
        }

        public async Task<IUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            return await _session.Query<User, UserByLoginInfoIndex>(u => u.LoginProvider == loginProvider && u.ProviderKey == providerKey).FirstOrDefaultAsync();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(IUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult<IList<UserLoginInfo>>(((User)user).LoginInfos);
        }

        public Task RemoveLoginAsync(IUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var externalLogins = ((User)user).LoginInfos;
            if (externalLogins != null)
            {
                var item = externalLogins.FirstOrDefault(c => c.LoginProvider == loginProvider && c.ProviderKey == providerKey);
                if (item != null)
                {
                    externalLogins.Remove(item);
                }
            }
            return Task.CompletedTask;
        }

        #endregion

        #region IUserClaimStore<IUser>
        public Task<IList<Claim>> GetClaimsAsync(IUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult<IList<Claim>>(((User)user).UserClaims.Select(x => x.ToClaim()).ToList());
        }

        public Task AddClaimsAsync(IUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            foreach (var claim in claims)
            {
                ((User)user).UserClaims.Add(new UserClaim{ClaimType = claim.Type, ClaimValue = claim.Value});
            }

            return Task.CompletedTask;
        }

        public Task ReplaceClaimAsync(IUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (claim == null)
                throw new ArgumentNullException(nameof(claim));
            if (newClaim == null)
                throw new ArgumentNullException(nameof(newClaim));

            foreach (var userClaim in ((User) user).UserClaims.Where(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type))
            {
                userClaim.ClaimValue = newClaim.Value;
                userClaim.ClaimType = newClaim.Type;
            }

            return Task.CompletedTask;
        }

        public Task RemoveClaimsAsync(IUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            foreach (var claim in claims)
            {
                foreach (var userClaim in ((User)user).UserClaims.Where(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList())
                    ((User)user).UserClaims.Remove(userClaim);
            }

            return Task.CompletedTask;
        }

        public async Task<IList<IUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            if (claim == null)
                throw new ArgumentNullException(nameof(claim));
            
            var users = await _session.Query<User, UserByClaimIndex>(uc => uc.ClaimType == claim.Type && uc.ClaimValue == claim.Value).ListAsync();

            return users.Cast<IUser>().ToList();
        }
        #endregion
    }
}
