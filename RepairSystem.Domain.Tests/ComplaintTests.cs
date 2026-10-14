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
}