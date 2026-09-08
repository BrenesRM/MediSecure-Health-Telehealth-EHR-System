using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediSecureApi.Data;
using MediSecureApi.Models;
using MediSecureApi.Services;

namespace MediSecureApi.Controllers;

[ApiController]
[Route("api/v1/labs")]
[Produces("application/json")]
public class LabsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IWebHostEnvironment _env;

    public LabsController(AppDbContext context, IJwtService jwtService, IWebHostEnvironment env)
    {
        _context = context;
        _jwtService = jwtService;
        _env = env;
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
    /// Get lab test reports for a patient.
    /// VULNERABILITY (TH-05 / IDOR): Any user can inspect another patient's lab files.
    /// </summary>
    [HttpGet("results/{patientId}")]
    public async Task<IActionResult> GetLabResults(int patientId)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var results = await _context.LabResults
            .Where(l => l.PatientId == patientId)
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Upload a medical diagnostic scan or laboratory report.
    /// VULNERABILITY (TH-09 / CWE-434 / CWE-22 / OWASP API8): Insecure File Upload & Path Traversal.
    /// 1. Allows arbitrary file extensions (.html, .exe, .sh, .py, .svg with XSS).
    /// 2. Uses raw unsanitized client-supplied filename susceptible to path traversal (../../).
    /// 3. Lacks file size limits (DoS TH-07).
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLabResult(
        [FromForm] int patientId,
        [FromForm] string testName,
        [FromForm] string? resultSummary,
        [FromForm] IFormFile file)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded." });
        }

        var uploaderName = principal.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";

        // FLAW (TH-09): Insecure path combination using raw untrusted file.FileName!
        // An attacker submitting fileName: "../../secret_override.txt" can write outside the uploads directory.
        var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var destinationPath = Path.Combine(uploadsFolder, file.FileName);

        using (var stream = new FileStream(destinationPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var labResult = new LabResult
        {
            PatientId = patientId,
            TestName = testName,
            ResultSummary = resultSummary ?? "Diagnostic file uploaded.",
            OriginalFileName = file.FileName,
            FilePath = Path.GetRelativePath(_env.ContentRootPath, destinationPath),
            UploadedAt = DateTime.UtcNow,
            UploadedBy = uploaderName
        };

        _context.LabResults.Add(labResult);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Lab result file uploaded successfully.",
            labResult,
            securityNotice = "Notice: Unrestricted file upload & raw filename path traversal present (TH-09)."
        });
    }
}
