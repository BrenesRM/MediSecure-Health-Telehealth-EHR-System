namespace MediSecureApi.DTOs;

public class CreateRecordRequest
{
    public int PatientId { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string Prescription { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string Allergies { get; set; } = string.Empty;
}

// TH-03: Parameter Tampering on Medical Records & Prescriptions
public class UpdateRecordRequest
{
    public string? Diagnosis { get; set; }
    public string? Prescription { get; set; } // Tamperable
    public string? Dosage { get; set; }       // Tamperable
    public string? ClinicalNotes { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
}

public class CreateAppointmentRequest
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string ConsultationType { get; set; } = "Telehealth Video";
}

public class UpdateAppointmentStatusRequest
{
    public string Status { get; set; } = "Completed"; // Scheduled, Completed, Cancelled
    public decimal? Fee { get; set; }                 // Tamperable fee
}

public class UploadLabResultRequest
{
    public int PatientId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? ResultSummary { get; set; }
    public IFormFile File { get; set; } = null!;
}

