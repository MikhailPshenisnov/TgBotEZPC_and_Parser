FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["TGBotEZPC/TGBotEZPC.csproj", "TGBotEZPC/"]
RUN dotnet restore "TGBotEZPC/TGBotEZPC.csproj"
COPY . .
WORKDIR "/src/TGBotEZPC"
RUN dotnet build "TGBotEZPC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TGBotEZPC.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TGBotEZPC.dll"]
