# Sử dụng .NET Core SDK làm base image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base
WORKDIR /app

# Sử dụng .NET Core SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ChatGpt.csproj", "./"]
RUN dotnet restore "./ChatGpt.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ChatGpt.csproj" -c Release -o /app/build

# Publish ứng dụng
FROM build AS publish
RUN dotnet publish "ChatGpt.csproj" -c Release -o /app/publish

# Chạy ứng dụng trong container
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatGpt.dll"]
