FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["MMS.NotificationService/MMS.NotificationService.csproj", "MMS.NotificationService/"]
COPY ["MMS.Protos/MMS.Protos.csproj", "MMS.Protos/"]
COPY ["src/MMS.Infrastructure/MMS.Infrastructure.csproj", "src/MMS.Infrastructure/"]
COPY ["src/MMS.Application/MMS.Application.csproj", "src/MMS.Application/"]
COPY ["src/MMS.Domain/MMS.Domain.csproj", "src/MMS.Domain/"]
RUN dotnet restore "MMS.NotificationService/MMS.NotificationService.csproj"

COPY . .
WORKDIR "/src/MMS.NotificationService"
RUN dotnet publish "MMS.NotificationService.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5102

ENV ASPNETCORE_ENVIRONMENT=Development \
    Kestrel__Endpoints__Grpc__Url=http://0.0.0.0:5102 \
    Kestrel__Endpoints__Grpc__Protocols=Http2

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MMS.NotificationService.dll"]
