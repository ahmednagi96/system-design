# =====================================================================
# المرحلة الأولى: البناء (Build)
# بنستخدم صورة الـ SDK الكاملة عشان نبني الكود
# =====================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. ملفات الإعدادات المركزية (إصدار .NET + إصدارات المكتبات)
#    لازم تتنسخ قبل الـ restore، وإلا المشاريع مش هتعرف الإصدارات
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]

# 2. ملفات csproj للمشاريع اللي الـ API محتاجها بس (من غير Tests)
#    (عشان Docker يحفظ طبقة الـ restore في الكاش، وميعيدش تحميل المكتبات مع كل تعديل كود)
COPY ["src/SystemDesign.Domain/SystemDesign.Domain.csproj",                 "src/SystemDesign.Domain/"]
COPY ["src/SystemDesign.Application/SystemDesign.Application.csproj",       "src/SystemDesign.Application/"]
COPY ["src/SystemDesign.Infrastructure/SystemDesign.Infrastructure.csproj", "src/SystemDesign.Infrastructure/"]
COPY ["src/SystemDesign.API/SystemDesign.API.csproj",                       "src/SystemDesign.API/"]

# 3. تحميل المكتبات لمشروع الـ API بس (وهو بيسحب باقي المشاريع معاه)
RUN dotnet restore "src/SystemDesign.API/SystemDesign.API.csproj"

# 4. نسخ باقي الكود (الملفات الممنوعة متحددة في .dockerignore)
COPY . .

# 5. النشر (Publish): الـ API بيجمع الـ DLLs بتاعة كل المشاريع معاه
RUN dotnet publish "src/SystemDesign.API/SystemDesign.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

    # 6. نرجّع أداة dotnet-ef من ملف الأدوات (نفس الإصدار في كل مكان)
RUN dotnet tool restore

# 7. نبني حزمة الـ Migrations (برنامج صغير بيطبق الـ Migrations)
RUN dotnet ef migrations bundle \
    --project src/SystemDesign.Infrastructure \
    --startup-project src/SystemDesign.API \
    --configuration Release \
    --runtime linux-x64 \
    --output /app/efbundle \
    --force

# =====================================================================
# المرحلة الثانية: التشغيل (Runtime)
# صورة خفيفة فيها بيئة التشغيل بس، من غير SDK ولا كود المصدر
# =====================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# ننسخ الناتج النهائي بس من مرحلة البناء
COPY --from=build /app/publish .

# ننسخ حزمة الـ Migrations جنب التطبيق
COPY --from=build /app/efbundle ./efbundle

# نشغّل التطبيق بيوزر عادي مش root (أمان)
USER $APP_UID

ENTRYPOINT ["dotnet", "SystemDesign.API.dll"]