# Tutorial System Setup Guide

## Required GameObjects and Components

### 1. Tutorial Manager Setup
Create a GameObject named "TutorialManager" with the following components:
- `TutorialManager` script
- `FeatureUnlockManager` script

### 2. Tutorial UI Setup
Create a Canvas for the tutorial UI with these child objects:

```
Tutorial Canvas (Canvas)
├── Tutorial Panel (Panel)
│   ├── Tutorial Text (TextMeshPro - UI)
│   ├── Next Button (Button)
│   ├── Skip Button (Button)
│   └── Background (Image)
└── Highlight Overlay (Image)
```

### 3. Assign References in TutorialManager:
- **Tutorial Panel**: The main panel containing tutorial text and buttons
- **Tutorial Text**: TextMeshPro component for displaying tutorial messages
- **Next Button**: Button to advance to next tutorial step
- **Skip Button**: Button to skip the entire tutorial
- **Highlight Overlay**: Image used to highlight UI elements
- **Tutorial Canvas**: The canvas containing all tutorial UI

### 4. Assign References in FeatureUnlockManager:
- **Shop Button**: Reference to the shop button in your main UI
- **Delete Button**: Reference to the delete mode button
- **Inventory Panel**: Reference to the inventory UI panel
- **Minimap Panel**: Reference to the minimap UI
- **Unlock Notification Prefab**: Prefab for showing unlock notifications
- **Notification Parent**: Transform where notifications will be spawned

### 5. Camera Setup
Add the `TutorialCameraController` script to your main camera or camera controller.

### 6. Optional: Performance Monitoring
Add `PerformanceMonitor` script to a persistent GameObject and assign:
- **FPS Text**: TextMeshPro component to display FPS
- **Memory Text**: TextMeshPro component to display memory usage

### 7. Optional: Object Pooling
Add `ObjectPool` script to a GameObject and configure pools for:
- Wolf spawning
- Projectiles
- Effect particles
- Any other frequently spawned objects

### 8. Optional: Structure LOD
Add `StructureLOD` components to structure prefabs and assign:
- **High LOD**: Detailed model for close viewing
- **Medium LOD**: Reduced detail model
- **Low LOD**: Simple model for distant viewing
- **Structure Collider**: Main collider component
- **Disableable Components**: Components to disable at lower LOD levels

## GameObject Tags Required
Make sure these tags exist in your project:
- "ShopButton"
- "MoneyUI" 
- "TimeUI"

## Event Integration Checklist
✅ ShopUIManager.OpenShop() calls tutorial condition
✅ BuildController.PlaceItem() calls tutorial condition  
✅ NightManager.StartNight() calls tutorial condition
✅ Structure.TakeDamage() triggers OnHealthChanged event
✅ All UI classes use override OnDestroy() methods

## Testing
1. Add `TutorialTestHelper` script to any GameObject
2. Use keyboard shortcuts to test tutorial progression:
   - T: Start tutorial
   - R: Reset progress
   - 1-4: Trigger specific conditions
3. Press F1 to toggle performance monitor

## Performance Tips
- Tutorial system is designed to be lightweight
- UI updates are event-driven, not frame-based
- Object pooling reduces garbage collection
- LOD system optimizes rendering performance
- Performance monitor helps identify bottlenecks

## Troubleshooting
- Ensure all UI references are assigned in inspector
- Check that GameObject names match highlight targets
- Verify tutorial canvas sorting order is high enough
- Make sure tutorial persists across scene loads if needed
