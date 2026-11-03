using System.ComponentModel.DataAnnotations;

namespace RepairSystem.Application.Contracts.Auth;

public sealed class UpdateUserStatusRequestDto
{
    [Required]
    public bool? IsActive { get; set; }
}