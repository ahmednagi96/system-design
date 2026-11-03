using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SystemDesign.API.Background;
using SystemDesign.Infrastructure.Data;
using SystemDesign.API.Services;
using SystemDesign.Domain.Entities;

namespace SystemDesign.API.Controllers;

[ApiController]
public class UrlShortenerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache; // في الـ Production الكبيرة بنبدلها بـ IDistributedCache (Redis)
    private readonly ClickAnalyticsQueue _analyticsQueue;

    public UrlShortenerController(
        AppDbContext db, 
        IMemoryCache cache, 
        ClickAnalyticsQueue analyticsQueue)
    {
        _db = db;
        _cache = cache;
        _analyticsQueue = analyticsQueue;
    }

    public record ShortenRequest(string OriginalUrl);

    // =========================================================================
    // 1. Endpoint إنشاء الرابط القصير (تتعامل مع المسجل ومع الـ Guest)
    // =========================================================================
    [HttpPost("api/shorten")]
    public async Task<IActionResult> CreateShortUrl([FromBody] ShortenRequest request)
    {
        // التأكد إن الرابط سليم
        if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
            return BadRequest("الرابط غير صالح.");

        // أ. فحص هل المستخدم عامل Login؟
        string? userId = User.Identity?.IsAuthenticated == true 
            ? User.FindFirstValue(ClaimTypes.NameIdentifier) 
            : null;

        // ب. لو مش عامل Login (يعني Guest)، نجيب أو نولّد كود الجلسة بتاعه من الكوكيز
        string? guestSessionId = null;
        if (userId == null)
        {
            if (!Request.Cookies.TryGetValue("guest_session_id", out guestSessionId))
            {
                guestSessionId = Guid.NewGuid().ToString("N");
                Response.Cookies.Append("guest_session_id", guestSessionId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30) // نحتفظ بهوية الـ Guest لمدة شهر
                });
            }
        }

        // ج. نجيب رقم جديد من الداتابيز ونحوله لـ Base62 (دايماً كود جديد حتى لو نفس الرابط)
        long nextId = await _db.GetNextSequenceValueAsync();
        string shortCode = Base62Encoder.Encode(nextId);

        var shortUrl = new ShortUrl
        {
            Id = nextId,
            ShortCode = shortCode,
            OriginalUrl = request.OriginalUrl,
            CreatedByUserId = userId,
            GuestSessionId = guestSessionId
        };

        _db.ShortUrls.Add(shortUrl);
        await _db.SaveChangesAsync();

        // نحطه في الكاش بالمرة عشان أول زبون يضغط عليه يفتحه في 1ms
        _cache.Set($"url:{shortCode}", new CachedUrlData(shortUrl.Id, shortUrl.OriginalUrl), TimeSpan.FromHours(24));

        var fullShortUrl = $"{Request.Scheme}://{Request.Host}/{shortCode}";

        return Ok(new
        {
            ShortCode = shortCode,
            ShortUrl = fullShortUrl,
            CreatedBy = userId != null ? "AuthenticatedUser" : $"Guest ({guestSessionId})"
        });
    }

    // =========================================================================
    // 2. Endpoint التحويل السريع وتسجيل بيانات الزبون (GET /{code})
    // =========================================================================
    [HttpGet("{code}")]
    public async Task<IActionResult> RedirectToOriginal(string code)
    {
        // أ. ندور في الكاش الأول، لو مش موجود نجيبه من الداتابيز ونحفظه في الكاش
        if (!_cache.TryGetValue($"url:{code}", out CachedUrlData? cachedUrl))
        {
            var urlEntity = await _db.ShortUrls
                .AsNoTracking()
                .Where(u => u.ShortCode == code)
                .Select(u => new CachedUrlData(u.Id, u.OriginalUrl))
                .FirstOrDefaultAsync();

            if (urlEntity == null)
                return NotFound("هذا الرابط القصير غير موجود.");

            cachedUrl = urlEntity;
            _cache.Set($"url:{code}", cachedUrl, TimeSpan.FromHours(24));
        }

        // ب. نجمع بيانات الزبون اللي ضغط على الرابط (حتى لو Guest)
        var userAgent = Request.Headers.UserAgent.ToString();
        var clickEvent = new UrlClick
        {
            ShortUrlId = cachedUrl!.Id,
            ClickedAt = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            // لو شغال ورا Cloudflare، بيبعتلك كود الدولة جاهز في الهيدر ده مجاناً!
            Country = Request.Headers["CF-IPCountry"].FirstOrDefault() ?? "Unknown",
            DeviceType = DetectDeviceType(userAgent),
            UserAgent = userAgent,
            Referrer = Request.Headers.Referer.ToString()
        };

        // ج. نرمي الضغطة في الـ Queue الخلفي بدون انتظار (Zero Latency)
        _analyticsQueue.EnqueueClick(clickEvent);

        // د. نحول الزبون فوراً للرابط الأصلي (302 Redirect عشان المتصفح ميكيشش التحويل ويضيع علينا العدّات الجاية!)
        return Redirect(cachedUrl.OriginalUrl);
    }

    // دالة مساعدة بسيطة لمعرفة نوع الجهاز من الـ User-Agent
    private static string DetectDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) || 
            userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) || 
            userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
            return "Mobile";
        if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) || 
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "Tablet";
        return "Desktop";
    }

    private record CachedUrlData(long Id, string OriginalUrl);
}