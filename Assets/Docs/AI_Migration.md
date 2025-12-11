# AI System Migration Guide

This document provides step-by-step instructions for migrating from the legacy `EnemyController` and `NPCController` to the new unified `AIController` system with state machine and component-based architecture.

## Table of Contents

1. [Overview](#overview)
2. [Architecture Changes](#architecture-changes)
3. [Migration Steps](#migration-steps)
   - [Migrating EnemyController](#migrating-enemycontroller)
   - [Migrating NPCController](#migrating-npccontroller)
4. [Component Reference](#component-reference)
5. [Field Mapping Reference](#field-mapping-reference)
6. [Common Issues and Solutions](#common-issues-and-solutions)
7. [Testing and Verification](#testing-and-verification)

---

## Overview

The new AI system introduces:

- **Unified AIController**: Single controller for both NPCs and Enemies, configured via `AIType` enum
- **State Machine**: Explicit states (Idle, Patrol, Suspicion, Attack) with clean transitions
- **Component Composition**: Modular components for patrol, detection, combat
- **ScriptableObject Configuration**: `AIData` assets for easy tuning without code changes
- **Better Testability**: Components and states can be unit tested independently

### Why Migrate?

- **Easier Maintenance**: One AI controller instead of multiple inheritance hierarchies
- **More Flexible**: Mix and match components for different behaviors
- **Designer Friendly**: Configure AI via ScriptableObjects without touching code
- **Scalable**: Add new states and components without modifying core AI logic

---

## Architecture Changes

### Legacy System

```
AIController (abstract base)
├── EnemyController (inherits, adds combat/detection)
└── NPCController (inherits, only patrol)
```

Behavior was hardcoded in `Update()` methods with manual state checking.

### New System

```
AIController (standalone component)
├── Uses AIStateMachine
├── References AIData ScriptableObject
└── Queries for optional components:
    ├── MovementComponent (required)
    ├── CombatComponent (optional, for combat)
    ├── PatrolComponent (optional, for patrol)
    └── DetectionComponent (optional, for enemies)
```

Behavior is defined by states (IdleState, PatrolState, SuspicionState, AttackState) that implement `IAIState` interface.

---

## Migration Steps

### Prerequisites

1. **Create AIData Assets**: Create ScriptableObject assets with tuning values
   - Right-click in Project → Create → PolyQuest → AI → AI Data
   - Configure sight range, patrol speed, attack cooldown, etc.

2. **Backup Your Prefabs**: Make a copy of existing prefabs before migration

3. **Review Component Dependencies**: Ensure GameObjects have required components:
   - `Animator`
   - `NavMeshAgent`
   - `MovementComponent`
   - `CombatComponent` (for enemies)
   - `HealthComponent` (typically)

---

### Migrating EnemyController

#### Step 1: Identify Legacy Settings

Open your existing `EnemyController` prefab/GameObject and note down:

| Legacy Field | Value | Notes |
|--------------|-------|-------|
| m_detectionRange | ___ | How far enemy can see |
| m_alertRange | ___ | Range to alert allies |
| m_suspicionTime | ___ | Time before returning to patrol |
| m_aggroCooldown | ___ | How long aggro lasts after hit |
| m_navigationPath | ___ | Patrol waypoints |
| m_waypointTolerance | ___ | Distance to consider waypoint reached |
| m_waypointDwellTime | ___ | Time to wait at waypoint |
| m_player | ___ | Reference to player GameObject |

#### Step 2: Create or Configure AIData Asset

Create a new `AIData` asset or use existing one:

```
Assets/Game/Data/AI/EnemyAIData.asset
```

Map legacy fields to AIData properties:

| Legacy Field | AIData Property |
|--------------|-----------------|
| m_detectionRange | SightRange |
| m_alertRange | AlertRange |
| m_suspicionTime | SuspicionTime |
| m_aggroCooldown | AggroCooldown |
| m_waypointTolerance | PatrolWaypointTolerance |
| m_waypointDwellTime | WaypointDwellTime |

Configure additional AIData fields:
- `FovAngle`: Field of view (default: 90°)
- `CheckInterval`: Detection check frequency (default: 0.5s)
- `PatrolSpeed`: Movement speed during patrol
- `ChaseSpeed`: Movement speed when chasing target
- `AttackRange`: Maximum attack distance
- `AttackCooldown`: Time between attacks

#### Step 3: Remove EnemyController, Add New Components

On the GameObject:

1. **Remove** the `EnemyController` component (or disable it for testing)

2. **Add** `AIController` component:
   - Set `AI Type` to `Enemy`
   - Assign `AI Data` to your AIData asset
   - Assign `Player Target` (or leave null to auto-find "Player" tag)

3. **Add** `DetectionComponent` component:
   - `Sight Range`: Use AIData value or override
   - `Fov Angle`: Typically 90-120 degrees
   - `Check Interval`: 0.5 seconds recommended
   - `Target Tag`: "Player"
   - `Require Line Of Sight`: Usually true
   - Configure `Target Layers` and `Obstacle Layers` as needed

4. **Add** `PatrolComponent` component (if enemy patrols):
   - Assign existing `Navigation Path` reference, OR
   - Manually add waypoint Transforms to `Waypoint Transforms` list
   - Set `Arrival Radius` (was m_waypointTolerance)
   - Set `Loop Path` (typically true)

5. **Verify** `CombatComponent` is present (should already exist)

6. **Verify** `MovementComponent` is present (should already exist)

#### Step 4: Test and Verify

1. Enter Play mode
2. Verify patrol behavior (if configured)
3. Trigger detection by getting close to enemy
4. Verify enemy transitions to attack state
5. Move out of range and verify suspicion state
6. After suspicion timer, verify return to patrol/idle

#### Step 5: Handle Special Cases

**If using EnemyTracker**:
```csharp
// TODO: Register with EnemyTracker in new system
// Legacy: m_enemyTracker.RegisterEnemy(m_healthComponent);
// New approach: Subscribe to AIController events or handle in custom component
```

**If alerting nearby enemies**:
- This is now handled automatically in `AttackState.AlertNearbyAllies()`
- Uses `AIData.AlertRange`

**If custom aggro behavior**:
- Call `aiController.Aggravate()` to manually trigger aggro
- Subscribe to `HealthComponent.OnHit` event:
  ```csharp
  healthComponent.OnHit += () => aiController.Aggravate();
  ```

---

### Migrating NPCController

#### Step 1: Identify Legacy Settings

Note down from NPCController/base AIController:

| Legacy Field | Value | Notes |
|--------------|-------|-------|
| m_navigationPath | ___ | Patrol waypoints |
| m_waypointTolerance | ___ | Distance to waypoint |
| m_waypointDwellTime | ___ | Wait time at waypoint |

#### Step 2: Create or Configure AIData Asset

Create AIData asset for NPC:

```
Assets/Game/Data/AI/NPCAIData.asset
```

Configure patrol-specific settings:
- `PatrolSpeed`: Movement speed
- `PatrolWaypointTolerance`: Distance to waypoint
- `WaypointDwellTime`: Time to wait at each waypoint

(Detection and combat settings can be left at defaults for NPCs)

#### Step 3: Remove NPCController, Add New Components

On the GameObject:

1. **Remove** the `NPCController` component

2. **Add** `AIController` component:
   - Set `AI Type` to `NPC`
   - Assign `AI Data` to your AIData asset

3. **Add** `PatrolComponent` component:
   - Assign existing `Navigation Path` reference, OR
   - Manually configure waypoint Transforms
   - Set `Arrival Radius` (was m_waypointTolerance)
   - Set `Loop Path` (typically true)
   - Optionally override `Patrol Speed`

4. **Verify** `MovementComponent` is present

#### Step 4: Test and Verify

1. Enter Play mode
2. Verify NPC patrols between waypoints
3. Verify NPC stops and waits at waypoints
4. Verify loop or ping-pong behavior

---

## Component Reference

### AIController

**Purpose**: Main AI brain, manages state machine and component coordination

**Key Properties**:
- `AI Type`: NPC or Enemy
- `AI Data`: ScriptableObject with tuning values
- `Player Target`: Optional player reference for enemies

**Key Methods**:
- `Aggravate()`: Make AI aggressive
- `SetTarget(GameObject)`: Set current attack target
- `ClearAggro()`: Clear aggro state

### AIData (ScriptableObject)

**Purpose**: Configuration asset for AI behavior tuning

**Categories**:
- **Detection**: SightRange, FovAngle, CheckInterval
- **Suspicion**: SuspicionTime
- **Patrol**: PatrolSpeed, PatrolWaypointTolerance, WaypointDwellTime
- **Chase**: ChaseSpeed
- **Combat**: AttackRange, AttackCooldown, AlertRange, AggroCooldown

### PatrolComponent

**Purpose**: Manages waypoint-based patrol behavior

**Key Properties**:
- `Waypoint Transforms`: List of waypoints
- `Navigation Path`: Alternative, use existing NavigationPath object
- `Patrol Speed`: Override AIData patrol speed
- `Arrival Radius`: Distance to consider waypoint reached
- `Loop Path`: Loop vs ping-pong

**Events**:
- `OnPatrolPointReached(int index)`: Fired when waypoint reached

### DetectionComponent

**Purpose**: Detects targets using sphere checks and raycasts

**Key Properties**:
- `Sight Range`: Maximum detection distance
- `Fov Angle`: Field of view in degrees
- `Check Interval`: How often to check for targets
- `Target Tag`: Tag to look for (default: "Player")
- `Require Line Of Sight`: Enable raycast check

**Events**:
- `OnTargetDetected(GameObject)`: Fired when target enters detection
- `OnTargetLost()`: Fired when target leaves detection

### CombatComponent

**Purpose**: Handles attack behavior (existing component, no changes needed)

**Key Methods** (for AI usage):
- `bool CanAttack(GameObject target)`: Check if target is attackable
- `void SetTarget(GameObject target)`: Set attack target
- `void AttackBehavior()`: Perform attack
- `void Cancel()`: Clear target and stop

See `Assets/Scripts/AI/Components/CombatComponentAdapter.cs` for usage examples.

---

## Field Mapping Reference

### EnemyController → New System

| Legacy Field | New Location | Notes |
|--------------|--------------|-------|
| m_detectionRange | AIData.SightRange or DetectionComponent.SightRange | |
| m_alertRange | AIData.AlertRange | Used in AttackState |
| m_suspicionTime | AIData.SuspicionTime | Used in SuspicionState |
| m_aggroCooldown | AIData.AggroCooldown | Tracked by states |
| m_navigationPath | PatrolComponent.m_navigationPath | Optional for enemies |
| m_waypointTolerance | AIData.PatrolWaypointTolerance or PatrolComponent.ArrivalRadius | |
| m_waypointDwellTime | AIData.WaypointDwellTime | |
| m_player | AIController.m_playerTarget | Can be left null to auto-find |
| m_enemyTracker | Custom integration | See special cases |

### NPCController → New System

| Legacy Field | New Location | Notes |
|--------------|--------------|-------|
| m_navigationPath | PatrolComponent.m_navigationPath | Can reuse existing NavigationPath |
| m_waypointTolerance | AIData.PatrolWaypointTolerance or PatrolComponent.ArrivalRadius | |
| m_waypointDwellTime | AIData.WaypointDwellTime | |

### Method Mapping

| Legacy Method | New System Equivalent |
|---------------|----------------------|
| EnemyController.Aggravate() | AIController.Aggravate() |
| EnemyController.AttackState() | Handled by AttackState.Tick() |
| EnemyController.SuspicionState() | Handled by SuspicionState.Tick() |
| EnemyController.IsPlayerDetected() | DetectionComponent.HasTarget |
| AIController.PatrolState() | Handled by PatrolState.Tick() |

---

## Common Issues and Solutions

### Issue: AI doesn't move

**Cause**: MovementComponent or NavMeshAgent not configured

**Solution**:
- Verify `MovementComponent` is present
- Verify `NavMeshAgent` is present and enabled
- Verify GameObject is on NavMesh (use NavMesh visualization)
- Check `AIData.PatrolSpeed` or `PatrolComponent.PatrolSpeed` is > 0

### Issue: Enemy doesn't detect player

**Cause**: DetectionComponent misconfigured or player not in range/FOV

**Solution**:
- Check `DetectionComponent.SightRange` is large enough
- Verify player has correct tag (default: "Player")
- Check `Target Layers` includes player layer
- Disable `Require Line Of Sight` temporarily to test
- Use Gizmos (select AI GameObject) to visualize detection range and FOV

### Issue: Enemy detects player but doesn't attack

**Cause**: Missing CombatComponent or target not valid

**Solution**:
- Verify `CombatComponent` is present
- Check `CombatComponent.CanAttack(player)` returns true
- Verify player has `HealthComponent`
- Check weapon range in CombatComponent
- Verify `AIData.AttackRange` is configured

### Issue: NPC doesn't patrol

**Cause**: No waypoints or PatrolComponent misconfigured

**Solution**:
- Verify `PatrolComponent` is present
- Check waypoints are assigned (either via `Navigation Path` or `Waypoint Transforms`)
- Verify `PatrolComponent.HasWaypoints` is true
- Check `PatrolComponent.ArrivalRadius` is reasonable (1-2 units)

### Issue: Compilation errors about AIControllerNew

**Cause**: References not updated after class rename

**Solution**:
- All references should use `AIController` (not `AIControllerNew`)
- Check state files in `Assets/Scripts/AI/States/`
- Rebuild solution in IDE if using Visual Studio

### Issue: Legacy warnings in console

**Cause**: Old EnemyController/NPCController still present

**Solution**:
- These warnings are expected during migration
- Remove deprecated components once migration is verified
- Use adapter scripts as temporary bridge if needed

---

## Testing and Verification

### Manual Test Checklist

#### For Enemies:
- [ ] Enemy patrols waypoints when no target detected
- [ ] Enemy stops and waits at waypoints
- [ ] Enemy detects player when in range and FOV
- [ ] Enemy chases player when detected
- [ ] Enemy attacks player when in range
- [ ] Enemy enters suspicion state when player leaves detection
- [ ] Enemy returns to patrol after suspicion timer
- [ ] Enemy responds to Aggravate() call
- [ ] Nearby enemies are alerted when one attacks

#### For NPCs:
- [ ] NPC patrols waypoints continuously
- [ ] NPC stops and waits at waypoints
- [ ] NPC loops or ping-pongs correctly
- [ ] NPC speed matches configuration

### Automated Testing

Consider adding unit tests for:
- State transitions in `AIStateMachine`
- Component null safety in states
- Detection range calculations in `DetectionComponent`
- Waypoint cycling in `PatrolComponent`

### Performance Testing

Monitor:
- Physics checks per frame (DetectionComponent uses rate-limiting)
- State machine overhead (minimal, simple dictionary lookups)
- NavMesh path calculations (same as before)

---

## Best Practices

1. **Use Prefabs**: Configure AI in prefabs, not at runtime
2. **Share AIData**: Create shared AIData assets for enemy types (e.g., "MeleeEnemyAI", "RangedEnemyAI")
3. **Visualize in Editor**: Use Gizmos to verify ranges and waypoints
4. **Iterate in Inspector**: Tune values in AIData assets without code changes
5. **Keep States Simple**: States should be thin wrappers around component calls
6. **Add Components Sparingly**: Only add components needed for behavior
7. **Document Special Behavior**: If custom states/components needed, document clearly

---

## Need Help?

If you encounter issues not covered here:

1. Check component warnings in Console
2. Review `ExampleAISetup.cs` for setup patterns
3. Check adapter scripts for field mapping TODOs
4. Review state code in `Assets/Scripts/AI/States/`
5. Verify all required components are present

---

## Future Enhancements

Planned improvements to the AI system:

- **Additional States**: Flee, Investigate, Follow, Cinematic
- **AI Templates**: Prefab-based AI configurations for common archetypes
- **Visual State Editor**: Graph-based state machine editor
- **Behavior Trees**: More complex decision making for advanced AI
- **Navigation Improvements**: Dynamic cover points, tactical positioning
- **Squad AI**: Coordinated group behavior

---

## Summary

The new AI system provides:
- ✅ Unified controller for all AI types
- ✅ Modular component composition
- ✅ Designer-friendly configuration
- ✅ Clear state machine architecture
- ✅ Backward compatible via adapters
- ✅ Unit testable components

Migration is straightforward:
1. Create AIData assets with your settings
2. Replace old controller with AIController + components
3. Test and verify behavior
4. Remove deprecated controllers

Welcome to the new AI system! 🎮
