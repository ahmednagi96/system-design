using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using SystemDesign.Infrastructure.Data;
using SystemDesign.Domain.Entities;

namespace SystemDesign.API.Background;

// 1. الـ Queue السريع اللي الـ Controller هيرمي فيه الضغطة في جزء من الملي ثانية
public class ClickAnalyticsQueue
{
    private readonly Channel<UrlClick> _channel = Channel.CreateUnbounded<UrlClick>();

    public void EnqueueClick(UrlClick clickData) => _channel.Writer.TryWrite(clickData);
    public IAsyncEnumerable<UrlClick> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

// 2. الـ Worker اللي شغال في الخلفية يسحب من الـ Queue ويحفظ في الداتابيز
public class ClickAnalyticsWorker : BackgroundService
{
    private readonly ClickAnalyticsQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClickAnalyticsWorker> _logger;

    public ClickAnalyticsWorker(
        ClickAnalyticsQueue queue, 
        IServiceScopeFactory scopeFactory,
        ILogger<ClickAnalyticsWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var click in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 1. إضافة تفاصيل الضغطة (الدولة، الجهاز، الوقت)
                db.UrlClicks.Add(click);

                // 2. تزويد العداد الإجمالي مباشرة في SQL بدون سحب الصف في الـ Memory (Atomic Update في EF Core 8+)
                await db.ShortUrls
                    .Where(u => u.Id == click.ShortUrlId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.TotalClicks, u => u.TotalClicks + 1), stoppingToken);

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "حدث خطأ أثناء تسجيل إحصائيات الضغطة في الخلفية");
            }
        }
    }
}