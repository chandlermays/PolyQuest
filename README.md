# PolyQuest

**A narrative-first 3D action RPG built in Unity 6 (C#), targeting Steam.**

PolyQuest is my capstone project and an ongoing solo production. The player arrives in the Caledon Basin carrying a relic meant for an old captain of a disbanded Order, is drawn into a conflict with a bandit faction called The Rust, and uncovers the ancient power buried in the Hollowforge. The design rule behind it: **mechanics serve the story**.

I'm the sole developer and designer — systems, gameplay, the Technical Design Document and world lore. Art comes from Synty Studios packs; animations from Mixamo and the Unity Asset Store.

---

## Highlights

- **A test suite of nearly 500 tests** (377 Edit Mode, 110 Play Mode) covering every core system — written to expose real, player-reachable bugs, then used to drive a full fix pass.
- **Data-driven design** with ScriptableObjects for dialogue, quests, items, abilities, progression and combat balance.
- **Robust saving** — atomic writes, `.bak` backups with fallback on load, a separate autosave slot, and isolated per-component restores so one bad entry can't break a save.
- **Branching dialogue** with player choices, conditions, dialogue history, and a close-up dialogue camera.

---

## Systems

| System | What it does |
| --- | --- |
| **Dialogue** | ScriptableObject node graphs with player choices, conditions, triggered events and a persistent dialogue history |
| **Quests** | Unordered objectives with prerequisites, dialogue-gated turn-ins, world-flag objectives (e.g. a cleared camp), and one-step completion with rewards |
| **Inventory & equipment** | Stackable and unique items, equipment slots, drag-and-drop UI, quest items that can't be sold |
| **Combat** | Melee and ranged weapons, projectiles, a damage calculator, factions, health and mana |
| **Abilities & action bar** | Targeted and ground-placed abilities, consumables, saved cooldowns, a shared potion cooldown |
| **Progression** | Experience, levels driven by a growth-curve table, attribute points confirmed before they apply |
| **Economy** | Wallet, shops with configurable stock, weighted enemy loot tables using a seeded XorShift128+ generator |
| **Saving** | `ISaveable` components captured per `SaveableEntity`, JSON serialization, backups, autosave |
| **AI** | NavMesh-driven enemies and NPCs |

---

## Testing

Tests are split into **Edit Mode** (pure logic) and **Play Mode** (scenes, prefabs, save/load round trips, dialogue UI, combat integration).

The goal of the suite is simple: **a failing test means a real bug a player could hit**. Tests are not written to lock in existing behavior. When the suite was introduced, bug-exposing tests were committed red first, so the history shows the tests finding each bug before the commit that fixed it.

**Running the tests:** open **Window → General → Test Runner** in the Unity Editor and run the Edit Mode and Play Mode tabs.

---

## Tech stack

- **Unity 6** (6000.3.8f1), C#
- Universal Render Pipeline
- Input System
- Cinemachine 3
- AI Navigation (NavMesh)
- Unity Test Framework (NUnit)

Code is organized into assembly definitions (`PolyQuest.Game`, `PolyQuest.Tests.PlayMode`) to keep compile times down and test boundaries clear.

---

## Getting started

1. Clone the repository.
2. Open the project with **Unity 6000.3.8f1** (via Unity Hub).
3. Open `Assets/Game/Scenes/Main Menu/Main Menu.unity` and press Play.

`Test Realm` is a sandbox scene for trying out systems in isolation.

---

## Roadmap

- [x] **Milestone 1** — Codebase review, full test suite, and the bug-fix pass it drove
- [ ] **Milestone 2** — Combat redesign: third-person camera with WASD movement, dodge roll with an endurance bar, Tab/click target selection, moving while fighting, and weapon swapping
- [ ] Expanded Caledon Basin: a larger Stonewick, outposts and bandit camps, more verticality
- [ ] Playable demo, ending at the road out of the Caledon Basin

---

## Credits

- 3D art: [Synty Studios](https://syntystore.com/) (POLYGON packs)
- Animations: [Mixamo](https://www.mixamo.com/) and the Unity Asset Store
- Skybox: Fantasy Skybox FREE
- UI: Fantasy RPG UI, RPG inventory icons, Universal Gold Icons, Pixel Cursors
- Outline effect: QuickOutline

Development updates are posted on [LinkedIn](https://www.linkedin.com/in/chandler-mays-40385516b/).
