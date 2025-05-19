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

### Layered AI Loop

#### 1. Sense Phase (Every N Ticks)
- Follow pheromone scent gradient
- Scan nearby grid cells (5x5 or less) for player structures
- If target found → Switch to Attack Mode

#### 2. Movement Phase
- **Attack Mode**: Direct movement toward target with simple obstacle avoidance
- **Navigation Mode**: Follow pheromone gradient with:
  - Neighbor cell evaluation
  - Small random noise
  - Congestion factor to prevent bunching
  - Occasional subgoal sampling for variance

#### 3. Subgoal Refresh (Optional)
- Periodically choose high-pheromone tile ahead as intermediate target
- Bias movement toward subgoal while evaluating local pheromones

### Optimization for Enemy Units

- **Staggered Updates**: 
  ```
  if tick % 8 == unit.id % 8: 
      // Run AI logic
  ```
- **Spatial Partitioning**: O(1) lookup for nearby targets
- **Read-Only Grid Access**: Units only read from pheromone grid
- **Congestion Management**: Detect and redistribute clumped units

## System Management

### Global Controls
- Monitor unit distribution and clustering
- Temporarily inhibit attack mode for percentage of units to force exploration
- Periodically check map coverage and adjust parameters

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