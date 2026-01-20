FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5097
EXPOSE 7078

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Synology.Photos.Slideshow.Api/Synology.Photos.Slideshow.Api.csproj", "src/Synology.Photos.Slideshow.Api/"]
RUN dotnet restore "src/Synology.Photos.Slideshow.Api/Synology.Photos.Slideshow.Api.csproj"
COPY . .
WORKDIR "/src/src/Synology.Photos.Slideshow.Api"
RUN dotnet build "Synology.Photos.Slideshow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Synology.Photos.Slideshow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# The container will run as root for now as it needs read access to the mounted volumes in the NAS. Will fix later.
#RUN mkdir -p /app/slides && chown -R $APP_UID:$APP_UID /app/slides
#RUN mkdir -p /app/logs && chown -R $APP_UID:$APP_UID /app/logs
#USER $APP_UID
RUN mkdir -p /app/slides
RUN mkdir -p /app/logs
ENTRYPOINT ["dotnet", "Synology.Photos.Slideshow.Api.dll"]
