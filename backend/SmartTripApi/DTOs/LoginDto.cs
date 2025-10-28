using System.ComponentModel.DataAnnotations;

namespace SmartTripApi.DTOs;

public class LoginDto
{
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
