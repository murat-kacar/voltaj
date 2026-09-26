using Voltflow.Domain.Common;

namespace Voltflow.Domain.Inventory;

public sealed class Warehouse : Entity
{
    public string Name { get; private set; } = string.Empty;
    public WarehouseType Type { get; private set; }

    private Warehouse() { }

    public Warehouse(string name, WarehouseType type)
    {
        Name = Guard.NotEmpty(name, nameof(name));
        Type = type;
    }
}

public enum WarehouseType
{
    Main,
    Van,
    Virtual
}
