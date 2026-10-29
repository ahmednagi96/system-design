# المرحلة الأولى: الـ Build (بنستخدم صورة الـ SDK الكاملة لبناء الكود)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 1. ننسخ ملف الـ Solution وملفات الـ .csproj للأربع مشاريع الأول
# (الحركة دي بتخلي Docker يكيش المكتبات وميحملهاش من الأول كل مرة تعدل فيها سطر كود)
COPY ["SystemDesign.sln", "./"]
COPY ["src/SystemDesign.API/SystemDesign.API.csproj", "src/SystemDesign.API/"]
COPY ["src/SystemDesign.Application/SystemDesign.Application.csproj", "src/SystemDesign.Application/"]
COPY ["src/SystemDesign.Domain/SystemDesign.Domain.csproj", "src/SystemDesign.Domain/"]
COPY ["src/SystemDesign.Infrastructure/SystemDesign.Infrastructure.csproj", "src/SystemDesign.Infrastructure/"]

# 2. نحمل كل الـ NuGet Packages للمشاريع الأربعة
RUN dotnet restore "SystemDesign.sln"

# 3. ننسخ باقي ملفات الكود كلها
COPY . .

# 4. نقف جوه فولدر الـ API ونعمل Publish (الـ API هيجمع الـ DLLs بتاعة باقي المشاريع معاه)
WORKDIR "/app/src/SystemDesign.API"
RUN dotnet publish "SystemDesign.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ------------------------------------------------------------------------

# المرحلة الثانية: الـ Runtime (صورة خفيفة جداً لتشغيل المشروع فقط بدون SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# ننسخ الملفات النهائية اللي طلعت من مرحلة الـ Publish
COPY --from=build /app/publish .

# أمر تشغيل مشروع الـ API
ENTRYPOINT ["dotnet", "SystemDesign.API.dll"]