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

# Publish the application
FROM build AS publish
RUN dotnet publish "Poker.csproj" -c Release -o /app/publish

# Use the base image to run the application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Poker.dll"]