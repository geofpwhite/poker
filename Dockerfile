# Use the official .NET image as a base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Copy the entire Poker directory
COPY Poker/ ./Poker/ 
WORKDIR /src/Poker
RUN dotnet restore "Poker.csproj"
RUN dotnet build "Poker.csproj" -c Release -o /app/build
# Add a stage to build the frontend
FROM node:23.9-alpine AS frontend-build
WORKDIR /frontend
COPY Poker.Frontend/poker-app/package*.json ./
RUN npm install
COPY Poker.Frontend/poker-app/ ./
RUN npm run build

# Publish the application
FROM build AS publish
RUN dotnet publish "Poker.csproj" -c Release -o /app/publish

# Copy the built frontend into the backend's wwwroot
RUN mkdir -p /app/publish/wwwroot
COPY --from=frontend-build /frontend/dist /app/publish/wwwroot

# Use the base image to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Poker.dll"]
