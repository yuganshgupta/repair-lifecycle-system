using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Contracts.Auth;

public class LoginResultDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}