FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/PriceProof.Api/PriceProof.Api.csproj", "src/PriceProof.Api/"]
COPY ["src/PriceProof.Application/PriceProof.Application.csproj", "src/PriceProof.Application/"]
COPY ["src/PriceProof.Domain/PriceProof.Domain.csproj", "src/PriceProof.Domain/"]
COPY ["src/PriceProof.Infrastructure/PriceProof.Infrastructure.csproj", "src/PriceProof.Infrastructure/"]

RUN dotnet restore "src/PriceProof.Api/PriceProof.Api.csproj"

COPY . .
WORKDIR /src/src/PriceProof.Api
RUN dotnet publish "PriceProof.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PriceProof.Api.dll", "--urls", "http://0.0.0.0:8080"]
