namespace SystemDesign.Domain.Entities;

public class UrlClick
{
    public long Id { get; set; }
    public long ShortUrlId { get; set; }
    public ShortUrl ShortUrl { get; set; } = null!;
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
    // بيانات الزبون اللي ضغط على الرابط
    public string? IpAddress { get; set; }
    public string? Country { get; set; }      // بتيجي من Cloudflare أو GeoIP
    public string? DeviceType { get; set; }   // Mobile, Desktop, Tablet
    public string? UserAgent { get; set; }    // نوع المتصفح ونظام التشغيل
    public string? Referrer { get; set; }     // جاي من فيسبوك ولا واتساب ولا مباشر
}
