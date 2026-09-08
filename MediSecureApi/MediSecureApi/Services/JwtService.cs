using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using MediSecureApi.Models;

namespace MediSecureApi.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateForgedToken(int userId, string email, string role, bool useNoneAlgorithm);
    ClaimsPrincipal? ValidateToken(string token);
}

// TH-01: Weak JWT Implementation (Spoofing)
// Demonstrates token algorithm confusion, "none" algorithm acceptance, and hardcoded secret fallbacks.
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        // Hardcoded secret key fallback (VULNERABLE)
        _secretKey = _configuration["JwtSettings:SecretKey"] ?? "SuperSecretDevKey1234567890!CarePulseHealthcareSecret2026";
        _issuer = _configuration["JwtSettings:Issuer"] ?? "MediSecureHealth";
        _audience = _configuration["JwtSettings:Audience"] ?? "MediSecureClients";
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("isAdmin", user.IsAdmin.ToString().ToLower())
            }),
            Expires = DateTime.UtcNow.AddMinutes(1440), // 24 hours without revocation
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // Educational helper: Forges tokens to demonstrate STRIDE Spoofing (TH-01)
    public string GenerateForgedToken(int userId, string email, string role, bool useNoneAlgorithm)
    {
        if (useNoneAlgorithm)
        {
            // Base64URL header with "alg": "none"
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"none\",\"typ\":\"JWT\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            
            var payloadObj = new
            {
                nameid = userId.ToString(),
                email = email,
                role = role,
                isAdmin = (role == "Admin").ToString().ToLower(),
                iss = _issuer,
                aud = _audience,
                exp = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds()
            };
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payloadObj))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

            // Token with no signature (unsigned JWT)
            return $"{header}.{payload}.";
        }
        else
        {
            // Signed with the weak known dev secret
            var user = new User { Id = userId, Email = email, FullName = "Forged Identity", Role = role, IsAdmin = (role == "Admin") };
            return GenerateToken(user);
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        try
        {
            // VULNERABILITY (TH-01): If token header declares "alg": "none", parse payload directly without verifying HMAC!
            var parts = token.Split('.');
            if (parts.Length >= 2)
            {
                var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
                if (headerJson.Contains("\"none\"", StringComparison.OrdinalIgnoreCase))
                {
                    // Insecurely bypass cryptographic signature verification!
                    var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                    var claimsDoc = JsonDocument.Parse(payloadJson);
                    var claims = new List<Claim>();

                    foreach (var prop in claimsDoc.RootElement.EnumerateObject())
                    {
                        var claimType = prop.Name switch
                        {
                            "nameid" => ClaimTypes.NameIdentifier,
                            "email" => ClaimTypes.Email,
                            "role" => ClaimTypes.Role,
                            "unique_name" => ClaimTypes.Name,
                            _ => prop.Name
                        };
                        claims.Add(new Claim(claimType, prop.Value.ToString()));
                    }

                    var identity = new ClaimsIdentity(claims, "InsecureCustomScheme");
                    return new ClaimsPrincipal(identity);
                }
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        return Convert.FromBase64String(output);
    }
}
