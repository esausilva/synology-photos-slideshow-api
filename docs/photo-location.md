# Photo Location

The API implements Google Maps Geocoding API to get the location of a photo from its GPS coordinates.

This feature is disabled by default. To enable it, you will need to set `EnableGeolocation` to `true` in the app settings:

```json
{
  "ThirdPartyServices": {
    "EnableGeolocation": false
  }
}
```

Then provide a Google Maps API key in the `GoogleMapsOptions` setting.

```json
{
  "GoogleMapsOptions": {
    "ApiKey": "<<GOOGLE_MAPS_API_KEY>>",
    "EnableMocks": true
  }
}
```

To get an API key, visit [https://developers.google.com/maps/documentation/geocoding/get-api-key](https://developers.google.com/maps/documentation/geocoding/get-api-key).

Google offers a very generous free tier for this service in the **Pay-as-you-go** plan. 10,000 free Billable events per month. Unless you are refreshing the slideshow feed multiple times a day, for the entire month and every single photo contains GPS metadata, the fee quota is more than enough. [https://developers.google.com/maps/billing-and-pricing/pricing](https://developers.google.com/maps/billing-and-pricing/pricing)

The Geocoding API is only called when tne below conditions are true to return the photo's location. Otherwise, the Geocoding API will never be called and the photo location will be set to an empty string.

- Geolocation is enabled in the app settings
- The photo has GPS metadata 
- This metadata is valid, able to convert to decimal lat/lon degrees

## Mocks

Mocks are enabled by default and the `location` property in the response will always be "(Mock)Nashville, TN" when running in development/**Debug** mode and `EnableGeolocation` is set to `true` in the app settings.

To disable mocks and make the call to the Geocoding API, given you have provided a valid Google Maps API key, set `EnableMocks` to `false`.

Mocks are not enabled in **Release** mode. In fact, mocks are not even wired-up to the DI container in **Release** mode.

## Caching

I have caching setup for the Geocoding API responses based on the lat/lon coordinates as the cache key, i.e., cache key: `36.130944444444445-86.76345555555555`. The cache is set to expire after 7 days.

Cashing happens in-memory with the **HybridCache** library, which was released with .NET 9. So when/if the API is restarted, this in-memory cache will be cleared. 

**HybridCache** can be configured with a distributed cache known as a "secondary cache" which I have plans to introduce Redis as an opt-in feature. The in-memory cache is the primary cache. And by having a secondary cache, the cache is preserved even after the API is restarted.

HybridCache checks the primary cache, if it is a cache miss, it will check the secondary cache. If this is also a cache miss, it will call the Geocoding API and store the response in both cache implementations.
 