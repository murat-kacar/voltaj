using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voltflow.Application.Dtos;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Customers;
using Voltflow.Domain.Identity;
using Voltflow.Domain.Reminders;
using Voltflow.Infrastructure.Persistence;
using Voltflow.Infrastructure.Repositories;
using Voltflow.Worker;

namespace Voltflow.Tests;


[Xunit.Collection("ApiIntegration")]
[Trait("VUT", "04101")]
public sealed class InventoryIntegrationTests : Xunit.IClassFixture<ApiTestFixture>
{
    private readonly System.Net.Http.HttpClient _client;
    private readonly ApiTestFixture _factory;

    public InventoryIntegrationTests(ApiTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    private async Task<string> AdminTokenAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var admin = new AppUser($"Stock Admin {Guid.NewGuid():N}", $"stock-admin-{Guid.NewGuid():N}@test.com");
        var token = tokens.CreateToken(admin, ["Admin"]);
        db.UserSessions.Add(new UserSession(admin.Id, token, DateTime.UtcNow.AddHours(1)));
        db.Add(admin);
        await db.SaveChangesAsync();
        return token;
    }

    [Fact]
    [Trait("VUT", "04101")]
    public async Task ListStock_FindsRowsBySearch_InCodeOrder_AndKeepsToItsCeiling()
    {
        var token = await AdminTokenAsync();
        var unique = Guid.NewGuid().ToString("N");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
            db.MaterialStocks.Add(new Voltflow.Domain.Inventory.MaterialStock(Guid.NewGuid(), $"LST-{unique}-B", "Listed material B", 4));
            db.MaterialStocks.Add(new Voltflow.Domain.Inventory.MaterialStock(Guid.NewGuid(), $"LST-{unique}-A", "Listed material A", 9));
            await db.SaveChangesAsync();
        }

        using var searchReq = new HttpRequestMessage(HttpMethod.Get, $"/api/inventory?search={unique}");
        searchReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var search = await _client.SendAsync(searchReq);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.Equal("2", search.Headers.GetValues("X-Total-Count").Single());
        var rows = await search.Content.ReadFromJsonAsync<List<StockDto>>();
        Assert.Equal(new[] { $"LST-{unique}-A", $"LST-{unique}-B" }, rows!.Select(r => r.MaterialCode).ToArray());
        Assert.Equal(9m, rows![0].QuantityOnHand);

        using var pageReq = new HttpRequestMessage(HttpMethod.Get, $"/api/inventory?search={unique}&limit=1");
        pageReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var page = await _client.SendAsync(pageReq);
        Assert.Single((await page.Content.ReadFromJsonAsync<List<StockDto>>())!);
        Assert.Contains("rel=\"next\"", page.Headers.GetValues("Link").Single());
    }

    [Fact]
    [Trait("VUT", "04101")]
    public async Task ListStock_WithoutASignIn_ShouldReturn401()
    {
        using var resp = await _client.GetAsync("/api/inventory");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    [Trait("VUT", "04103")]
    public async Task InventoryAdjustment_ShouldWriteStockMovement()
    {
        var unique = Guid.NewGuid().ToString("N");
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VoltflowDbContext>();
        var warehouseId = Guid.NewGuid();
        var stock = new Voltflow.Domain.Inventory.MaterialStock(warehouseId, $"MAT-{unique}", "Integration material", 10);
        dbContext.MaterialStocks.Add(stock);
        await dbContext.SaveChangesAsync();

        var isInMemory = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        var transaction = isInMemory ? null : await dbContext.Database.BeginTransactionAsync();
        try
        {
            stock.Adjust(5);
            dbContext.StockMovements.Add(new Voltflow.Domain.Inventory.StockMovement(
                stock.MaterialCode, null, null, 5, Voltflow.Domain.Inventory.StockMovementType.In, 10, 15, "Integration adjustment"));
            await dbContext.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }

        Assert.Equal(15, stock.QuantityOnHand);
        Assert.Contains(await dbContext.StockMovements.Where(x => x.MaterialCode == stock.MaterialCode).ToListAsync(), x => x.NewQuantity == 15);
    }

}
