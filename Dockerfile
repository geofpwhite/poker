# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Copy the entire Poker directory
COPY Poker/ ./Poker/ 
WORKDIR /src/Poker
RUN dotnet restore "Poker.csproj"
RUN dotnet publish "Poker.csproj" -c Release -o /app/publish

# Add a stage to build the frontend
FROM node:23.9-alpine AS frontend-build
WORKDIR /frontend
COPY Poker.Frontend/poker-app/package*.json ./
RUN npm install
COPY Poker.Frontend/poker-app/ ./
RUN npm run build

# Copy the built frontend into the backend's wwwroot
FROM build AS publish
RUN mkdir -p /app/publish/wwwroot
COPY --from=frontend-build /frontend/dist /app/publish/wwwroot
COPY Poker/wwwroot/cards /app/publish/wwwroot/cards

# Create the final image with only the executable and required files
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Poker.dll"]
