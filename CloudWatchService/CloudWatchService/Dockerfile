# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CloudWatchService.csproj", "."]
RUN dotnet restore "CloudWatchService.csproj"
COPY . .
RUN dotnet build "CloudWatchService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CloudWatchService.csproj" -c Release -o /app/publish

# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CloudWatchService.dll"]