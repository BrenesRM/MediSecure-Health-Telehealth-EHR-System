namespace MediSecureApi.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // Insecurely exposed in excessive data exposure
    public string Role { get; set; } = "Patient"; // "Patient", "Doctor", "Admin"
    public string NationalIdOrSSN { get; set; } = string.Empty; // Sensitive PII
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false; // Vulnerable to Mass Assignment
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Appointment> Appointments { get; set; } = new();
}
