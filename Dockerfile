FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AIComplianceAgent.sln ./
COPY AIComplianceAgent.Core/AIComplianceAgent.Core.csproj AIComplianceAgent.Core/
COPY AIComplianceAgent.Api/AIComplianceAgent.Api.csproj AIComplianceAgent.Api/
COPY AIComplianceAgent.Cli/AIComplianceAgent.Cli.csproj AIComplianceAgent.Cli/
COPY AIComplianceAgent.Tests/AIComplianceAgent.Tests.csproj AIComplianceAgent.Tests/
RUN dotnet restore AIComplianceAgent.Api/AIComplianceAgent.Api.csproj

COPY . .
RUN dotnet publish AIComplianceAgent.Api/AIComplianceAgent.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY config ./config
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AIComplianceAgent.Api.dll"]
