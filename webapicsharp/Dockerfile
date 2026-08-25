FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish webapicsharp.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
# En contenedores con límite bajo de inotify (como el plan free de Render),
# el watcher nativo de archivos de .NET agota las instancias disponibles y
# crashea al iniciar ("The configured user limit (128) on the number of
# inotify instances..."). Forzar polling evita usar inotify por completo.
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
EXPOSE 8080
ENTRYPOINT ["dotnet", "webapicsharp.dll"]
