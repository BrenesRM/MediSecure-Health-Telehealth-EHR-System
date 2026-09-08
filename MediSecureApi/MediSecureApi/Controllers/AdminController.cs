using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediSecureApi.Data;
using MediSecureApi.Services;

namespace MediSecureApi.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AdminController(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
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
    /// Export complete EHR database dump including all patients, PII, SSNs, and medical records.
    /// VULNERABILITY (TH-08 / CWE-285 / OWASP API5): Broken Function Level Authorization (BFLA).
    /// Verifies authentication token exists, but OMITS role-based authorization check (Role == "Admin")!
    /// Any low-privileged Patient token can access and download this complete hospital dump.
    /// </summary>
    [HttpGet("export-database")]
    public async Task<IActionResult> ExportAllHospitalData()
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        // FLAW (TH-08): Missing role check:
        // if (!principal.IsInRole("Admin")) return Forbid("Administrative privilege required.");

        var users = await _context.Users.ToListAsync();
        var records = await _context.MedicalRecords.ToListAsync();
        var appointments = await _context.Appointments.ToListAsync();
        var labs = await _context.LabResults.ToListAsync();

        return Ok(new
        {
            exportedAt = DateTime.UtcNow,
            requestedBy = principal.FindFirst(ClaimTypes.Email)?.Value,
            callerRole = principal.FindFirst(ClaimTypes.Role)?.Value,
            securityAlert = "CRITICAL: BFLA Flaw (TH-08) - Non-admin user successfully dumped entire EHR database.",
            summary = new
            {
                totalUsers = users.Count,
                totalRecords = records.Count,
                totalAppointments = appointments.Count,
                totalLabResults = labs.Count
            },
            data = new
            {
                users,
                medicalRecords = records,
                appointments,
                labResults = labs
            }
        });
    }

    /// <summary>
    /// View system operations logs.
    /// VULNERABILITY (TH-08): BFLA on administrative system logs.
    /// </summary>
    [HttpGet("audit-logs")]
    public IActionResult GetSystemLogs()
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        // Mock operational logs
        var logs = new[]
        {
            new { timestamp = DateTime.UtcNow.AddMinutes(-120), level = "INFO", message = "EHR Server started on port 5000." },
            new { timestamp = DateTime.UtcNow.AddMinutes(-90), level = "INFO", message = "Lab partner Webhook received from QuestDiagnostics." },
            new { timestamp = DateTime.UtcNow.AddMinutes(-45), level = "WARN", message = "Unauthenticated webhook dispatched to /api/v1/webhooks/lab-sync." },
            new { timestamp = DateTime.UtcNow.AddMinutes(-10), level = "INFO", message = "Patient record query executed." }
        };

        return Ok(new
        {
            logs,
            callerRole = principal.FindFirst(ClaimTypes.Role)?.Value
        });
    }
}
