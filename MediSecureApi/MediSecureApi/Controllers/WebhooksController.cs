using Microsoft.AspNetCore.Mvc;
using MediSecureApi.Data;
using MediSecureApi.Models;

namespace MediSecureApi.Controllers;

public class LabSyncWebhookPayload
{
    public int PatientId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public string PartnerId { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/webhooks")]
[Produces("application/json")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _context;

    public WebhooksController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ingest lab test results from third-party clinical diagnostic providers.
    /// VULNERABILITY (TH-01 / Spoofing & TH-02 / Tampering):
    /// Endpoint does NOT require authentication, API keys, or HMAC signature validation (e.g. X-Webhook-Signature)!
    /// Any external attacker can forge synthetic lab reports and inject malicious records into the clinical system.
    /// </summary>
    [HttpPost("lab-sync")]
    public async Task<IActionResult> ReceiveLabSync([FromBody] LabSyncWebhookPayload payload)
    {
        // FLAW (TH-01): Missing webhook signature verification (HMAC-SHA256)!
        var labResult = new LabResult
        {
            PatientId = payload.PatientId,
            TestName = payload.TestName,
            ResultSummary = payload.ResultSummary,
            OriginalFileName = "automated_webhook_feed.json",
            FilePath = "uploads/automated_webhook_feed.json",
            UploadedAt = DateTime.UtcNow,
            UploadedBy = string.IsNullOrWhiteSpace(payload.PartnerId) ? "Unauthenticated_External_Webhook" : payload.PartnerId
        };

        _context.LabResults.Add(labResult);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            status = "Success",
            message = "Lab sync payload ingested into clinical database.",
            labResultId = labResult.Id,
            securityAlert = "CRITICAL: Unauthenticated webhook ingested without signature validation (TH-01 Spoofing / TH-02 Tampering)."
        });
    }
}
