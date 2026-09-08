using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediSecureApi.Data;
using MediSecureApi.DTOs;
using MediSecureApi.Models;
using MediSecureApi.Services;

namespace MediSecureApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthController(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Register a new user.
    /// VULNERABILITY (TH-02 / Registration Escalation): Accepts arbitrary 'Role' in payload.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { error = "Email is already registered." });
        }

        using var sha = SHA256.Create();
        var passwordHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(request.Password))).ToLower();

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash,
            // VULNERABLE: Allows client to specify Role = "Doctor" or "Admin" during registration!
            Role = !string.IsNullOrWhiteSpace(request.Role) ? request.Role : "Patient",
            IsAdmin = request.Role == "Admin",
            NationalIdOrSSN = request.NationalIdOrSSN,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsAdmin = user.IsAdmin
        });
    }

    /// <summary>
    /// Authenticate user and issue JWT token.
    /// VULNERABILITY (TH-07): Unthrottled login endpoint (No rate limiting, susceptible to credential stuffing / brute force).
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(request.Password))).ToLower();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.PasswordHash == hash);
        if (user == null)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsAdmin = user.IsAdmin
        });
    }

    /// <summary>
    /// User Logout.
    /// VULNERABILITY (TH-01): Lacks server-side token revocation or blacklist. The token remains valid until expiration.
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // No token blacklist or revocation is performed!
        return Ok(new { message = "Logged out successfully (client should discard token)." });
    }

    /// <summary>
    /// Educational Endpoint: Demonstrates STRIDE Spoofing (TH-01).
    /// Generates unsigned or weak-secret forged JWT tokens.
    /// </summary>
    [HttpPost("forge-token")]
    public IActionResult ForgeToken([FromBody] ForgedTokenRequest request)
    {
        var token = _jwtService.GenerateForgedToken(request.UserId, request.Email, request.Role, request.UseNoneAlgorithm);
        return Ok(new
        {
            threatId = "TH-01",
            vulnerability = "Weak JWT / Algorithm None / Hardcoded Secret",
            forgedToken = token,
            targetRole = request.Role,
            algorithmUsed = request.UseNoneAlgorithm ? "none" : "HS256 (Hardcoded Key)",
            instruction = "Send this token in 'Authorization: Bearer <forgedToken>' header to simulate spoofing."
        });
    }
}
