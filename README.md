Cell State System — Unity Gameplay Systems Showcase

The Cell State system is a modular, event-driven grid cell state system built in C# for Unity. Designed to handle dynamic environmental hazards — fire, ice, oil, fog, and danger zones — on a tile-based grid. The system is fully decoupled via an event bus and driven entirely by data — making it trivial to extend without touching existing logic.


##  File Structure

```
CellState System
┣  CellStateData.cs           — Runtime data classes; one per state type (the live data layer)
┣  CellStateInstance.cs       — Runtime instance tracking duration, position & ownership
┣  CellStateManager.cs        — Central controller: event subscriptions & state orchestration
┣  CellStateRegistry.cs       — Data layer: initialisation, lookups & asset loading
┣  CellStateSettings.cs       — ScriptableObject configuration for all state types
┣  CellStateEvents.cs         — Structs defining all system-wide events
┃
┣ Concrete State Implementations
┃  ┣  BurningCellState.cs     — Per-turn damage to occupying unit; FMOD audio on tick
┃  ┣  BaseCellState.cs           — Abstract base class defining the state lifecycle interface
┃  ┣  BaseCellState<T>.cs        — Generic subclass for typed cell state data binding
┃  ┣  DangerCellState.cs      — Delayed burst damage triggered at end of state life
┃  ┣  FoggyCellState.cs       — Applies invisibility status on enter, removes on exit; covers cell
┃  ┣  OilyCellState.cs        — Converts to fire on ignition; spreads burn to AoE cells
┃  ┣  SlipperyCellState.cs    — Pushes units from the cell on movement; applies slip damage
┃  ┗  StickyCellState.cs      — Modifies cell exit cost on apply; restores on cleanup

```

##  Architecture & Data Flow

### System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      EXTERNAL SYSTEMS                        │
│           (Combat, Movement, Spell Systems, etc.)            │
└───────────────────────────┬─────────────────────────────────┘
                            │ Publishes Events
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                        EVENT BUS                             │
│  CellStateCreated │ CellStateDestroyed │ EffectInvoked       │
│  UnitEnteredCellState │ UnitExitedCellState                  │
└──────────────┬───────────────────────────┬──────────────────┘
               │ Subscribes                │ Provides Data
               ▼                           ▼
┌──────────────────────┐      ┌────────────────────────────────┐
│   CellStateManager   │────▶│       CellStateRegistry         │
│   (Orchestration)    │      │   (Data / Asset Management)     │
└──────────┬───────────┘      └──────────────┬─────────────────┘
           │ Creates / Manages               │ Loads from Addressables
           ▼                                 ▼
┌──────────────────────┐      ┌────────────────────────────────┐
│  CellStateInstance   │      │      CellStateSettings          │
│  (Runtime Instance)  │      │   (ScriptableObject Config)     │
└──────────┬───────────┘      └──────────────┬─────────────────┘
           │ Delegates to                     │ Initialises
           ▼                                 ▼
