using FoodManagement.Contracts;

namespace FoodManagement.Presenters
{
    public class LoginPresenter
    {
        private readonly ILoginView _view;
        private readonly IAdminRepository _repo;
        private readonly IPasswordHasher _hasher;
        private readonly IAuthService _auth;
        private readonly IAuditService _audit;
        private const int MaxFailedAttempts = 5;
        private readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public LoginPresenter(ILoginView view, IAdminRepository repo, IPasswordHasher hasher, IAuthService auth, IAuditService audit)
        {
            _view = view;
            _repo = repo;
            _hasher = hasher;
            _auth = auth;
            _audit = audit;
        }

        public async Task HandleLoginAsync(CancellationToken ct = default)
        {
            var username = _view.Username?.Trim();
            var password = _view.Password ?? string.Empty;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _view.ShowError("Tên đăng nhập và mật khẩu không được bỏ trống.");
                return;
            }

            var admin = await _repo.GetByUsernameAsync(username, ct);
            if (admin == null)
            {
                await _audit.LogAsync(null, "FailedLogin", $"Unknown username {username}", ct);
                _view.ShowError("Tên đăng nhập hoặc mật khẩu không đúng.");
                return;
            }

            if (!admin.isActive)
            {
                await _audit.LogAsync(admin.id, "FailedLogin", "Account disabled", ct);
                _view.ShowError("Tài khoản đã bị vô hiệu hóa.");
                return;
            }

            if (admin.LockoutEnd > DateTime.UtcNow)
            {
                _view.ShowError($"Tài khoản bị khóa tới {admin.LockoutEnd:u}");
                return;
            }

            var ok = _hasher.VerifyPassword(password, admin.passwordHashBase64, admin.passwordSaltBase64);
            if (!ok)
            {
                admin.failedLoginAttempts++;
                if (admin.failedLoginAttempts >= MaxFailedAttempts)
                {
                    admin.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                    await _audit.LogAsync(admin.id, "Lockout", $"Locked until {admin.LockoutEnd:u}", ct);
                }
                await _repo.UpdateAsync(admin, ct);
                await _audit.LogAsync(admin.id, "FailedLogin", $"Wrong password attempt #{admin.failedLoginAttempts}", ct);
                _view.ShowError("Tên đăng nhập hoặc mật khẩu không đúng.");
                return;
            }

            admin.failedLoginAttempts = 0;
            admin.LastLoginAt = DateTime.UtcNow;
            admin.LockoutEnd = DateTime.MinValue;
            await _repo.UpdateAsync(admin, ct);
            await _auth.SignInAsync(admin, _view.RememberMe);
            await _audit.LogAsync(admin.id, "Login", "Successful", ct);
            _view.RedirectTo("/Dashboard");
        }
    }
}
