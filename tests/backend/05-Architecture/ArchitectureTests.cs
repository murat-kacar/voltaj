using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Voltflow.Domain.Common;
using Voltflow.Application.Services;
using Voltflow.Infrastructure.Repositories;
using Voltflow.Api.Endpoints;

namespace Voltflow.ArchitectureTests;

public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(VoltflowTaxonomy).Assembly, // Domain
            typeof(QuoteService).Assembly, // Application
            typeof(QuoteRepository).Assembly, // Infrastructure
            typeof(QuoteEndpoints).Assembly // Api
        )
        .Build();

    private readonly IObjectProvider<IType> DomainLayer = Types().That().ResideInAssembly(typeof(VoltflowTaxonomy).Assembly).As("Domain Layer");
    private readonly IObjectProvider<IType> ApplicationLayer = Types().That().ResideInAssembly(typeof(QuoteService).Assembly).As("Application Layer");
    private readonly IObjectProvider<IType> InfrastructureLayer = Types().That().ResideInAssembly(typeof(QuoteRepository).Assembly).As("Infrastructure Layer");
    private readonly IObjectProvider<IType> PresentationLayer = Types().That().ResideInAssembly(typeof(QuoteEndpoints).Assembly).As("Presentation Layer");

    [Fact]
    [Trait("Category", "Architecture")]
    public void DomainLayer_ShouldNotHaveDependenciesOnOtherLayers()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .AndShould().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(PresentationLayer);

        rule.Check(Architecture);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void ApplicationLayer_ShouldNotHaveDependenciesOnInfrastructureOrPresentation()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(PresentationLayer);

        rule.Check(Architecture);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void InfrastructureLayer_ShouldNotHaveDependenciesOnPresentation()
    {
        IArchRule rule = Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(PresentationLayer);

        rule.Check(Architecture);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void Domain_ShouldNotUseEntityFramework()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Microsoft.EntityFrameworkCore.*", true)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespace("System.ComponentModel.DataAnnotations.*", true);

        rule.Check(Architecture);
    }
}
