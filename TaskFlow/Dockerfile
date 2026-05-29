FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

RUN find . -name "*.csproj"

RUN dotnet restore TaskFlow/TaskFlow.csproj
RUN dotnet publish TaskFlow/TaskFlow.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TaskFlow.dll"]