# Photo Location

The Synology Photos Slideshow API implements **Google Maps Geocoding API** to get the location of a photo from its GPS coordinates.

This feature is **disabled by default**. To enable it, you will need to set `ThirdPartyServices.EnableGeolocation` to `true` in the app settings:

```json
{
  "ThirdPartyServices": {
    "EnableGeolocation": false
  }
}
```

Then provide a Google Maps API key in the `GoogleMapsOptions.ApiKey` setting.

```json
{
  "GoogleMapsOptions": {
    "ApiKey": "<<GOOGLE_MAPS_API_KEY>>",
    "EnableMocks": true
  }
}
```

To get an API key, visit [https://developers.google.com/maps/documentation/geocoding/get-api-key](https://developers.google.com/maps/documentation/geocoding/get-api-key).

Google offers a very generous free tier for this service in the **Pay-as-you-go** plan. 10,000 free Billable Events per month. Unless you are refreshing the slideshow feed multiple times a day, for the entire month and every single photo contains GPS metadata, the fee quota is more than enough. [https://developers.google.com/maps/billing-and-pricing/pricing](https://developers.google.com/maps/billing-and-pricing/pricing)

The Geocoding API is _only_ called when tne following conditions are met. Otherwise, the Geocoding API will never be called and the photo location will be set to an empty string.

- Geolocation is enabled in the app settings
- The photo has GPS metadata 
- This metadata is valid and able to convert to decimal lat/lon degrees

## Mocks

Mocks are enabled by default and the `location` property in the response will always be "(Mock)Nashville, TN" when running in development/**Debug** and the above riteria is met.

To disable mocks and make the call to the Geocoding API, given you have provided a valid Google Maps API key, set `GoogleMapsOptions.EnableMocks` to `false`.

Mocks are not enabled in **Release** mode. In fact, mocks are not even wired-up to the DI container in **Release** mode.

## Caching

I have caching setup for the Geocoding API responses based on the lat/lon coordinates as the cache key, e.g., cache key: `36.130944444444445-86.76345555555555`. The cache is set to expire after 10 days.

Caching is configured with the `HybridCache` library (introduced in .NET 9) which uses a two-tier caching strategy:

1. L1 (Local/Memory): Fast, per-instance cache
2. L2 (Distributed): Shared cache across all instances (e.g., Redis)

The L1 cache clears upon restart of the API. The L2 cache is persisted across restarts of the API.

The way it works is `HybridCache` checks the L1 cache first, if it is a cache miss, it will check the L2 cache next. If this is also a cache miss, it will call the Geocoding API and store the response in both cache implementations.

I have configured Redis as the L2 cache. Refer to [Redis](redis.md) for more details.
 