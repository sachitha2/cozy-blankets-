FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SellerService/SellerService.csproj", "SellerService/"]
RUN dotnet restore "SellerService/SellerService.csproj"
COPY . .
WORKDIR "/src/SellerService"
RUN dotnet build "SellerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SellerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SellerService.dll"]
