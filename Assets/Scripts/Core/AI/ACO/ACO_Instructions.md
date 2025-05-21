## Implementation Plan

### Level 1: Basic Ant System
1. Implement scout ants that can navigate from map edges to player base using Flow Field
2. Create pheromone laying mechanism for return journey
3. Visualize pheromones (color-coded based on enemy unit type)
4. Test ant behavior with simple player structures
5. Implement the `TriggerAnts()` function with dirty update mechanism

### Level 2: Structure Typing System
1. Add structure type properties to all player buildings:
   - Main Building
   - Defense Structure
   - Resource Structure
2. Create global structure registry that tracks all player structures
3. Update visualization to show structure types
4. Test that ants can identify structure types correctly

### Level 3: Advanced Ant Discovery
1. Implement structure discovery system:
   - Create Manager with array of player-placed structures
   - Track which structures have been discovered by ants
   - Calculate required number of ants based on `numStructures / AntPossibleStructures`
   - Implement logic to ignore already visited structures
2. Each ant has a limited number of structures they can visit
3. Ants prioritize undiscovered structures
4. Once all structures are discovered, system scales back ant spawning

### Level 4: Basic Enemy Unit AI
1. Modify existing flow-field agent script to:
   - Check for pheromones in current and neighboring cells
   - Follow pheromones when detected
   - Fall back to flow field ONLY when no pheromones detected
2. Test enemy movement with simple pheromone trails
3. Implement basic congestion avoidance

### Level 5: Priority-Based System
1. Implement full priority-based target selection for enemy units:
   - Define different priority lists for each enemy type
   - Create target selection logic that follows priority order
2. Enhance ant pheromone laying to mark structures based on enemy type preferences
3. Implement enemy unit type-specific pheromone following
4. Test full system with multiple enemy types and structure types
5. Fine-tune parameters for optimal performance

> **Implementation Note**: Each level should be implemented as simply as possible, with efficient and polished code at each stage before proceeding to the next level.## Structure System

### Structure Types
Each player structure must be categorized into one of these types:
- **Main Building**: Primary base structures (command centers, headquarters)
- **Defense Structures**: Turrets, walls, shields, defensive emplacements
- **Resource Structures**: Resource collectors, refineries, storages
- **Other**: Miscellaneous structures with lower priority

### Structure Registration
- All player-placed structures must be registered in a global structures array
- Each structure entry contains:
  - Reference to the structure object
  - Structure type
  - Location (GridCell coordinates)
  - Discovery status (whether ants have found it)
- This registry is what ants consult during exploration# Comprehensive Ant-Based Navigation & Combat System

## System Overview

This system creates intelligent enemy unit behavior using lightweight ant-like agents that build pheromone trails, enabling efficient and believable enemy movement without expensive pathfinding algorithms.

### Core Components

1. **Flow Field (Global Base Attraction)**
   - Pre-computed vector field pointing toward player's main base
   - Serves as a global guidance system for ants and enemy units
   - Can be implemented as a simple Dijkstra-based gradient or baked field

2. **Scout Ants**
   - Lightweight agents that explore and mark targets with pheromones
   - Maximum ~10 active at once for CPU efficiency
   - Spawn from edges all around the map (not just enemy spawn points)
   - Triggered at specific events with a delayed update system:
     - Building destruction calls `TriggerAnts()` function
     - Function waits a designated time before spawning (dirty update)
     - Prevents over-updating when multiple buildings are destroyed quickly
     - Also triggered at other events like nightfall

3. **Pheromone Grid**
   - Uses existing `GridCell` 2D array structure
   - Each cell contains a `pheromonesArray` where indices correspond to enemy types:
     - Index 0: Regular enemy
     - Index 1: Raider enemy
     - Index 2: Tank enemy
     - (And so on for additional types)
   - Enables smart pathfinding without expensive algorithms
   - Supports diffusion (blurring) for wider trails

4. **Enemy Units (Swarm AI)**
   - Follow pheromone trails to reach targets
   - Use layered decision-making with staggered updates
   - Balance pheromone following with direct targeting
   - Requires modification of existing flow-field agent script to integrate with ACO system

## Scout Ant Behavior

### Movement Phases

#### 1. Initial Phase (Flow Field Driven)
- Ants spawn from edges all around the map, not just enemy spawn points
- Follow Flow Field (FF) as global guide toward player base
- FF influence is strongest during this phase
- Ants do not lay any pheromones during this phase
- Use FF exclusively until player territory is detected

#### 2. Snoop Mode (Player Territory)
- Triggered when entering grid cells marked as player-owned
- FF influence remains but is reduced
- Local decision-making prioritizes:
  - Unvisited tiles
  - Avoiding backtracking
  - Slight bias toward current direction
  - Bonus for alignment with FF direction

