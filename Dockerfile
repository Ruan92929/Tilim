# Multi-stage build so the image shipped to production has no SDK, no
# source code and no NuGet cache in it — only the published app. This is
# what lets Tilim.Api run as N identical, stateless containers behind a
# load balancer (Railway, Fly.io, any PaaS that takes a Dockerfile) without
# any code change — see docs/ARCHITECTURE.md.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Tilim.sln .
COPY src/Tilim.Domain/Tilim.Domain.csproj src/Tilim.Domain/
COPY src/Tilim.Application/Tilim.Application.csproj src/Tilim.Application/
COPY src/Tilim.Infrastructure/Tilim.Infrastructure.csproj src/Tilim.Infrastructure/
COPY src/Tilim.Api/Tilim.Api.csproj src/Tilim.Api/
RUN dotnet restore src/Tilim.Api/Tilim.Api.csproj

COPY src/ src/
RUN dotnet publish src/Tilim.Api/Tilim.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "Tilim.Api.dll"]
