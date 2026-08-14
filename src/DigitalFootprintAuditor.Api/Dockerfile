FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore DigitalFootprintAuditor.slnx

RUN dotnet publish \
    src/DigitalFootprintAuditor.Api/DigitalFootprintAuditor.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


FROM build AS migration-build

RUN dotnet tool install --global dotnet-ef

ENV PATH="$PATH:/root/.dotnet/tools"

RUN dotnet ef migrations bundle \
    --project src/DigitalFootprintAuditor.Infrastructure \
    --startup-project src/DigitalFootprintAuditor.Api \
    --self-contained \
    -r linux-x64 \
    -o /app/efbundle


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrations
WORKDIR /app

COPY --from=migration-build /app/efbundle ./efbundle

ENTRYPOINT ["./efbundle"]


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DigitalFootprintAuditor.Api.dll"]