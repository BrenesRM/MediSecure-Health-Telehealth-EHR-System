namespace MediSecureApi.DTOs;

// TH-02: Vulnerable to Mass Assignment
// Accepts IsAdmin and Role directly from user-submitted JSON payload!
public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Role { get; set; }      // VULNERABLE: Attacker can inject "Role": "Admin"
    public bool? IsAdmin { get; set; }     // VULNERABLE: Attacker can inject "IsAdmin": true
}

// TH-06: Excessive Data Exposure DTO (or direct model return)
public class UserResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string NationalIdOrSSN { get; set; } = string.Empty; // VULNERABLE: Leaks SSN / PII
    public string PasswordHash { get; set; } = string.Empty;    // VULNERABLE: Leaks password hash!
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
}
