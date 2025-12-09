# Build stage - publish Blazor WebAssembly assets
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RestEleven.sln ./
COPY RestEleven.Client/RestEleven.Client.csproj RestEleven.Client/
COPY RestEleven.Shared/RestEleven.Shared.csproj RestEleven.Shared/
RUN dotnet workload install wasm-tools --skip-manifest-update \
	&& dotnet restore RestEleven.Client/RestEleven.Client.csproj

COPY . .
WORKDIR /src/RestEleven.Client
RUN dotnet publish RestEleven.Client.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - serve static files via nginx
FROM nginx:1.27-alpine AS final
COPY RestEleven.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
