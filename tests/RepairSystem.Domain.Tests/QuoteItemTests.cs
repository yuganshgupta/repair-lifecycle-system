
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Tests;

[TestClass]
public class QuoteItemTests
{
    [TestMethod]
    public void TotalPrice_IsQuantityMultipliedByUnitPrice()
    {
        var item = QuoteItem.Create(
            QuoteItemType.Part,
            "Replacement fan",
            3,
            250m);

        Assert.AreEqual(750m, item.TotalPrice);
    }

    [TestMethod]
    public void Create_WithNegativeQuantity_ThrowsException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            QuoteItem.Create(
                QuoteItemType.Part,
                "Replacement fan",
                -1,
                250m));
    }

    [TestMethod]
    public void Create_WithNegativePrice_ThrowsException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            QuoteItem.Create(
                QuoteItemType.Labor,
                "Installation",
                1,
                -500m));
    }
}
