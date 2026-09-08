using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediSecureApi.Data;
using MediSecureApi.DTOs;
using MediSecureApi.Models;
using MediSecureApi.Services;

namespace MediSecureApi.Controllers;

[ApiController]
[Route("api/v1/appointments")]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AppointmentsController(AppDbContext context, IJwtService jwtService)
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
    /// List all scheduled appointments.
    /// VULNERABILITY (TH-05): Broken Object Level Authorization (BOLA).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAppointments()
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var appointments = await _context.Appointments.ToListAsync();
        return Ok(appointments);
    }

    /// <summary>
    /// Get appointments for a specific patient ID.
    /// VULNERABILITY (TH-05 / IDOR): Any patient can list other patients' consultation schedule.
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientAppointments(int patientId)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patientId)
            .ToListAsync();

        return Ok(appointments);
    }

    /// <summary>
    /// Book a new telehealth consultation.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> BookAppointment([FromBody] CreateAppointmentRequest request)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var patient = await _context.Users.FindAsync(request.PatientId);
        var doctor = await _context.Users.FindAsync(request.DoctorId);

        if (patient == null || doctor == null)
        {
            return BadRequest(new { error = "Invalid patient ID or doctor ID." });
        }

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            PatientName = patient.FullName,
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            Reason = request.Reason,
            AppointmentDate = request.AppointmentDate,
            ConsultationType = request.ConsultationType,
            Status = "Scheduled",
            Fee = 85.00m
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAllAppointments), new { id = appointment.Id }, appointment);
    }

    /// <summary>
    /// Update appointment status and billing fee.
    /// VULNERABILITY (TH-03 / Parameter Tampering): Client can tamper with consultation billing fee.
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusRequest request)
    {
        var principal = GetCurrentPrincipal();
        if (principal == null) return Unauthorized(new { error = "Authentication required." });

        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound(new { error = "Appointment not found." });

        appointment.Status = request.Status;
        if (request.Fee.HasValue)
        {
            appointment.Fee = request.Fee.Value; // Tamperable fee!
        }

        await _context.SaveChangesAsync();
        return Ok(appointment);
    }
}
