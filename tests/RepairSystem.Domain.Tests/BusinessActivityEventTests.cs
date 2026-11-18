using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Tests;

[TestClass]
public sealed class BusinessActivityEventTests
{
    [TestMethod]
    public void Create_StoresBusinessActivityInformation()
    {
        var beforeCreation = DateTime.UtcNow;

        var activity = CreateValidActivity();

        var afterCreation = DateTime.UtcNow;

        Assert.AreEqual(
            BusinessActivityType.TechnicianAssigned,
            activity.EventType);

        Assert.AreEqual(5, activity.ActorUserId);
        Assert.AreEqual("Admin User", activity.ActorName);
        Assert.AreEqual("Admin", activity.ActorRole);

        Assert.AreEqual(20, activity.TargetUserId);
        Assert.AreEqual("Technician User", activity.TargetUserName);

        Assert.AreEqual(10, activity.CustomerId);
        Assert.AreEqual("Customer User", activity.CustomerName);

        Assert.AreEqual(101, activity.ComplaintId);
        Assert.AreEqual("TKT-20261117-ABC12345", activity.TicketNumber);

        Assert.AreEqual(
            "Technician assigned",
            activity.Title);

        Assert.AreEqual(
            DateTimeKind.Utc,
            activity.OccurredAtUtc.Kind);

        Assert.IsTrue(
            activity.OccurredAtUtc >= beforeCreation &&
            activity.OccurredAtUtc <= afterCreation);
    }

    [TestMethod]
    public void Create_TrimsTextFields()
    {
        var activity = BusinessActivityEvent.Create(
            BusinessActivityType.CustomerRegistered,
            "  Customer Name  ",
            "  Customer  ",
            "  Customer registered  ",
            "  A new customer account was created.  ");

        Assert.AreEqual("Customer Name", activity.ActorName);
        Assert.AreEqual("Customer", activity.ActorRole);
        Assert.AreEqual("Customer registered", activity.Title);

        Assert.AreEqual(
            "A new customer account was created.",
            activity.Description);
    }

    [TestMethod]
    public void Create_RejectsUndefinedEventType()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => CreateValidActivity(
                eventType: (BusinessActivityType)999));
    }

    [TestMethod]
    public void Create_RejectsInvalidActorId()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => BusinessActivityEvent.Create(
                BusinessActivityType.CustomerRegistered,
                "Customer",
                "Customer",
                "Customer registered",
                "A new account was created.",
                actorUserId: 0));
    }

    [TestMethod]
    public void Create_RequiresActorName()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => BusinessActivityEvent.Create(
                BusinessActivityType.CustomerRegistered,
                " ",
                "Customer",
                "Customer registered",
                "A new account was created."));
    }

    [TestMethod]
    public void Create_RejectsTitleLongerThanMaximumLength()
    {
        var longTitle = new string('A', 121);

        Assert.ThrowsExactly<ArgumentException>(
            () => BusinessActivityEvent.Create(
                BusinessActivityType.CustomerRegistered,
                "Customer",
                "Customer",
                longTitle,
                "A new account was created."));
    }

    [TestMethod]
    public void Create_ConvertsBlankOptionalValuesToNull()
    {
        var activity = BusinessActivityEvent.Create(
            BusinessActivityType.CustomerRegistered,
            "Customer",
            "Customer",
            "Customer registered",
            "A new account was created.",
            targetUserName: " ",
            ticketNumber: " ");

        Assert.IsNull(activity.TargetUserName);
        Assert.IsNull(activity.TicketNumber);
    }

    private static BusinessActivityEvent CreateValidActivity(
        BusinessActivityType eventType =
            BusinessActivityType.TechnicianAssigned)
    {
        return BusinessActivityEvent.Create(
            eventType: eventType,
            actorName: "Admin User",
            actorRole: "Admin",
            title: "Technician assigned",
            description: "An administrator assigned a technician.",
            actorUserId: 5,
            targetUserId: 20,
            targetUserName: "Technician User",
            customerId: 10,
            customerName: "Customer User",
            complaintId: 101,
            ticketNumber: "TKT-20261117-ABC12345");
    }
}