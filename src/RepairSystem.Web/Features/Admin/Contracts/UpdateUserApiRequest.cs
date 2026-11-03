using System.ComponentModel.DataAnnotations;

namespace RepairSystem.Web.Features.Admin.Contracts;

public sealed class UpdateUserApiRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        "^(Customer|Technician|Admin)$",
        ErrorMessage = "Choose Customer, Technician, or Admin.")]
    public string Role { get; set; } = "Customer";
}