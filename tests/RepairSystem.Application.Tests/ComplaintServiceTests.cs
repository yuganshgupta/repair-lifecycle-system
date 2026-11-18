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

        Assert.IsNotNull(
            complaintRepository.SavedAssignmentActivity);

        var activity = complaintRepository.SavedAssignmentActivity;

        Assert.AreEqual(
            BusinessActivityType.TechnicianAssigned,
            activity.EventType);

        Assert.AreEqual(5, activity.ActorUserId);
        Assert.AreEqual("Test Administrator", activity.ActorName);
        Assert.AreEqual("Admin", activity.ActorRole);

        Assert.AreEqual(20, activity.TargetUserId);
        Assert.AreEqual("Test Technician", activity.TargetUserName);

        Assert.AreEqual(1, activity.CustomerId);
        Assert.AreEqual("Demo Customer", activity.CustomerName);

        Assert.AreEqual(101, activity.ComplaintId);
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

        var userRepository = new FakeUserRepository
        {
            UserToReturn = new User
            {
                UserId = 20,
                FullName = "Test Technician",
                Role = UserRole.Technician,
                IsActive = true
            }
        };

        var service = new ComplaintService(
            repository,
            userRepository);

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

        Assert.IsNotNull(repository.SavedStatusActivity);

        var activity = repository.SavedStatusActivity;

        Assert.AreEqual(
            BusinessActivityType.DiagnosisStarted,
            activity.EventType);

        Assert.AreEqual(20, activity.ActorUserId);
        Assert.AreEqual("Test Technician", activity.ActorName);
        Assert.AreEqual("Technician", activity.ActorRole);
        Assert.AreEqual(101, activity.ComplaintId);
        Assert.AreEqual(1, activity.CustomerId);
    }


    [TestMethod]
    [DataRow(
        ComplaintStatus.Approved,
        ComplaintStatus.InRepair,
        BusinessActivityType.RepairStarted)]
    [DataRow(
        ComplaintStatus.InRepair,
        ComplaintStatus.Completed,
        BusinessActivityType.RepairCompleted)]
    public async Task UpdateTechnicianComplaintStatusAsync_RecordsRepairActivity(
        ComplaintStatus initialStatus,
        ComplaintStatus newStatus,
        BusinessActivityType expectedEventType)
    {
        // Arrange: an existing complaint assigned to technician 20.
        var complaint = CreateSubmittedComplaint();
        complaint.AssignedTechnicianId = 20;
        complaint.Status = initialStatus;

        var repository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var userRepository = new FakeUserRepository
        {
            UserToReturn = new User
            {
                UserId = 20,
                FullName = "Test Technician",
                Role = UserRole.Technician,
                IsActive = true
            }
        };

        var service = new ComplaintService(
            repository,
            userRepository);

        // Act: the technician advances the repair.
        var result = await service.UpdateTechnicianComplaintStatusAsync(
            101,
            20,
            newStatus);

        // Assert: status, history and activity are recorded.
        Assert.AreEqual(
            TechnicianStatusUpdateResult.Updated,
            result);

        Assert.AreEqual(newStatus, complaint.Status);

        Assert.AreEqual(
            1,
            repository.SaveStatusChangeCallCount);

        Assert.IsNotNull(repository.SavedStatusHistory);

        Assert.AreEqual(
            initialStatus,
            repository.SavedStatusHistory.PreviousStatus);

        Assert.AreEqual(
            newStatus,
            repository.SavedStatusHistory.NewStatus);

        var activity = repository.SavedStatusActivity;
        Assert.IsNotNull(activity);

        Assert.AreEqual(expectedEventType, activity.EventType);
        Assert.AreEqual(20, activity.ActorUserId);
        Assert.AreEqual("Test Technician", activity.ActorName);
        Assert.AreEqual(101, activity.ComplaintId);
        Assert.AreEqual(1, activity.CustomerId);
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

    [TestMethod]
    public async Task CreateCustomerComplaintAsync_CreatesInitialSubmittedHistory()
    {
        var repository = new FakeComplaintRepository();

        var userRepository = new FakeUserRepository
        {
            UserToReturn = new User
            {
                UserId = 1,
                FullName = "Demo Customer",
                Role = UserRole.Customer,
                IsActive = true
            }
        };

        var service = new ComplaintService(
            repository,
            userRepository);

        var result = await service.CreateCustomerComplaintAsync(
            1,
            new CreateComplaintRequestDto
            {
                DeviceType = "Laptop",
                DeviceBrand = "Dell",
                DeviceModel = "Inspiron 15",
                IssueDescription = "Laptop is not charging."
            });

        Assert.AreEqual("Submitted", result.Status);

        Assert.AreEqual(1, repository.AddWithHistoryCallCount);

        Assert.IsNotNull(repository.AddedInitialHistory);

        Assert.IsNull(repository.AddedInitialHistory.PreviousStatus);

        Assert.AreEqual(
            ComplaintStatus.Submitted,
            repository.AddedInitialHistory.NewStatus);

        Assert.AreEqual(
            1,
            repository.AddedInitialHistory.ChangedByUserId);

        // Verify that a business activity was recorded.
        Assert.HasCount(
            1,
            repository.RecordedBusinessActivities);

        var activity = repository.RecordedBusinessActivities[0];

        Assert.AreEqual(
            BusinessActivityType.ComplaintSubmitted,
            activity.EventType);

        Assert.AreEqual(1, activity.ActorUserId);
        Assert.AreEqual("Demo Customer", activity.ActorName);
        Assert.AreEqual("Customer", activity.ActorRole);

        Assert.AreEqual(1, activity.CustomerId);
        Assert.AreEqual(101, activity.ComplaintId);

        Assert.AreEqual(
            result.TicketNumber,
            activity.TicketNumber);
    }

    [TestMethod]
    public async Task GetCustomerComplaintHistoryAsync_ReturnsHistoryForComplaintOwner()
    {
        var complaint = CreateSubmittedComplaint();

        var changedAt = DateTime.UtcNow;

        var repository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint,
            StatusHistoryToReturn =
            [
                new ComplaintStatusHistory
                {
                    ComplaintStatusHistoryId = 10,
                    ComplaintId = 101,
                    PreviousStatus = null,
                    NewStatus = ComplaintStatus.Submitted,
                    ChangedByUserId = 1,
                    ChangedAt = changedAt,
                    ChangedByUser = new User
                    {
                        UserId = 1,
                        FullName = "Demo Customer",
                        Role = UserRole.Customer
                    }
                }
            ]
        };

        var service = new ComplaintService(
            repository,
            new FakeUserRepository());

        var result = await service.GetCustomerComplaintHistoryAsync(
            customerId: 1,
            complaintId: 101);

        Assert.IsNotNull(result);
        Assert.HasCount(1, result);

        Assert.AreEqual("Submitted", result[0].NewStatus);
        Assert.AreEqual("Demo Customer", result[0].ChangedByName);
        Assert.AreEqual("Customer", result[0].ChangedByRole);
        Assert.AreEqual(changedAt, result[0].ChangedAt);
    }

    [TestMethod]
    public async Task GetCustomerComplaintHistoryAsync_ReturnsNull_WhenComplaintBelongsToAnotherCustomer()
    {
        var complaint = CreateSubmittedComplaint();
        complaint.CustomerId = 2;

        var repository = new FakeComplaintRepository
        {
            ComplaintToReturn = complaint
        };

        var service = new ComplaintService(
            repository,
            new FakeUserRepository());

        var result = await service.GetCustomerComplaintHistoryAsync(
            customerId: 1,
            complaintId: 101);

        Assert.IsNull(result);

        Assert.AreEqual(0, repository.HistoryReadCallCount);
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
        var repository = new FakeUserRepository
        {
            UserToReturn = new User
            {
                UserId = 20,
                FullName = "Test Technician",
                Role = UserRole.Technician,
                IsActive = true
            }
        };

        repository.AdditionalUsers[5] = new User
        {
            UserId = 5,
            FullName = "Test Administrator",
            Role = UserRole.Admin,
            IsActive = true
        };

        repository.AdditionalUsers[1] = new User
        {
            UserId = 1,
            FullName = "Demo Customer",
            Role = UserRole.Customer,
            IsActive = true
        };

        return repository;
    }

    private sealed class FakeComplaintRepository : IComplaintRepository
    {
        public Complaint? ComplaintToReturn { get; set; }

        public Complaint? SavedComplaint { get; private set; }

        public ComplaintStatusHistory? SavedHistory { get; private set; }

        public int SaveAssignmentCallCount { get; private set; }

        public Complaint? SavedStatusComplaint { get; private set; }

        public ComplaintStatusHistory? SavedStatusHistory { get; private set; }

        public BusinessActivityEvent? SavedStatusActivity { get; private set; }

        public BusinessActivityEvent? SavedAssignmentActivity { get; private set; }

        public int SaveStatusChangeCallCount { get; private set; }

        public Complaint? AddedComplaint { get; private set; }

        public ComplaintStatusHistory? AddedInitialHistory { get; private set; }

        public int AddWithHistoryCallCount { get; private set; }

        public IReadOnlyList<ComplaintStatusHistory> StatusHistoryToReturn { get; set; } = [];

        public List<BusinessActivityEvent> RecordedBusinessActivities { get; } = new();

        public int HistoryReadCallCount { get; private set; }

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

        public Task AddWithHistoryAsync(
            Complaint complaint,
            ComplaintStatusHistory history)
        {
            AddWithHistoryCallCount++;

            // Simulate the identity ID generated by SQL Server.
            if (complaint.ComplaintId == 0)
            {
                complaint.ComplaintId = 101;
            }

            // Simulate the saved history's foreign key.
            history.ComplaintId = complaint.ComplaintId;

            AddedComplaint = complaint;
            AddedInitialHistory = history;

            return Task.CompletedTask;
        }

        public async Task AddWithHistoryAndActivityAsync(
            Complaint complaint,
            ComplaintStatusHistory history,
            Func<Complaint, BusinessActivityEvent> createActivity)
        {
            ArgumentNullException.ThrowIfNull(createActivity);

            // Reuse the existing fake complaint/history persistence.
            await AddWithHistoryAsync(complaint, history);

            // Create the event after the fake has saved the complaint.
            var activity = createActivity(complaint);

            // Store the event so tests can inspect it.
            RecordedBusinessActivities.Add(activity);
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

        public Task<IReadOnlyList<ComplaintStatusHistory>>
            GetStatusHistoryByComplaintIdAsync(int complaintId)
        {
            HistoryReadCallCount++;

            return Task.FromResult(StatusHistoryToReturn);
        }

        public Task SaveAssignmentAsync(
            Complaint complaint,
            ComplaintStatusHistory history,
            BusinessActivityEvent activity)
        {
            SaveAssignmentCallCount++;

            SavedComplaint = complaint;
            SavedHistory = history;
            SavedAssignmentActivity = activity;

            return Task.CompletedTask;
        }

        public Task SaveStatusChangeAsync(
            Complaint complaint,
            ComplaintStatusHistory history,
            BusinessActivityEvent activity)
        {
            SaveStatusChangeCallCount++;
            SavedStatusComplaint = complaint;
            SavedStatusHistory = history;
            SavedStatusActivity = activity;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? UserToReturn { get; set; }

        public Dictionary<int, User> AdditionalUsers { get; } = new();

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
            if (AdditionalUsers.TryGetValue(userId, out var user))
            {
                return Task.FromResult<User?>(user);
            }

            var result = UserToReturn?.UserId == userId
                ? UserToReturn
                : null;

            return Task.FromResult(result);
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