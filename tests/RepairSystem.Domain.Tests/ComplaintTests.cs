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

        Assert.AreEqual(ComplaintStatus.Assigned, complaint.Status);
    }

    [TestMethod]
    public void ChangeStatus_FromSubmittedToCompleted_ThrowsInvalidOperationException()
    {
        var complaint = new Complaint();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => complaint.ChangeStatus(ComplaintStatus.Completed));
    }

    [TestMethod]
    public void ChangeStatus_FromQuotePendingToCancelled()
    {
        var complaint = new Complaint();
        complaint.ChangeStatus(ComplaintStatus.Assigned);
        complaint.ChangeStatus(ComplaintStatus.InDiagnosis);
        complaint.ChangeStatus(ComplaintStatus.QuotePending);
        complaint.ChangeStatus(ComplaintStatus.Cancelled);
        

        Assert.AreEqual(ComplaintStatus.Cancelled, complaint.Status);
    }

    [TestMethod]
    public void ChangeStatus_FromApprovedToCancelled_ThrowsInvalidOperationException()
    {
        var complaint = new Complaint();
        complaint.ChangeStatus(ComplaintStatus.Assigned);
        complaint.ChangeStatus(ComplaintStatus.InDiagnosis);
        complaint.ChangeStatus(ComplaintStatus.QuotePending);
        complaint.ChangeStatus(ComplaintStatus.Approved);

        Assert.ThrowsExactly<InvalidOperationException>(() => complaint.ChangeStatus(ComplaintStatus.Cancelled));
    }

}