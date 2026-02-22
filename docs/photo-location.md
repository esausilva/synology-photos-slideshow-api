# Photo Location

The API can look up a human-readable location (city/state) from a photo's GPS metadata using **Google Maps Geocoding API**.

This feature is **disabled by default**.

## Enable geolocation

```json
{
  "ThirdPartyServices": {
    "EnableGeolocation": true
  },
  "GoogleMapsOptions": {
    "ApiKey": "<<GOOGLE_MAPS_API_KEY>>",
    "EnableMocks": true
  }
}
```

Notes:
- `GoogleMapsOptions.ApiKey` is required by validation even if geolocation is disabled. Use any non-empty value for local dev.
- `EnableMocks` only applies in **Debug** builds.

To get an API key, visit [https://developers.google.com/maps/documentation/geocoding/get-api-key](https://developers.google.com/maps/documentation/geocoding/get-api-key).

Google offers a very generous free tier for this service in the **Pay-as-you-go** plan. 10,000 free Billable Events per month. Unless you are refreshing the slideshow feed multiple times a day, for the entire month and every single photo contains GPS metadata, the fee quota is more than enough. [https://developers.google.com/maps/billing-and-pricing/pricing](https://developers.google.com/maps/billing-and-pricing/pricing)

## When the API calls Google

The Geocoding API is called only if **all** of these are true:

- Geolocation is enabled
- The photo has GPS metadata
- The GPS metadata can be converted to decimal latitude/longitude

Otherwise, `location` is returned as an empty string.

## Mocks

In **Debug** builds, if `EnableMocks` is `true`, the API returns `(Mock)Nashville, TN` and does not call Google.

Mocks are not wired in **Release** builds.

To disable mocks and make the call to the Geocoding API, given you have provided a valid Google Maps API key, set `GoogleMapsOptions.EnableMocks` to `false`.

## Caching

Geocoding responses are cached for 10 days using .NET HybridCache:

- L1 (memory) is always used.
- L2 (distributed) is used only when Redis is enabled.

The L1 cache clears upon restart of the API. The L2 cache is persisted across restarts of the API.

The cache key is the concatenated latitude/longitude string: `36.130944444444445-86.76345555555555`.

The way it works is `HybridCache` checks the L1 cache first, if it is a cache miss, it will check the L2 cache next. If this is also a cache miss, it will call the Geocoding API and store the response in both cache implementations.

I have configured Redis as the L2 cache. Refer to [Redis](redis.md) for more details.
