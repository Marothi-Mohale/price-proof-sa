FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PriceProofSA.slnx ./
COPY src/PriceProofSA.Api/PriceProofSA.Api.csproj src/PriceProofSA.Api/
COPY src/PriceProofSA.Application/PriceProofSA.Application.csproj src/PriceProofSA.Application/
COPY src/PriceProofSA.Domain/PriceProofSA.Domain.csproj src/PriceProofSA.Domain/
COPY src/PriceProofSA.Infrastructure/PriceProofSA.Infrastructure.csproj src/PriceProofSA.Infrastructure/

RUN dotnet restore src/PriceProofSA.Api/PriceProofSA.Api.csproj

COPY . .
RUN dotnet publish src/PriceProofSA.Api/PriceProofSA.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PriceProofSA.Api.dll"]
