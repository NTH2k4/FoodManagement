using FoodManagement.Contracts;
using System.Text.Json;

namespace FoodManagement.Security
{
    public class FirebaseAuditService : IAuditService
    {
        private readonly HttpClient _http;
        private readonly string _databaseUrl;
        private readonly string? _authToken;
        private readonly ILogger<FirebaseAuditService> _logger;
        private const string AuditNode = "auditLogs";

        public FirebaseAuditService(IConfiguration config, ILogger<FirebaseAuditService> logger)
        {
            _logger = logger;
            _http = new HttpClient();
            _databaseUrl = config["Firebase:DatabaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("DatabaseUrl not configured");
            _authToken = config["Firebase:AuthToken"];
        }

        private string BuildUrl()
        {
            var url = $"{_databaseUrl}/{AuditNode}.json";
            if (!string.IsNullOrEmpty(_authToken)) url += $"?auth={_authToken}";
            return url;
        }

        public async Task LogAsync(string? adminId, string action, string? details = null, CancellationToken ct = default)
        {
            var obj = new
            {
                adminId = adminId,
                action = action,
                details = details,
                atUtc = DateTime.UtcNow
            };
            var url = BuildUrl();
            var json = JsonSerializer.Serialize(obj);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            try
            {
                var resp = await _http.PostAsync(url, content, ct);
                resp.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit log failed");
            }
        }
    }
}
