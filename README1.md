# Journey of Endless

> *"Get up. We are not finished, Zenki."* — Ulteria

A 2D fantasy platformer-RPG prototype built in Unity with C#. 
This is a **test level** featuring the game's core movement, combat, 
and one boss encounter.

---

## 📖 Synopsis

**Zenki** is a 15-year-old boy with white hair, wearing a tattered hood 
and rusted armour older than he is. He wields **Ulteria** — a sentient, 
shape-shifting sword that never stops talking.

Zenki can tear holes in reality itself. Ulteria can grow, glow, and 
release bursts of angelic light. Together they are less a hero and his 
weapon, and more two reluctant partners who argue their way through 
every fight.

This prototype ends in a ruined stone hall. A **Golem** blocks the only 
way forward.

---

## 🎮 Controls

| Action            | Key         |
|-------------------|-------------|
| Move              | `A` / `D`   |
| Aim / Climb Up    | `W`         |
| Crouch / Aim Down | `S`         |
| Jump / Double Jump| `Space`     |
| Dash (Dark Burst) | `Shift`     |
| Attack            | `J`         |
| Parry             | `K`         |
| Place Portal      | `L`         |
| Backflip          | `U`         |
| Taunt             | `T`         |

---

## ⚔️ Mechanics

### Movement
- **Double Jump** — one extra jump in the air. A void ring flashes on the second.
- **Dark Burst Dash** — a short, invulnerable burst of void energy. Leaves a shadow trail. Refreshes on landing.
- **Wall Scaling** — press into a wall and hold `W` to climb it.
- **Wall Slide & Wall Jump** — hold into a wall to slide slowly; jump to kick off it.
- **Backflip** — a quick evasive flip with i-frames. Great for dodging the Golem's slam.
- **Coyote Time & Jump Buffering** — jumps feel fair, not frustrating.

### Combat
- **Omnidirectional Attacks** — swing Ulteria in four directions based on the movement input:
  - Hold `W` + Attack → **Upward slash**
  - Hold `S` + Attack → **Downward slash**
  - Hold `A` + Attack → **Left slash**
  - Hold `D` + Attack → **Right slash**
- **Combo Finisher** — perform the sequence **Up → Down → Left → Right**. 
  Ulteria grows, glows, and unleashes a devastating radial strike 
  followed by an **Angelic Burst**.
- **Parry** — press `K` within a tight window as an attack lands. 
  Reflects projectiles and **staggers** the Golem. Parried rocks deal 
  double damage when reflected.

### Hyperspace Manipulation (Portals)
- Press `L` to place a **blue portal**, press again to place an **orange portal**.
- Walk into one to be flung out of the other, preserving momentum.
- Use them to cross gaps, dodge boss attacks, or set up repositioning tricks.
- Portals last 20 seconds and can be replaced at any time.

### Ulteria
- **Speaks** — she comments on kills, taunts, and your inevitable death.
- **Shape-shifts** — her blade grows during the combo finisher and returns to normal afterward.
- **Angelic Burst** — a radial blast of holy light that damages everything nearby.

---

## 👹 Boss — The Golem

A slow, heavy stone construct guarding the end of the test level.

**Behaviour:**
- Chases when the player is within range.
- **Slam** — telegraphs with a red flash, then smashes the ground for heavy damage in a radius.
- **Rock Throw** — hurls a projectile. **Can be parried** to reflect it back.
- Occasionally follows a slam with a throw for pressure.

**Health:** 600 HP

**Tips:**
- **Parry the rocks** — it's the fastest way to stagger him and open a damage window.
- Use the **combo finisher** when he's staggered for maximum burst.
- **Backflip** to escape a slam if your dash is on cooldown.
- Set up portals on either side of the arena and use them to kite.

---

## 🛠️ Project Setup (Unity)

### Requirements
- **Unity 2021.3 LTS** or newer
- Input system: **Legacy Input Manager** (this prototype uses `Input.GetKey`)
- Unity 6 users: rename `Rigidbody2D.velocity` → `linearVelocity` (find & replace)

### Layers
Create these layers in **Project Settings → Tags and Layers**:
- `Ground` — for all solid ground/wall surfaces
- `Enemy` — for the Golem and rocks
- `Player` — tag the player GameObject as `Player` (tag, not layer)

### Scene Hierarchy
```text
TestLevel
├── Main Camera        [CameraFollow]
├── Player (Zenki)     [ZenkiController, CombatController, PortalAbility]
│   ├── GroundCheck    (empty transform at feet)
│   ├── WallCheck      (empty transform at chest)
│   ├── AttackOrigin   (empty transform at center)
│   └── UlteriaSprite  [Ulteria component on the blade pivot]
├── Golem              [GolemBoss, Rigidbody2D, Collider2D]
│   ├── GroundPoint    (empty transform below the boss)
│   └── ThrowPoint     (empty transform at hand)
└── LevelGeometry      (Tilemap or sprites, layer: Ground)
