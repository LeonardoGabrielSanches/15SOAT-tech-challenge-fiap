FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Oficina.sln ./
COPY src/Oficina.Domain/Oficina.Domain.csproj src/Oficina.Domain/
COPY src/Oficina.Application/Oficina.Application.csproj src/Oficina.Application/
COPY src/Oficina.Infrastructure/Oficina.Infrastructure.csproj src/Oficina.Infrastructure/
COPY src/Oficina.Api/Oficina.Api.csproj src/Oficina.Api/
RUN dotnet restore src/Oficina.Api/Oficina.Api.csproj
COPY src/ src/
RUN dotnet publish src/Oficina.Api/Oficina.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Oficina.Api.dll"]
