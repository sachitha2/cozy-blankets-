FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ManufacturerService/ManufacturerService.csproj", "ManufacturerService/"]
RUN dotnet restore "ManufacturerService/ManufacturerService.csproj"
COPY . .
WORKDIR "/src/ManufacturerService"
RUN dotnet build "ManufacturerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ManufacturerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ManufacturerService.dll"]
