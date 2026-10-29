using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StackExchange.Redis;
using SystemDesign.Application.DTOs;


namespace SystemDesign.API.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private readonly int _cacheHours;
    private readonly int _lockSeconds;

    public IdempotentAttribute(int cacheHours = 24, int lockSeconds = 30)
    {
        _cacheHours = cacheHours;
        _lockSeconds = lockSeconds;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        // 1. التأكد من وجود الـ Idempotency-Key في الـ Header
        if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValue) ||
            !Guid.TryParse(idempotencyKeyValue, out var idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(new 
            { 
                error = "Header 'Idempotency-Key' is required and must be a valid UUID." 
            });
            return;
        }

        // 2. حساب الـ SHA-256 Hash لبيانات الطلب (عشان نضمن إن العميل مغيرش الداتا بنفس الـ Key)
        var requestHash = ComputeRequestHash(context.ActionArguments);
        var redisKey = $"idempotency:{httpContext.Request.Path}:{idempotencyKey}";

        var redis = httpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();

        var inFlightRecord = new IdempotencyRecord
        {
            Status = "InFlight",
            RequestHash = requestHash
        };

        // 3. محاولة حجز القفل الذري (Atomic SETNX) في خطوة واحدة لمنع الـ Race Condition
        bool lockAcquired = await db.StringSetAsync(
            redisKey,
            JsonSerializer.Serialize(inFlightRecord),
            TimeSpan.FromSeconds(_lockSeconds),
            When.NotExists);

        if (!lockAcquired)
        {
            // المفتاح موجود بالفعل! نجيب قيمته ونشوف حالته إيه
            var existingValue = await db.StringGetAsync(redisKey);
            if (!existingValue.IsNullOrEmpty)
            {
                var existingRecord = JsonSerializer.Deserialize<IdempotencyRecord>(existingValue!);

                // أ) لو العميل بعت نفس الـ Key بس غير المبلغ أو الداتا
                if (existingRecord!.RequestHash != requestHash)
                {
                    context.Result = new UnprocessableEntityObjectResult(new
                    {
                        error = "Idempotency-Key is already used with a different request payload."
                    });
                    return;
                }

                // ب) لو الطلب الأولاني لسه شغال جوه السيرفر ومخلصش
                if (existingRecord.Status == "InFlight")
                {
                    context.Result = new ConflictObjectResult(new
                    {
                        error = "A request with this Idempotency-Key is currently being processed."
                    });
                    return;
                }

                // ج) لو الطلب الأولاني خلص قبل كدة، نرجع نفس النتيجة بالظبط من الكاش
                httpContext.Response.Headers.Append("X-Idempotent-Replayed", "true");
                context.Result = new ContentResult
                {
                    StatusCode = existingRecord.StatusCode,
                    Content = existingRecord.ResponseBody,
                    ContentType = "application/json"
                };
                return;
            }
        }

        // 4. لو الطلب جديد تماماً، ننفذ الـ Controller Action
        try
        {
            var executedContext = await next();

            // 5. لو حصل Exception أو خطأ سيرفر، نمسح القفل عشان العميل يقدر يعمل Retry
            if (executedContext.Exception != null && !executedContext.ExceptionHandled)
            {
                await db.KeyDeleteAsync(redisKey);
                return;
            }

            // 6. لو العملية نجحت (2xx)، نحفظ النتيجة النهائية لمدة 24 ساعة
            if (executedContext.Result is ObjectResult objectResult &&
                objectResult.StatusCode is >= 200 and <= 299)
            {
                var completedRecord = new IdempotencyRecord
                {
                    Status = "Completed",
                    RequestHash = requestHash,
                    StatusCode = objectResult.StatusCode ?? 200,
                    ResponseBody = JsonSerializer.Serialize(objectResult.Value)
                };

                await db.StringSetAsync(
                    redisKey,
                    JsonSerializer.Serialize(completedRecord),
                    TimeSpan.FromHours(_cacheHours),
                    When.Always);
                    //لي بنسمحها عشان استهلاك الميموري
            }
            else
            {
                // لو رجع 400 BadRequest أو 500، نمسح القفل
                await db.KeyDeleteAsync(redisKey);
            }
        }
        catch
        {
            // أمان إضافي: أي كراش غير متوقع يمسح القفل فوراً
            await db.KeyDeleteAsync(redisKey);
            throw;
        }
    }

    private static string ComputeRequestHash(IDictionary<string, object?> actionArguments)
    {
        var json = JsonSerializer.Serialize(actionArguments);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}