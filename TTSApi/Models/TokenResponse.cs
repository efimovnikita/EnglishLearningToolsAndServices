using System.Text.Json.Serialization;

namespace TTSApi.Models;

public class TokenResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }

    [JsonPropertyName("expires_at")] public long ExpiresAt { get; set; }
}