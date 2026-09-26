namespace Voltflow.Domain.Common;

public sealed class ReferenceValue : Entity
{
    public string Type { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private ReferenceValue() { }

    public ReferenceValue(string type, string code, string name)
    {
        Type = Guard.NotEmpty(type, nameof(type));
        Code = Guard.NotEmpty(code, nameof(code));
        Name = Guard.NotEmpty(name, nameof(name));
    }

    public void Deactivate() => IsActive = false;
}