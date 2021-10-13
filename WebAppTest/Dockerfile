FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app
EXPOSE 8082/tcp

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["WebAppTest/WebAppTest.csproj", "WebAppTest/"]
RUN dotnet restore "WebAppTest/WebAppTest.csproj"
COPY . .
WORKDIR "/src/WebAppTest"
RUN dotnet build "WebAppTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAppTest.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppTest.dll"]
