FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/DeskFlow.Api/DeskFlow.Api.csproj", "src/DeskFlow.Api/"]
COPY ["src/DeskFlow.Application/DeskFlow.Application.csproj", "src/DeskFlow.Application/"]
COPY ["src/DeskFlow.Domain/DeskFlow.Domain.csproj", "src/DeskFlow.Domain/"]
COPY ["src/DeskFlow.Infrastructure/DeskFlow.Infrastructure.csproj", "src/DeskFlow.Infrastructure/"]
RUN dotnet restore "src/DeskFlow.Api/DeskFlow.Api.csproj"

COPY . .
RUN dotnet publish "src/DeskFlow.Api/DeskFlow.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "DeskFlow.Api.dll"]
