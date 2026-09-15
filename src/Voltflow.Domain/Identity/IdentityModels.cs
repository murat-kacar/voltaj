using Voltflow.Domain.Common;

namespace Voltflow.Domain.Identity;

public sealed class AppUser : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool EmailVerified { get; private set; }
    public bool IsApproved { get; private set; }

    private AppUser() { }

    public AppUser(string name, string email)
    {
        Name = Guard.NotEmpty(name, nameof(name));
        Email = Guard.NotEmpty(email, nameof(email)).Trim().ToLowerInvariant();
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = Guard.NotEmpty(passwordHash, nameof(passwordHash));
    public void SetVerified() => EmailVerified = true;
    public void Approve() => IsApproved = true;
}

public sealed class AppRole : Entity
{
    public string Name { get; private set; } = string.Empty;

    private AppRole() { }

    public AppRole(string name)
    {
        Name = Guard.NotEmpty(name, nameof(name)).Trim();
    }
}

public sealed class AppUserRole : Entity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    private AppUserRole() { }

    public AppUserRole(Guid userId, Guid roleId)
    {
        UserId = Guard.AgainstEmptyGuid(userId, nameof(userId));
        RoleId = Guard.AgainstEmptyGuid(roleId, nameof(roleId));
    }
}
