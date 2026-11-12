using RepairSystem.Application.Contracts.Complaints;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Application.Services.Complaints;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Tests;

[TestClass]
public sealed class ComplaintServiceTests
{
    [TestMethod]
    public async Task AssignTechnicianAsync_ReturnsComplaintNotFound_WhenComplaintDoesNotExist()
    {
        var complaintRepository = new FakeComplaintRepository();
        var userRepository = new FakeUserRepository();

        var service = new ComplaintService(
            complaintRepository,
            userRepository);

        var result = await service.AssignTechnicianAsync(101, 20, 5);

        Assert.AreEqual(
            ComplaintAssignmentResult.ComplaintNotFound,
            result);

        Assert.AreEqual(0, complaintRepository.SaveAssignmentCallCount);
    }

    [TestMethod]
    public async Task AssignTechnicianAsync_ReturnsTechnicianNotFound_WhenTechnicianDoesNotExist()
    {
        var complaintRepository = new FakeComplaintRepository
        {
            ComplaintToReturn = CreateSubmittedComplaint()
        };

        var userRepository = new FakeUserRepository();

        var service = new ComplaintService(
            complaintRepository,
            userRepository);

        var result = await service.AssignTechnicianAsync(101, 20, 5);

        Assert.AreEqual(
            ComplaintAssignmentResult.TechnicianNotFound,
            result);

        Assert.AreEqual(0, complaintRepository.SaveAssignmentCallCount);
    }

    [TestMethod]
    public async Task AssignTechnicianAsync_ReturnsUserIsNotTechnician_WhenUserHasDifferentRole()
    {
        var complaintRepository = new FakeComplaintRepository
        {
            ComplaintToReturn = CreateSubmittedComplaint()
        };

        var userRepository = new FakeUserRepository
        {
            UserToReturn = new User
            {
                UserId = 20,
                Role = UserRole.Customer,
                IsActive = true
            }
        };

        var service = new ComplaintService(
            complaintRepository,
            userRepository);

        var result = await service.AssignTechnicianAsync(101, 20, 5);

        Assert.AreEqual(
            ComplaintAssignmentResult.UserIsNotTechnician,
            result);

        Assert.AreEqual(0, complaintRepository.SaveAssignmentCallCount);
    }

    [TestMethod]
    public async Task AssignTechnicianAsync_ReturnsTechnicianInactive_WhenAccountIsInactive()
    {
        var complaintRepository = new FakeComplaintRepository
        {
            ComplaintToReturn = CreateSubmittedComplaint()
        };

        var userRepository = new FakeUserRepository
        {
            UserToReturn = new User
            {
                UserId = 20,
                Role = UserRole.Technician,
                IsActive = false
            }
        };

        var service = new ComplaintService(
            complaintRepository,
            userRepository);

        var result = await service.AssignTechnicianAsync(101, 20, 5);

        Assert.AreEqual(
            ComplaintAssignmentResult.TechnicianInactive,
            result);

        Assert.AreEqual(0, complaintRepository.SaveAssignmentCallCount);
    }

    [TestMethod]
    public async Task AssignTechnicianAsync_ReturnsAlreadyAssigned_WhenComplaintHasTechnician()
    {
        var complaint = CreateSubmittedComplaint();
        complaint.AssignedTechnicianId = 30;

        var complaintRepository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var userRepository = CreateActiveTechnicianRepository();

        var service = new ComplaintService(
            complaintRepository,
            userRepository);

        var result = await service.AssignTechnicianAsync(101, 20, 5);

        Assert.AreEqual(
            ComplaintAssignmentResult.AlreadyAssigned,
            result);

        Assert.AreEqual(0, complaintRepository.SaveAssignmentCallCount);
    }

    [TestMethod]
    public async Task AssignTechnicianAsync_ReturnsInvalidComplaintStatus_WhenComplaintIsNotSubmitted()
    {
        var complaint = CreateSubmittedComplaint();
        complaint.ChangeStatus(ComplaintStatus.Assigned);
        complaint.AssignedTechnicianId = null;

        var complaintRepository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var userRepository = CreateActiveTechnicianRepository();

        var service = new ComplaintService(
            complaintRepository,
            userRepository);

        var result = await service.AssignTechnicianAsync(101, 20, 5);

        Assert.AreEqual(
            ComplaintAssignmentResult.InvalidComplaintStatus,
            result);

        Assert.AreEqual(0, complaintRepository.SaveAssignmentCallCount);
    }

