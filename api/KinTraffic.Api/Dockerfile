FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["KinTraffic.Api.csproj", "./"]
RUN dotnet restore "KinTraffic.Api.csproj"

COPY . .
RUN dotnet publish "KinTraffic.Api.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

RUN groupadd --system --gid 10001 appgroup \
    && useradd --system --uid 10001 --gid appgroup --no-create-home appuser

COPY --from=build /app/publish .
USER appuser

ENTRYPOINT ["dotnet", "KinTraffic.Api.dll"]