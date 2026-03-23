FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5100

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY src/Nuts.Domain/Nuts.Domain.csproj Nuts.Domain/
COPY src/Nuts.Application/Nuts.Application.csproj Nuts.Application/
COPY src/Nuts.Infrastructure/Nuts.Infrastructure.csproj Nuts.Infrastructure/
COPY src/Nuts.Api/Nuts.Api.csproj Nuts.Api/
RUN dotnet restore Nuts.Api/Nuts.Api.csproj

COPY src/Nuts.Domain/ Nuts.Domain/
COPY src/Nuts.Application/ Nuts.Application/
COPY src/Nuts.Infrastructure/ Nuts.Infrastructure/
COPY src/Nuts.Api/ Nuts.Api/
RUN dotnet publish Nuts.Api/Nuts.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5100
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Nuts.Api.dll"]