    [TestMethod]
    public async Task AssignTechnicianAsync_AssignsComplaintAndCreatesHistory_WhenRequestIsValid()
    {
        var previousUpdatedAt = DateTime.UtcNow.AddMinutes(-5);
        var complaint = CreateSubmittedComplaint();
        complaint.UpdatedAt = previousUpdatedAt;

        var complaintRepository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var userRepository = CreateActiveTechnicianRepository();

        var service = new ComplaintService(
            complaintRepository,
            userRepository);

        var result = await service.AssignTechnicianAsync(
            complaintId: 101,
            technicianId: 20,
            assignedByAdminId: 5);

        Assert.AreEqual(
            ComplaintAssignmentResult.Assigned,
            result);

        Assert.AreEqual(20, complaint.AssignedTechnicianId);
        Assert.AreEqual(ComplaintStatus.Assigned, complaint.Status);
        Assert.IsTrue(complaint.UpdatedAt > previousUpdatedAt);

        Assert.AreEqual(1, complaintRepository.SaveAssignmentCallCount);

        var history = complaintRepository.SavedHistory;

        Assert.IsNotNull(history);
        Assert.AreEqual(101, history.ComplaintId);
        Assert.AreEqual(ComplaintStatus.Submitted, history.PreviousStatus);
        Assert.AreEqual(ComplaintStatus.Assigned, history.NewStatus);
        Assert.AreEqual(5, history.ChangedByUserId);
        Assert.IsTrue(history.ChangedAt > previousUpdatedAt);
    }

    [TestMethod]
    public async Task UpdateTechnicianComplaintStatusAsync_UpdatesStatusAndCreatesHistory()
    {
        var complaint = CreateSubmittedComplaint();
        complaint.AssignToTechnician(20);

        var repository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var service = new ComplaintService(
            repository,
            new FakeUserRepository());

        var result = await service.UpdateTechnicianComplaintStatusAsync(
            101,
            20,
            ComplaintStatus.InDiagnosis);

        Assert.AreEqual(TechnicianStatusUpdateResult.Updated, result);
        Assert.AreEqual(ComplaintStatus.InDiagnosis, complaint.Status);

        Assert.AreEqual(1, repository.SaveStatusChangeCallCount);

        Assert.IsNotNull(repository.SavedStatusHistory);
        Assert.AreEqual(
            ComplaintStatus.Assigned,
            repository.SavedStatusHistory.PreviousStatus);
        Assert.AreEqual(
            ComplaintStatus.InDiagnosis,
            repository.SavedStatusHistory.NewStatus);
        Assert.AreEqual(
            20,
            repository.SavedStatusHistory.ChangedByUserId);
        Assert.AreEqual(
            complaint.UpdatedAt,
            repository.SavedStatusHistory.ChangedAt);
    }

    [TestMethod]
    public async Task UpdateTechnicianComplaintStatusAsync_ReturnsNotFound_WhenComplaintDoesNotExist()
    {
        var repository = new FakeComplaintRepository();

        var service = new ComplaintService(
            repository,
            new FakeUserRepository());

        var result = await service.UpdateTechnicianComplaintStatusAsync(
            101, 20, ComplaintStatus.InDiagnosis);

        Assert.AreEqual(
            TechnicianStatusUpdateResult.ComplaintNotFound,
            result);

        Assert.AreEqual(0, repository.SaveStatusChangeCallCount);
    }

    [TestMethod]
    public async Task UpdateTechnicianComplaintStatusAsync_RejectsComplaintOwnedByAnotherTechnician()
    {
        var complaint = CreateSubmittedComplaint();
        complaint.AssignToTechnician(30);

        var repository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var service = new ComplaintService(
            repository,
            new FakeUserRepository());

        var result = await service.UpdateTechnicianComplaintStatusAsync(
            101, 20, ComplaintStatus.InDiagnosis);

        Assert.AreEqual(
            TechnicianStatusUpdateResult.ComplaintNotAssignedToTechnician,
            result);

        Assert.AreEqual(ComplaintStatus.Assigned, complaint.Status);
        Assert.AreEqual(0, repository.SaveStatusChangeCallCount);
    }

