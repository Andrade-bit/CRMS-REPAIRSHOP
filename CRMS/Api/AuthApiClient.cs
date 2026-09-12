using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CRMS.winforms.Api
{
    public class LoginResult
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();

        [JsonPropertyName("companyId")]
        public int? CompanyId { get; set; }
    }

    public class UserAccount
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class TicketDto
    {
        [JsonPropertyName("ticketId")]
        public int TicketId { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [JsonPropertyName("concern")]
        public string Concern { get; set; } = string.Empty;

        [JsonPropertyName("estimatedCost")]
        public decimal EstimatedCost { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("pickedUpAt")]
        public DateTime? PickedUpAt { get; set; }

        [JsonPropertyName("staffName")]
        public string StaffName { get; set; } = string.Empty;
    }

    public class AuthApiClient
    {
        // Same address used in CRMS.api.http. Move this to a config file later.
        private const string BaseUrl = "http://localhost:5029";

        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Calls POST /api/auth/login. Returns null if the credentials were
        /// rejected (401/400) or the request failed for any reason.
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            var payload = new { username, password };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync("/api/auth/login", content);

                if (!response.IsSuccessStatusCode)
                    return null; // wrong credentials, or server-side validation error

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LoginResult>(body, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                // API not running / unreachable
                return null;
            }
        }

        /// <summary>
        /// Calls POST /api/auth/register-temp to create a Staff/Manager/Admin
        /// account. companyId ties the new account to the same tenant as the
        /// Admin creating it (omit for Superadmin/tenant-agnostic accounts).
        /// Returns (true, serverMessage) on success, (false, errorMessage) on failure.
        /// </summary>
        public async Task<(bool Success, string Message)> RegisterAsync(
            string token, string username, string password, string fullName, string role, int? companyId = null)
        {
            var payload = new { username, password, fullName, role, companyId };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register-temp")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, body);
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>
        /// Calls GET /api/auth/users, scoped to one tenant. Returns an empty
        /// list if the request fails for any reason.
        /// </summary>
        public async Task<List<UserAccount>> GetUsersAsync(int? companyId)
        {
            string url = companyId.HasValue ? $"/api/auth/users?companyId={companyId}" : "/api/auth/users";

            try
            {
                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new List<UserAccount>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<UserAccount>>(body, _jsonOptions) ?? new List<UserAccount>();
            }
            catch (HttpRequestException)
            {
                return new List<UserAccount>();
            }
        }

        /// <summary>
        /// Calls POST /api/auth/reset-password-temp to set a new password for
        /// an existing account. Returns (true, serverMessage) on success,
        /// (false, errorMessage) on failure.
        /// </summary>
        public async Task<(bool Success, string Message)> ResetPasswordAsync(string username, string newPassword)
        {
            var payload = new { username, newPassword };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _http.PostAsync("/api/auth/reset-password-temp", content);
                string body = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, body.Trim('"'));

                return (false, body);
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>
        /// Calls POST /api/tickets to create a real Customer + ServiceTicket
        /// in the caller's own tenant database, attributed to their Staff record.
        /// </summary>
        public async Task<(bool Success, string Message, TicketDto? Ticket)> CreateTicketAsync(
            string token, string firstName, string middleName, string lastName,
            string phone, string email, string deviceName, string concern, decimal estimatedCost)
        {
            var payload = new
            {
                firstName,
                middleName,
                lastName,
                phone,
                email,
                deviceName,
                concern,
                estimatedCost
            };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tickets") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, body, null);

                var ticket = JsonSerializer.Deserialize<TicketDto>(body, _jsonOptions);
                return (true, "Created.", ticket);
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.", null);
            }
        }

        /// <summary>
        /// Calls GET /api/tickets, scoped to the caller's tenant.
        /// status: "pending", "history", or null for everything.
        /// Returns an empty list if the request fails for any reason.
        /// </summary>
        public async Task<List<TicketDto>> GetTicketsAsync(string token, string? status = null)
        {
            string url = status is null ? "/api/tickets" : $"/api/tickets?status={status}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return new List<TicketDto>();

                string body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<TicketDto>>(body, _jsonOptions) ?? new List<TicketDto>();
            }
            catch (HttpRequestException)
            {
                return new List<TicketDto>();
            }
        }

        /// <summary>Calls PUT /api/tickets/{id}/status. Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> UpdateTicketStatusAsync(string token, int ticketId, string status)
        {
            var payload = new { status };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}/status") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, body);
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }

        /// <summary>Calls PUT /api/tickets/{id}/pickup. Returns (true, "") on success, (false, errorMessage) on failure.</summary>
        public async Task<(bool Success, string Message)> UpdateTicketPickupStatusAsync(string token, int ticketId, bool pickedUp)
        {
            var payload = new { pickedUp };
            string json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/tickets/{ticketId}/pickup") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                    return (true, "");

                string body = await response.Content.ReadAsStringAsync();
                return (false, body);
            }
            catch (HttpRequestException)
            {
                return (false, "Could not reach the server.");
            }
        }
    }
}