┌──────────────────────┐      ┌────────────────────────────────┐
│   BaseCellState<T>   │◀─────│        CellStateData            │
│  (State Behaviour)   │      │  (Typed Runtime Data Objects)   │
└──────────────────────┘      └────────────────────────────────┘
```

### Three-Layer Data Pipeline

A key architectural decision is the **clean separation of data across three distinct layers**, each with its own responsibility:

```
┌─────────────────────────────────────────────────────────────────────┐
│  LAYER 1 — DESIGNER CONFIG          CellStateSettings.cs            │
│  ScriptableObject assets edited in the Unity Inspector.             │
│  Stores base values: duration, danger rating, override chain.       │
│  Never changes at runtime. Zero coupling to gameplay code.          │
├─────────────────────────────────────────────────────────────────────┤
│  LAYER 2 — RUNTIME DATA             CellStateData.cs                │
│  Typed data classes (one per state) derived from CellStateData.     │
│  Initialised from Layer 1 at startup. Can be mutated during play    │
│  (e.g. burn stacks accumulate, movement costs are temporarily       │
│  modified). Holds the live values that gameplay logic reads from.   │
├─────────────────────────────────────────────────────────────────────┤
│  LAYER 3 — BEHAVIOUR                BaseCellState<T>.cs             │
│  Abstract state classes that consume Layer 2 data.                  │
│  Implement OnEnter, OnTick, OnExit, OnUnitEnter, OnUnitExit.        │
│  Zero knowledge of where data came from or how it was configured.   │
└─────────────────────────────────────────────────────────────────────┘
```

This pipeline means designers tune values in the Inspector, programmers write behaviour in C#, and the two **never need to touch each other's work**.

---

##  File Breakdown

### `CellStateData.cs` — Typed Runtime Data Objects

The **live data layer** of the system. Each cell state type has its own strongly-typed data class derived from the `CellStateData` base, holding the runtime values that state behaviour classes read and write during gameplay. These are initialised from `CellStateSettings` at startup and can mutate during play.

**`CellStateData` — Base Class Properties:**

| Property | Type | Description |
|---|---|---|
| `DangerCost` | `int` | Used for the project's pathfinding system which prioritize safer Grid Cells (in project refrence for a tile |
| `Duration` | `int` | How many game turns this state persists before expiring |
| `overridingCellStates` | `List<ECellStateType> ` | A list of enums representing the types of cell states (status effects) that it can be ovveriden (removed) by if they are added to the same cell state as them |

**Derived Classes & Their Runtime Properties:**

####  `BurningCellStateData`
| Property | Description |
|---|---|
| `BurnDamagePerTurn` | Damage dealt to units on the cell each turn |

####  `OilyCellStateData`
| Property | Description |
|---|---|
| `AdditionalBurnStacks` | Extra burn stacks applied to cell occupant when ignited (very confusing variable name in retrospect, I acknowledge that|

####  `SlipperyCellStateData`
| Property | Description |
|---|---|
| `SlipDistance` | How many extra tiles a unit slides when entering the cell |
| `SlipDamage` | Damage caused by slip |

####  `StickyCellStateData`
| Property | Description |
|---|---|
| `tempModifiedExitMovementCost` | The cost to leave the cell when effect is applied |
| `defaultExitMovementCost` | The cost to leave cell when effect is not applied; this variable is applied upon the expiration of cell state |

####  `DangerCellStateData`
| Property | Description |
|---|---|
| `Damage` | Damage applied by cell state before expiry |

####  `FoggyCellStateData`
| Property | Description |
 *None* - Purely custom behavior

**Why This Design?**

Rather than a single monolithic data class with nullable fields for every possible property, each state type owns *only the data it needs*. This avoids bloated objects, makes the intent of each class immediately readable, and means adding a new state type requires zero changes to existing data classes

**Design Principles Demonstrated:**
-  **Inheritance over Composition (where appropriate)** — shared base guarantees every data object has `DangerCost` and `Duration` for the override and expiry systems
-  **Single Responsibility** — each data class holds only the properties relevant to its state type
-  **Open/Closed Principle** — new state data is added as a new subclass, zero changes to the base or registry
-  **Type Safety via Generics** — `BaseCellState<T>` binds directly to the correct data class, eliminating runtime casting

---

### `BaseCellState.cs` — State Behaviour Interface

The abstract foundation every cell state behaviour inherits from. Defines a clean **lifecycle contract** all states must fulfil, ensuring the manager can call any state uniformly without knowing its concrete type.

**Key Overridable Methods:**

| Method | Trigger | Purpose | 
|---|---|---|
| `OnTurnStart(instance, e)` | when a battle participant's turn starts, an event is invoked | invoke behvarior at start of turn |
| `OnTurnEnd(instance, e)` | when a battle participant's turn ends, an event is invoked| invoke behvarior at End of turn |
| `OnCellStateLifeEnd(instance)` | State duration reaches zero | Execute any behavior BEFORE being process from removal (from cell state manager) |
| `OnCellEnter(instance, e)` | when OnCellEnter is published | Invoke behavior when entity enters a cell |
| `OnCellExit(instance, e)` | when OnCellExit event is published | Invoke behavior when entity leaves a cell |
| `OnUnitMovingBetweenCells(instance, e)` | A unit is in transit between cells | Invoke behavior during movement |
| `OnStatusEffectApplied(e)` | when OnStatusEffect is published | Invoke behavior when an eneity recieves a status effect|
| `ImmediateEffect(instance)` | not triggered by an event; is directly called/ used | Invoke behavior upon the creation of cell state |
| `CleanUpEffect(instance)` | State is removed or expired | Undo persistent mutations (e.g. restore original exit cost or removing invisibility from any occupant in a foggy cell (I resued invisibility to be efficent) |

Most take an intance (CellStateInstance and an Event) as a parameter, using data from both to properly execute their functions. For example OnTurnEnd event has a uint property of the battle participant ID. With that property, I can use the cellInstancesByOwner dictionary to get cell state's owned by said partipant, and tick their durations down by one point at the end of a turn.

The generic subclass `BaseCellState<T>` binds a concrete `CellStateData` derived class, giving each state behaviour direct, fully type-safe access to its runtime data with zero casting:

```csharp
// The generic constraint locks T to CellStateData subclasses only
public abstract class BaseCellState<T> : BaseCellState where T : CellStateData
{
    protected T cellStateData { get; private set; }
    public BaseCellState(T data) => cellStateData = data;
}
```

**Design Principles Demonstrated:**
-  **Template Method Pattern** — base class defines the full overridable lifecycle skeleton; subclasses only implement what they need
-  **Open/Closed Principle** — new state types are added purely by subclassing, zero modifications to the base
-  **Liskov Substitution Principle** — the manager treats all states as `BaseCellState` uniformly and safely
-  **Interface Segregation** — no state is forced to implement methods it doesn't use; all lifecycle hooks are opt-in overrides

---

### Concrete State Implementations

Each concrete state is a minimal, focused class that inherits from `BaseCellState<T>`, overrides a function based on the event it is reponsible for invoking it's behavior upon.

---

####  `BurningCellState`

Deals damage to whichever entity occupies the cell at the start **and** end of each turn. Uses the `ServiceLocator` to query the grid, resolves the occupying entity by ID, then publishes an `OnHit` event through the event bus — keeping the damage system fully decoupled.

```csharp
public override void OnTurnStart(CellStateInstance instance, OnTurnStart e)
{
    Burn(instance);
}

