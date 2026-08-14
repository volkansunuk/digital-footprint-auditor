FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DigitalFootprintAuditor.slnx ./
COPY src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj src/DigitalFootprintAuditor.Api/
COPY src/DigitalFootprintAuditor.Application/DigitalFootprintAuditor.Application.csproj src/DigitalFootprintAuditor.Application/
COPY src/DigitalFootprintAuditor.Domain/DigitalFootprintAuditor.Domain.csproj src/DigitalFootprintAuditor.Domain/
COPY src/DigitalFootprintAuditor.Infrastructure/DigitalFootprintAuditor.Infrastructure.csproj src/DigitalFootprintAuditor.Infrastructure/

RUN dotnet restore src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj

COPY src/ src/
RUN dotnet publish src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj --configuration Release --output /app/publish --no-restore

FROM build AS migration
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.10
ENTRYPOINT ["/tools/dotnet-ef"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DigitalFootprintAuditor.Api.dll"]
