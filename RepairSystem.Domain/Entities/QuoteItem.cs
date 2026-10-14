using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Entities;

public class QuoteItem
{
    public int QuoteItemId { get; set; }

    public int QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public QuoteItemType ItemType { get; set; } = QuoteItemType.Part;

    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}