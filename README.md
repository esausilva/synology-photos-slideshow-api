# Synology Photos Slideshow API

> An API to randomly fetch and serve images from Synology NAS devices, optimized for use in slideshow applications.

The idea behind this API is to download a set of photos picked at random from your Synology NAS device to be displayed in a client slideshow application.

This set of photos can be refreshed at any time by calling the [Download Photos](#download-photos) endpoint.

The API is meant to be deployed on a Synology NAS device on your local network and accessed from within your local network. This is to keep everything private on your own cloud.

I have the API deployed and tested in Synology Container Manager, but it should work on any Docker host. e.g. Portainer.

Table of Contents:

  - [Endpoints](#endpoints)
    - [Download Photos](#download-photos)
    - [Get Photo Slides](#get-photo-slides)
    - [Bulk Delete Photos](#bulk-delete-photos)
  - [Logging](#logging)
  - [Local Development](#local-development)
  - [Docker Local](#docker-local)
  - [Deployment To Your Synology NAS Device](#deployment-to-your-synology-nas-device)
  - [Important !!!!!!!](#important)
  - [Future Enhancements](#future-enhancements)
  - [Client App](#client-app)
  - [Shameless Plug](#shameless-plug)
  - [Giving Back](#giving-back)

---

## Endpoints

The API exposes two ports: **5097** for **HTTP** and **7078** for **HTTPS**. (HTTPS only working locally for now)

The API exposes the following endpoints:

- Download Photos
- Get Photo Slides
- Bulk Delete Photos

The base URL is `http://<your-nas-ip>:5097`. When running the API for local development, replace the NAS IP with `localhost`.

### Download Photos

```text
GET /photos/download
```

This endpoint randomly selects, downloads, and converts the downloaded photos to WebP format.

The photos are taken from a configured folder(s) on your Synology NAS device, these downloads are then placed in a configured folder where the API has access to. Typically, the photos would be located on the `/photo` volume on your NAS or your home share. e.g. `/[username]/Photos/PhotoLibrary`.  

Every time this endpoint is called, it will clean the previously downloaded photos and download a new set of photos.

Refer to "[Local Development](#local-development)", "[Docker Local](#docker-local)", and "[Deployment](#deployment-to-your-synology-nas-device)" for more information on configuration.

**Note 1:** There is an issue with the number of photos to download. It seems to be limited to 79; however, this number limit works for now. I will look into this later and try to figure this out.

**Note 2:** This is a process-intensive endpoint as it searches the Synology NAS, downloads the photos, processes them into a flattened hierarchy, and then converts them to WebP format. I will break these up later into background services after implementing real-time client notifications.

#### Response Codes

| Status Code | Description                                                                                                            |
| :---------- |:-----------------------------------------------------------------------------------------------------------------------|
| `204`       | Success                                                                                                                |
| `503`       | This is returned if the API is unable to download photos due to issues with the official Synology API. e.g., timeouts. |
| `500`       | Any other unexpected errors.                                                                                           |

An example of the error response:

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc9110#status.503",
  "title": "Failed to download photos.",
  "status": 503,
  "detail": "Search operation timed out after 10 attempts",
  "traceId": "00-d1f393c5f6b5377af412bee5a15cd61d-89602e76756bbe5c-00"
}
```

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc9110#status.503",
  "title": "An error occured",
  "status": 500,
  "detail": "[Error message]",
  "traceId": "00-d1f393c5f6b5377af412bee5a15cd61d-89602e76756bbe5c-00"
}
```

### Get Photo Slides

```text
GET /photos/slides
```

This endpoint returns a collection of slides with info about the photos previously downloaded by the [Download Photos](#download-photos) endpoint with the following properties:

| Property         | Description                                                                                                                                                                                                                                                                                                                                                                     |
| :--------------- |:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `relativeUrl`    | The relative photo URL. To get the full URL of the photo, the client application needs to concatenate the base URL with this value.                                                                                                                                                                                                                                             |
| `dateTaken`      | The date the photo was taken.<br>This value can be empty if the photo does not have the "Date Time" metadata.<br>If there is no date time offset in the photo metadata, it will return as unspecified and the client application will translate to local timezone.                                                                                                              |
| `googleMapsLink` | A link to the photo location on Google Maps.<br>This value can be empty if the photo does not have GPS metadata.                                                                                                                                                                                                                                                                |
| `location`       | The photo location in the following format: _City, State_.<br>This value can be empty if the photo does not have GPS metadata.<br>I implement **Google Maps API** to get the location from the photo metadata, which is opt-in and disabled by default, and the API will return empty if not enabled. Refer to [Photo Location](./docs/photo-location.md) for more information. |

#### Response Codes

| Status Code | Description                                                      |
| :---------- |:-----------------------------------------------------------------|
| `200`       | Success                                                          |
| `500`       | Any unexpected errors. _Refer to the previous example response_ |

An example of the success response:

```json
[
  {
    "relativeUrl": "/slideshow/20250723_135938.webp",
    "dateTaken": "2025-07-23 13:59:38 -07:00",
    "googleMapsLink": "https://www.google.com/maps?q=37.7493922,-119.5492962",
    "location": "Yosemite Valley, CA"
  },
  {
    "relativeUrl": "/slideshow/IMG_20200323_083612.webp",
    "dateTaken": "2020-03-23 08:36:12 -05:00",
    "googleMapsLink": "",
    "location": ""
  }
]
```

Example of the full photo's URL:

```text
http://<your-nas-ip>:5097/slideshow/20240618_141316.jpg
```

### Bulk Delete Photos

```text
POST /photos/bulk-delete
```

This endpoint deletes photos from the slideshow folder. 

Accepts the list of photo names to delete.

Payload example:

```js
[
  "20240303_154856.jpg",
  "20240818_154700.jpg",
  "20250403_080106.jpg"
]
```

The endpoint will attempt to delete all the photos in the payload and return the list of photos that were not found. In case where all the photos were not found, it will return a `404 Not Found` error.

#### Response Codes

| Status Code | Description                                                                                            |
| :---------- |:-------------------------------------------------------------------------------------------------------|
| `200`       | Success. Will return the list of photos that were not found. Or an empty list if all photos were found |
| `404`       | Not Found. This is returned if all photos were not found                                               |
| `500`       | Any unexpected errors. _Refer to the previous example response_                                        |

An example of the success response:

```json
{
  "unmatchedPhotos": [
    "20240818_154700.jpg",
    "20250403_080106.jpg"
  ]
}
```

```json
{
  "unmatchedPhotos": []
}
```

## Logging

The API logs to a file in a `logs` folder, and will create it relative to the deployment root if it doesn't exist; it will also place a JSON file in the `logs` folder per day.

The file name format is `api-logs_20251006.json`.

I might switch this to a database in the future. But for now, it's good enough.

## Local Development

Update the following app settings in `appsettings.json` or create a .NET User Secrets (Secret Manager) file:

```json
{
  "UriBase": {
    "ServerIpOrHostname": "<<SERVER_IP_OR_HOSTNAME>>",
    "Port": 5000
  },
  "SynologyUser": {
    "Account": "<<ACCOUNT>>",
    "Password": "<<PASSWORD>>"
  },
  "SynoApiOptions": {
    "FileStationSearchFolders": [
      "/path/to/server/photos"
    ],
    "NumberOfPhotoDownloads": 10,
    "DownloadAbsolutePath": "/path/to/slideshow/downloads"
  },
  "ThirdPartyServices": {
    "EnableGeolocation": true,
    "EnableDistributedCache": true
  },
  "GoogleMapsOptions": {
    "ApiKey": "<<GOOGLE_MAPS_API_KEY>>",
    "EnableMocks": true
  },
  "ConnectionStrings": {
    "Redis": "<<REDIS_CONNECTION_STRING>>"
  }
}
```

| Configuration Key | Description | Example/Default Value                                                            |
|------------------|-------------|----------------------------------------------------------------------------------|
| `UriBase.ServerIpOrHostname` | The IP or hostname of your Synology NAS device | e.g., `192.168.1.100` or `localhost`                                             |
| `UriBase.Port` | The port for Synology NAS devices | **5000** (default). If you are using a different port, update this value to match. |
| `SynologyUser.Account` | The username for your Synology NAS device | Main account or service account with file access privileges                      |
| `SynologyUser.Password` | The password for your Synology NAS device | Your account password                                                            |
| `SynoApiOptions.FileStationSearchFolders` | List of folders on your Synology NAS to search for photos | Must be absolute paths (e.g., `/photo/family`)                                   |
| `SynoApiOptions.NumberOfPhotoDownloads` | The number of photos to download | Any integer value                                                                |
| `SynoApiOptions.DownloadAbsolutePath` | The absolute path to download photos to | Must exist before API starts, or it will throw exception at bootup               |
| `ThirdPartyServices.EnableGeolocation` | Enable Google Maps API to get photo location | **false** (default). |
| `ThirdPartyServices.EnableDistributedCache` | Enable Redis distributed cache to speed up photo location lookup | **false** (default). |
| `GoogleMapsOptions.ApiKey` | Google Maps API key | Your API key. |
| `GoogleMapsOptions.EnableMocks` | Enable mock Google Maps API responses for testing | **true** (default). |
| `ConnectionStrings.Redis` | Redis connection string | e.g.,`localhost:6379,abortConnect=false,connectTimeout=10000` |

Refer to [Endpoints](#endpoints) on how to call the API endpoints.

Refer to [Photo Location](./docs/photo-location.md) for more information about getting the Google Maps API key and third party services/distributed cache.

## Docker Local

The `dockerfile` has instructions to create the `SynoApiOptions.DownloadAbsolutePath` folder. It will create it at the root of the deployment folder: `/app/slides`.

The `docker-compose.yml` file is already setting up an environment variable for the `SynoApiOptions.DownloadAbsolutePath` folder pointing to `/app/slides`.

I suggest creating a `docker-compose.local.yml` file to override some of the other app settings variables. Which is what I am doing, but not including in the repo.

This is a sample of the `docker-compose.local.yml` file:

```yaml
services:
  synology.photos.slideshow.api:
    image: esausilva/synology.photos.slideshow.api:local
    environment:
      - UriBase:ServerIpOrHostname=<<SERVER_IP_OR_HOSTNAME>>
      - UriBase:Port=<<CUSTOM_PORT>>
      - SynologyUser:Account=<<ACCOUNT>>
      - SynologyUser:Password=<<PASSWORD>>
      - SynoApiOptions:FileStationSearchFolders:0=<<PATH_TO_PHOTOS_FOLDER_IN_NAS>>
      - SynoApiOptions:FileStationSearchFolders:1=<<PATH_TO_PHOTOS_FOLDER_IN_NAS>> ## If you have more than one folder to search
      - SynoApiOptions:NumberOfPhotoDownloads=10
      - ThirdPartyServices:EnableGeolocation=true
      - ThirdPartyServices:EnableDistributedCache=true
      - GoogleMapsOptions:ApiKey=<<GOOGLE_MAPS_API_KEY>>
      - ConnectionStrings:Redis=redis.slideshow:6379,abortConnect=false,connectTimeout=10000
    volumes:
      - ./.slides:/app/slides
      - ./.logs:/app/logs
    depends_on:
      redis.slideshow:
        condition: service_healthy

  redis.slideshow:
    healthcheck:
      test: [ "CMD", "redis-cli", "ping" ]
      interval: 5s
      timeout: 3s
      retries: 5
    command: redis-server --appendonly yes
    volumes:
      - ./.redis-data:/data
```

Volumes are optional, but I find them useful to be able to access the downloaded photos and logs.

Refer to [Photo Location](./docs/photo-location.md) for more information about getting the Google Maps API key and third party services/distributed cache.

To build the image, run the following command:

```shell
docker-compose -f docker-compose.yaml -f docker-compose.local.yaml build
```

To create the container and start it, run the following command:

```shell
docker-compose -f docker-compose.yaml -f docker-compose.local.yaml up -d
```

`-d` is optional if you want to run/create the container as a detached (background) process.

Note: It would be a good idea to rename the image in both Docker compose files and remove my name from the image name.

## Deployment To Your Synology NAS Device

Two options:

1. Download the latest image from my Docker Hub Repo: [esausilva/synology.photos.slideshow.api](https://hub.docker.com/r/esausilva/synology.photos.slideshow.api).
2. Build the image yourself and push it to your own Docker Hub repository. Following this route, you will need to rename the image to match your repository in the `docker-compose.yml` file. 

**For option 2:**

Run the following command to build the image:

```shell
docker-compose build
```

This will take the default docker compose file, `docker-compose.yaml`, and build the image, skipping the local docker compose file, `docker-compose.local.yml`.

Run the following command to push the image to your Docker Hub repository:

```shell
docker push [your-repo]/synology.photos.slideshow.api:latest
```

**For both options:**

From Synology Container Manager, click on the "**Registry**" tab and search for the appropriate repository and image.

Right-click on the image and select "Download this image".

![Registry Search esausilva](./docs/registry-search-esausilva.jpg "Registry Search esausilva")

Once the image is downloaded, you can create a container from it by going to the "**Image**" tab, then right-clicking on the image, and selecting "**Run**".

![Synology Photos Slideshow API Docker Image](./docs/synology-photo-slideshow-api-docker-image.jpg "Synology Photos Slideshow API Docker Image")

From there, you can configure the container. In the first screen you will need to set the container name, I would suggest checking-off the "**Enable auto-restart**" option.

On the second screen, configure the local (to the NAS) ports. You can choose to use the default ports, or you can change them to whatever you want. Just be mindful that the API endpoint ports will need to match the ports you configure here.

Setting up volumes is optional, but I find them useful to be able to access the downloaded photos and logs. You will need to create the folders at your desired location in the NAS with File Station, then map them to the container by clicking the "**Add Folder**" button under the "**Volume Settings**" heading.

The volume maps in the container will  be `/app/slides` and `/app/logs`, make sure you assign Read/Write permissions to the volumes.

Finally, you need to configure the environment variables under the "**Environment**" heading.

The environment variables will be as follows:

| **Environment Variable**                  | **Value**                                                  |
| ----------------------------------------- | ---------------------------------------------------------- |
| ASPNETCORE_URLS                           | http://+:5097                                              |
| UriBase:ServerIpOrHostname                | [[SERVER_IP]]                                              |
| UriBase:Port                              | 5000                                                       |
| SynologyUser:Account                      | [[ACCOUNT]]                                                |
| SynologyUser:Password                     | [[PASSWORD]]                                               |
| SynoApiOptions:FileStationSearchFolders:0 | [[PATH_TO_PHOTOS_FOLDER_IN_NAS]]                           |
| SynoApiOptions:NumberOfPhotoDownloads     | 79                                                         |
| SynoApiOptions:DownloadAbsolutePath       | /app/slides                                                |
| ThirdPartyServices:EnableGeolocation      | true                                                       |
| ThirdPartyServices:EnableDistributedCache | true                                                       |
| GoogleMapsOptions:ApiKey                  | [[GOOGLE_MAPS_API_KEY]]                                    |
| ConnectionStrings:Redis                   | [[SERVER_IP]]:6379,abortConnect=false,connectTimeout=10000 |

Refer to [Photo Location](./docs/photo-location.md) for more information about getting the Google Maps API key and third party services/distributed cache.

## Important!!!!!!!

I highly suggest you create a DHCP reservation in your router for the IP address of your Synology NAS device.

This will make the IP predictable and not change every time your NAS restarts, or DHCP assigns a new IP address.

## Future Enhancements

I would like to add the following features (in no particular order):

| Feature                     | Description                                                                                                                                     | Status |
| :-------------------------- |:------------------------------------------------------------------------------------------------------------------------------------------------|:-------|
| **Scheduled Jobs**          | Automates downloading new photo sets in the background at set intervals.                                                                        |        |
| **Real-time Notifications** | Uses SignalR or SSE to notify the client when new photos are available. A predecessor to this is to have the background job feature completed.  |        |
| **Permanent Folder**        | A dedicated folder for specific photos (e.g., recent trips) that bypasses the auto-clean process.                                               |        |
| **Delete Endpoint**         | Allows removing specific photos from the slideshow cache without deleting the original NAS files.                                               | ✅     |
| **Metadata Refactoring**    | Updates endpoints to include photo date, location, and mapping data.                                                                            | ✅     |
| **Blacklist System**        | An endpoint to permanently prevent specific photos from appearing in the slideshow.                                                             |        |
| **Download Configuration**  | Enables the client application to define how many photos are fetched.                                                                           |        |

What else? Will see...

## Client App

The web client app is now available at: [Synology Photos Slideshow Client](https://github.com/esausilva/synology-photos-slideshow-client)

## Shameless Plug

I am using my own **Synology API SDK** to do the heavy lifting of interacting with the official Synology API to fetch the photos and request the download.

Check it out: 

- GitHub Repo: [Synology API SDK](https://github.com/esausilva/synology-api-sdk)
- NuGet Package: [Synology.API.SDK](https://www.nuget.org/packages/Synology.API.SDK)

## Giving Back

If you find this project useful in any way, consider getting me a coffee by clicking on the image below. I would really appreciate it!

[![Buy Me A Coffee](https://www.buymeacoffee.com/assets/img/custom_images/black_img.png)](https://www.buymeacoffee.com/esausilva)
