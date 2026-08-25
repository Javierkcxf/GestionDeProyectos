FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY webapicsharp.csproj .
RUN dotnet restore webapicsharp.csproj
COPY . .
RUN dotnet publish webapicsharp.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
# Evita que .NET intente vigilar appsettings.json con inotify: en contenedores
# con límites bajos (como el plan free de Render) eso provoca un crash al iniciar
# ("The configured user limit (128) on the number of inotify instances...").
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
EXPOSE 8080
ENTRYPOINT ["dotnet", "webapicsharp.dll"]
