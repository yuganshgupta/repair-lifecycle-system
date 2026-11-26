
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Tests;

[TestClass]
public class QuoteTests
{
    [TestMethod]
    public void CreateQuote_CalculatesRepairCostWithoutInspectionFee()
    {
        var quote = CreateQuote();

        Assert.AreEqual(2300m, quote.EstimatedTotalCost);
        Assert.AreEqual(300m, quote.InspectionFee);
        Assert.AreEqual(0m, quote.InspectionFeeDue);
        Assert.HasCount(2, quote.Items);
        Assert.AreEqual(
            QuoteDecision.Pending,
            quote.CustomerDecision);
    }

    [TestMethod]
    public void AcceptedQuote_DoesNotChargeInspectionFee()
    {
        var quote = CreateQuote();

        quote.RecordDecision(QuoteDecision.Accepted);

        Assert.AreEqual(0m, quote.InspectionFeeDue);
        Assert.AreEqual(
            QuoteDecision.Accepted,
            quote.CustomerDecision);
    }

    [TestMethod]
    public void RejectedQuote_ChargesInspectionFee()
    {
        var quote = CreateQuote();

        quote.RecordDecision(
            QuoteDecision.Rejected,
            "Repair is too expensive.");

        Assert.AreEqual(300m, quote.InspectionFeeDue);

        Assert.AreEqual(
            "Repair is too expensive.",
            quote.DecisionReason);

        Assert.IsNotNull(quote.DecisionTimestamp);
    }

    [TestMethod]
    public void RecordDecision_Twice_ThrowsException()
    {
        var quote = CreateQuote();

        quote.RecordDecision(QuoteDecision.Accepted);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            quote.RecordDecision(QuoteDecision.Rejected));
    }

    [TestMethod]
    public void OutOfWarranty_WithoutReason_ThrowsException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Quote.Create(
                101,
                20,
                false,
                null,
                300m,
                "Faulty cooling fan",
                new[]
                {
                    (QuoteItemType.Part, "Fan", 1, 1500m)
                }));
    }

    [TestMethod]
    public void ValidWarranty_HasZeroRepairCost()
    {
        var quote = Quote.Create(
            101,
            20,
            true,
            null,
            0m,
            "Faulty cooling fan",
            new[]
            {
                (QuoteItemType.Part, "Fan", 1, 0m),
                (QuoteItemType.Labor, "Installation", 1, 0m)
            });

        Assert.AreEqual(0m, quote.EstimatedTotalCost);

        quote.RecordDecision(QuoteDecision.Rejected);

        Assert.AreEqual(0m, quote.InspectionFeeDue);
    }

    [TestMethod]
    public void ValidWarranty_WithRepairCharges_ThrowsException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Quote.Create(
                101,
                20,
                true,
                null,
                0m,
                "Faulty cooling fan",
                new[]
                {
                    (QuoteItemType.Part, "Fan", 1, 1500m)
                }));
    }

    [TestMethod]
    public void CreateQuote_WithoutItems_ThrowsException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Quote.Create(
                101,
                20,
                false,
                "Warranty expired",
                300m,
                "Faulty cooling fan",
                Array.Empty<(
                    QuoteItemType,
                    string,
                    int,
                    decimal)>()));
    }

    [TestMethod]
    public void CreateQuote_WithoutDiagnosticFindings_ThrowsException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Quote.Create(
                101,
                20,
                false,
                "Warranty expired",
                300m,
                "",
                new[]
                {
                    (QuoteItemType.Part, "Fan", 1, 1500m)
                }));
    }

    private static Quote CreateQuote()
    {
        return Quote.Create(
            101,
            20,
            false,
            "Warranty period expired",
            300m,
            "Cooling fan failure causing overheating.",
            new[]
            {
                (QuoteItemType.Part, "Cooling fan", 1, 1500m),
                (QuoteItemType.Labor, "Installation", 1, 800m)
            });
    }
}
