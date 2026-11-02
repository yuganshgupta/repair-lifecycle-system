using System.ComponentModel.DataAnnotations;

namespace RepairSystem.Web.Features.Dashboard.Contracts;

public sealed class CreateComplaintApiRequest
{
    [Required]
    [StringLength(50)]
    public string DeviceType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string DeviceBrand { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DeviceModel { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SerialNumber { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public string IssueDescription { get; set; } = string.Empty;

    public bool ClaimedWarranty { get; set; }
}