# PolyQuest AI System

## Overview

This directory contains the unified AI system for PolyQuest, featuring a state machine pattern with component-based composition. The system replaces the old `EnemyController` and `NPCController` with a single, robust `AIController`.

## Architecture

### Core Components

#### AIController.cs
The main entry point for all AI-driven GameObjects. Manages the state machine and coordinates between components.

**Features:**
- AIType enum to distinguish between NPC and Enemy behaviors
- State machine management
- Component composition (patrol, detection, combat, movement)
- Event-driven architecture
- Health and aggro tracking

#### AIStateMachine.cs
Generic state machine that handles state transitions with proper Enter/Exit semantics.

**Features:**
- CurrentState tracking
- Time-in-state tracking
- Event forwarding to current state
- Safe state transitions

#### IAIState.cs
Interface defining the contract for all AI states.

**Methods:**
- `Enter(AIController)` - Called when entering state
- `Tick(AIController)` - Called every frame
- `Exit(AIController)` - Called when exiting state
- `OnEvent(AIController, string, object)` - Handles events

#### AIData.cs
ScriptableObject for data-driven AI configuration.

**Settings:**
- Detection: sight range, FOV, layers, alert range
- Patrol: speed, waypoint tolerance, dwell time
- Combat: chase speed, attack range, cooldown
- Suspicion: suspicion time, aggro cooldown

### States

#### IdleState
AI stops and waits. Can transition to patrol or attack based on configuration and events.

#### PatrolState
AI moves between waypoints defined in PatrolComponent. Enemies transition to attack when targets detected.

#### SuspicionState
Timed state after losing sight of target. Returns to patrol/idle if target not regained.

#### AttackState
AI pursues and attacks target. Handles chase behavior and combat coordination.

### Components

#### PatrolComponent
Manages waypoint navigation with support for looping and ping-pong patterns.

**Features:**
- Integration with NavigationPath
- Custom waypoint support
- Event callbacks when waypoints reached
- Gizmo visualization

#### DetectionComponent
Handles target detection using physics-based sensing with FOV and line-of-sight.

**Features:**
- Configurable sight range and FOV angle
- Layer-based detection
- Obstacle avoidance via raycasts
- OnTargetDetected/OnTargetLost events
- Robust null handling

#### AICombatComponent
AI-specific wrapper around CombatComponent for attack behavior.

**Features:**
- Attack range management
- Cooldown tracking
- CanAttack/TryAttack interface
- Integration with existing CombatComponent

#### AIMovementComponent
Wrapper around MovementComponent with NavMeshAgent fallback support.

**Features:**
- NavMeshAgent integration
- Simple transform-based movement fallback
- Speed management (patrol/chase)
- Destination checking
- LookAt functionality

## Usage Examples

### Creating an NPC

```csharp
// 1. Add components
gameObject.AddComponent<AIController>();
gameObject.AddComponent<PatrolComponent>();
gameObject.AddComponent<AIMovementComponent>();
gameObject.AddComponent<HealthComponent>();

// 2. Configure AIController
var controller = GetComponent<AIController>();
// Set AIType to NPC via inspector
// Assign AIData asset via inspector

// 3. Set up patrol waypoints via PatrolComponent
```

### Creating an Enemy

```csharp
// 1. Add all necessary components
gameObject.AddComponent<AIController>();
gameObject.AddComponent<PatrolComponent>();
gameObject.AddComponent<DetectionComponent>();
gameObject.AddComponent<AICombatComponent>();
gameObject.AddComponent<AIMovementComponent>();
gameObject.AddComponent<HealthComponent>();
gameObject.AddComponent<CombatComponent>();
gameObject.AddComponent<MovementComponent>();

// 2. Configure AIController
var controller = GetComponent<AIController>();
// Set AIType to Enemy via inspector
// Assign AIData asset via inspector

// 3. Configure detection layers and combat settings
```

### Creating AIData Assets

1. Right-click in Project window
2. Select `Create > PolyQuest > AI > AI Data`
3. Configure settings for your AI profile
4. Assign to AIController instances

## State Transitions

### NPC Flow
```
Idle ←→ Patrol
```

### Enemy Flow
```
Idle/Patrol → Attack → Suspicion → Idle/Patrol
              ↑                ↓
              └────────────────┘
              (target reacquired)
```

