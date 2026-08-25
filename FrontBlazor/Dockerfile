FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# No se separa el restore (copiando solo el .csproj primero) como optimización
# de caché de capas: los "static web assets" de Blazor (blazor.web.js, etc.)
# se resuelven durante el restore/build a partir del árbol completo del
# proyecto (wwwroot incluido), así que con --no-restore tras un restore hecho
# sin wwwroot presente el publish queda sin esos archivos (404 en runtime).
COPY . .
RUN dotnet publish FrontendBlazorApi.csproj -c Release -o /app

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
ENTRYPOINT ["dotnet", "FrontendBlazorApi.dll"]
