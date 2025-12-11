# AI System Migration Guide

## Overview

The AI system has been refactored to use a state machine architecture with component-based composition. The new system provides better modularity, testability, and extensibility compared to the legacy `EnemyController` and `NPCController` classes.

## Key Changes

### Old System
- `EnemyController` and `NPCController` as separate classes
- Monolithic behavior implementation
- Hard-coded state transitions
- Limited extensibility

### New System
- Single `AIController` class for both NPCs and Enemies
- State machine with `IAIState` interface
- Component-based architecture (`PatrolComponent`, `DetectionComponent`, `AICombatComponent`)
- Configurable via `AIData` ScriptableObject
- Reuses existing `MovementComponent`

## Migration Steps

### 1. Create AIData Asset

First, create an `AIData` ScriptableObject to store tuning parameters:

1. In Unity, right-click in the Project window
2. Select `Create > PolyQuest > AI > AI Data`
3. Name it appropriately (e.g., "BasicEnemyAI" or "PatrolNPCAI")
4. Configure the settings:
   - **Detection Settings**: Sight range, FOV angle, target layers
   - **Suspicion Settings**: Duration to remain suspicious
   - **Movement Settings**: Patrol speed, chase speed, waypoint tolerance
   - **Combat Settings**: Attack range, attack cooldown, alert range

### 2. Migrating EnemyController

#### Old Setup
```
GameObject with:
- EnemyController
- MovementComponent
- CombatComponent
- HealthComponent
- NavigationPath reference
```

#### New Setup
```
GameObject with:
- AIController (AIType = Enemy)
- MovementComponent (existing, required)
- CombatComponent (existing, for actual combat)
- HealthComponent (existing, for health/damage)
- PatrolComponent (new, optional for patrolling)
- DetectionComponent (new, for target detection)
- AICombatComponent (new, wraps CombatComponent)
- AIData reference
```

#### Step-by-Step Migration

1. **Backup your prefab** before making changes

2. **Add new components**:
   - Add `AIController` component
   - Set `AIType` to `Enemy`
   - Assign the `AIData` asset you created

3. **Add AI components**:
   - Add `DetectionComponent`
     - It will automatically use settings from AIData
     - Configure target layers (usually "Player")
     - Configure obstacle layers for line-of-sight checks
   
   - Add `AICombatComponent`
     - It will automatically use settings from AIData
     - It will find and use the existing `CombatComponent`
   
   - Add `PatrolComponent` (if enemy patrols):
     - Option A: Assign Transform waypoints to the waypoint list
     - Option B: Assign a `NavigationPath` asset (recommended for compatibility)

4. **Configure detection**:
   - In `DetectionComponent`, set:
     - Target Layers: Include "Player" layer
     - Obstacle Layers: Include "Default", "Terrain", etc.
     - Require Line of Sight: Usually true

5. **Remove or disable old component**:
   - You can keep `EnemyController` temporarily for reference
   - Disable it to prevent conflicts with new `AIController`
   - Once tested, remove `EnemyController` completely

6. **Test the migration**:
   - Run the scene
   - Verify patrol behavior
   - Verify target detection (enemy should detect player)
   - Verify attack behavior
   - Verify suspicion state after losing target

### 3. Migrating NPCController

#### Old Setup
```
GameObject with:
- NPCController
- MovementComponent
- NavigationPath reference
```

#### New Setup
```
GameObject with:
- AIController (AIType = NPC)
- MovementComponent (existing, required)
- PatrolComponent (new, for patrolling)
- AIData reference
```

#### Step-by-Step Migration

1. **Create AIData** for NPC if you haven't already
   - Set appropriate patrol speeds
   - Set waypoint tolerance and dwell time
   - Detection settings can be minimal or disabled

2. **Add new components**:
   - Add `AIController` component
   - Set `AIType` to `NPC`
   - Assign the `AIData` asset

3. **Add PatrolComponent**:
   - Assign Transform waypoints or NavigationPath asset
   - Configure patrol speed, tolerance, and dwell time
   - Or let it inherit from AIData

4. **Remove or disable old component**:
   - Disable `NPCController` to test
   - Once verified, remove `NPCController`

5. **Test the migration**:
   - Run the scene
   - Verify patrol behavior
   - Verify NPC doesn't react to player (unless you want it to)

## Component Reference

### AIController
- **AIType**: Set to `NPC` or `Enemy`
- **AIData**: Reference to AIData ScriptableObject
- **Start In Patrol State**: Whether to start patrolling or idle

