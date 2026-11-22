FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY ["global.json", "./"]
COPY ["src/Ptax.Api/Ptax.Api.csproj", "src/Ptax.Api/"]
COPY ["src/Ptax.Bcb/Ptax.Bcb.csproj", "src/Ptax.Bcb/"]
COPY ["src/Ptax.Data/Ptax.Data.csproj", "src/Ptax.Data/"]
COPY ["src/Ptax.Worker/Ptax.Worker.csproj", "src/Ptax.Worker/"]
RUN dotnet restore "src/Ptax.Api/Ptax.Api.csproj" && dotnet restore "src/Ptax.Worker/Ptax.Worker.csproj"
COPY . .
RUN dotnet publish "src/Ptax.Api/Ptax.Api.csproj" -c Release -o /app/api --no-restore && dotnet publish "src/Ptax.Worker/Ptax.Worker.csproj" -c Release -o /app/worker --no-restore

RUN dotnet tool install --global dotnet-ef --version 10.0.14
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle --project "src/Ptax.Data/Ptax.Data.csproj" -o /app/migrate/efbundle

FROM base AS migrate
WORKDIR /app
COPY --from=build /app/migrate .
USER app
ENTRYPOINT ["sh", "-c", "./efbundle --connection \"$ConnectionStrings__Ptax\""]

FROM base AS api
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/api .
USER app
ENTRYPOINT ["dotnet", "Ptax.Api.dll"]

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS worker
WORKDIR /app
COPY --from=build /app/worker .
USER app
ENTRYPOINT ["dotnet", "Ptax.Worker.dll"]