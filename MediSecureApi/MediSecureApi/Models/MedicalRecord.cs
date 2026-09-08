namespace MediSecureApi.Models;

public class MedicalRecord
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Prescription { get; set; } = string.Empty; // Insecurely mutable (TH-03)
    public string Dosage { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string Allergies { get; set; } = string.Empty;
    public DateTime RecordDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Patient { get; set; }
}
