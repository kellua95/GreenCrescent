FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GreenCrescent.Core/GreenCrescent.Core.csproj", "GreenCrescent.Core/"]
COPY ["GreenCrescent.Application/GreenCrescent.Application.csproj", "GreenCrescent.Application/"]
COPY ["GreenCrescent.Infrastructure/GreenCrescent.Infrastructure.csproj", "GreenCrescent.Infrastructure/"]
COPY ["GreenCrescent.Web/GreenCrescent.Web.csproj", "GreenCrescent.Web/"]

RUN dotnet restore "GreenCrescent.Web/GreenCrescent.Web.csproj"

COPY . .

WORKDIR "/src/GreenCrescent.Web"

RUN dotnet publish "GreenCrescent.Web.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

USER app

ENTRYPOINT ["dotnet", "GreenCrescent.Web.dll"]