# Synology Photos Slideshow API

> An API to randomly fetch and serve images from Synology NAS devices, optimized for use in slideshow applications.

The API is meant to be deployed on a Synology NAS device on your local network.

I have the API deployed and tested in Synology Container Manager, but it should work on any Docker host. i.e. Portainer.

## Endpoints

The base URL is `http://<your-nas-ip>:5000`. Unless you have changed the default port, then it will be `http://<your-nas-ip>:<your-port>`.

The API has two endpoints: Download Photos and Get Photo URLs.

### Download Photos

```text
/download-photos
```

This endpoint randomly selects and downloads photos from a specified folder, or folders, on your Synology NAS device.

*MORE TO COME*

### Get Photo URLs

```text
/get-photo-urls
```

*MORE TO COME*

## Deployment

*MORE TO COME*

