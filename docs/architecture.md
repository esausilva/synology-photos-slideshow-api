# Architecture Overview

The Synology Photos Slideshow API follows a decoupled architecture using background workers and channels for long-running photo processing tasks.

## System Architecture

```mermaid
graph TD
    Client[Slideshow Client]
    API[ASP.NET Core API]
    MW[Authentication Middleware]
    EP[Endpoints]
    NAS[Synology NAS]
    FS[Local File System]
    SignalR[SignalR Hub]
    
    subgraph Background Processing
        PhotoChannel[Photo Processing Channel]
        ThumbChannel[Thumbnail Processing Channel]
        PhotoWorker[Photo Processing Worker]
        ThumbWorker[Thumbnail Processing Worker]
        Hangfire[Hangfire Scheduled Job]
    end

    Client -->|HTTP Request| API
    API --> MW
    MW --> EP
    
    EP -->|Search & Download Zip| NAS
    EP -->|Extract & Flatten| FS
    EP -->|Publish| PhotoChannel
    EP -->|Return 204/Metadata| Client

    PhotoWorker -->|Read| PhotoChannel
    PhotoWorker -->|Convert WebP| FS
    PhotoWorker -->|Notify| SignalR
    PhotoWorker -->|Publish| ThumbChannel

    ThumbWorker -->|Read| ThumbChannel
    ThumbWorker -->|Generate Thumbnails| FS
    ThumbWorker -->|Notify| SignalR

    Hangfire -->|Execute| EP
    SignalR -.->|Real-time Updates| Client
```

---

## Call Flows

### 1. Download Photos (`GET /photos/download`)

This flow handles the initial discovery, download, and asynchronous processing of photos.

```mermaid
sequenceDiagram
    participant Client
    participant EP as DownloadPhotos Endpoint
    participant NAS as Synology NAS
    participant FS as Local File System
    participant Ch as PhotoProcessingChannel
    participant Worker as PhotoProcessingWorker
    participant Sig as SignalR Hub

    Client->>EP: GET /photos/download
    EP->>NAS: Search Photos
    NAS-->>EP: File List
    EP->>NAS: Download Zip
    NAS-->>EP: Zip File
    EP->>FS: Extract & Flatten Files
    EP->>Ch: Publish Processing Request
    EP-->>Client: 204 No Content
    
    Note over Worker: Background Execution
    Worker->>Ch: Read Message
    Worker->>FS: Convert Photos to WebP (IPhotosService)
    Worker->>Sig: Invoke RefreshSlideshow
    Sig-->>Client: RefreshSlideshow
    Worker->>FS: Trigger Thumbnail Generation
```

### 2. Get Photo Slides (`GET /photos/slides`)

Retrieves metadata and locations for currently processed photos.

```mermaid
sequenceDiagram
    participant Client
    participant EP as Slides Endpoint
    participant Svc as PhotosService
    participant FS as Local File System
    participant Google as Google Maps API

    Client->>EP: GET /photos/slides
    EP->>Svc: GetSlides()
    Svc->>FS: List WebP Files
    Svc->>FS: Read EXIF (Date/GPS)
    opt Geolocation Enabled
        Svc->>Google: Reverse Geocode GPS
        Google-->>Svc: City, State
    end
    Svc-->>EP: List of SlideResponse
    EP-->>Client: 200 OK (JSON)
```

### 3. Get Thumbnails (`GET /photos/thumbnails`)

Retrieves the list of available thumbnail URLs.

```mermaid
sequenceDiagram
    participant Client
    participant EP as Thumbnails Endpoint
    participant Svc as PhotosService
    participant FS as Local File System

    Client->>EP: GET /photos/thumbnails
    EP->>Svc: GetThumbnails()
    Svc->>FS: List *__thumb.webp Files
    Svc-->>EP: List of Strings
    EP-->>Client: 200 OK (JSON)
```

### 4. Bulk Delete Photos (`POST /photos/bulk-delete`)

Deletes specific photos from the local cache.

```mermaid
sequenceDiagram
    participant Client
    participant EP as DeletePhotos Endpoint
    participant Svc as PhotosService
    participant FS as Local File System
    participant Sig as SignalR Hub

    Client->>EP: POST /photos/bulk-delete (PhotoNames)
    EP->>Svc: GetSlides() to verify existence
    EP->>FS: Delete Files
    EP->>Sig: Invoke RefreshSlideshow
    Sig-->>Client: RefreshSlideshow
    EP-->>Client: 200 OK (Unmatched List)
```

### 5. Scheduled Job (`Hangfire`)

Automated weekly download flow.

```mermaid
sequenceDiagram
    participant Job as PhotoDownloadJob
    participant NAS as Synology NAS
    participant FS as Local File System
    participant Sig as SignalR Hub
    participant Ch as ThumbnailChannel

    Job->>NAS: Authenticate & Search
    Job->>NAS: Download & Extract
    Job->>FS: Convert to WebP
    Job->>Sig: Invoke RefreshSlideshow
    Job->>Ch: Publish Thumbnail Request
```
