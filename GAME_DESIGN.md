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

### Turn structure

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
| Block | Absorbs attack damage before health | A hero's Block is removed at the start of the next player turn. An enemy's Block is removed when it next acts. |
| Guarded | Attacks (not statuses) aimed at this hero hit their guardian instead. A hero can't guard themselves. | Lasts until the next player turn |
| Exposed | Takes 50% more attack damage | Loses 1 stack at the end of each round |
| Weak | Deals 25% less attack damage | Loses 1 stack at the end of each round |
| Bleed | Takes damage equal to its stacks at the end of the round, ignoring Block | Then loses 1 stack |
| Hex | Does nothing by itself; Ashe's cards consume stacks for effects | Never ticks down |
| Riposte | A hero's: when an enemy's attack hits them and they are still standing, the attacker takes that much damage | Removed at the start of the next player turn, like Block |
| Fury | An enemy's: each of its attacks deals that much more base damage | Never ticks down |

**Damage is calculated in a fixed order:** base damage, plus flat bonuses, then multiplied by Weak and then
by Exposed. The result is rounded down, then Block absorbs what it can and the rest comes off health.
Combat rules use no randomness apart from shuffling.

### Falling and defeat

- A hero at 0 health is **down**. Their own cards are set aside for the rest of the combat. Dual cards stay
  playable through the other owner.
- After a won combat, downed heroes stand back up with a quarter of their maximum health (rounded down).
- If all three heroes are down, the run is lost.
- An enemy whose shown target goes down before it acts turns on the healthiest standing hero instead.

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

### How the clues work (Milestone 4)

These fill in details the table above leaves open. Each one is a default until David agrees or a playtest
says otherwise.

- **Investigation rooms.** When the map is made, each Investigation room is given one clue that isn't
  already placed. Entering shows a short scene (under 60 words) with two choices: a search with a cost
  (one hero of your choice loses some health) that finds the clue, or leaving with nothing. The cost
  depends on the clue, so what to give up is a real decision. The Broken Seal only appears on layers 5
  to 7.
