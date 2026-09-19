using Voltflow.Domain.Common;

namespace Voltflow.Domain.Projects;

public sealed class Project : Entity
{
    public Guid CustomerId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Budget { get; private set; }

    private readonly List<ProjectPhase> _phases = new();
    public IReadOnlyCollection<ProjectPhase> Phases => _phases.AsReadOnly();

    private Project() { }

    public Project(Guid customerId, string name, decimal budget)
    {
        CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Name = Guard.NotEmpty(name, nameof(name));
        Budget = Guard.AgainstNegative(budget, nameof(budget));
        Number = $"PR-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    public void AddPhase(string title, decimal plannedAmount)
    {
        _phases.Add(new ProjectPhase(title, plannedAmount));
        Touch();
    }
}

public sealed class ProjectPhase
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public decimal PlannedAmount { get; private set; }

    private ProjectPhase() { }

    public ProjectPhase(string title, decimal plannedAmount)
    {
        Title = Guard.NotEmpty(title, nameof(title));
        PlannedAmount = Guard.AgainstNegative(plannedAmount, nameof(plannedAmount));
    }
}

public sealed class BillingEntry : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }

    private BillingEntry() { }

    public BillingEntry(Guid projectId, Guid customerId, decimal amount)
    {
        ProjectId = Guard.AgainstEmptyGuid(projectId, nameof(projectId));
        CustomerId = Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Amount = Guard.AgainstNegativeOrZero(amount, nameof(amount));
    }
}