#### 3. Target Scouting
- Structures found are marked in the pheromone grid per enemy unit type
- While discovering structures, FF impact remains diminished
- When discovery slows, FF influence increases to pull them into unexplored areas

#### 4. Return & Pheromone Laying
- Triggered by reaching lifetime threshold or filling target list
- Ants navigate toward enemy spawn areas (not their original spawn points)
- Begin laying pheromones on return journey, creating trails from targets to enemy spawns
- Critical: Ants MUST be able to reach enemy spawn areas even if lifetime is about to expire
  - System ensures ants have enough lifetime reserve for the return journey
  - Or implements emergency "return home" acceleration when lifetime is critical

### Pheromone Management

- Global manager tracks which structures have been marked
- Ants consult this list to avoid redundancy
- No pheromones laid during initial exploration phase
- Pheromones only dropped after targets are found and ant begins return journey
- Return path selection prioritizes:
  - Cells with no or minimal existing pheromones
  - Cells that still lead toward enemy spawn areas
  - If a cell already has pheromones, its desirability for the ant is lowered
  - If no better path is found, ants will use already pheromoned paths (pheromone deposit amount does not decrease on already marked cells)
- Multiple ants with staggered paths ensure trail diversity and redundancy

### Optimization for Ants
- Ants run on staggered frames to distribute computational load
- Shared global structure registry prevents duplication
- Lightweight movement logic with simple scoring system
- Priority on reaching enemy spawn areas before lifetime expires

## Enemy Unit Behavior

### Priority-Based Target Selection

Each enemy unit type operates on a priority list system when selecting targets:

1. **Priority Categories**:
   - Main Building structures
   - Defense structures 
   - Resource structures
   - Player unit locations
   - Pheromone trails (type-specific)
   - Flow Field (last resort fallback)

2. **Dynamic Priority Assignment**:
   - Each unit type has different priority values for each category
   - Example for regular units:
     ```
     Main Building - 1 (highest)
     Resource Structures - 2
     Player Units - 3
     Defense Structures - 4
     Flow Field - 5 (lowest)
     ```
   - Other unit types will have different priority orders

3. **Target Selection Logic**:
   - Check for highest priority target first
   - If not found, fall back to next priority
   - Continue down priority list until a target is found
   - If no targets found from any category, default to Flow Field
   - **Important**: Units should NEVER use Flow Field if pheromones are sensed
   - Flow Field is strictly a last resort when no pheromones are detected

### Layered AI Loop

#### 1. Sense Phase (Every N Ticks)
- Check for targets in priority order
- Scan for pheromones related to unit type
- If no pheromones are detected, then and only then use Flow Field

#### 2. Movement Phase
- **Target Mode**: Direct movement toward target based on priority
- **Pheromone Mode**: Follow specific pheromone type with:
  - Neighbor cell evaluation
  - Small random noise
  - Congestion factor to prevent bunching
- **Flow Field Mode**: Only activated when no pheromones are detected

### Optimization for Enemy Units

- **Staggered Updates**: Run AI logic on distributed frames to spread computational load
  ```csharp
  if (tick % 8 == unit.id % 8) {
      // Run AI logic
  }
  ```
- **Spatial Partitioning**: O(1) lookup for nearby targets
- **Read-Only Grid Access**: Units only read from pheromone grid
- **Congestion Management**:
  - Detect clumped units (too many units in same area)
  - Temporarily inhibit attack mode for a percentage of those units
  - Force exploration based on pheromones to spread them out
  - After designated time, remove inhibition and return to normal behavior

## System Management

### Global Controls
- **Ant Manager**:
  - Controls ant spawning based on structure discovery needs
  - Calculates required ant count: `Math.Ceiling(numStructures / AntPossibleStructures)`
  - Handles delayed updates via `TriggerAnts()` function
  - Maintains global list of discovered structures
  
- **Unit Manager**:
  - Monitors unit distribution to prevent clustering
  - Periodically checks map coverage
  - Adjusts parameters as needed during runtime

### Pheromone System
- Each `GridCell` contains a `pheromonesArray` with values per unit type
- Blur mechanism spreads influence to nearby tiles
- Optional decay over time or in high-traffic areas
- Complete reset after battle cycle ends

## Implementation Notes

1. Integrate with existing `GridCell` 2D array structure
2. Modify existing flow-field agent script to:
   - Check for pheromone values in current and neighboring cells
   - Add pheromone-following behavior while maintaining flow field fallback
   - Implement unit type-specific pheromone response
3. Use tight inner loops for movement calculations
4. Scale to 1000+ enemy units and dozens of ants
5. Implement unit behaviors as simple state machines
6. Balance between global guidance (FF) and local decision-making
7. Provide sufficient randomness to avoid predictable patterns
8. Implement the delayed update system for ant triggering to prevent over-updating

This system combines the lightweight exploration of ant scouts with efficient pheromone-based movement for enemy units, creating emergent swarm behavior without expensive pathfinding algorithms.