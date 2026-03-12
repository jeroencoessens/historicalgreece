# Historical Greece — AR Tourism App

## Architecture Overview

Mobile AR app for historical Greek (and future) architecture tourism. Two core modes:

### 1. On-Location AR
GPS detects you're near a curated site → tap to see the full-scale AR reconstruction overlaid on the real ruins.

### 2. Remote Preview
Browse the catalog from home → tap any site → place a scaled 3D model on your table to explore before you travel.

---

## Project Structure

```
Assets/HistoricalGreece/
├── Scripts/
│   ├── HistoricalGreece.asmdef        # Main assembly definition
│   ├── AppManager.cs                   # Top-level coordinator (entry point)
│   ├── Core/
│   │   ├── HistoricalSite.cs           # ScriptableObject: site data model
│   │   └── SiteDatabase.cs             # ScriptableObject: queryable site collection
│   ├── Location/
│   │   ├── LocationService.cs          # GPS + compass with permission handling
│   │   └── ProximityDetector.cs        # Geofencing & nearby site detection
│   ├── AR/
│   │   ├── ARExperienceManager.cs      # On-location & preview AR controller
│   │   └── PreviewPlacementManager.cs  # Tap-to-place, pinch-to-scale for preview
│   ├── UI/
│   │   ├── Navigation/
│   │   │   └── AppNavigationManager.cs # Tab bar + screen stack navigation
│   │   ├── Screens/
│   │   │   ├── ExploreScreenManager.cs # Browse/filter catalog
│   │   │   ├── NearbyScreenManager.cs  # GPS-sorted nearby sites
│   │   │   ├── SiteDetailScreenManager.cs # Full site detail view
│   │   │   ├── ARHUDScreenManager.cs   # Minimal AR overlay controls
│   │   │   └── WelcomeScreenManager.cs # First-launch onboarding
│   │   └── Components/
│   │       ├── SiteCard.cs             # Reusable site card UI component
│   │       └── NotificationBanner.cs   # Proximity alert banner
│   ├── Data/
│   │   └── SampleSiteData.cs           # 7 real Greek sites with coordinates
│   └── Editor/
│       ├── HistoricalGreece.Editor.asmdef
│       └── SiteCreatorEditor.cs        # Menu tool to generate sample sites
└── Data/
    ├── Sites/                          # Generated ScriptableObject instances
    └── SiteDatabase.asset              # Generated database
```

---

## Getting Started

### 1. Generate Sample Data
In Unity: **Historical Greece > Create Sample Sites**
This creates 7 real Greek historical sites (Parthenon, Knossos, Delphi, etc.) as ScriptableObjects.

### 2. Scene Setup
Set up the scene hierarchy:

```
Scene Root
├── AppManager                  [AppManager component]
├── AR Session                  [existing from template]
├── XR Origin (AR Rig)          [existing + ARExperienceManager, PreviewPlacementManager]
├── LocationServices            [LocationService, ProximityDetector]
├── UI Canvas
│   ├── TabBar                  [3 tabs: Explore | Nearby | AR View]
│   ├── ExploreScreen           [ExploreScreenManager]
│   ├── NearbyScreen            [NearbyScreenManager]
│   ├── ARViewScreen            [camera passthrough area]
│   ├── SiteDetailScreen        [SiteDetailScreenManager, overlay]
│   ├── ARHUDScreen             [ARHUDScreenManager, overlay]
│   ├── WelcomeScreen           [WelcomeScreenManager, overlay]
│   └── NotificationBanner      [NotificationBanner, top-of-screen]
└── EventSystem                 [existing]
```

### 3. Wire References
- Drag `SiteDatabase` asset into `AppManager.m_SiteDatabase`
- Connect all manager cross-references in Inspector
- Assign AR Foundation components (PlaneManager, RaycastManager, etc.)

### 4. Create AR Content
- Model/import 3D reconstructions for each site
- Create prefabs and assign to each `HistoricalSite.arPrefabOnLocation` / `arPrefabPreview`
- Set appropriate `previewScale` (0.01–0.1 for table-top) and `onLocationScale` (1.0 for real-world)

---

## System Flow

```
[App Launch]
    → AppManager.Start()
    → LocationService.StartTracking() (requests GPS permission)
    → ProximityDetector starts checking against SiteDatabase
    → AppNavigationManager shows Explore tab

[User Browsing - Explore Tab]
    → ExploreScreenManager displays site cards from SiteDatabase
    → User can search, filter by period/category/region
    → Tap card → SiteDetailScreen with full info
    → "View in AR" → Preview mode (place on any surface)

[User Walking - Nearby Tab]
    → NearbyScreenManager shows sites sorted by GPS distance
    → ProximityDetector fires OnEnteredSiteRadius events
    → NotificationBanner slides in: "You're near the Parthenon!"
    → "Start AR" → On-location mode (aligned to real GPS coordinates)

[AR Experience]
    → ARExperienceManager manages the AR session
    → Preview: PreviewPlacementManager handles tap-to-place + gestures
    → On-Location: GPS + compass alignment to real-world site
    → ARHUDScreen shows minimal controls (exit, reset, capture, info)
```

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **ScriptableObject data model** | Easy to author in Unity Inspector, supports drag-and-drop prefab assignment, no database dependency |
| **Event-driven architecture** | Loose coupling between systems; UI reacts to service events |
| **Tab bar navigation** | Familiar to all smartphone users (travel app pattern) |
| **Two AR modes in one manager** | Shared AR session, cleaner state management |
| **Haversine for GPS distance** | Accurate spherical distance for tourist-scale distances |
| **Proximity geofencing in-app** | No external dependency; customizable radius per site |

## Next Steps

- [ ] Create UI prefabs (Canvas, tab bar, cards, screens) in Unity
- [ ] Import/create 3D reconstruction models (start with Parthenon)
- [ ] Add thumbnail images for each site
- [ ] Build the SiteCard prefab with proper layout
- [ ] Test GPS flow on physical device
- [ ] Add AR cloud anchors for persistent on-location alignment
- [ ] Add native iOS/Android notification support for background proximity alerts
- [ ] Add multi-language support (Greek, English, etc.)
- [ ] Add analytics for tourism board metrics