## Events

### Detection Events
- `OnTargetDetected(GameObject)` - Fired when valid target enters detection range
- `OnTargetLost()` - Fired when target leaves detection range or becomes invalid

### Patrol Events
- `OnWaypointReached(Vector3)` - Fired when AI reaches a waypoint

### Combat Events
- Handled internally through existing CombatComponent

### Health Events
- `OnHit` - Triggers aggro behavior in enemies

## Performance Considerations

1. **Detection Interval**: `DetectionComponent` checks for targets every 0.2s by default. Adjust for performance.
2. **Physics Queries**: Uses OverlapSphere and Raycast. Proper layer masks reduce overhead.
3. **State Updates**: States tick every frame. Keep `Tick()` methods lightweight.
4. **Component Queries**: Components are cached in `Awake()`, no runtime GetComponent calls.

## Extension Points

### Adding Custom States

1. Implement `IAIState` interface
2. Add state transition logic in existing states or via events
3. Example: FleeState, InvestigateState, GuardState

### Adding Custom Components

1. Create MonoBehaviour with desired functionality
2. Query via `GetComponent<>()` in states
3. Use events for loose coupling

### Custom AI Behaviors

Subclass `AIController` or create custom states for unique behaviors while maintaining the framework.

## Testing

### Manual Testing
1. Use `ExampleAISetup.cs` to spawn test AIs
2. Enable Gizmos to visualize detection ranges and waypoints
3. Use debug builds to see state names above AI

### Automated Testing
Components and states are designed to be testable:
- States are plain C# objects
- Components expose public methods and events
- State machine can be driven programmatically

## Migration from Legacy

See `Assets/Docs/AI_Migration.md` for complete migration guide from old `EnemyController`/`NPCController`.

**Key Points:**
- Old controllers marked as `[Obsolete]` but still functional
- New system uses `AIController` with `AIType` enum
- Components are modular - only add what you need
- AIData makes configuration reusable

## Backward Compatibility

### LegacyAIController
Abstract base class for old `EnemyController` and `NPCController`. Maintained for compatibility.

### Deprecation Warnings
Compilation will show warnings for old controllers, encouraging migration.

## File Structure

```
AI/
├── AIController.cs           # Main controller
├── AIStateMachine.cs         # State machine
├── IAIState.cs              # State interface
├── AIData.cs                # Configuration ScriptableObject
├── ExampleAISetup.cs        # Usage example
├── LegacyAIController.cs    # Legacy base class
├── EnemyController.cs       # Deprecated enemy controller
├── NPCController.cs         # Deprecated NPC controller
├── NavigationPath.cs        # Waypoint path definition
├── Components/
│   ├── PatrolComponent.cs
│   ├── DetectionComponent.cs
│   ├── AICombatComponent.cs
│   └── AIMovementComponent.cs
└── States/
    ├── IdleState.cs
    ├── PatrolState.cs
    ├── SuspicionState.cs
    └── AttackState.cs
```

## Best Practices

1. **Separate Concerns**: Each component handles one aspect of AI behavior
2. **Data-Driven**: Use AIData assets for different AI profiles
3. **Event-Driven**: Use events for state transitions instead of polling
4. **Composition**: Only add components your AI needs
5. **Reusability**: Share AIData assets between similar AIs
6. **Testing**: Test states and components independently
7. **Documentation**: Use XML comments for public APIs

## Troubleshooting

### "Component not found" errors
- Ensure all required components are added
- Check component references in inspector

### AI not moving
- Verify NavMeshAgent is on NavMesh
- Check MovementComponent is present
- Enable AIMovementComponent fallback

### Enemy not detecting
- Verify detection layers include target
- Check FOV angle is sufficient
- Ensure no obstacles block line-of-sight
- Enable Gizmos to visualize detection

### State transition issues
- Add debug logging in state methods
- Check state transition conditions
- Verify events are being fired

## Future Enhancements

Potential additions to the system:
- Sound/animation integration
- Team/faction system
- Cover system
- Formation AI
- Dynamic difficulty adjustment
- Behavior trees as alternative to state machine
- AI Director for encounter management

## Contributing

When adding new features:
1. Follow existing code patterns
2. Add XML documentation
3. Update this README
4. Test with both NPC and Enemy types
5. Consider performance impact
