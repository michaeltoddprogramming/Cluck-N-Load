# Chain Cost Display Setup Guide

This guide explains how to set up the Chain Cost Display UI that follows the cursor during chain building.

## What it does:
- Shows the total cost of the chain being built
- Displays how many structures are affordable vs total structures
- Updates in real-time as the cursor moves
- Changes color to red when some structures exceed budget
- Automatically hides when not chain building

## Unity Setup Instructions:

### 1. Create the UI GameObject Hierarchy:
1. In your main game scene, find or create a Canvas that has "Screen Space - Overlay" mode
2. Right-click the Canvas → Create Empty → Name it "ChainCostDisplay"
3. Add the `ChainCostDisplay` script to this GameObject

### 2. Set up the UI Elements:
1. Right-click the ChainCostDisplay GameObject → UI → Text - TextMeshPro → Name it "CostText"
2. Right-click the ChainCostDisplay GameObject → UI → Text - TextMeshPro → Name it "AffordableText"

### 3. Configure the ChainCostDisplay component:
- **Cost Text**: Drag the "CostText" GameObject to this field
- **Affordable Text**: Drag the "AffordableText" GameObject to this field
- **Cost Format**: Keep default "Cost: {0} {1}" (shows "Cost: 150 Coins")
- **Affordable Format**: Keep default "Affordable: {0}/{1}" (shows "Affordable: 3/5")

### 4. Style the Text Elements:
#### CostText styling:
- Font Size: 14-16
- Color: White or light color
- Alignment: Center
- Auto Size: Best Fit (optional)

#### AffordableText styling:
- Font Size: 12-14
- Color: White (will change to red/green automatically)
- Alignment: Center
- Auto Size: Best Fit (optional)

### 5. Position the UI Container:
1. Select the ChainCostDisplay GameObject
2. Set RectTransform:
   - Anchor: Bottom Left (0, 0)
   - Pivot: (0, 0)
   - Position: Will be set automatically by cursor following

### 6. Connect to BuildController:
1. Select your BuildController GameObject in the scene
2. In the BuildController component, find "Chain Cost Display" field
3. Drag the ChainCostDisplay GameObject to this field

### 7. Optional Styling:
You can add a background panel:
1. Right-click ChainCostDisplay → UI → Image → Name it "Background"
2. Set it as the first child (move to top of hierarchy)
3. Configure:
   - Color: Semi-transparent black (0, 0, 0, 0.5)
   - Make it slightly larger than the text elements

## How it works:
- The display automatically appears when chain building mode starts
- It follows the cursor with the same offset as the delete icon
- Cost updates in real-time as you move the cursor
- Shows green text when all structures are affordable
- Shows red text when some structures exceed budget
- Automatically hides when chain building ends

## Text Display Examples:
- "Cost: 300 Coins" (when building 3 structures at 100 each)
- "Affordable: 2/3" (when player can only afford 2 out of 3 structures)
- Green color when affordable: 3/3
- Red color when over budget: 2/5