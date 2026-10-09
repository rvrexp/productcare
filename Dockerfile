FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ProductCare.Api/ProductCare.Api.csproj", "ProductCare.Api/"]
COPY ["ProductCare.Application/ProductCare.Application.csproj", "ProductCare.Application/"]
COPY ["ProductCare.AI/ProductCare.AI.csproj", "ProductCare.AI/"]
COPY ["ProductCare.Infrastructure/ProductCare.Infrastructure.csproj", "ProductCare.Infrastructure/"]

RUN dotnet restore "ProductCare.Api/ProductCare.Api.csproj"

COPY . .

WORKDIR "/src/ProductCare.Api"
RUN dotnet publish "ProductCare.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductCare.Api.dll"]