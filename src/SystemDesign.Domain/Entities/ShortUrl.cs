namespace SystemDesign.Domain.Entities;

public class ShortUrl
{
    public long Id { get; set; }
    // الكود القصير المميز (مثلاً: xY7a9)
    public string ShortCode { get; set; } = string.Empty;
    // الرابط الطويل الأصلي
    public string OriginalUrl { get; set; } = string.Empty;
    // لو المستخدم عامل Login هيتسجل هنا الـ ID بتاعه، لو Guest هيبقى null
    public string? CreatedByUserId { get; set; }
    // لو المستخدم Guest بنحفظ له كود جلسة عشان نربط روابطه ببعض
    public string? GuestSessionId { get; set; }
    // عداد إجمالي سريع لعدد الضغطات
    public long TotalClicks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // علاقة One-to-Many مع جدول تفاصيل الضغطات
    public ICollection<UrlClick> Clicks { get; set; } = new List<UrlClick>();
}
