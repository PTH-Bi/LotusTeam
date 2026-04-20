using LotusTeam.Data;
using LotusTeam.Models;
using LotusTeam.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using LotusTeam.Service;

namespace LotusTeam.Controllers
{
    [ApiController]
    [Route("")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public GoogleAuthController(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpGet("oauth2callback")]
        public async Task<IActionResult> OAuthCallback(string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("No authorization code returned from Google.");

            try
            {
                using var client = new HttpClient();

                var values = new Dictionary<string, string>
                {
                    { "client_id", _config["Google:ClientId"]! },
                    { "client_secret", _config["Google:ClientSecret"]! },
                    { "code", code },
                    { "grant_type", "authorization_code" },
                    { "redirect_uri", "https://localhost:7010/oauth2callback" }
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    content
                );

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, result);
                }

                var json = JsonDocument.Parse(result);

                string? accessToken = json.RootElement.GetProperty("access_token").GetString();
                int expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();

                string? refreshToken = null;
                if (json.RootElement.TryGetProperty("refresh_token", out var refreshElement))
                {
                    refreshToken = refreshElement.GetString();
                }

                // Nếu có refresh_token thì lưu DB
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var encrypted = TokenEncryption.Encrypt(refreshToken);

                    // Disable token cũ
                    var oldTokens = await _context.GoogleTokens
                        .Where(x => x.IsActive)
                        .ToListAsync();

                    foreach (var token in oldTokens)
                        token.IsActive = false;

                    _context.GoogleTokens.Add(new GoogleTokens
                    {
                        Email = "huypttb01129@fpt.edu.vn", // Có thể lấy động nếu cần
                        RefreshToken = encrypted,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    });

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Google OAuth success",
                    access_token = accessToken,
                    expires_in = expiresIn,
                    refresh_token_saved = !string.IsNullOrEmpty(refreshToken)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    detail = ex.Message
                });
            }
        }
    }
}