### PatrolComponent
- **Waypoints**: List of Transform waypoints
- **Navigation Path**: Alternative to waypoint list (recommended)
- **Loop Waypoints**: Whether to loop or ping-pong
- **Patrol Speed**: Override AIData patrol speed if needed
- **Waypoint Tolerance**: Distance to consider waypoint reached
- **Dwell Time**: Time to wait at each waypoint

### DetectionComponent
- **Sight Range**: Maximum detection distance
- **Field of View Angle**: FOV in degrees (90 = forward cone, 360 = all directions)
- **Target Layers**: Layers that can be detected as targets
- **Obstacle Layers**: Layers that block line of sight
- **Eye Height**: Height offset for raycast origin
- **Detection Interval**: How often to check (performance vs responsiveness)
- **Require Line of Sight**: Whether obstacles block detection

### AICombatComponent
- **Attack Range**: Maximum attack distance
- **Attack Cooldown**: Minimum time between attacks
- Automatically finds and uses existing `CombatComponent`

## State Machine Flow

### NPC Flow
```
Idle/Patrol → (No transitions unless damaged/events)
```

### Enemy Flow
```
Patrol → [Target Detected] → Attack
Attack → [Target Lost] → Suspicion
Suspicion → [Timeout] → Patrol
Suspicion → [Target Detected] → Attack
```

## Troubleshooting

### Issue: AI doesn't move
- **Check**: Does GameObject have `MovementComponent`?
- **Check**: Is NavMeshAgent on NavMesh?
- **Check**: Are waypoints set in PatrolComponent?
- **Check**: Is AIController enabled?

### Issue: Enemy doesn't detect player
- **Check**: Is `DetectionComponent` present?
- **Check**: Are target layers configured correctly?
- **Check**: Is player on the correct layer?
- **Check**: Is sight range sufficient?
- **Check**: Are obstacles blocking line of sight?

### Issue: Enemy doesn't attack
- **Check**: Is `AICombatComponent` present?
- **Check**: Does GameObject have `CombatComponent` (legacy)?
- **Check**: Is attack range sufficient?
- **Check**: Is target within attack range?

### Issue: Compilation errors
- **Fix**: Ensure all new scripts are in correct namespaces
- **Fix**: Ensure using directives are present:
  ```csharp
  using PolyQuest.AI;
  using PolyQuest.AI.Components;
  using PolyQuest.Components;
  ```

## Best Practices

1. **Create multiple AIData assets**: Don't use the same AIData for all AI types. Create specialized ones for:
   - Melee enemies
   - Ranged enemies
   - Fast/slow enemies
   - Patrolling NPCs
   - Stationary NPCs

2. **Use NavigationPath assets**: Instead of Transform waypoints, use NavigationPath ScriptableObjects for reusability

3. **Layer organization**: 
   - Put players on "Player" layer
   - Put enemies on "Enemy" layer
   - Configure target layers accordingly

4. **Performance**: 
   - Adjust `DetectionComponent.DetectionInterval` based on number of AI entities
   - Use appropriate sight ranges (don't make them too large)
   - Consider using trigger colliders for large-scale detection

5. **Testing**: 
   - Keep old controllers disabled rather than deleted initially
   - Test in play mode before saving prefab changes
   - Use Gizmos (visible in Scene view) to debug detection ranges

## Advanced: Extending the System

### Creating Custom States

Implement `IAIState` interface:

```csharp
using UnityEngine;
using PolyQuest.AI;

public class FleeState : IAIState
{
    public void Enter(AIController controller) { }
    public void Tick(AIController controller) { }
    public void Exit(AIController controller) { }
    public void OnEvent(AIController controller, string eventName, object eventData = null) { }
}
```

### Creating Custom Components

Extend functionality with new components:

```csharp
using UnityEngine;
using PolyQuest.AI;

public class AlertComponent : MonoBehaviour
{
    public void AlertAllies(Vector3 position, float radius)
    {
        // Custom alert logic
    }
}
```

## Support

For issues or questions about migration:
1. Check console logs - the new system provides detailed debug logging
2. Review the `ExampleAISetup.cs` script for reference implementations
3. Use Scene view Gizmos to visualize AI ranges and states

## Backward Compatibility

The legacy `EnemyController` and `NPCController` classes remain functional but are marked as deprecated. They will continue to work for existing prefabs, but you'll see compiler warnings encouraging migration to the new system.
