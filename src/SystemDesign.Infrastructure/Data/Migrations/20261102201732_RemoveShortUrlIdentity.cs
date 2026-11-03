using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SystemDesign.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShortUrlIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server لا يشيل خاصية IDENTITY بـ ALTER COLUMN، والجدول فاضي فبنعيد إنشاء العمود.
            migrationBuilder.Sql("""
                ALTER TABLE [UrlClicks] DROP CONSTRAINT [FK_UrlClicks_ShortUrls_ShortUrlId];
                ALTER TABLE [ShortUrls] DROP CONSTRAINT [PK_ShortUrls];
                ALTER TABLE [ShortUrls] DROP COLUMN [Id];
                ALTER TABLE [ShortUrls] ADD [Id] bigint NOT NULL;
                ALTER TABLE [ShortUrls] ADD CONSTRAINT [PK_ShortUrls] PRIMARY KEY ([Id]);
                ALTER TABLE [UrlClicks] ADD CONSTRAINT [FK_UrlClicks_ShortUrls_ShortUrlId]
                    FOREIGN KEY ([ShortUrlId]) REFERENCES [ShortUrls] ([Id]) ON DELETE CASCADE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE [UrlClicks] DROP CONSTRAINT [FK_UrlClicks_ShortUrls_ShortUrlId];
                ALTER TABLE [ShortUrls] DROP CONSTRAINT [PK_ShortUrls];
                ALTER TABLE [ShortUrls] DROP COLUMN [Id];
                ALTER TABLE [ShortUrls] ADD [Id] bigint IDENTITY(1,1) NOT NULL;
                ALTER TABLE [ShortUrls] ADD CONSTRAINT [PK_ShortUrls] PRIMARY KEY ([Id]);
                ALTER TABLE [UrlClicks] ADD CONSTRAINT [FK_UrlClicks_ShortUrls_ShortUrlId]
                    FOREIGN KEY ([ShortUrlId]) REFERENCES [ShortUrls] ([Id]) ON DELETE CASCADE;
                """);
        }
    }
}
