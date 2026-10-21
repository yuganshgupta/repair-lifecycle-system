
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepairSystem.Domain.Entities;

namespace RepairSystem.Domain.Tests;

[TestClass]
public class QuoteItemTests
{
    [TestMethod]
    public void TotalPrice_IsQuantityMultipliedByUnitPrice()
    {
        var item = new QuoteItem
        {
            Quantity = 3,
            UnitPrice = 250m
        };

        Assert.AreEqual(750m, item.TotalPrice);
    }
}
