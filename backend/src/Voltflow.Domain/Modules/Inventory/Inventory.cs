using Voltflow.Domain.Common;

namespace Voltflow.Domain.Inventory;

public sealed class MaterialStock : Entity
{
    public Guid WarehouseId { get; private set; }
    public string MaterialCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal QuantityOnHand { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity => QuantityOnHand - ReservedQuantity;

    private MaterialStock() { }

    public MaterialStock(Guid warehouseId, string materialCode, string name, decimal quantityOnHand)
    {
        WarehouseId = Guard.AgainstEmptyGuid(warehouseId, nameof(warehouseId));
        MaterialCode = Guard.NotEmpty(materialCode, nameof(materialCode));
        Name = Guard.NotEmpty(name, nameof(name));
        QuantityOnHand = Guard.AgainstNegative(quantityOnHand, nameof(quantityOnHand));
    }

    public void Adjust(decimal delta)
    {
        var next = QuantityOnHand + delta;
        if (next < 0) throw new InvalidOperationException("Stock cannot go below zero.");

        QuantityOnHand = next;
        Touch();
    }

    public void Reserve(decimal quantity)
    {
        Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
        if (AvailableQuantity < quantity) throw new InvalidOperationException("Insufficient available quantity.");

        ReservedQuantity += quantity;
        Touch();
    }

    public void Release(decimal quantity)
    {
        Guard.AgainstNegativeOrZero(quantity, nameof(quantity));
        if (ReservedQuantity < quantity) throw new InvalidOperationException("Reserved quantity cannot be below zero.");

        ReservedQuantity -= quantity;
        Touch();
    }
}

public sealed class StockMovement : Entity
{
    public string MaterialCode { get; private set; } = string.Empty;
    public Guid? SourceWarehouseId { get; private set; }
    public Guid? DestinationWarehouseId { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public StockMovementType Direction { get; private set; }
    public decimal PreviousQuantity { get; private set; }
    public decimal NewQuantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    private StockMovement() { }

    public StockMovement(string materialCode, decimal quantityDelta, StockMovementType direction)
        : this(materialCode, null, null, quantityDelta, direction, 0, quantityDelta, "Manual adjustment")
    {
    }

    public StockMovement(string materialCode, Guid? sourceWarehouseId, Guid? destinationWarehouseId, decimal quantityDelta, StockMovementType direction, decimal previousQuantity, decimal newQuantity, string reason)
    {
        MaterialCode = Guard.NotEmpty(materialCode, nameof(materialCode));
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        Direction = direction;
        Guard.AgainstNegative(previousQuantity, nameof(previousQuantity));
        Guard.AgainstNegative(newQuantity, nameof(newQuantity));
        if (quantityDelta == 0) throw new ArgumentOutOfRangeException(nameof(quantityDelta), "Quantity delta cannot be zero.");
        QuantityDelta = quantityDelta;
        PreviousQuantity = previousQuantity;
        NewQuantity = newQuantity;
        Reason = Guard.NotEmpty(reason, nameof(reason));
    }
}

public enum StockMovementType
{
    In,
    Out,
    Transfer,
    Return,
    Scrap
}
