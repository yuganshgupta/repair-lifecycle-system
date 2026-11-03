using System.ComponentModel.DataAnnotations;

namespace RepairSystem.Web.Features.Admin.Contracts;

public sealed class UserFormModel : IValidatableObject
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

    public string? Password { get; set; }

    public bool IsCreate { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (!IsCreate)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult(
                "A temporary password is required.",
                [nameof(Password)]);

            yield break;
        }

        if (Password.Length < 8)
        {
            yield return new ValidationResult(
                "Password must contain at least 8 characters.",
                [nameof(Password)]);
        }
    }
}