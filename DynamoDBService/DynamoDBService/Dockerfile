# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DynamoDBService.csproj", "."]
RUN dotnet restore "DynamoDBService.csproj"
COPY . .
RUN dotnet build "DynamoDBService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DynamoDBService.csproj" -c Release -o /app/publish

# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DynamoDBService.dll"]