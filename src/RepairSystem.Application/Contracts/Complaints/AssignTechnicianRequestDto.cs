
using System.ComponentModel.DataAnnotations;

namespace RepairSystem.Application.Contracts.Complaints;

public sealed class AssignTechnicianRequestDto
{
    [Range(1, int.MaxValue)]
    public int TechnicianId { get; set; }
}
