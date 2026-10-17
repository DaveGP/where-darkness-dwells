# Where Darkness Dwells: Game Design

The current agreed design. Anything marked **(proposed)** is a default chosen in Milestone 0 and is waiting
on David's review or a playtest. Change this file when a decision changes.

## Pitch

A single-player dark-fantasy roguelite deckbuilder. A party of three heroes enters the Black Keep, an ancient
fortress where a corruption began. They choose a route through its rooms, fight what lives there, and gather
clues about what happened. Then they confront the thing at its heart. What they learn on the way changes that
final fight.

## First release: the smallest complete game

The first release is one complete, polished run. It is not a large game.

| Area | First release | Later, if it earns its place |
| --- | --- | --- |
| Heroes | 3 fixed heroes (Warden, Hunter, Occultist), then a 4th so the party is a real choice of 3 from 4 | More heroes |
| Cards | About 15 starting cards and 30 to 40 reward cards, including dual-hero cards | Card upgrades, rarer cards |
| Enemies | 5 or 6 normal enemies, 2 elites, 1 boss | More per villain |
| Map | One floor of the Keep: 7 layers of branching rooms plus the boss | More floors or acts |
| Rooms | Combat, Elite, Investigation, Event, Sanctuary, Boss | Treasure, shop |
| Villain | One villain with a full clue trail and two endings | More villains, chosen per run |
| Events | About 8 short narrative events, some changed by clues | More events |
| Keepsakes (equipment) | About 8 passive items from elites and events | More |
| Save | Resume a run between rooms | Save mid-combat |
| Platform **(proposed)** | Windows and macOS, landscape 16:9, mouse | Web build (itch.io), controller |

**Non-goals for the first release:** multiplayer, explorable 3D rooms, procedural 3D dungeons, complex
physics, elaborate animation systems, meta-progression unlocks, a shop or currency, and localisation.

## The run

1. **Party.** Confirm the party (fixed three at first, then choose 3 of 4).
2. **Map.** Enter the Keep and see the floor map. Each room shows its type icon but not its details.
3. **Rooms.** Pick a connected room on the next layer. Resolve it, take its reward, and return to the map.
4. **Boss.** The last layer is the villain's lair. Clues collected on the way change this fight.
5. **End.** Win and get an ending that depends on what was discovered, or lose when the party falls. Either
   way the run ends with a summary and a "New expedition" button.

A run should take 30 to 45 minutes.

## Combat

### Heroes

Each hero has their own health, colour and card pool. The colours are the main way the interface shows who
owns a card.

