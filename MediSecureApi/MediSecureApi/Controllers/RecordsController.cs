using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediSecureApi.Data;
using MediSecureApi.DTOs;
using MediSecureApi.Models;
using MediSecureApi.Services;

namespace MediSecureApi.Controllers;

[ApiController]
[Route("api/v1/records")]
[Produces("application/json")]
public class RecordsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public RecordsController(AppDbContext context, IJwtService jwtService)
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
    /// Get all medical records in the system.
    /// VULNERABILITY (TH-05 / OWASP API1): BOLA / Missing Object Access Control.
    /// Allows any logged-in user to fetch medical records belonging to all patients.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllRecords()
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var records = await _context.MedicalRecords.ToListAsync();
        return Ok(records);
    }

    /// <summary>
    /// Get all medical records for a specific patient ID.
    /// VULNERABILITY (TH-05 / CWE-639 / OWASP API1): Broken Object Level Authorization (IDOR/BOLA).
    /// Does NOT verify that the calling user ID matches 'patientId' or is the assigned doctor!
    /// Any patient can view another patient's complete psychiatric, diagnosis, and prescription history.
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientRecords(int patientId)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        // FLAW (TH-05): Missing ownership check!
        // Intentionally omits: if (currentUser.Id != patientId && currentUser.Role != "Doctor") return Forbid();
        var records = await _context.MedicalRecords
            .Where(r => r.PatientId == patientId)
            .ToListAsync();

        return Ok(records);
    }

    /// <summary>
    /// Get a single medical record by its Record ID.
    /// VULNERABILITY (TH-05): IDOR on single record retrieval.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecordById(int id)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var record = await _context.MedicalRecords.FindAsync(id);
        if (record == null) return NotFound(new { error = "Medical record not found." });

        return Ok(record);
    }

    /// <summary>
    /// Create a new medical record for a patient.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRecord([FromBody] CreateRecordRequest request)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var currentUserIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdClaim, out var doctorId);
        var doctorName = principal.FindFirst(ClaimTypes.Name)?.Value ?? "Dr. Assigned";

        var patient = await _context.Users.FindAsync(request.PatientId);
        if (patient == null) return NotFound(new { error = "Patient not found." });

        var record = new MedicalRecord
        {
            PatientId = patient.Id,
            PatientName = patient.FullName,
            DoctorId = doctorId,
            DoctorName = doctorName,
            Diagnosis = request.Diagnosis,
            Prescription = request.Prescription,
            Dosage = request.Dosage,
            ClinicalNotes = request.ClinicalNotes,
            BloodType = request.BloodType,
            Allergies = request.Allergies,
            RecordDate = DateTime.UtcNow
        };

        _context.MedicalRecords.Add(record);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRecordById), new { id = record.Id }, record);
    }

    /// <summary>
    /// Modify an existing medical record.
    /// VULNERABILITY (TH-03 / CWE-472 / OWASP API3): Prescription & Diagnosis Tampering.
    /// Allows any user to modify clinical prescriptions, dosages, and notes with no integrity signature or audit check.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecord(int id, [FromBody] UpdateRecordRequest request)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var record = await _context.MedicalRecords.FindAsync(id);
        if (record == null) return NotFound(new { error = "Medical record not found." });

        // FLAW (TH-03): Direct tampering allowed without verifying medical role or cryptographic signing
        if (!string.IsNullOrWhiteSpace(request.Diagnosis)) record.Diagnosis = request.Diagnosis;
        if (!string.IsNullOrWhiteSpace(request.Prescription)) record.Prescription = request.Prescription;
        if (!string.IsNullOrWhiteSpace(request.Dosage)) record.Dosage = request.Dosage;
        if (!string.IsNullOrWhiteSpace(request.ClinicalNotes)) record.ClinicalNotes = request.ClinicalNotes;
        if (!string.IsNullOrWhiteSpace(request.BloodType)) record.BloodType = request.BloodType;
        if (!string.IsNullOrWhiteSpace(request.Allergies)) record.Allergies = request.Allergies;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Medical record updated successfully.",
            record,
            securityAlert = "Prescription/dosage modified without authorization or clinical verification (TH-03)."
        });
    }

    /// <summary>
    /// Delete a medical record.
    /// VULNERABILITY (TH-04 / CWE-778 / OWASP A09): Repudiation & Missing Audit Logging.
    /// Destructive record deletion occurs with no tamper-proof audit log or trail.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecord(int id)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var record = await _context.MedicalRecords.FindAsync(id);
        if (record == null) return NotFound(new { error = "Medical record not found." });

        // FLAW (TH-04): Record deleted with zero persistent audit trail or non-repudiation record!
        _context.MedicalRecords.Remove(record);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Medical record #{id} permanently deleted.",
            auditStatus = "VULNERABILITY: Action executed with NO security audit log generated (Repudiation Risk TH-04)."
        });
    }

    /// <summary>
    /// Search medical records by clinical keyword or regex.
    /// VULNERABILITY (TH-07 / CWE-400 / OWASP API4): Unrestricted Resource Consumption / Regex DoS.
    /// Unpaginated full-table scan evaluating uncompiled complex regular expressions on every record in memory.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchRecords([FromQuery] string query)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Search query parameter is required." });
        }

        // FLAW (TH-07): Evaluates regex in memory against all records without pagination, size limits, or timeout
        var allRecords = await _context.MedicalRecords.ToListAsync();
        var matching = allRecords.Where(r => 
            Regex.IsMatch(r.Diagnosis + " " + r.ClinicalNotes + " " + r.Prescription, query, RegexOptions.IgnoreCase)
        ).ToList();

        return Ok(new
        {
            totalMatches = matching.Count,
            queryEvaluated = query,
            results = matching
        });
    }
}
