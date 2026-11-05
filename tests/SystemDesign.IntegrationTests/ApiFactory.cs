using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using SystemDesign.Infrastructure.Data;
// using SystemDesign.Infrastructure.xxx;   ← ضيف الـ namespace بتاع الـ DbContext

namespace SystemDesign.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
   // 🐳 حاوية SQL Server
private readonly MsSqlContainer _sql =
    new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

// 🐳 حاوية Redis
private readonly RedisContainer _redis =
    new RedisBuilder("redis:alpine").Build();

    private string _sqlConnectionString = string.Empty;

    // ⏯️ بيشتغل مرة واحدة قبل كل الاختبارات
    public async Task InitializeAsync()
    {
        // 1. نشغّل الحاويتين في نفس الوقت (أسرع)
        await Task.WhenAll(_sql.StartAsync(), _redis.StartAsync());

        // 2. Testcontainers بيدّينا Connection String على قاعدة master
        //    فبنغيّر اسم القاعدة عشان الجداول متتعملش في master
        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "SystemDesignTests"
        };
        _sqlConnectionString = builder.ConnectionString;

        // 3. نطبّق الـ Migrations (بيعمل القاعدة والجداول)
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    // ⚙️ هنا بنقول للـ API: استخدم الحاويات دي بدل اللي في الإعدادات
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _sqlConnectionString);
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
    }

    // 🧹 بيشتغل مرة واحدة بعد كل الاختبارات
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
        await _redis.DisposeAsync();
    }
}

// 📦 بنعرّف "مجموعة" عشان كل كلاسات الاختبار تشارك نفس الحاويات
[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
}