FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5006

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ClientAppWeb/ClientAppWeb.csproj", "ClientAppWeb/"]
RUN dotnet restore "ClientAppWeb/ClientAppWeb.csproj"
COPY . .
WORKDIR "/src/ClientAppWeb"
RUN dotnet build "ClientAppWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClientAppWeb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClientAppWeb.dll"]