public override void OnTurnEnd(CellStateInstance instance, OnTurnEnd e)
{
    Burn(instance);
}

 public void Burn(CellStateInstance instance)
        {
            if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;
            
            GridCell grid = gridManagementService.GetCell(instance.cellPosition);
            if (grid == null)
                return;
            
            uint occupantId = grid.occupyingEntityId;
            //if occupantId is uint max value, then the cell is not occupied, and we can skip the rest of the method
            if(occupantId == UInt32.MaxValue)
                return;
            IBattleEntity entity = EntityRegistry.GetEntityById(occupantId);
            if (entity == null)
            {
                Debug.LogWarning(
                    $"Entity with ID {occupantId} not found for BurningCellState. Cannot apply damage.");
                return;
            }
            Debug.Log($"Burning State place by entity {instance.sourceEntityID} burnt entity {occupantId} for {cellStateData.burnDamagePerTurn} damage.");

            EventBus.Publish(new OnHit()
            {
                damage = cellStateData.burnDamagePerTurn,
                damageSourceId = instance.sourceEntityID, 
                damageTargetId = occupantId,
                notifyOtherProcs = true,
                damageSourceData = new ActionSourceData
                {
                    actionSourceType = EActionSourceType.ENVIRONMENT,
                    actionEffectType = EActionEffectType.DAMAGE,
                    actionSourceGUID = string.Empty
                }
            });
            
            instance.PublishCellStateInvocation();
            AudioManager.Instance.PlayOneShot(FMODEvents.burn);
        }
