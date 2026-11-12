
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Tests;

[TestClass]
public class ComplaintTests
{
    [TestMethod]
    public void NewComplaint_StartsWithSubmittedStatus()
    {
        var complaint = new Complaint();

        Assert.AreEqual(
            ComplaintStatus.Submitted,
            complaint.Status);
    }

    [TestMethod]
    public void ChangeStatus_FromSubmittedToAssigned_UpdatesStatus()
    {
        var complaint = new Complaint();

        complaint.ChangeStatus(ComplaintStatus.Assigned);

        Assert.AreEqual(
            ComplaintStatus.Assigned,
            complaint.Status);
    }

    [TestMethod]
    public void ChangeStatus_FollowsValidRepairLifecycle()
    {
        var complaint = new Complaint();

        complaint.ChangeStatus(ComplaintStatus.Assigned);
        complaint.ChangeStatus(ComplaintStatus.InDiagnosis);
        complaint.ChangeStatus(ComplaintStatus.QuotePending);
        complaint.ChangeStatus(ComplaintStatus.Approved);
        complaint.ChangeStatus(ComplaintStatus.InRepair);
        complaint.ChangeStatus(ComplaintStatus.Completed);

        Assert.AreEqual(
            ComplaintStatus.Completed,
            complaint.Status);
    }

    [TestMethod]
    public void ChangeStatus_FromQuotePendingToCancelled_IsAllowed()
    {
        var complaint = CreateComplaintAtStatus(
            ComplaintStatus.QuotePending);

        complaint.ChangeStatus(ComplaintStatus.Cancelled);

        Assert.AreEqual(
            ComplaintStatus.Cancelled,
            complaint.Status);
    }

    [TestMethod]
    public void ChangeStatus_FromSubmittedToCompleted_ThrowsInvalidOperationException()
    {
        var complaint = new Complaint();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => complaint.ChangeStatus(ComplaintStatus.Completed));
    }

    [TestMethod]
    public void ChangeStatus_FromApprovedToCancelled_ThrowsInvalidOperationException()
    {
        var complaint = CreateComplaintAtStatus(
            ComplaintStatus.Approved);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => complaint.ChangeStatus(ComplaintStatus.Cancelled));

        Assert.AreEqual(
            ComplaintStatus.Approved,
            complaint.Status);
    }

    [TestMethod]
    public void ChangeStatus_CannotSkipDiagnosis()
    {
        var complaint = new Complaint();

        complaint.ChangeStatus(ComplaintStatus.Assigned);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => complaint.ChangeStatus(ComplaintStatus.QuotePending));

        Assert.AreEqual(
            ComplaintStatus.Assigned,
            complaint.Status);
    }

    [TestMethod]
    public void ChangeStatus_CompletedComplaintCannotBeChanged()
    {
        var complaint = CreateComplaintAtStatus(
            ComplaintStatus.Completed);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => complaint.ChangeStatus(ComplaintStatus.InRepair));
    }

    [TestMethod]
    public void ChangeStatus_CancelledComplaintCannotBeChanged()
    {
        var complaint = CreateComplaintAtStatus(
            ComplaintStatus.Cancelled);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => complaint.ChangeStatus(ComplaintStatus.Assigned));
    }

    [TestMethod]
    public void ChangeStatus_ToSameStatus_ThrowsInvalidOperationException()
    {
        var complaint = new Complaint();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => complaint.ChangeStatus(ComplaintStatus.Submitted));
    }

    [TestMethod]
    public void ChangeStatus_WithUndefinedStatus_ThrowsArgumentOutOfRangeException()
    {
        var complaint = new Complaint();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => complaint.ChangeStatus((ComplaintStatus)999));
    }

    [TestMethod]
    public void ChangeStatus_UpdatesUpdatedAt()
    {
        var complaint = new Complaint();

        var beforeChange = DateTime.UtcNow;

        complaint.ChangeStatus(ComplaintStatus.Assigned);

        var afterChange = DateTime.UtcNow;

        Assert.IsTrue(
            complaint.UpdatedAt >= beforeChange &&
            complaint.UpdatedAt <= afterChange);
    }

    [TestMethod]
    public void AssignToTechnician_WhenComplaintIsSubmitted_SetsTechnicianAndStatus()
    {
        var complaint = new Complaint();

        complaint.AssignToTechnician(10);

        Assert.AreEqual(10, complaint.AssignedTechnicianId);

        Assert.AreEqual(
            ComplaintStatus.Assigned,
            complaint.Status);
    }

    [TestMethod]
    public void AssignToTechnician_Throws_WhenComplaintIsAlreadyAssigned()
    {
        var complaint = new Complaint();

        complaint.AssignToTechnician(10);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => complaint.AssignToTechnician(20));
    }

    [TestMethod]
    public void AssignToTechnician_Throws_WhenTechnicianIdIsInvalid()
    {
        var complaint = new Complaint();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => complaint.AssignToTechnician(0));
    }

    private static Complaint CreateComplaintAtStatus(
    ComplaintStatus targetStatus)
    {
        var complaint = new Complaint();

        var transitions = targetStatus switch
        {
            ComplaintStatus.Submitted =>
                Array.Empty<ComplaintStatus>(),

            ComplaintStatus.Assigned =>
                new[] { ComplaintStatus.Assigned },

            ComplaintStatus.InDiagnosis =>
                new[]
                {
                ComplaintStatus.Assigned,
                ComplaintStatus.InDiagnosis
                },

            ComplaintStatus.QuotePending or ComplaintStatus.Cancelled =>
                new[]
                {
                ComplaintStatus.Assigned,
                ComplaintStatus.InDiagnosis,
                ComplaintStatus.QuotePending
                },

            ComplaintStatus.Approved =>
                new[]
                {
                ComplaintStatus.Assigned,
                ComplaintStatus.InDiagnosis,
                ComplaintStatus.QuotePending,
                ComplaintStatus.Approved
                },

            ComplaintStatus.InRepair =>
                new[]
                {
                ComplaintStatus.Assigned,
                ComplaintStatus.InDiagnosis,
                ComplaintStatus.QuotePending,
                ComplaintStatus.Approved,
                ComplaintStatus.InRepair
                },

            ComplaintStatus.Completed =>
                new[]
                {
                ComplaintStatus.Assigned,
                ComplaintStatus.InDiagnosis,
                ComplaintStatus.QuotePending,
                ComplaintStatus.Approved,
                ComplaintStatus.InRepair,
                ComplaintStatus.Completed
                },

            _ => throw new ArgumentOutOfRangeException(
                nameof(targetStatus))
        };

        foreach (var status in transitions)
        {
            complaint.ChangeStatus(status);
        }

        if (targetStatus == ComplaintStatus.Cancelled)
        {
            complaint.ChangeStatus(ComplaintStatus.Cancelled);
        }

        return complaint;
    }
}
