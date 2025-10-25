# Pete3D Tutorial System - Implementation Guide

## Overview
This system integrates 3D Pete with your existing tutorial system, providing contextual positioning, emotional responses, and reduced text-heavy UI while maintaining backward compatibility.

## Setup Instructions

### 1. Scene Setup
1. **Create Pete3DGuide GameObject:**
   - Create empty GameObject named "Pete3DGuide"
   - Add `Pete3DGuide.cs` script
   - Position as child of your Tutorial system

2. **Setup Pete Model Containers:**
   - Create 3 empty GameObjects as children of Pete3DGuide:
     - "WorldSpaceContainer" (for world Pete)
     - "UISpaceContainer" (for UI Pete)
     - "CornerBuddyContainer" (for corner Pete)

3. **Setup Pete Model Variants:**
   - Drag your Pete 3D model into each container
   - Name them: "WorldPete", "UIPete", "CornerPete"
   - Set CornerPete scale to (0.6, 0.6, 0.6)
   - Initially disable all Pete variants

### 2. Pete3DGuide Configuration

#### Required References:
```csharp
// In Inspector for Pete3DGuide:
pete3DModel = [Your Pete 3D Model Prefab]
gameCamera = [Main Camera]
worldSpaceContainer = [WorldSpaceContainer]
uiSpaceContainer = [UISpaceContainer] 
cornerBuddyContainer = [CornerBuddyContainer]
worldPete = [WorldPete GameObject]
uiPete = [UIPete GameObject]
cornerPete = [CornerPete GameObject]
```

#### Particle Systems (Optional but Recommended):
- Create particle systems for Pete emotions:
  - excitementParticles (stars/sparkles)
  - worryParticles (sweat drops/question marks)
  - thinkingParticles (thought bubbles)
  - celebrationParticles (confetti/fireworks)

#### Audio Setup:
- Assign AudioSource for Pete sounds
- Add audio clips for different emotions:
  - excitedSounds[] (happy mumbles)
  - worriedSounds[] (concerned mumbles)
  - neutralSounds[] (normal mumbles)

### 3. Speech Bubble System
1. **Create Speech Bubble Prefab:**
   - Create UI prefab with Image background + TextMeshPro text
   - Add speech bubble tail pointing downward
   - Save as prefab and assign to speechBubblePrefab

2. **Setup Canvas:**
   - Create or assign World Space Canvas for speech bubbles
   - Set as speechBubbleCanvas in Pete3DGuide

### 4. TutorialManager Integration

#### In TutorialManager Inspector:
```csharp
// New Pete 3D Integration section:
pete3DGuide = [Pete3DGuide GameObject]
usePete3D = true
minimizeChecklistWithPete = true
```

### 5. Tutorial Step Configuration

Each TutorialStep now supports Pete configuration:

#### Example Enhanced Step:
```csharp
new TutorialStep
{
    stepId = "build_farmhouse",
    title = "Build Farmhouse", 
    instructionText = "Build your Farmhouse first!",
    triggerToWaitFor = TutorialTrigger.BuiltFarmHouse,
    
    // Pete Configuration:
    peteContext = PeteContext.CornerBuddy,  // Auto, WorldGuide, UIHelper, CornerBuddy, Hidden
    peteEmotion = PeteEmotion.Worried,      // Neutral, Excited, Worried, Thinking, Celebrating, Pointing
    peteWorldPosition = Vector3.zero,       // Specific world position (optional)
    peteWorldTarget = null,                 // Transform to position near (optional)
    peteUITarget = null,                    // UI element to position near (optional)
    peteLooksAtTarget = true,               // Should Pete look at the target?
    
    // Legacy system still works:
    uiToHighlight = farmhouseButton
}
```

## Pete Context Types

### PeteContext.Auto (Default)
- System automatically chooses best context based on step content
- WorldGuide for world targets
- UIHelper for simple UI
- CornerBuddy for complex UI (shop, barracks, etc.)

### PeteContext.WorldGuide
- Pete appears in 3D world space
- Best for: Building placement, world exploration, farm overview
- Pete positions near world targets or at specified world position

### PeteContext.UIHelper  
- Pete appears in screen space near UI elements
- Best for: Simple UI interactions, buttons, panels
- Pete positions contextually near UI targets

### PeteContext.CornerBuddy
- Pete appears as small companion in screen corner
- Best for: Complex UI (shop, barracks), multi-step interactions
- Doesn't interfere with complex UI layouts

### PeteContext.Hidden
- Pete is hidden for this step
- Use for steps where Pete would be distracting

## Pete Emotions

- **Neutral:** Default state, normal sounds
- **Excited:** Sparkle particles, happy sounds, bigger scale
- **Worried:** Sweat particles, concerned sounds, worried posture
- **Thinking:** Thought bubble particles, contemplative sounds
- **Celebrating:** Confetti particles, celebration sounds, bouncing
- **Pointing:** Gesture toward target, attention-directing behavior

## Key Features

### Automatic Context Switching
Pete seamlessly transitions between contexts based on tutorial content:
- Teaching world building → WorldGuide
- Simple UI interaction → UIHelper  
- Complex shop UI → CornerBuddy

### Reduced Text Overload
- Pete's presence and emotions convey information
- Speech bubbles replace heavy dialogue panels
- Visual feedback > text explanations

### Backward Compatibility
- All existing tutorial steps work unchanged
- Pete features are optional enhancements
- Can disable Pete3D system entirely with `usePete3D = false`

### Integrated Checklist Minimization
When Pete3D is active:
- Large checklist panel shrinks to small corner indicator
- Dialogue panels adapt based on Pete's context
- Focus shifts from checklist to Pete guidance

## Testing Your Implementation

1. **Start Tutorial:** Pete should appear for welcome step
2. **Movement Step:** Pete should switch to corner buddy mode
3. **Shop Step:** Pete should position near shop button as UI helper
4. **Building Steps:** Pete should adapt to complex UI as corner buddy
5. **Step Completion:** Pete should celebrate with particles/sounds

## Troubleshooting

### Pete Not Appearing:
- Check Pete3DGuide is assigned in TutorialManager
- Verify usePete3D = true
- Ensure Pete model containers are properly setup

### Pete Positioning Issues:
- Check gameCamera assignment
- Verify world/UI target assignments in steps
- Test with simple peteWorldPosition first

### Performance Concerns:
- Use object pooling for particle systems
- Limit Pete animation frequency
- Consider LOD system for Pete model if needed

## Advanced Customization

### Custom Pete Behaviors:
Extend Pete3DGuide to add custom behaviors:
```csharp
public void CustomPeteBehavior()
{
    // Add your custom Pete animations/reactions
}
```

### Tutorial Step Templates:
Create common step configurations:
```csharp
// Template for world building steps
var worldBuildingStep = new TutorialStep {
    peteContext = PeteContext.WorldGuide,
    peteEmotion = PeteEmotion.Excited,
    peteLooksAtTarget = true
};

// Template for UI teaching steps  
var uiTeachingStep = new TutorialStep {
    peteContext = PeteContext.UIHelper,
    peteEmotion = PeteEmotion.Pointing,
    peteLooksAtTarget = true
};
```

This system transforms your tutorial from a checklist experience into an interactive farm buddy experience while preserving all existing functionality!