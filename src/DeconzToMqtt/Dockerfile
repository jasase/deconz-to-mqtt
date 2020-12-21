#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "DeconzToMqtt/DeconzToMqtt.csproj"
WORKDIR "/src/DeconzToMqtt"
RUN dotnet build "DeconzToMqtt.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeconzToMqtt.csproj" -c Release -o /app/publish

FROM base AS final
RUN mkdir /logs
RUN mkdir /configuration
VOLUME /logs /configuration
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeconzToMqtt.dll"]