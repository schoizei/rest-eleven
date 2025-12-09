# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RestEleven.sln ./
COPY RestEleven.PersonioBridge/RestEleven.PersonioBridge.csproj RestEleven.PersonioBridge/
COPY RestEleven.Shared/RestEleven.Shared.csproj RestEleven.Shared/
RUN dotnet restore RestEleven.PersonioBridge/RestEleven.PersonioBridge.csproj

COPY . .
WORKDIR /src/RestEleven.PersonioBridge
RUN dotnet publish RestEleven.PersonioBridge.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "RestEleven.PersonioBridge.dll"]
