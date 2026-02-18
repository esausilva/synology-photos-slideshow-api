# Redis

_Refer to [Photo location](photo-location.md) first if you haven't already, then come back here._

Redis is completely optional and is **disabled by default**. To enable it, you will need to set `ThirdPartyServices.EnableDistributedCache` to `true` in the app settings:

```json
{
  "ThirdPartyServices": {
    "EnableDistributedCache": false
  }
}
```

Leaving it disabled will not affect the API in any way, and the API will only have the L1 cache available.

If you choose to enable Redis, you will need to provide a connection string in the `ConnectionStrings.Redis` app setting.

```json
{
  "ConnectionStrings": {
    "Redis": "<<REDIS_CONNECTION_STRING>>"
  }
}
```

**Note**: I am using Redis 8.6.0, but you can use whatever version you like.

## Running Redis locally

Running Redis locally as a stand-alone Docker container, the connection string will look something like this:

```text
localhost:6379,abortConnect=false,connectTimeout=10000
```

And to get it up and running, run the following command:

```shell
docker run -d --name redis -p 6379:6379 -v redis-data:/data redis:8.6.0-alpine redis-server --appendonly yes -d
```

I have Redis configured in the `docker-compose.yml` file, and an override in the `docker-compose.local.yml` file that will use the `redis.slideshow` network if you run this API with Docker Compose.

In this case, the connection string will look something like this:

```text
redis.slideshow:6379,abortConnect=false,connectTimeout=10000
```

Refer to [Docker Local](../README.md#docker-local) for a sample of the `docker-compose.local.yml` file and the commands to build and run the API with Docker Compose.

## Running Redis on your Synology NAS

From Synology Container Manager, click on the "**Registry**" tab and search for the appropriate repository and image.

Right-click on the image and select "Download this image" then select "8.6.0-alpine".

![Registry Search redis](./registry-search-redis.jpg "Registry Search redis")

Once the image is downloaded, you can create a container from it by going to the "**Image**" tab, then right-clicking on the image, and selecting "**Run**".

![Redis Docker Image](./redis-docker-image.jpg "Redis Docker Image")

From there, you can configure the container. In the first screen you will need to set the container name, I would suggest checking-off the "**Enable auto-restart**" option.

On the second screen, configure the local (to the NAS) ports, which in this case is `6379`.

Setting up volumes is optional, but you can configure them to store the cache data on your NAS. You will need to create the folders at your desired location in the NAS with File Station, then map them to the container by clicking the "**Add Folder**" button under the "**Volume Settings**" heading.

The volume map in the container will  be `/data`, make sure you assign Read/Write permissions to the volume.

One last configuration if you choose to store the cache data on your NAS, assign the following command to the "**Command**" field under "**Execute Command**" heading:

```text
redis-server --appendonly yes
```

Now that you have the container running in your NAS, the connection string will look something like this:

```text
[[SERVER_IP]]:6379,abortConnect=false,connectTimeout=10000
```

Replace `[SERVER_IP]` with the IP address of your Synology NAS.

Refer to [Deployment To Your Synology NAS Device](../README.md#deployment-to-your-synology-nas-device) for API environment variable configuration and more details.

## Another Redis Desktop Manager (ARDM)

As a side note, I am using [Another Redis Desktop Manager](https://goanother.com/), a cross-platform GUI client for Redis.

It is free and open-source, but support the project if you are able to.
