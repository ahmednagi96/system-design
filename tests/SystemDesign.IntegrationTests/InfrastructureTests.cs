using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using SystemDesign.Infrastructure.Data;
// using SystemDesign.Infrastructure.xxx;   ← ضيف الـ namespace بتاع الـ DbContext

namespace SystemDesign.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class InfrastructureTests(ApiFactory factory)
{
    [Fact]
    public async Task Database_Is_Reachable_And_Migrations_Are_Applied()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Database.CanConnectAsync());
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Redis_Can_Write_And_Read()
    {
        var redis = factory.Services.GetRequiredService<IConnectionMultiplexer>();
        var cache = redis.GetDatabase();

        await cache.StringSetAsync("ci:test", "hello");
        var value = await cache.StringGetAsync("ci:test");

        Assert.Equal("hello", (string?)value);
    }

    [Fact]
    public async Task Api_Starts_And_Serves_Swagger()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}