using System.ComponentModel.DataAnnotations;

namespace RepairSystem.Application.Contracts.Auth;

public sealed class CreateUserRequestDto
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        "^(Customer|Technician|Admin)$",
        ErrorMessage = "Role must be Customer, Technician, or Admin.")]
    public string Role { get; set; } = string.Empty;
}