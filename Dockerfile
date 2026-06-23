FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Marketplace.Orders.Api/Marketplace.Orders.Api.csproj", "Marketplace.Orders.Api/"]
COPY ["Marketplace.Orders.Application/Marketplace.Orders.Application.csproj", "Marketplace.Orders.Application/"]
COPY ["Marketplace.Orders.Domain/Marketplace.Orders.Domain.csproj", "Marketplace.Orders.Domain/"]
COPY ["Marketplace.Orders.Infrastructure/Marketplace.Orders.Infrastructure.csproj", "Marketplace.Orders.Infrastructure/"]
COPY ["Marketplace.Orders.Migrations/Marketplace.Orders.Migrations.csproj", "Marketplace.Orders.Migrations/"]

RUN dotnet restore "Marketplace.Orders.Api/Marketplace.Orders.Api.csproj"

COPY . .
WORKDIR "/src/Marketplace.Orders.Api"
RUN dotnet build "Marketplace.Orders.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Marketplace.Orders.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Marketplace.Orders.Api.dll"]