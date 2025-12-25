FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/FieldTicket.Api/FieldTicket.Api.csproj", "src/FieldTicket.Api/"]
COPY ["src/FieldTicket.Core/FieldTicket.Core.csproj", "src/FieldTicket.Core/"]
COPY ["src/FieldTicket.Domain/FieldTicket.Domain.csproj", "src/FieldTicket.Domain/"]
COPY ["src/FieldTicket.Infrastructure/FieldTicket.Infrastructure.csproj", "src/FieldTicket.Infrastructure/"]
COPY ["src/FieldTicket.Shared/FieldTicket.Shared.csproj", "src/FieldTicket.Shared/"]

RUN dotnet restore "src/FieldTicket.Api/FieldTicket.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/FieldTicket.Api"
RUN dotnet build "FieldTicket.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FieldTicket.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FieldTicket.Api.dll"]