```

The shared `Burn()` method resolves the occupant, guards against empty cells (`occupantId == UInt32.MaxValue`), and publishes damage tagged as `EActionSourceType.ENVIRONMENT` — meaning it correctly bypasses ability-specific proc logic in the combat system. FMOD's `burn` one-shot fires on every damage application.

**Key design choices:**
- `UInt32.MaxValue` as a sentinel for "no occupant" avoids a nullable type on a hot-path field
- `EActionSourceType.ENVIRONMENT` ensures burn damage is classified correctly by the broader combat system without any hardcoded special-casing
- Calling `instance.PublishCellStateInvocation()` after damage keeps VFX triggers decoupled from the damage logic
- publishing `OnHit` to notify other system to damage the entity, therby invoking  

---

####  `DangerCellState`

A **delayed area threat** — does nothing while active, then detonates damage at the exact moment its duration ends via `OnCellStateLifeEnd()`. This models telegraphed attacks (e.g. a boss marking a tile that explodes after two turns).

```csharp
public override void OnCellStateLifeEnd(CellStateInstance instance)
{
    // Resolve occupant, then publish OnHit tagged as ABILITY source
    EventBus.Publish(new OnHit()
    {
        damage = cellStateData.Damage,
        damageSourceId = instance.sourceEntityID,
        damageTargetId = occupantId,
        damageSourceData = new ActionSourceData
        {
            actionSourceType = EActionSourceType.ABILITY, // triggers ability procs
            actionEffectType = EActionEffectType.DAMAGE,
        }
    });
}
```

Unlike `BurningCellState`, this is tagged `EActionSourceType.ABILITY` — meaning it *will* trigger ability-related procs and reactions in the combat system, treating it as an intentional player or enemy ability rather than a passive hazard.

---

####  `FoggyCellState`

Manages **cell visibility and unit invisibility** as a paired state. On `ImmediateEffect`, the cell is marked as covered (`cell.isCovered = true`). On `CleanUpEffect`, it's uncovered. When a unit enters, an `OnStatusEffectGained` event grants invisibility using a hardcoded `INVISIBLE_GUID` with a sentinel stack value (`23092003`) representing infinite stacks — meaning it persists until explicitly removed. If an entity leaves a 'foggy' cell, invisibility is removed, showing that it is no longer 'hidden.'

```csharp
private const string INVISIBLE_GUID = "a62dfd12-ff3c-4580-8504-f9724d541e3e";

public override void CleanUpEffect(CellStateInstance instance)
{
    SetCellVisibility(instance.cellPosition, false);
    base.CleanUpEffect(instance); 
}

public override void OnCellEnter(CellStateInstance instance, OnCellEnter e)
{ 
    EventBus.Publish(new OnStatusEffectGained()
    {
        statusId = INVISIBLE_GUID,
        stackAmount = 23092003, // Infinite stacks until removed
        sourceId = instance.sourceEntityID,
        targetId = e.unitId,
        notifyOtherProcs = true,
    });
    instance.PublishCellStateInvocation();
}

public override void OnCellExit(CellStateInstance instance, OnCellExit e)
{
    IBattleEntity entity = EntityRegistry.GetEntityById(e.unitId);
    if (entity == null)
    {
        Debug.LogWarning($"Entity with ID {e.unitId} not found for SlipperCellState. Cannot apply evade.");
        return;
    }
    if (entity.entityGO.TryGetComponent(out BaseUnit unit))
        unit.RemoveStatusEffect(INVISIBLE_GUID);
}


public override void ImmediateEffect(CellStateInstance instance)
{
    SetCellVisibility(instance.cellPosition, true);
}

private void SetCellVisibility(Vector2Int position, bool visibility)
{
    if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
        return;
    
    GridCell cell = gridManagementService.GetCell(position);
    if (cell == null)
        return;

    cell.isCovered = !visibility;
}
```

I expropriated invisibility's functionality to work for the foggy cell state instead of making my own custom behavior. I simply had to manage it through OnCellExit and OnCellEnter.

---

####  `OilyCellState`

The most complex state — it **listens for a global `OnStatusEffectGained` event** and reacts specifically when a burn status is applied anywhere near oily cells. On ignition it:

1. Finds all cardinal + origin cells within range 1 of the burning entity
2. For each cell that has an active oily instance, publishes a `CellStateEvents.OnCellStateCreated` to convert it to `Burning`
3. Scales burn stacks by the **number of oily instances** on each cell — rewarding stacked oil applications with more intense burns
4. If the newly ignited cell is occupied, applies burn status directly to that unit

```csharp
public override void OnStatusEffectApplied(OnStatusEffectGained e)
{
    if (!ServiceLocator.FindService(out GridManagementService gridManagementService))
        return;

    if (e.statusId != BURNING_GUID)
        return;

    uint sourceID = e.sourceId;
    IBattleEntity entity = EntityRegistry.GetEntityById(sourceID);
    if (entity == null)
        return;

    if (!entity.entityGO.TryGetComponent(out BaseUnit unit))
        return;

    System.Collections.Generic.List<GridCell> aoeCells = gridManagementService.GetCellsInDirectionFromOrigin(EDirectionType.Cardinal,
        gridManagementService.GetEntityPosition(e.targetId), 1, out GridCell originCell);
    aoeCells.Add(originCell);

    foreach (GridCell cell in aoeCells)
    {
        if (!IsValidPosition(cell.gridPosition, out List<CellStateInstance> instances))
            continue;

        //replaces 'Oily' CellState with 'Burning' CellState
        EventBus.Publish(new CellStateEvents.OnCellStateCreated()
        {
            participantID = unit.GetOwnerParticipant().ParticipantId,
            sourceEntityID = e.sourceId,
            cellStateType = ECellStateType.Burning,
            gridCellPosition = cell.gridPosition
        });

        foreach (CellStateInstance oilyInstance in instances)
        {
            EventBus.Publish(new CellStateEvents.OnCellStateEffectInvoked()
            {
                gridCellPosition = oilyInstance.cellPosition,
                sourceEntityID = oilyInstance.sourceEntityID,
                participantID = oilyInstance.sourceEntityOwnerID,
                cellStateType = oilyInstance.cellStateType,
            });
        }

        if (!cell.isOccupied)
            continue;

        EventBus.Publish(new OnStatusEffectGained()
        {
            sourceId = e.sourceId,
            targetId = cell.occupyingEntityId,
            statusId = BURNING_GUID,
            stackAmount = (uint)cellStateData.additionalBurnStacks * (uint)instances.Count, //TODO: Check on Xander if this is cool ~ NB
            notifyOtherProcs = true
        });
    }
}

