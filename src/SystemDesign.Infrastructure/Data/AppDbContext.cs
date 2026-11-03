using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SystemDesign.Domain.Entities;

namespace SystemDesign.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;

    public DbSet<ShortUrl> ShortUrls { get; set; } = null!;
    public DbSet<UrlClick> UrlClicks { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // إنشاء عداد (Sequence) في SQL Server يبدأ من 100,000 عشان الأكواد تطلع من 4 لـ 7 حروف
        modelBuilder.HasSequence<long>("ShortUrlIdSeq")
            .StartsAt(100_000)
            .IncrementsBy(1);

        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.HasKey(e => e.Id);
            // الرقم جاي من ShortUrlIdSeq قبل الحفظ، مش Identity من SQL Server
            entity.Property(e => e.Id).ValueGeneratedNever();
            // أهم سطر في السيستم: Unique Index على الكود القصير عشان الـ SELECT في الـ Redirect تبقى في 1ms
            entity.HasIndex(e => e.ShortCode).IsUnique();
            entity.Property(e => e.ShortCode).HasMaxLength(12).IsRequired();
            entity.Property(e => e.OriginalUrl).HasMaxLength(2048).IsRequired();
            // Index عشان لو حبينا نجيب كل الروابط بتاعت مستخدم معين أو Guest معين بسرعة
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.GuestSessionId);
        });

        modelBuilder.Entity<UrlClick>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShortUrlId);
            entity.HasIndex(e => e.ClickedAt);

            entity.HasOne(c => c.ShortUrl)
                  .WithMany(u => u.Clicks)
                  .HasForeignKey(c => c.ShortUrlId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
    // NEXT VALUE FOR لازم يتنفّذ كأمر مباشر. SqlQueryRaw بيلفّه جوّه subquery وSQL Server بيرفض ده.
    public async Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default)
    {
        var connection = Database.GetDbConnection();
        await Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR ShortUrlIdSeq";
            if (Database.CurrentTransaction is not null)
                command.Transaction = Database.CurrentTransaction.GetDbTransaction();

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        finally
        {
            await Database.CloseConnectionAsync();
        }
    }
}