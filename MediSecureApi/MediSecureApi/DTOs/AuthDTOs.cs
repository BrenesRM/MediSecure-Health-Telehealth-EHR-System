namespace MediSecureApi.DTOs;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string NationalIdOrSSN { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Role { get; set; } // Vulnerable: regular registration allows sending Role = "Doctor" or "Admin"
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class ForgedTokenRequest
{
    public int UserId { get; set; } = 1;
    public string Email { get; set; } = "admin@medisecure.health";
    public string Role { get; set; } = "Admin";
    public bool UseNoneAlgorithm { get; set; } = true;
}
