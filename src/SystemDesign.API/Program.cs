using StackExchange.Redis;
using SystemDesign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SystemDesign.API.Background;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// register redis 
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

// register db context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!));
    // register memory cache
    // 2. تسجيل الكاش
builder.Services.AddMemoryCache();
// 3. تسجيل الـ Queue والـ Background Worker بتاع الإحصائيات
builder.Services.AddSingleton<ClickAnalyticsQueue>();
builder.Services.AddHostedService<ClickAnalyticsWorker>();


var app = builder.Build();

// =====================================================================
// 🌟 كود عمل Database Update (Migration) أوتوماتيك أول ما المشروع يشتغل
// =====================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // السطر ده بيعمل نفس وظيفة dotnet ef database update بالظبط!
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "حصل خطأ أثناء عمل Migration للداتابيز وقت التشغيل.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//
// 
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