    [TestMethod]
    public async Task UpdateTechnicianComplaintStatusAsync_DoesNotAllowTechnicianToSetQuotePendingWithoutQuote()
    {
        var complaint = CreateSubmittedComplaint();
        complaint.AssignToTechnician(20);
        complaint.ChangeStatus(ComplaintStatus.InDiagnosis);

        var repository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var service = new ComplaintService(
            repository,
            new FakeUserRepository());

        var result = await service.UpdateTechnicianComplaintStatusAsync(
            101, 20, ComplaintStatus.QuotePending);

        Assert.AreEqual(
            TechnicianStatusUpdateResult.InvalidTransition,
            result);

        Assert.AreEqual(ComplaintStatus.InDiagnosis, complaint.Status);
        Assert.AreEqual(0, repository.SaveStatusChangeCallCount);
    }

    [TestMethod]
    public async Task UpdateTechnicianComplaintStatusAsync_RejectsUndefinedStatus()
    {
        var complaint = CreateSubmittedComplaint();
        complaint.AssignToTechnician(20);

        var repository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var service = new ComplaintService(
            repository,
            new FakeUserRepository());

        var result = await service.UpdateTechnicianComplaintStatusAsync(
            101, 20, (ComplaintStatus)999);

        Assert.AreEqual(
            TechnicianStatusUpdateResult.InvalidStatus,
            result);

        Assert.AreEqual(0, repository.SaveStatusChangeCallCount);
    }

    private static Complaint CreateSubmittedComplaint()
    {
        return new Complaint
        {
            ComplaintId = 101,
            CustomerId = 1,
            Status = ComplaintStatus.Submitted,
            AssignedTechnicianId = null
        };
    }

    private static FakeUserRepository CreateActiveTechnicianRepository()
    {
        return new FakeUserRepository
        {
            UserToReturn = new User
            {
                UserId = 20,
                FullName = "Test Technician",
                Role = UserRole.Technician,
                IsActive = true
            }
        };
    }

    private sealed class FakeComplaintRepository : IComplaintRepository
    {
        public Complaint? ComplaintToReturn { get; set; }

        public Complaint? SavedComplaint { get; private set; }

        public ComplaintStatusHistory? SavedHistory { get; private set; }

        public int SaveAssignmentCallCount { get; private set; }

        public Complaint? SavedStatusComplaint { get; private set; }

        public ComplaintStatusHistory? SavedStatusHistory { get; private set; }

        public int SaveStatusChangeCallCount { get; private set; }

        public Task<IReadOnlyList<Complaint>> GetByCustomerIdAsync(
            int customerId)
        {
            return Task.FromResult<IReadOnlyList<Complaint>>(
                Array.Empty<Complaint>());
        }

        public Task AddAsync(Complaint complaint)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Complaint>> GetAssignedToTechnicianAsync(
            int technicianId)
        {
            return Task.FromResult<IReadOnlyList<Complaint>>(
                Array.Empty<Complaint>());
        }

        public Task<IReadOnlyList<Complaint>> GetUnassignedAsync()
        {
            return Task.FromResult<IReadOnlyList<Complaint>>(
                Array.Empty<Complaint>());
        }

        public Task<Complaint?> GetByIdAsync(int complaintId)
        {
            var complaint = ComplaintToReturn?.ComplaintId == complaintId
                ? ComplaintToReturn
                : null;

            return Task.FromResult(complaint);
        }

        public Task SaveAssignmentAsync(
            Complaint complaint,
            ComplaintStatusHistory history)
        {
            SaveAssignmentCallCount++;
            SavedComplaint = complaint;
            SavedHistory = history;

            return Task.CompletedTask;
        }

        public Task SaveStatusChangeAsync(
            Complaint complaint,
            ComplaintStatusHistory history)
        {
            SaveStatusChangeCallCount++;
            SavedStatusComplaint = complaint;
            SavedStatusHistory = history;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? UserToReturn { get; set; }

        public Task<bool> EmailExistsAsync(string email)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(User user)
        {
            return Task.CompletedTask;
        }

        public Task<User?> FindByEmailAsync(string email)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<IReadOnlyList<User>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyList<User>>(
                Array.Empty<User>());
        }

        public Task<User?> GetByIdAsync(int userId)
        {
            var user = UserToReturn?.UserId == userId
                ? UserToReturn
                : null;

            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user)
        {
            return Task.CompletedTask;
        }

        public Task<int> CountActiveAdminsAsync()
        {
            return Task.FromResult(0);
        }
    }
}