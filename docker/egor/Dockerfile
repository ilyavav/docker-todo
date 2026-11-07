# Этап 1: сборка, SDK в итоговый образ не попадает
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только csproj: пока он не менялся, restore берётся из кеша
COPY src/TodoApi/TodoApi.csproj .
RUN dotnet restore

# Весь исходный код, включая wwwroot
COPY src/TodoApi/ .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Этап 2: лёгкий runtime без компилятора
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Соглашение команды: API слушает HTTP на 8080 (Nginx идёт на api:8080)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Не от root — пользователь app уже есть в образе aspnet
USER $APP_UID

ENTRYPOINT ["dotnet", "TodoApi.dll"]