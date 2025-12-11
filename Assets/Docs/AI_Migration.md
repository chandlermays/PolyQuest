# AI System Migration Guide

## Overview

This guide explains how to migrate from the legacy `EnemyController` and `NPCController` classes to the new unified AI system using `AIController` with state machine and component-based architecture.

## Why Migrate?

The new AI system offers several advantages:

- **Unified Architecture**: One controller (`AIController`) handles both NPCs and Enemies
- **State Machine Pattern**: Clear separation of behaviors (Idle, Patrol, Suspicion, Attack)
- **Component-Based**: Modular design with pluggable components (Detection, Combat, Patrol, Movement)
- **Data-Driven**: Configuration via `AIData` ScriptableObjects for easy tuning
- **Extensible**: Easy to add new states and behaviors without modifying core code
- **Testable**: Components and states can be tested independently

## Migration Steps

### Step 1: Understanding the New Architecture

The new AI system consists of:

1. **AIController**: Main controller that manages state machine
2. **AIType Enum**: Defines behavior type (NPC or Enemy)
3. **AIData ScriptableObject**: Stores configuration values
4. **States**: IdleState, PatrolState, SuspicionState, AttackState
5. **Components**: 
   - `PatrolComponent`: Manages waypoint navigation
   - `DetectionComponent`: Handles target detection with FOV and line-of-sight
   - `AICombatComponent`: Wrapper for combat behavior
   - `AIMovementComponent`: Wrapper for movement behavior

### Step 2: Create AIData Assets

1. Right-click in Project window
2. Select `Create > PolyQuest > AI > AI Data`
3. Configure the settings:
   - **Detection Settings**: Sight range, FOV angle, detection layers
   - **Patrol Settings**: Patrol speed, waypoint tolerance, dwell time
   - **Combat Settings**: Chase speed, attack range, attack cooldown
   - **Suspicion Settings**: Suspicion time, aggro cooldown

Create separate AIData assets for different AI profiles (e.g., "Aggressive Enemy", "Cautious Patrol", "Passive NPC").

### Step 3: Migrate Enemy Prefabs

#### Old EnemyController Setup:
```
GameObject
├── EnemyController (script)
├── HealthComponent
├── CombatComponent
├── MovementComponent
└── NavigationPath (optional)
```

#### New Setup:
```
GameObject
├── AIController (script) [AIType = Enemy]
├── AIData (reference to ScriptableObject)
├── PatrolComponent (if patrol behavior needed)
├── DetectionComponent
├── AICombatComponent
├── AIMovementComponent
├── HealthComponent (existing)
├── CombatComponent (existing)
├── MovementComponent (existing)
└── NavigationPath (optional, referenced by PatrolComponent)
```

#### Migration Process:

1. **Add AIController**:
   - Add `AIController` component to the GameObject
   - Set `AI Type` to `Enemy`
   - Assign appropriate `AIData` asset

2. **Add Detection**:
   - Add `DetectionComponent`
   - Configure layers that can be detected (typically "Player" layer)
   - Set obstacle layers for line-of-sight checks

3. **Add Combat Wrapper**:
   - Add `AICombatComponent`
   - This wraps the existing `CombatComponent`
   - The existing `CombatComponent` should remain

4. **Add Movement Wrapper**:
   - Add `AIMovementComponent`
   - This wraps the existing `MovementComponent`
   - The existing `MovementComponent` and `NavMeshAgent` should remain

5. **Add Patrol (optional)**:
   - If enemy should patrol, add `PatrolComponent`
   - Reference existing `NavigationPath` or configure custom waypoints

6. **Remove Old Controller**:
   - Once verified working, remove the old `EnemyController` component
   - Keep `HealthComponent`, `CombatComponent`, `MovementComponent`

#### Field Mapping:

| Old EnemyController | New System |
|---------------------|------------|
| `m_detectionRange` | `AIData.SightRange` |
| `m_alertRange` | `AIData.AlertRange` |
| `m_suspicionTime` | `AIData.SuspicionTime` |
| `m_aggroCooldown` | `AIData.AggroCooldown` |
| `m_navigationPath` | `PatrolComponent.m_navigationPath` |
| `m_waypointTolerance` | `AIData.WaypointTolerance` |
| `m_waypointDwellTime` | `AIData.WaypointDwellTime` |

### Step 4: Migrate NPC Prefabs

#### Old NPCController Setup:
```
GameObject
├── NPCController (script)
├── MovementComponent
└── NavigationPath
```

#### New Setup:
```
GameObject
├── AIController (script) [AIType = NPC]
├── AIData (reference to ScriptableObject)
├── PatrolComponent
├── AIMovementComponent
├── HealthComponent (add if not present)
├── MovementComponent (existing)
└── NavigationPath (referenced by PatrolComponent)
```

#### Migration Process:

1. **Add AIController**:
   - Add `AIController` component
   - Set `AI Type` to `NPC`
   - Assign appropriate `AIData` asset

2. **Add Movement Wrapper**:
   - Add `AIMovementComponent`

3. **Add Patrol**:
   - Add `PatrolComponent`
   - Reference existing `NavigationPath`

4. **Add Health** (if not present):
   - NPCs need `HealthComponent` for consistency
   - Set appropriate health values

5. **Remove Old Controller**:
   - Remove `NPCController` component
   - Keep `MovementComponent` and `NavigationPath`

### Step 5: Test and Verify

1. **Test NPC Behavior**:
   - Verify patrol routes work correctly
   - Check waypoint dwell times
   - Ensure smooth movement

2. **Test Enemy Behavior**:
   - Verify target detection works (FOV, range, line-of-sight)
   - Test transition from patrol → attack
   - Test attack → suspicion → patrol flow
   - Verify alert behavior (nearby enemies react)
   - Test aggro on hit

3. **Performance Check**:
   - Monitor frame rate with multiple AIs
   - Check that detection intervals are appropriate

## Advanced Configuration

### Custom States

To add custom states:

1. Create a new class implementing `IAIState`
2. Implement `Enter()`, `Tick()`, `Exit()`, and `OnEvent()` methods
3. Transition to your custom state from existing states or via events

Example:
```csharp
public class CustomFleeState : IAIState
{
    public void Enter(AIController controller) { /* ... */ }
    public void Tick(AIController controller) { /* ... */ }
    public void Exit(AIController controller) { /* ... */ }
    public void OnEvent(AIController controller, string eventName, object eventData = null) { /* ... */ }
}
```

### Custom Components

Create custom components by:

1. Inheriting from `MonoBehaviour`
2. Providing public methods/events for state interaction
3. Adding to AI GameObject and querying via `GetComponent<>()`

### Event System

Use the event system to trigger state transitions:

```csharp
aiController.ChangeState(new CustomState());
```

Or send events to current state:
```csharp
// From within a state
controller.ChangeState(new AttackState());
```

## Backward Compatibility

The old `EnemyController` and `NPCController` classes are marked as `[Obsolete]` but remain functional. They will generate warnings during compilation encouraging migration.

### Using Adapters

If immediate migration is not possible, the old controllers will continue to work with the existing prefabs. However, they will not benefit from the new state machine and component features.

## Troubleshooting

### AI Not Moving

- Verify `AIMovementComponent` is present
- Check that `NavMeshAgent` is on NavMesh
- Ensure `MovementComponent` is present and configured

### Enemy Not Detecting Player

- Check `DetectionComponent` is present
- Verify `DetectionLayers` includes player layer
- Confirm FOV angle is sufficient (90-180 degrees typical)
- Check for obstacles blocking line-of-sight

### AI Stuck in One State

- Add debug logging in states' `Tick()` methods
- Use `OnDrawGizmosSelected` to visualize current state
- Check state transition conditions

### NavMeshAgent Errors

- Ensure all AI objects are placed on NavMesh surface
- Verify NavMesh is baked in scene
- Check `AIMovementComponent` has fallback enabled

## Performance Tips

1. **Detection Interval**: Increase `m_detectionInterval` in `DetectionComponent` for distant enemies
2. **Layer Masks**: Use specific layers to reduce physics queries
3. **LOD**: Consider different AIData profiles for near/far enemies
4. **Pooling**: Use object pooling for frequently spawned enemies

## Best Practices

1. **Use AIData Assets**: Create reusable profiles instead of per-instance configuration
2. **Component Composition**: Only add components your AI needs (e.g., no `DetectionComponent` for NPCs)
3. **State Design**: Keep states simple and focused on single behavior
4. **Event-Driven**: Use events for state transitions instead of polling
5. **Testing**: Test each AI type in isolation before integrating into full scenes

## Support

For issues or questions about the new AI system:

1. Check console for error messages and warnings
2. Use Unity's Debug mode to inspect component values
3. Add debug visualization in `OnDrawGizmosSelected()`
4. Review `ExampleAISetup.cs` for reference implementation

## Example Configurations

### Aggressive Melee Enemy
- AIType: Enemy
- SightRange: 15
- FOV: 120
- AttackRange: 2
- ChaseSpeed: 6
- SuspicionTime: 3

### Cautious Ranged Enemy
- AIType: Enemy
- SightRange: 20
- FOV: 90
- AttackRange: 10
- ChaseSpeed: 4
- SuspicionTime: 8

### Patrol Guard NPC
- AIType: NPC
- PatrolSpeed: 2
- WaypointDwellTime: 5
- (No detection or combat components)

## Conclusion

The new AI system provides a robust, extensible foundation for AI behavior. While migration requires some effort, the benefits in maintainability, extensibility, and performance make it worthwhile for long-term development.
