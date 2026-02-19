FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["DistributorService/DistributorService.csproj", "DistributorService/"]
RUN dotnet restore "DistributorService/DistributorService.csproj"
COPY . .
WORKDIR "/src/DistributorService"
RUN dotnet build "DistributorService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DistributorService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DistributorService.dll"]
