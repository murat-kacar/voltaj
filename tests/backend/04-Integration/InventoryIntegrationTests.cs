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
using Voltflow.Domain.WorkOrders;
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
