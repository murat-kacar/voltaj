using System.Net;
using Voltflow.Application.Dtos;
using static Voltflow.Tests.TestApi;

namespace Voltflow.Tests;

// The customer module end to end through the API: who can do what, what is refused, and how the list narrows down.
// Every test brings its own users and customers (named with a tag of its own), so none depends on another.
[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "02101")]
public sealed class CustomerModuleIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _factory;

    public CustomerModuleIntegrationTests(ApiTestFixture factory) => _factory = factory;

    [Fact]
    [Trait("VUT", "02102")]
    public async Task ACustomer_NeedsOnlyANameAndAPhone_AndIsFoundByAnyOfTheirDetails()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var tag = Tag();

        var doorCustomer = await CreateAsync(manager, $"Ali {tag}", email: null, phone: $"0532{tag[..6]}");
        var company = await CreateAsync(manager, $"Firma {tag} A.Ş.", $"info-{tag}@firma.example", "02120000000", taxNumber: $"TAX{tag}");

        Assert.Equal((string.Empty, "Lead", true), (doorCustomer.Email, doorCustomer.Type, doorCustomer.IsActive));
        Assert.Equal(doorCustomer.Id, Assert.Single(await SearchAsync(manager, $"ali {tag}")).Id);          // name, in any case
        Assert.Equal(doorCustomer.Id, Assert.Single(await SearchAsync(manager, $"0532{tag[..6]}")).Id);     // phone
        Assert.Equal(company.Id, Assert.Single(await SearchAsync(manager, $"INFO-{tag}")).Id);              // email, in any case
        Assert.Equal(company.Id, Assert.Single(await SearchAsync(manager, $"TAX{tag}")).Id);               // tax number

        using var byType = await manager.Client.GetAsync($"/api/customers?search={tag}&type=Active");
        Assert.Empty(await ReadAsync<List<CustomerDto>>(byType));
        using var page = await manager.Client.GetAsync($"/api/customers?search={tag}&limit=1");
        Assert.Equal("2", page.Headers.GetValues("X-Total-Count").Single());
        Assert.Single(await ReadAsync<List<CustomerDto>>(page));
        using var unknownType = await manager.Client.GetAsync("/api/customers?type=Nonsense");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownType.StatusCode);
    }

    [Fact]
    [Trait("VUT", "02103")]
    public async Task ACustomer_IsRefused_WhenTheDetailsBreakARule()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var tag = Tag();

        using var noName = await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest(" ", null, "555", null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noName.StatusCode);
        using var noPhone = await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"No phone {tag}", null, " ", null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noPhone.StatusCode);
        using var badEmail = await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Bad email {tag}", "not-an-email", "555", null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badEmail.StatusCode);
        using var tooLong = await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest(new string('x', 201), null, "555", null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooLong.StatusCode);
        Assert.Empty(await SearchAsync(manager, tag));
    }

    [Fact]
    [Trait("VUT", "02104")]
    public async Task AnEmailAndATaxNumber_BelongToOneCustomer_ButBlankOnesDoNotCount()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var tag = Tag();
        var first = await CreateAsync(manager, $"First {tag}", $"first-{tag}@example.com", "555", $"TAX{tag}");

        using var sameEmail = await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Second {tag}", $"FIRST-{tag}@EXAMPLE.COM", "555", null));
        await AssertRejectedAsync(sameEmail, "CUSTOMER_EMAIL_EXISTS");
        using var sameTax = await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest($"Second {tag}", null, "555", $"TAX{tag}"));
        await AssertRejectedAsync(sameTax, "CUSTOMER_TAXNUMBER_EXISTS");

        // no email and no tax number is common, and any number of customers can be like that
        await CreateAsync(manager, $"Blank one {tag}", null, "555");
        await CreateAsync(manager, $"Blank two {tag}", null, "555");

        // a customer keeps their own email and tax number when they are saved, and cannot take another's
        var second = await CreateAsync(manager, $"Second {tag}", $"second-{tag}@example.com", "555");
        var kept = await ReadAsync<CustomerDto>(await PutAsync(manager.Client, $"/api/customers/{first.Id}", new UpdateCustomerRequest($"First renamed {tag}", first.Email, "556", first.TaxNumber)));
        Assert.Equal(($"First renamed {tag}", "556"), (kept.FullName, kept.Phone));
        using var takeOthers = await PutAsync(manager.Client, $"/api/customers/{second.Id}", new UpdateCustomerRequest(second.FullName, first.Email, second.Phone, null));
        await AssertRejectedAsync(takeOthers, "CUSTOMER_EMAIL_EXISTS");
        var unchanged = await ReadAsync<CustomerDto>(await manager.Client.GetAsync($"/api/customers/{second.Id}"));
        Assert.Equal(second.Email, unchanged.Email);
    }

    [Fact]
    [Trait("VUT", "02105")]
    public async Task ACustomer_CanBeChanged_SetInactiveAndActiveAgain_AndConvertedOnce()
    {
        using var manager = await _factory.ActorAsync("Manager");
        var tag = Tag();
        var customer = await CreateAsync(manager, $"Lifecycle {tag}", null, "555");

        var updated = await ReadAsync<CustomerDto>(await PutAsync(manager.Client, $"/api/customers/{customer.Id}", new UpdateCustomerRequest($"Lifecycle two {tag}", "l@example.com", "556", $"TAX{tag}")));
        Assert.Equal(($"Lifecycle two {tag}", "l@example.com", $"TAX{tag}"), (updated.FullName, updated.Email, updated.TaxNumber));
        using var unknown = await PutAsync(manager.Client, $"/api/customers/{Guid.NewGuid()}", new UpdateCustomerRequest("Nobody", null, "555", null));
        await AssertRejectedAsync(unknown, "CUSTOMER_NOT_FOUND");
        using var withoutName = await PutAsync(manager.Client, $"/api/customers/{customer.Id}", new UpdateCustomerRequest(" ", null, "555", null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutName.StatusCode);

        var inactive = await ReadAsync<CustomerDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/deactivate"));
        Assert.False(inactive.IsActive);
        Assert.Empty(await ReadAsync<List<CustomerDto>>(await manager.Client.GetAsync($"/api/customers?search={tag}&active=true")));
        Assert.Equal(customer.Id, Assert.Single(await ReadAsync<List<CustomerDto>>(await manager.Client.GetAsync($"/api/customers?search={tag}&active=false"))).Id);
        var again = await ReadAsync<CustomerDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/deactivate"));
        Assert.False(again.IsActive);
        var active = await ReadAsync<CustomerDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/activate"));
        Assert.True(active.IsActive);

        var converted = await ReadAsync<CustomerDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/convert-to-active"));
        Assert.Equal("Active", converted.Type);
        Assert.Equal(customer.Id, Assert.Single(await ReadAsync<List<CustomerDto>>(await manager.Client.GetAsync($"/api/customers?search={tag}&type=Active"))).Id);
        using var unknownConvert = await PostAsync(manager.Client, $"/api/customers/{Guid.NewGuid()}/convert-to-active");
        await AssertRejectedAsync(unknownConvert, "CUSTOMER_NOT_FOUND");
    }

    [Fact]
    [Trait("VUT", "02106")]
    public async Task EveryoneSignedInCanLookCustomersUp_ButOnlyManagersChangeThem()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var technician = await _factory.ActorAsync("Technician");
        using var viewer = await _factory.ActorAsync("Viewer");
        using var anonymous = _factory.CreateClient();
        var customer = await CreateAsync(manager, $"Visible {Tag()}", null, "555");

        foreach (var reader in new[] { technician, viewer })
        {
            Assert.Equal(customer.Id, (await ReadAsync<CustomerDto>(await reader.Client.GetAsync($"/api/customers/{customer.Id}"))).Id);
            Assert.NotEmpty(await ReadAsync<List<CustomerDto>>(await reader.Client.GetAsync("/api/customers")));

            using var create = await PostAsync(reader.Client, "/api/customers", new CreateCustomerRequest("Nope", null, "555", null));
            Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
            using var update = await PutAsync(reader.Client, $"/api/customers/{customer.Id}", new UpdateCustomerRequest("Nope", null, "555", null));
            Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
            using var deactivate = await PostAsync(reader.Client, $"/api/customers/{customer.Id}/deactivate");
            Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
            using var site = await PostAsync(reader.Client, $"/api/customers/{customer.Id}/sites", new SaveSiteRequest("Home", "Somewhere"));
            Assert.Equal(HttpStatusCode.Forbidden, site.StatusCode);
        }

        using var noToken = await anonymous.GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
        Assert.True((await ReadAsync<CustomerDto>(await manager.Client.GetAsync($"/api/customers/{customer.Id}"))).IsActive);
    }

    [Fact]
    [Trait("VUT", "02107")]
    public async Task ACustomer_HasAddressesWithDevicesAtThem_AndSerialNumbersAreUnique()
    {
        using var manager = await _factory.ActorAsync("Manager");
        using var technician = await _factory.ActorAsync("Technician");
        var tag = Tag();
        var customer = await CreateAsync(manager, $"Sites {tag}", null, "555");
        var other = await CreateAsync(manager, $"Other {tag}", null, "555");

        var home = await ReadAsync<CustomerSiteDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites", new SaveSiteRequest("Home", "1 Main Street")));
        var shop = await ReadAsync<CustomerSiteDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites", new SaveSiteRequest("Shop", "2 Market Street")));
        Assert.Equal((customer.Id, true, 0), (home.CustomerId, home.IsActive, home.Assets.Count));
        using var noAddress = await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites", new SaveSiteRequest("No address", " "));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noAddress.StatusCode);

        var boiler = await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{home.Id}/assets",
            new SaveAssetRequest("Boiler", $"SN-{tag}", new DateOnly(2024, 5, 17))));
        Assert.Equal(($"SN-{tag}", new DateOnly(2024, 5, 17), home.Id), (boiler.SerialNumber, boiler.InstallationDate, boiler.SiteId));
        using var sameSerial = await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{shop.Id}/assets", new SaveAssetRequest("Another boiler", $"SN-{tag}", null));
        await AssertRejectedAsync(sameSerial, "ASSET_SERIAL_EXISTS");
        // devices without a serial number are fine, as many as there are
        await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{home.Id}/assets", new SaveAssetRequest("Thermostat", null, null)));
        await ReadAsync<CustomerAssetDto>(await PostAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{home.Id}/assets", new SaveAssetRequest("Radiator", "", null)));

        var renamed = await ReadAsync<CustomerAssetDto>(await PutAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{home.Id}/assets/{boiler.Id}",
            new SaveAssetRequest("Combi boiler", $"SN-{tag}", boiler.InstallationDate, IsActive: false)));
        Assert.Equal(("Combi boiler", false), (renamed.Name, renamed.IsActive));
        var moved = await ReadAsync<CustomerSiteDto>(await PutAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{shop.Id}", new SaveSiteRequest("Shop (closed)", "2 Market Street", IsActive: false)));
        Assert.Equal(("Shop (closed)", false), (moved.Name, moved.IsActive));

        // what belongs to another customer is not reachable through this one
        using var wrongCustomer = await PostAsync(manager.Client, $"/api/customers/{other.Id}/sites/{home.Id}/assets", new SaveAssetRequest("Sneaky", null, null));
        await AssertRejectedAsync(wrongCustomer, "SITE_NOT_FOUND");
        using var wrongSite = await PutAsync(manager.Client, $"/api/customers/{customer.Id}/sites/{shop.Id}/assets/{boiler.Id}", new SaveAssetRequest("Sneaky", null, null));
        await AssertRejectedAsync(wrongSite, "ASSET_NOT_FOUND");
        using var noSuchCustomer = await manager.Client.GetAsync($"/api/customers/{Guid.NewGuid()}/sites");
        Assert.Equal(HttpStatusCode.NotFound, noSuchCustomer.StatusCode);

        // anyone signed in can read them: addresses in name order, each with its devices
        var sites = await ReadAsync<List<CustomerSiteDto>>(await technician.Client.GetAsync($"/api/customers/{customer.Id}/sites"));
        Assert.Equal(["Home", "Shop (closed)"], sites.Select(site => site.Name));
        Assert.Equal(["Combi boiler", "Radiator", "Thermostat"], sites[0].Assets.Select(asset => asset.Name));
        Assert.Empty(sites[1].Assets);
    }



    private static string Tag() => Guid.NewGuid().ToString("N")[..10];

    private static async Task<CustomerDto> CreateAsync(Actor manager, string name, string? email, string phone, string? taxNumber = null)
        => await ReadAsync<CustomerDto>(await PostAsync(manager.Client, "/api/customers", new CreateCustomerRequest(name, email, phone, taxNumber)));

    private static async Task<List<CustomerDto>> SearchAsync(Actor actor, string search)
        => await ReadAsync<List<CustomerDto>>(await actor.Client.GetAsync($"/api/customers?search={Uri.EscapeDataString(search)}"));
}
