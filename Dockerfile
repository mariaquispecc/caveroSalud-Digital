FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY *.sln ./
COPY src/CaveroSalud.Api/*.csproj ./src/CaveroSalud.Api/
RUN dotnet restore src/CaveroSalud.Api/CaveroSalud.Api.csproj

COPY . ./
RUN dotnet publish src/CaveroSalud.Api/CaveroSalud.Api.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:${PORT:-5000}

ENTRYPOINT ["dotnet", "CaveroSalud.Api.dll"]
