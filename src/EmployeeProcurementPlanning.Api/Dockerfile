FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EmployeeProcurementPlanning.sln ./
COPY Directory.Build.props ./
COPY src/EmployeeProcurementPlanning.Domain/EmployeeProcurementPlanning.Domain.csproj src/EmployeeProcurementPlanning.Domain/
COPY src/EmployeeProcurementPlanning.Application/EmployeeProcurementPlanning.Application.csproj src/EmployeeProcurementPlanning.Application/
COPY src/EmployeeProcurementPlanning.Infrastructure/EmployeeProcurementPlanning.Infrastructure.csproj src/EmployeeProcurementPlanning.Infrastructure/
COPY src/EmployeeProcurementPlanning.Api/EmployeeProcurementPlanning.Api.csproj src/EmployeeProcurementPlanning.Api/

RUN dotnet restore src/EmployeeProcurementPlanning.Api/EmployeeProcurementPlanning.Api.csproj

COPY src ./src
RUN dotnet publish src/EmployeeProcurementPlanning.Api/EmployeeProcurementPlanning.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EmployeeProcurementPlanning.Api.dll"]
