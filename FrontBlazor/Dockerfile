FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY FrontendBlazorApi.csproj .
RUN dotnet restore FrontendBlazorApi.csproj
COPY . .
RUN dotnet publish FrontendBlazorApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FrontendBlazorApi.dll"]
