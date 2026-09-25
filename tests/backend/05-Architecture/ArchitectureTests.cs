using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Voltflow.Domain.Common;
using Voltflow.Application.Services;
using Voltflow.Infrastructure.Repositories;
using Voltflow.Api.Endpoints;
using ArchUnitNET.xUnit;

namespace Voltflow.ArchitectureTests;

public class ArchBoundaryTests
{
        private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(VoltflowTaxonomy).Assembly, // Domain
            typeof(CustomerService).Assembly, // Application
            typeof(CustomerRepository).Assembly, // Infrastructure
            typeof(CustomerEndpoints).Assembly // Api
        )
        .Build();

    private readonly IObjectProvider<IType> DomainLayer = Types().That().ResideInAssembly(typeof(VoltflowTaxonomy).Assembly).As("Domain Layer");
    private readonly IObjectProvider<IType> ApplicationLayer = Types().That().ResideInAssembly(typeof(CustomerService).Assembly).As("Application Layer");
    private readonly IObjectProvider<IType> InfrastructureLayer = Types().That().ResideInAssembly(typeof(CustomerRepository).Assembly).As("Infrastructure Layer");
    private readonly IObjectProvider<IType> PresentationLayer = Types().That().ResideInAssembly(typeof(CustomerEndpoints).Assembly).As("Presentation Layer");

    [Fact]
    [Trait("Category", "Architecture")]
    public void DomainLayerShouldNotHaveDependenciesOnOtherLayers()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .AndShould().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(PresentationLayer);

        rule.Check(Architecture);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void ApplicationLayerShouldNotHaveDependenciesOnInfrastructureOrPresentation()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .AndShould().NotDependOnAny(PresentationLayer);

        rule.Check(Architecture);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void InfrastructureLayerShouldNotHaveDependenciesOnPresentation()
    {
        IArchRule rule = Types().That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(PresentationLayer);

        rule.Check(Architecture);
    }

    [Fact]
    [Trait("Category", "Architecture")]
    public void DomainShouldNotUseEntityFramework()
    {
        IArchRule rule = Types().That().Are(DomainLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Microsoft.EntityFrameworkCore")
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespace("System.ComponentModel.DataAnnotations");

        rule.Check(Architecture);
    }
}