- **Clues that come from Events** (the Thrall's Confession, and His True Name from an Event) can also turn
  up in Investigations until Events exist in Milestone 5. His True Name can also come from an Elite
  reward, as the table says.
- **Before the Ledger,** the boss is shown as "The Master of the Keep". His intents read as vague lines such as
  "Blood gathers", with no numbers or previews. The Ledger shows his real name and exact intents.
- **The Cistern** is a hidden room on layer 5. It is always generated, but only shown once the Cistern
  Door is found, so the door only appears on layers 1 to 4. Every room on layer 4 leads down to it, and it
  leads on to one room on layer 6. Entering it, each
  hero loses 4 health to destroy Vael's heart, and he starts the fight with 30% less health.
- **Thralls.** The boss fight is Vael plus two Blood Thralls. The Thrall's Confession removes them.
- **Blood Tithe** is one of Vael's intents: he heals. **Speak his name** is a button in the boss fight,
  usable once and free, that cancels a Blood Tithe he is showing.
- **Silvered Bells:** +2 to the damage of every card that hits Vael. It is a flat bonus, so it is added
  before Weak and Exposed, following the damage order.
- **Endings.** Beating Vael gives the ending "The Keep is quiet". With the Broken Seal, the true ending
  follows: what the steward bargained with is still beneath the chapel.
- **The journal** is a button on the map. It shows six slots, each found clue with its text and what it
  changed, and the rest as blanks.
- **Data, not code.** A villain (`VillainDef`) lists its boss encounter, its clues and each clue's
  effects. An effect is one of a few kinds Core understands: reveal the boss, bonus damage against the
  boss, show a hidden room, remove the boss's allies, grant a once-per-fight cancel, and unlock an ending.
  Nothing checks the villain's name.

## Events, Keepsakes and the party (proposed, Milestone 5)

These fill in what the tables above leave open. Each one is a default until David agrees or a playtest says
otherwise.

### Choosing the party

- The party screen shows six heroes. You pick three before entering the Keep.
- Every pair of heroes has one dual card, which makes fifteen. The starting deck is the chosen heroes' four
  cards each plus the three dual cards for their pairs, so it is still 15 cards.
- Rewards only offer cards that someone in the party can play.

### Keepsakes

A Keepsake is a passive item the party keeps for the rest of the run. Each one has a single effect that
Core understands, so a new Keepsake is data. An Elite gives a Keepsake the party doesn't have yet, plus its
card choice, and some event choices give one. These are the first eight:

| Keepsake | Effect |
| --- | --- |
| Gaoler's Keyring | The first card each fight costs 1 less. |
| Siege Lantern | Every hero starts each fight with 4 Block. |
| Chapel Candle | After each won fight, every standing hero heals 3. |
| Rusted Caltrops | At the start of each fight, every enemy gets Bleed 2. |
| Widow's Veil | At the start of each fight, every enemy gets Weak 1. |
| Steward's Seal-ring | The first activation each fight gets 1 more Resolve. |
| Marked Deck | Draw 2 more cards on the first turn of each fight. |
| Bone Charm | Each hero's maximum health goes up by 4, and they heal 4. |

### Events

An Event room shows a short scene (under 60 words) with 2 or 3 choices. A choice's results are data from a
small set Core understands:
- heal or hurt the party, or the hero you choose
- gain a card (chosen from three) or remove one
- gain a Keepsake or a clue
- start a fight

Some choices only appear once a clue has been found, and some cost a hero health, so who pays is your call.
Each event is used at most once per run, picked from the "rewards" stream. These are the first eight:

| Event | The choice |
| --- | --- |
| The Chained Thrall | Loosen his chains for the Thrall's Confession (one hero loses 3), or leave. With the Ledger, you can name his master instead, and he confesses for free. |
| The Chapel Register | Read the torn page for His True Name (one hero loses 4), or burn it to remove a card. |
| The Bell-founder's Widow | Trade her a card from the deck for the Silvered Bells, or take her Widow's Veil. |
| The Drowned Well | Drink (the party heals 6), or draw up the bucket for a Keepsake and a fight with the Drowned Watch. |
| The Quartermaster's Store | Choose one of three cards, or a Keepsake that costs one hero 6 health. |
| The Siege Graves | Dig for a Keepsake (every hero loses 3), or pray to remove a card. With the Broken Seal, a third choice heals everyone 8. |
| The Survivor | She begs you to follow her. If you do, it is an ambush: a fight. With the Ledger, you recognise the steward's servant and take her Keepsake. |
| The Mirror Hall | One hero you choose gains 4 maximum health, or removes a card at the cost of 5 health. |

With events in place, the clues return to the sources in the table above: the Thrall's Confession only comes
from its event, and His True Name from an Elite or its event. The Silvered Bells come from an Investigation
or the Widow.

### Three new heroes **(decided: David, 2026-10-17)**

David chose to add all three proposed heroes, so the party is a choice of 3 from 6.
- **Corvin, the Penitent** (Bone, 32 health): a healer who pays in his own blood. He heals allies, cleanses
  Bleed and Weak, and trades his health for Resolve. New effects: heal, cleanse, pay health.
- **Mara, the Duelist** (Steel, 34 health): precise strikes, and Riposte to punish whoever attacks her.
- **Hollis, the Bellwright** (Bronze, 32 health): bells that reach every enemy or every hero at once, with
  Weak, Exposed and Block.

### Vael's Bloodrage **(decided: David, 2026-10-17)**

Some new parties heal faster than Vael hurts them and deal less than his Blood Tithe heals, so the fight could
never end: 44% of Bram, Corvin and Hollis's boss fights in the balance tool. The fix: a sixth step in
Vael's pattern, Bloodrage, gives him Fury 3, so each time his pattern comes round his attacks deal 3 more.
Fury is a general enemy status (data), not a rule about Vael. With it, no boss fight stalled in 2,000 runs.

## Saving and menus (Milestone 6)

- **Saving.** The run is saved each time the party is back on the map, and once more just before they enter a
  room. One save slot. It is deleted when the run is won or lost.
- **Continue** puts the party back on the map. If they quit inside a room, Continue enters that same room again
  from its start, with the same fight, draws or event, so quitting can't dodge a room or reroll it.
- The save holds the seed, the party and their health, the deck, Keepsakes, clues, events seen and the route.
  The map isn't saved: the seed builds it again. A save that no longer fits the content (a card that was
  removed, say) is refused with a reason, and the title screen offers a new expedition instead.
- **Title:** Continue (only when there is a save), New Expedition, Settings, Quit.
- **Pause (Esc):** Resume, Settings, Quit to Title. Quitting keeps the save.
- **Settings:** fullscreen, and animation speed (normal or fast). Volume comes with sound in Milestone 6.5.
- **Map:** the first layer's rooms carry an entrance marker before the first room, and every room the party can
  enter next glows and pulses.

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
- Milestone 6.5 brings in David's Synty packs (dungeon, horror mansion, and possibly dark fantasy or fortress)
  for room backdrops, combat stage characters and props. First check which packs are actually in the project.

## Open questions

1. ~~**Hand rules.**~~ **Decided (Milestone 2): keep unplayed cards.** In the balance tool, discarding the hand
   each turn made Bram nearly useless (activated on 15% of turns instead of 30%), because his cards wait for
   the right moment.
2. ~~**Benched heroes.**~~ **Decided (Milestone 2): no bonus.** Giving waiting heroes 1 Block changed results by
   about 2 percentage points, so it isn't worth the extra rule.
3. ~~**Downed heroes.**~~ **Decided (Milestone 3): stand back up with a quarter of max health.** Over 300 runs in the
   balance tool, a hero brought back with 1 health fell again in the next fight 45% of the time, against 35% with a
   quarter. David chose the quarter so a fall still costs something without making that hero useless.
4. ~~**Fourth hero.**~~ **Decided (Milestone 5): three new heroes, and a choice of 3 from 6.** See "Three new
   heroes" above.
5. **Platform.** Steam, itch.io or both? This changes Milestone 7, not the earlier ones.
6. **Meta-progression.** Should anything unlock between runs, such as new heroes or villains? Currently a
   non-goal.
