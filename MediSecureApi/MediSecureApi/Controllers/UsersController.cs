using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediSecureApi.Data;
using MediSecureApi.DTOs;
using MediSecureApi.Services;

namespace MediSecureApi.Controllers;

[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public UsersController(AppDbContext context, IJwtService jwtService, IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    private ClaimsPrincipal? GetCurrentPrincipal()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        var token = authHeader.Substring("Bearer ".Length).Trim();
        return _jwtService.ValidateToken(token);
    }

    /// <summary>
    /// List all registered users in the hospital system.
    /// VULNERABILITY (TH-06 / OWASP API3): Excessive Data Exposure.
    /// Returns unmasked National ID/SSN and Password Hashes in API response!
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var users = await _context.Users.ToListAsync();
        
        // VULNERABLE: Direct model serialization or DTO exposing sensitive PHI & password hashes
        var response = users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            NationalIdOrSSN = u.NationalIdOrSSN, // PII LEAK
            PasswordHash = u.PasswordHash,       // SENSITIVE CREDENTIAL LEAK
            PhoneNumber = u.PhoneNumber,
            Address = u.Address,
            IsAdmin = u.IsAdmin,
            CreatedAt = u.CreatedAt
        });

        return Ok(response);
    }

    /// <summary>
    /// Get current logged-in user profile.
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return BadRequest(new { error = "Invalid token claims." });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { error = "User not found." });

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            NationalIdOrSSN = user.NationalIdOrSSN,
            PasswordHash = user.PasswordHash,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Update current user profile.
    /// VULNERABILITY (TH-02 / CWE-915 / OWASP API3): Mass Assignment.
    /// Directly binds client-submitted JSON fields ('Role', 'IsAdmin') to the database model!
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return BadRequest(new { error = "Invalid token claims." });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { error = "User not found." });

        // Normal profile updates
        if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) user.PhoneNumber = request.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(request.Address)) user.Address = request.Address;

        // MASS ASSIGNMENT FLAW (TH-02):
        // Allows regular patient to pass "Role": "Admin" or "IsAdmin": true to elevate privileges!
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            user.Role = request.Role;
        }
        if (request.IsAdmin.HasValue)
        {
            user.IsAdmin = request.IsAdmin.Value;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Profile updated successfully.",
            user = new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsAdmin = user.IsAdmin,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            },
            securityAlert = user.IsAdmin ? "Privilege elevated to Admin via Mass Assignment parameter tampering (TH-02)." : null
        });
    }

    /// <summary>
    /// Telemetry & Server Diagnostics endpoint.
    /// VULNERABILITY (TH-06 / CWE-209): Information Disclosure.
    /// Exposes internal server environment, database connection string, and memory stats.
    /// </summary>
    [HttpGet("diagnostics")]
    public IActionResult GetDiagnostics([FromQuery] bool debug = false)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        if (debug || _configuration.GetValue<bool>("SecuritySettings:EnableVerboseErrorStackTraces"))
        {
            return Ok(new
            {
                serverRuntime = Environment.Version.ToString(),
                osVersion = Environment.OSVersion.ToString(),
                machineName = Environment.MachineName,
                connectionString = _configuration.GetConnectionString("DefaultConnection"), // SENSITIVE LEAK
                jwtSecretKey = _configuration["JwtSettings:SecretKey"],                     // CRITICAL SECRET LEAK
                workingDirectory = Directory.GetCurrentDirectory(),
                activeThreads = Environment.ProcessorCount
            });
        }

        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}