private bool IsValidPosition(Vector2Int gridPosition, out List<CellStateInstance> instances)
{
    instances = null;
    if (!ServiceLocator.FindService(out CellStateManager cellStateManager))
        return false;

    instances = cellStateManager.GetAllInstancesOfTypeAtPosition(ECellStateType.Oily, gridPosition).ToList();
    return instances != null && instances.Count > 0;
}
```

The `IsValidPosition()` helper queries `CellStateManager` for all oily instances at a position, making the stack count calculation data-driven rather than hardcoded. A `TODO` comment flags a design question for a teammate (`~NB`) — demonstrating real collaborative development workflow.

Note: on retrospect, additonal
---

####  `SlipperyCellState`

Intercepts movement via `OnUnitMovingBetweenCells` and **pushes the unit away from the entry point** using the grid service's `PushUnitFromPoint` method. The push distance and slip damage are both sourced directly from `cellStateData`, keeping all tuning in the data layer.

```csharp
public override void OnUnitMovingBetweenCells(CellStateInstance instance, CellStateEvents.OnUnitMoveToCell e)
        {
            if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
                return;
            
            IBattleEntity entity = EntityRegistry.GetEntityById(e.unitId);
            if (entity == null)
                return;
            
            if (!entity.entityGO.TryGetComponent(out BaseUnit unit))
                return;    

            gridManagementService.PushUnitFromPoint(unit, 
                e.fromCellPosition,
                cellStateData.slipDistance, 
                cellStateData.slipDamage, 
                instance.sourceEntityID); 
            
            instance.PublishCellStateInvocation();
            //TODO: Play sfx here
        }
```

The expropriated push functionality into here.

---

####  `StickyCellState`

Uses `ImmediateEffect` and `CleanUpEffect` to **directly mutate the grid cell's `exitCost` property**, making it expensive to leave the tile. On cleanup, the cost is explicitly restored to `defaultExitMovementCost` — guaranteeing no persistent grid corruption after the state expires.

```csharp
  public override void CleanUpEffect(CellStateInstance instance)
  {
      if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
          return;
      
      GridCell cell = gridManagementService.GetCell(instance.cellPosition);
      if(cell == null)
          return;

      cell.dangerCost = (uint)cellStateData.defaultExitMovementCost;
      base.CleanUpEffect(instance);
  }

  public override void ImmediateEffect(CellStateInstance instance)
  {
      if(!ServiceLocator.FindService(out GridManagementService gridManagementService))
          return;
      
      GridCell cell = gridManagementService.GetCell(instance.cellPosition);
      if(cell == null)
          return;
      
      instance.PublishCellStateInvocation();
      cell.exitCost = (uint)cellStateData.tempModifiedExitMovementCost;
  }
