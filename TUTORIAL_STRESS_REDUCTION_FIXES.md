# Tutorial Stress Reduction & Timing Improvements

## Changes Made

### 1. **Much Longer Days**
- Changed day duration from ~12 minutes to ~72 minutes (6x longer)
- Tutorial mode makes days even slower (2x slower again = ~144 minutes for tutorial)
- Players now have plenty of time to complete each step without rushing

### 2. **Controlled Crop Growth**
- Crops no longer grow automatically during tutorial
- Crops only become ready when the tutorial specifically asks for it
- Triggered after "Buy Chickens" step completes
- Prevents surprise "harvest now!" interruptions

### 3. **Improved Tutorial Step Descriptions**
- Added reassuring language like "Take your time, no rush!"
- Explained that days are extra long for learning
- Made it clear when crops will be ready
- Reduced stress-inducing language

### 4. **Better Step Sequencing**
- Crops trigger for harvest only after buying chickens
- Clear progression: Build → Plant → Buy Animals → Harvest → Feed
- No more overlapping or interrupting steps

## User Experience Improvements

### Before:
- Fast days created time pressure
- Crops became ready unexpectedly
- Tutorial steps overlapped
- User felt rushed and stressed

### After:
- Extra long days for stress-free learning
- Predictable crop timing
- Clear step progression
- User can focus on one task at a time
- Reassuring tutorial text

## Technical Details

**NightManager.cs:**
- `inGameMinVSSec`: 0.0625f → 0.02f (3x slower base time)
- Added `IsInTutorialMode()` method
- Tutorial mode uses 50% slower time (total 6x slower than original)

**TutorialConditionTracker.cs:**
- Removed automatic crop growth trigger
- Added `TriggerCropGrowthForTutorial()` method
- Crops only grow when explicitly requested

**TutorialManager.cs:**
- Added crop growth trigger in `CompleteCurrentStep()`
- Updated step descriptions with reassuring language
- Crops grow after "buy_chickens" step completes

## Result
Players can now learn at their own pace without feeling rushed or interrupted by unexpected events!
