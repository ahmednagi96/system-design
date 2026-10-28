# المرحلة الأولى: ورشة البناء والترجمة باستخدام قالب التطوير الشامل من مايكروسوفت
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Catalog.API.csproj", "./"]
RUN dotnet restore "Catalog.API.csproj"
COPY . .
RUN dotnet publish "Catalog.API.csproj" -c Release -o /app/publish

# المرحلة الثانية: وضع النتيجة النهائية داخل قالب التشغيل الخفيف جداً
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Catalog.API.dll"]