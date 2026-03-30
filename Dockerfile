# Stage 1: Build React frontend
FROM node:22-slim AS frontend-build
WORKDIR /app/client
COPY client/package.json ./
RUN npm install
COPY client/ ./
RUN npm run build

# Stage 2: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /app
COPY src/Epsilon.Core/Epsilon.Core.csproj src/Epsilon.Core/
COPY src/Epsilon.Web/Epsilon.Web.csproj src/Epsilon.Web/
RUN dotnet restore src/Epsilon.Web/Epsilon.Web.csproj
COPY src/Epsilon.Core/ src/Epsilon.Core/
COPY src/Epsilon.Web/ src/Epsilon.Web/
RUN dotnet publish src/Epsilon.Web/Epsilon.Web.csproj -c Release -o /out --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy backend
COPY --from=backend-build /out ./

# Copy frontend build to wwwroot
COPY --from=frontend-build /app/client/dist ./wwwroot/

# Cloud Run uses PORT env var
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "Epsilon.Web.dll"]
