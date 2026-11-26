
using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Entities;

public class QuoteItem
{
    internal const decimal MaximumMoney =
        9999999999999999.99m;

    private QuoteItem()
    {
    }

    public int QuoteItemId { get; private set; }

    public int QuoteId { get; private set; }
    public Quote Quote { get; private set; } = null!;

    public QuoteItemType ItemType { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TotalPrice => Quantity * UnitPrice;

    public static QuoteItem Create(
        QuoteItemType itemType,
        string description,
        int quantity,
        decimal unitPrice)
    {
        if (!Enum.IsDefined(itemType))
        {
            throw new ArgumentOutOfRangeException(nameof(itemType));
        }

        if (string.IsNullOrWhiteSpace(description) ||
            description.Trim().Length > 250)
        {
            throw new ArgumentException(
                "A description of up to 250 characters is required.",
                nameof(description));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        ValidateMoney(unitPrice, nameof(unitPrice));

        if (quantity * unitPrice > MaximumMoney)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "The item total exceeds the supported amount.");
        }

        return new QuoteItem
        {
            ItemType = itemType,
            Description = description.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    internal static void ValidateMoney(
        decimal amount,
        string parameterName)
    {
        if (amount < 0m ||
            amount > MaximumMoney ||
            decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "The amount must be non-negative and fit decimal(18,2).");
        }
    }
}