| Hero **(proposed)** | Colour | Health | Role | Signature mechanics |
| --- | --- | --- | --- | --- |
| **Bram, the Warden** | Iron (amber) | 40 | Protect and control | Block, Guard (take hits meant for an ally), Stagger (weaken an enemy's next action) |
| **Wren, the Hunter** | Thorn (green) | 28 | Damage and precision | Big single hits, Bleed, bonus damage against Exposed enemies |
| **Sister Ashe, the Occultist** | Ash (violet) | 30 | Conditions and combinations | Hex, Weak, gaining Resolve, drawing cards, consuming Hex for big effects |

### Cards

- Every card has an **owner**: one hero, or two heroes for a **dual card**. The card frame shows the
  owner's colour, or both colours split, plus the owners' portraits.
- A card is playable only while **one of its owners is the active hero** and that hero can pay its cost.
- The **starting deck** has 15 cards: 4 for each hero and 3 dual cards, one for each pair of heroes.
- All cards share one draw pile, one hand and one discard pile.

### Turn structure **(proposed, to be tested in Milestone 1)**

1. **Draw.** Draw until the hand holds 6 cards. Shuffle the discard pile in when the draw pile runs out.
2. **Activate.** Choose one standing hero to activate. That hero gets **3 Resolve**. It is the only resource.
3. **Play.** Play any number of eligible cards. Each card shows its Resolve cost and its target.
4. **End activation.** Unspent Resolve is lost. **Cards you didn't play stay in your hand.** The next draw
   only tops the hand back up to 6, so planning a turn ahead is rewarded.
5. **Enemies act.** Each enemy carries out its shown intent, then shows its next one.
6. **End of round.** Statuses tick down. Hero Block is removed at the start of the next player turn.

Every enemy intent shows the action, the amount and **which hero it targets**. That is the main reason to
pick one hero over another. For example, Bram guards Wren, who is about to take 12 damage, or Ashe weakens
the enemy that is about to hit hardest.

### Statuses (kept small on purpose)

| Status | Effect | How it ticks |
| --- | --- | --- |
| Block | Absorbs damage before health | Removed at the start of the next player turn |
| Guarded | Attacks aimed at this hero hit Bram instead | Lasts until the next player turn |
| Exposed | Takes 50% more attack damage | Loses 1 stack at the end of each round |
| Weak | Deals 25% less attack damage | Loses 1 stack at the end of each round |
| Bleed | Takes damage equal to its stacks at the end of the round | Then loses 1 stack |
| Hex | Does nothing by itself; Ashe's cards consume stacks for effects | Never ticks down |

**Damage is calculated in a fixed order:** base damage, plus flat bonuses, then multiplied by Weak and then
by Exposed. The result is rounded down, then Block absorbs what it can and the rest comes off health.
Combat rules use no randomness apart from shuffling.

### Falling and defeat

- A hero at 0 health is **down**. Their own cards are set aside for the rest of the combat. Dual cards stay
  playable through the other owner.
- After a won combat, downed heroes stand back up with 1 health.
- If all three heroes are down, the run is lost.

## The map

- One floor with 7 layers of 2 to 4 rooms each, then the boss.
- Each room connects to 1 to 3 rooms on the next layer.
- The layout is assembled from the run seed. Generation rules guarantee a path that passes at least
  2 Investigation rooms and 1 Sanctuary, and they never put an Elite on the first two layers.
- Rooms show their type icon but not their contents. Rumours from events and clues can reveal more.

| Room | What happens |
| --- | --- |
| Combat | A fight. Reward: choose 1 of 3 cards or skip. |
| Elite | A harder fight. Reward: a Keepsake plus a card choice. |
| Investigation | Search the room for a clue. Some searches have a cost or a risk. |
| Event | A short scene with 2 or 3 choices that have mechanical results. |
| Sanctuary | Rest (heal everyone 30% of max health) or Reflect (remove a card from the deck). |
| Boss | The villain. |

## The mystery

The design is data-driven. A villain is data: its boss encounter, its clue set, and what each clue changes.
Adding a villain later means adding data, not rewriting systems.

### First villain **(proposed)**: Vael, the Bloodbound Steward

The Keep's last steward kept the household alive through a siege by bargaining with something sealed beneath
the chapel. Now he feeds the corruption, and it feeds him. At the start of a run, the party only hears
rumours: a plague, a curse, a missing lord. The clues reveal who is really responsible.

| Clue | Found in | Effect on the run |
| --- | --- | --- |
| The Steward's Ledger | Investigation | Names the villain. The boss's intent descriptions become exact instead of vague. |
| Silvered Chapel Bells | Investigation or Event | The party's attacks deal +2 damage to Vael. |
| The Cistern Door | Investigation | Reveals a hidden room on the map: the Cistern, where Vael's heart can be destroyed so he starts the fight with 30% less health. |
| The Thrall's Confession | Event (spare the thrall) | Vael's thralls don't join the boss fight. |
| His True Name | Elite reward or Event | Unlocks "Speak his name": once per fight, cancel Vael's Blood Tithe healing. |
| The Broken Seal | Investigation, deep route | Unlocks the true ending, which hints at what lies beneath the Keep. |

A typical route finds 3 or 4 of the 6 clues. You can win without any clues, but each one makes the boss
noticeably easier or changes the ending. The journal shows found clues and blank spaces for missing ones.

## Narrative style

- Short, concrete writing, no more than about 60 words per event screen.
- The Keep has one consistent history: the siege, the bargain and the steward. Rooms, enemies and items all
  point back to it.
- Use "darkness" imagery sparingly. Prefer specific details such as ledgers, bells, cistern water and
  candle-wax seals.
- Some allies lie, and not every question gets an answer.

## Art direction

- Stylised dark fantasy: old stone, restrained warm light against cold shadow, and strong silhouettes.
- Each hero's colour runs through their cards, portrait frame and activation highlight.
- Milestones 1 to 5 use placeholders: flat UI, coloured shapes and portrait placeholders.
- Milestone 6 brings in David's Synty packs (dungeon, horror mansion, and possibly dark fantasy or fortress)
  for room backdrops, combat stage characters and props. First check which packs are actually in the project.

## Open questions

1. **Hand rules.** Is keeping unplayed cards the right call, or should the hand be discarded each turn with
   a larger draw? This needs a playtest in Milestone 1.
2. **Benched heroes.** Should heroes who weren't activated get a small benefit, such as 1 Block, so
   activating only one hero per turn never feels like a waste?
3. **Downed heroes.** Is "stand back up with 1 health" too forgiving or too harsh?
4. **Fourth hero.** Who is it? This is needed so choosing a party is a real decision.
5. **Platform.** Steam, itch.io or both? This changes Milestone 7, not the earlier ones.
6. **Meta-progression.** Should anything unlock between runs, such as new heroes or villains? Currently a
   non-goal.