```

This is a clean example of **reversible state mutation** — any change made by `ImmediateEffect` has a guaranteed counterpart in `CleanUpEffect`, making the system robust against unexpected early removal or override.

---

### `CellStateInstance.cs` — Runtime Instance Tracking

A lightweight container representing **one active state on one specific cell** at runtime. Deliberately thin — it holds the *where*, *who*, and *how long*, and delegates all behaviour to its `BaseCellState` handler.

**Key Properties:**

| Property | Type | Description |
|---|---|---|
| `StateType` | `CellStateType` | Enum identifier for this state |
| `GridPosition` | `Vector2Int` | The tile this instance lives on |
| `Owner` | `GameObject` | The entity that applied this state |
| `RemainingDuration` | `int` | Ticks remaining before expiry |
| `DangerRating` | `int` | Sourced from `CellStateData.DangerCost` — used by override checks |
| `StateHandler` | `BaseCellState` | The behaviour object that handles lifecycle calls |

**Key Methods:**
- `DecrementDuration()` — decrements the tick counter and publishes an effect event
- `PublishEffectEvent()` — fires a `CellStateEffectInvokedEvent` through the event bus

```csharp
public void DecrementDuration()
{
    RemainingDuration--;
    PublishEffectEvent();
}
```

**Design Principles Demonstrated:**
-  **Separation of Concerns** — instance metadata is cleanly split from behaviour logic
-  **Encapsulation** — duration management and event publishing are self-contained
-  **Composition over Inheritance** — the instance *has a* behaviour handler rather than *being* one

---

### `CellStateManager.cs` — Central Orchestrator

The **brain of the system**. Subscribes to game-wide events and coordinates the full lifecycle of cell states — from creation and override checks, through per-tick effects, to expiry and cleanup.

**Core Responsibilities:**

| Method | Responsibility |
|---|---|
| `OnCellStateCreated()` | Validates, instantiates, and activates new cell state instances |
| `OnCellStateDestroyed()` | Removes a state and calls `OnExit()` for cleanup |
| `OnEffectInvoked()` | Routes effect events to the correct state handler's `OnEffectInvoked()` |
| `OnUnitEntered()` | Notifies the state at the entered position via `OnUnitEnter()` |
| `OnUnitExited()` | Notifies the state at the exited position via `OnUnitExit()` |
| `AddCellState()` | Applies override logic before registering a new state |
| `ClearCellState()` | Fully removes a state and cleans up all registry references |
| `TickAllStates()` | Iterates all active instances and decrements their duration |

**State Override ** — When applying a new cell state to a grid cell, it will retrieve `List<ECellStateType> overridingCellStates` property fron the baseCellState property of the cell state instance; it will produce a hashmap of instances of the types that are present in overridingCellStates, and remove them from that cell. This can be changed in the editor and was done give designers to freedom to decide which cell state cancels which, or to ensure there can be no more that one instance of a type.

**Design Principles Demonstrated:**
-  **Observer / Event-Driven Pattern** — zero coupling between this manager and any external system
-  **Single Responsibility** — the manager orchestrates; it never implements state-specific logic
-  **Defensive Programming** — event unsubscription in `OnDestroy()` prevents memory leaks and ghost callbacks
-  **Dependency Inversion** — depends entirely on abstractions (events, `BaseCellState`, `CellStateData`) never on concrete state types

---

### `CellStateRegistry.cs` — Data & Asset Management

The **data layer** of the system. Loads `CellStateSettings` from Unity's Addressable asset catalogue at startup, constructs typed `CellStateData` objects and `BaseCellState<T>` behaviour instances, and provides O(1) lookups at runtime via multiple dictionaries.

**Data Structures:**

| Dictionary | Key | Value | Purpose |
|---|---|---|---|
| `_instancesByPosition` | `Vector2Int` | `CellStateInstance` | Positional lookup — useful for grid based events/ procs? |
| `_instancesByOwner` | `GameObject` | `List<CellStateInstance>` | Filters by battleparticipant, used for turn procs? |
| `_instancesByType` | `CellStateType` | `List<CellStateInstance>` | Type queries — where is fire currently active? |
| `_stateDefinitions` | `CellStateType` | `BaseCellState` | Cached behaviour objects per state type |
| `_stateData` | `CellStateType` | `CellStateData` | Cached runtime data objects per state type |

**Initialisation Flow:**
1. Load `CellStateSettings` ScriptableObject from the Addressable asset catalogue
2. Iterate every `CellStateType` enum value
3. Instantiate the correct `CellStateData` subclass (e.g. `BurningCellStateData`) from settings
4. Instantiate the matching `BaseCellState<T>` subclass and inject the data object
5. Cache both in their respective dictionaries for O(1) runtime retrieval
6. All subsequent state creation pulls from these pre-built caches — no runtime allocation of new definition objects

**Disposal** — implements `IDisposable`. All dictionaries are cleared on disposal, severing managed references and allowing GC to collect state objects cleanly.

**Design Principles Demonstrated:**
-  **Repository Pattern** — single source of truth for all state data and instance lookups
-  **Factory Initialisation** — all definition objects built once at startup; zero per-state-creation overhead
-  **RAII / IDisposable** — guaranteed cleanup contract, no manual teardown required by callers
-  **Data-Driven Initialisation** — the registry constructs the entire system from asset data; the code never hard-codes a state's values

---

### `CellStateSettings.cs` — ScriptableObject Configuration

A Unity `ScriptableObject` acting as the **designer-facing configuration layer**. Contains a `CellStateSettingsEntry` for every state type, fully editable in the Unity Inspector. No code changes needed for tuning.

**`CellStateSettingsEntry` — Shared Config Properties:**

| Property | Description |
|---|---|
| `DangerRating` | Override priority; feeds into `CellStateData.DangerCost` at initialisation |
| `Duration` | Base tick count; copied into the runtime `CellStateData.Duration` |
| `OverridingStateType` | Which states this state can ovveride/ replace on a grid cell (e.g. `Oily` → `Burning`) |

**Per-State Config Highlights:**

| State | Designer-Tunable Values |
|---|---|
| **Burning** | Damage per tick, spread chance, burn stack count |
| **Oily** | Movement modifier, ignition target state type |
| **Slippery** | Slip distance, exit movement cost override |
| **Sticky** | Movement cost multiplier, escape difficulty |
| **Danger** | Danger rating, trigger radius |
| **Foggy** | Visibility reduction, unit concealment toggle |

**Design Principles Demonstrated:**
-  **Data-Driven Design** — designers iterate in the Inspector, programmers don't touch tuning values
-  **ScriptableObject Pattern** — single shared asset instance, no per-object data duplication
-  **State Chaining via Data** — `OverridingStateType` enables emergent multi-step interactions (oily → burning → danger) purely through config

---

### `CellStateEvents.cs` — Decoupled Event Contracts

Defines all event **structs** used by the system. Using value-type structs (not classes) for events ensures zero heap allocation on high-frequency gameplay messages.

**Event Contracts:**

| Event Struct | Key Payload | Fired When |
|---|---|---|
| `CellStateCreatedEvent` | `CellStateType`, `Vector2Int`, `GameObject` owner | An external system requests a state be applied |
| `CellStateDestroyedEvent` | `CellStateInstance` | A state expires or is forcibly removed |
| `CellStateEffectInvokedEvent` | `CellStateInstance` | A state triggers its per-tick effect |
| `UnitEnteredCellStateEvent` | `Unit`, `Vector2Int` | A unit moves onto a state-affected tile |
| `UnitExitedCellStateEvent` | `Unit`, `Vector2Int` | A unit moves off a state-affected tile |

**Design Principles Demonstrated:**
-  **Event-Driven Architecture** — zero coupling between any two systems; all communication is message-based
-  **Value-Type Events** — struct-based events avoid heap pressure during high-frequency game loops
-  **Explicit Contracts** — each struct is a self-documenting interface between systems; readable without context

This also allows other systems to listen an trigger their own behavior upon an event published by the Cell State Manager. Could be used -for example- by a VFX service that will instansiate or invoke a vfx upon cell state creation, invocation or destruction.
---


## Technologies & Tools

- **Engine:** Unity (C#)
- **Asset Loading:** Unity Addressables / Asset Catalogue
- **Patterns:** Template Method, Observer, Repository, Factory, Three-Layer Data Pipeline, RAII
- **Key C# Features:** Generics, Abstract Classes, IDisposable, Struct Events, ScriptableObjects

---

*Built as part of a game in current development (as of 03/04/2026): Terminal Horizon - https://store.steampowered.com/app/4358600/Terminal_Horizon/*

