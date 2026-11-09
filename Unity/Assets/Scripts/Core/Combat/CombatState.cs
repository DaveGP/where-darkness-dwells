using System;
using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    public enum CombatPhase
    {
        NotStarted,
        /// <summary>The player picks which hero to activate this turn.</summary>
        ChooseHero,
        /// <summary>One hero is active and the player is playing their cards.</summary>
        Activation,
        Won,
        Lost,
    }

    /// <summary>
    /// One fight: the party, the enemies, the cards, and the turn rules in GAME_DESIGN.md. The interface reads the
    /// state and calls the Try* commands. A command either does everything or nothing; a refused command returns a
    /// reason for the player and changes nothing.
    /// </summary>
    public sealed class CombatState
    {
        public const int HandSize = 6;
        public const int MaxHandSize = 10;
        /// <summary>Discards the active hero may make each activation, before Keepsakes.</summary>
        public const int DiscardsPerActivation = 1;

        readonly ContentLibrary _content;
        readonly Rng _rng;
        readonly List<Combatant> _heroes;
        readonly List<Combatant> _enemies;
        readonly List<CardInstance> _draw = new List<CardInstance>();
        readonly List<CardInstance> _hand = new List<CardInstance>();
        readonly List<CardInstance> _discard = new List<CardInstance>();
        readonly List<CardInstance> _setAside = new List<CardInstance>();
        readonly List<CombatEvent> _log = new List<CombatEvent>();
        readonly CombatModifiers _modifiers;
        List<CombatEvent> _pending;

        /// <param name="heroIds">The party in seat order.</param>
        /// <param name="deck">The party's deck. The combat shuffles its own copy of the list.</param>
        /// <param name="enemyIds">The enemies in the fight, left to right.</param>
        /// <param name="rng">The combat's random stream: shuffles and enemy targets.</param>
        /// <param name="heroHealth">Health carried in from the run; heroes not listed start at full health.</param>
        /// <param name="modifiers">What the run's clues change in this fight; null for none.</param>
        public CombatState(ContentLibrary content, IReadOnlyList<string> heroIds, IEnumerable<CardInstance> deck,
            IReadOnlyList<string> enemyIds, Rng rng, IReadOnlyDictionary<string, int> heroHealth = null, CombatModifiers modifiers = null)
        {
            _modifiers = modifiers ?? CombatModifiers.None;
            CancelsLeft = _modifiers.Cancel?.Uses ?? 0;
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            if (heroIds == null || heroIds.Count == 0) throw new ArgumentException("A fight needs at least one hero.", nameof(heroIds));
            if (heroIds.Distinct().Count() != heroIds.Count) throw new ArgumentException("A hero can only fight once per party.", nameof(heroIds));
            if (enemyIds == null || enemyIds.Count == 0) throw new ArgumentException("A fight needs at least one enemy.", nameof(enemyIds));

            _heroes = heroIds.Select(id =>
            {
                HeroDef def = content.Hero(id);
                _modifiers.MaxHealthBonus.TryGetValue(id, out int bonus);
                int max = def.MaxHealth + bonus;
                int health = max;
                if (heroHealth != null && heroHealth.TryGetValue(id, out int carried)) health = Math.Max(0, Math.Min(carried, max));
                return Combatant.ForHero(def, max, health);
            }).ToList();
            _enemies = enemyIds.Select((id, i) =>
            {
                EnemyDef def = content.Enemy(id);
                _modifiers.Names.TryGetValue(id, out string name);
                _modifiers.HealthLossPercent.TryGetValue(id, out int loss);
                return Combatant.ForEnemy(def, i, name, Math.Max(1, def.MaxHealth - def.MaxHealth * loss / 100));
            }).ToList();

            foreach (CardInstance card in deck ?? throw new ArgumentNullException(nameof(deck)))
            {
                CardDef def = content.Card(card.DefId);
                if (!def.Owners.Any(heroIds.Contains))
                    throw new ArgumentException($"{def.Name} belongs to no hero in this party.", nameof(deck));
                _draw.Add(card);
            }
            if (_draw.Select(c => c.Id).Distinct().Count() != _draw.Count) throw new ArgumentException("Two cards share an instance id.", nameof(deck));
        }

        /// <summary>A copy of everything in the fight, so a choice can be tried without changing this one.</summary>
        CombatState(CombatState other)
        {
            _content = other._content;
            _modifiers = other._modifiers;
            CancelsLeft = other.CancelsLeft;
            CardsPlayed = other.CardsPlayed;
            Activations = other.Activations;
            Discards = other.Discards;
            _rng = other._rng.Copy();
            _heroes = other._heroes.Select(c => c.Copy()).ToList();
            _enemies = other._enemies.Select(c => c.Copy()).ToList();
            _draw.AddRange(other._draw);
            _hand.AddRange(other._hand);
            _discard.AddRange(other._discard);
            _setAside.AddRange(other._setAside);
            _log.AddRange(other._log);
            Phase = other.Phase;
            Round = other.Round;
            ActiveHeroId = other.ActiveHeroId;
        }

        /// <summary>An independent copy of the fight. Commands on the copy don't change this one.</summary>
        public CombatState Clone() => new CombatState(this);

        public CombatPhase Phase { get; private set; } = CombatPhase.NotStarted;
        public int Round { get; private set; }
        /// <summary>The active hero's id during an activation, otherwise null.</summary>
        public string ActiveHeroId { get; private set; }

        public IReadOnlyList<Combatant> Heroes => _heroes;
        public IReadOnlyList<Combatant> Enemies => _enemies;
        public IReadOnlyList<CardInstance> DrawPile => _draw;
        public IReadOnlyList<CardInstance> Hand => _hand;
        public IReadOnlyList<CardInstance> DiscardPile => _discard;
        /// <summary>Cards out of this fight because every hero who owns them is down.</summary>
        public IReadOnlyList<CardInstance> SetAside => _setAside;
        /// <summary>Everything that has happened in this fight, in order.</summary>
        public IReadOnlyList<CombatEvent> Log => _log;

        /// <summary>What the run brings to this fight: clue effects and Keepsakes.</summary>
        public CombatModifiers Modifiers => _modifiers;

        /// <summary>The once-per-fight cancel the run's clues allow, or null. See <see cref="TryCancelIntent"/>.</summary>
        public CancelAbility Cancel => _modifiers.Cancel;
        public int CancelsLeft { get; private set; }
        /// <summary>Cards played and heroes activated so far this fight. Some Keepsakes only help the first.</summary>
        public int CardsPlayed { get; private set; }
        public int Activations { get; private set; }
        /// <summary>Cards the active hero has discarded this activation.</summary>
        public int Discards { get; private set; }
        /// <summary>How many cards the active hero may discard each activation, with Keepsakes.</summary>
        public int DiscardLimit => DiscardsPerActivation + _modifiers.Keepsake(KeepsakeEffectKind.ExtraDiscard);
        /// <summary>True once the active hero has used this activation's discards.</summary>
        public bool HasDiscarded => Discards >= DiscardLimit;
        /// <summary>The hand is refilled to this many cards at the start of each turn, with Keepsakes.</summary>
        public int HandLimit => HandSize + _modifiers.Keepsake(KeepsakeEffectKind.LargerHand);

        /// <summary>True while the party only sees this enemy's intents as vague text, with no numbers.</summary>
        public bool IsVague(Combatant enemy) => enemy?.Enemy != null && _modifiers.VagueIntents.Contains(enemy.Enemy.Id);

        /// <summary>The text the party sees for an enemy's action: exact, or vague while they don't understand it.</summary>
        public string IntentText(Combatant enemy, IntentDef intent) =>
            intent == null ? null : IsVague(enemy) ? intent.Vague ?? "Something stirs" : intent.Description;

        public bool IsOver => Phase == CombatPhase.Won || Phase == CombatPhase.Lost;

        public CardDef CardOf(CardInstance card) => _content.Card(card.DefId);

        public Combatant Find(string id) => _heroes.Concat(_enemies).FirstOrDefault(c => c.Id == id);

        /// <summary>The card with this instance id, wherever it is in the fight, or null.</summary>
        public CardInstance FindCard(int instanceId) =>
            _hand.Concat(_draw).Concat(_discard).Concat(_setAside).FirstOrDefault(c => c.Id == instanceId);

        // ---------- commands ----------

        /// <summary>Shuffles the deck, deals the first hand, and shows the enemies' first intents.</summary>
        public CombatResult Start()
        {
            if (Phase != CombatPhase.NotStarted) return CombatResult.Refused("The fight has already started.");
            return Run(() =>
            {
                _rng.Shuffle(_draw);
                if (_heroes.All(h => h.IsDown)) { Lose(); return; }
                foreach (Combatant hero in _heroes.Where(h => h.IsDown)) KnockDown(hero);
                BeginRound();
                ApplyStartingKeepsakes();
                foreach (Combatant enemy in _enemies) ShowIntent(enemy);
            });
        }

        public CombatResult TryActivate(string heroId)
        {
            string reason = Over() ?? (Phase == CombatPhase.Activation ? $"{Name(ActiveHeroId)} is already active. End the activation first." : null);
            if (reason != null) return CombatResult.Refused(reason);
            Combatant hero = _heroes.FirstOrDefault(h => h.Id == heroId);
            if (hero == null) return CombatResult.Refused("Choose a hero from your party.");
            if (hero.IsDown) return CombatResult.Refused($"{hero.Name} is down and can't act.");

            return Run(() =>
            {
                Phase = CombatPhase.Activation;
                ActiveHeroId = hero.Id;
                Discards = 0;
                Emit(CombatEventKind.HeroActivated, hero.Id);
                Activations++;
            });
        }

        /// <summary>Plays a card from the hand. <paramref name="targetId"/> is an enemy or hero id, or null for untargeted cards.</summary>
        public CombatResult TryPlay(int cardInstanceId, string targetId = null)
        {
            string reason = WhyCantPlay(cardInstanceId);
            if (reason != null) return CombatResult.Refused(reason);

            CardInstance card = _hand.First(c => c.Id == cardInstanceId);
            CardDef def = CardOf(card);
            Combatant actor = Find(ActiveHeroId);
            Combatant target = null;
            if (def.Target == CardTarget.Enemy)
            {
                target = _enemies.FirstOrDefault(e => e.Id == targetId);
                if (target == null || target.IsDown) return CombatResult.Refused($"Choose an enemy for {def.Name}.");
            }
            else if (def.Target == CardTarget.Ally)
            {
                target = _heroes.FirstOrDefault(h => h.Id == targetId);
                if (target == null || target.IsDown) return CombatResult.Refused($"Choose a standing hero for {def.Name}.");
                if (target == actor && def.Effects.Any(e => e.Kind == EffectKind.Guard))
                    return CombatResult.Refused($"{actor.Name} can't guard themselves. Choose another hero.");
            }

            return Run(() =>
            {
                _hand.Remove(card);
                Emit(CombatEventKind.CardPlayed, actor.Id, target?.Id, cardInstanceId: card.Id);
                CardsPlayed++;
                foreach (EffectDef effect in def.Effects)
                {
                    Apply(effect, actor, target);
                    if (IsOver) break;
                }
                if (OwnersAllDown(def)) _setAside.Add(card);
                else _discard.Add(card);
            });
        }

        /// <summary>
        /// Moves a card from the hand to the discard pile without playing it, so the next draw replaces it. Any card
        /// can be discarded, once per activation (more with a Keepsake).
        /// </summary>
        public CombatResult TryDiscard(int cardInstanceId)
        {
            string reason = WhyCantDiscard(cardInstanceId);
            if (reason != null) return CombatResult.Refused(reason);
            CardInstance card = _hand.First(c => c.Id == cardInstanceId);
            return Run(() =>
            {
                _hand.Remove(card);
                _discard.Add(card);
                Discards++;
                Emit(CombatEventKind.CardDiscarded, ActiveHeroId, cardInstanceId: card.Id);
            });
        }

        /// <summary>Why a card in the hand can't be discarded right now, written for the player, or null if it can.</summary>
        public string WhyCantDiscard(int cardInstanceId)
        {
            string over = Over();
            if (over != null) return over;
            if (Phase != CombatPhase.Activation) return "Activate a hero to discard a card.";
            if (_hand.All(c => c.Id != cardInstanceId)) return "That card isn't in your hand.";
            if (HasDiscarded) return DiscardLimit == 1 ? $"{Name(ActiveHeroId)} has already discarded a card this activation."
                : $"{Name(ActiveHeroId)} has already discarded {DiscardLimit} cards this activation.";
            return null;
        }

        /// <summary>Ends the active hero's turn: the enemies act, statuses tick, and the next turn is dealt.</summary>
        public CombatResult TryEndActivation()
        {
            string reason = Over() ?? (Phase != CombatPhase.Activation ? "Choose a hero to activate first." : null);
            if (reason != null) return CombatResult.Refused(reason);

            return Run(() =>
            {
                Emit(CombatEventKind.ActivationEnded, ActiveHeroId);
                ActiveHeroId = null;

                foreach (Combatant enemy in _enemies.Where(e => !e.IsDown).ToList())
                {
                    Act(enemy);
                    if (IsOver) return;
                }

                EndRound();
                if (IsOver) return;

                BeginRound();
                foreach (Combatant enemy in _enemies.Where(e => !e.IsDown)) ShowIntent(enemy);
            });
        }

        /// <summary>Uses the run's cancel ability on an enemy's shown intent, so it does nothing this round.</summary>
        public CombatResult TryCancelIntent(string enemyId)
        {
            string reason = WhyCantCancel(enemyId);
            if (reason != null) return CombatResult.Refused(reason);
            Combatant enemy = Find(enemyId);
            return Run(() =>
            {
                CancelsLeft--;
                enemy.IntentCancelled = true;
                Emit(CombatEventKind.IntentCancelled, enemy.Id, intent: enemy.Intent);
            });
        }

        /// <summary>Why the cancel ability can't be used on this enemy now, or null if it can.</summary>
        public string WhyCantCancel(string enemyId)
        {
            CancelAbility cancel = _modifiers.Cancel;
            if (cancel == null) return "The party has no way to do that yet.";
            string over = Over();
            if (over != null) return over;
            if (CancelsLeft <= 0) return $"{cancel.Label} has already been used this fight.";
            Combatant enemy = _enemies.FirstOrDefault(e => e.Id == enemyId);
            if (enemy == null || enemy.IsDown || enemy.Enemy.Id != cancel.EnemyId) return $"{cancel.Label} only works on {_content.Enemy(cancel.EnemyId).Name}.";
            if (enemy.IntentCancelled) return "That action is already cancelled.";
            if (enemy.Intent == null || enemy.Intent.Kind != cancel.Kind) return $"{cancel.Label} only stops a {cancel.Kind.ToString().ToLowerInvariant()}.";
            return null;
        }

        /// <summary>
        /// Why a card in the hand can't be played right now, written for the player, or null if it can (given a
        /// valid target). The interface uses this to dim cards and explain them.
        /// </summary>
        public string WhyCantPlay(int cardInstanceId)
        {
            string over = Over();
            if (over != null) return over;
            CardInstance card = _hand.FirstOrDefault(c => c.Id == cardInstanceId);
            if (card == null) return "That card isn't in your hand.";
            CardDef def = CardOf(card);
            if (Phase != CombatPhase.Activation) return $"Activate {OwnerNames(def)} to play {def.Name}.";
            if (!def.IsOwnedBy(ActiveHeroId)) return $"Only {OwnerNames(def)} can play {def.Name}.";
            if (def.Target == CardTarget.Enemy && _enemies.All(e => e.IsDown)) return "There's no enemy to target.";
            return null;
        }

        public bool CanPlay(int cardInstanceId) => WhyCantPlay(cardInstanceId) == null;

        /// <summary>For tests: moves a card from any pile into the hand.</summary>
        internal void PutInHandForTest(int cardInstanceId)
        {
            foreach (List<CardInstance> pile in new[] { _draw, _discard, _setAside })
            {
                CardInstance card = pile.FirstOrDefault(c => c.Id == cardInstanceId);
                if (card == null) continue;
                pile.Remove(card);
                _hand.Add(card);
            }
        }

        /// <summary>
        /// What the enemies' shown intents would do if they acted now, in order: the same events the enemy phase
        /// produces, with Block, Weak, Exposed, Guard and retargeting already applied. Nothing in this fight
        /// changes. Statuses ticking at the end of the round aren't included.
        /// </summary>
        public IReadOnlyList<CombatEvent> PreviewEnemyPhase()
        {
            if (Phase != CombatPhase.ChooseHero && Phase != CombatPhase.Activation) return new CombatEvent[0];
            CombatState copy = Clone();
            return copy.Run(() =>
            {
                foreach (Combatant enemy in copy._enemies.Where(e => !e.IsDown).ToList())
                {
                    copy.Act(enemy);
                    if (copy.IsOver) return;
                }
            }).Events;
        }

        // ---------- card effects ----------

        void Apply(EffectDef effect, Combatant actor, Combatant target)
        {
            switch (effect.Kind)
            {
                case EffectKind.Damage:
                    foreach (Combatant t in Targets(effect.Scope, actor, target))
                    {
                        int amount = effect.Amount + (t.Status(StatusKind.Exposed) > 0 ? effect.BonusIfExposed : 0);
                        Attack(actor, t, amount);
                    }
                    break;

                case EffectKind.ConsumeHex:
                    foreach (Combatant t in Targets(effect.Scope, actor, target))
                    {
                        int stacks = t.Status(StatusKind.Hex);
                        SetStatus(t, StatusKind.Hex, 0);
                        if (stacks > 0) Attack(actor, t, stacks * effect.Amount);
                    }
                    break;

                case EffectKind.Block:
                    foreach (Combatant t in Targets(effect.Scope, actor, target)) GainBlock(t, effect.Amount);
                    break;

                case EffectKind.ApplyStatus:
                    foreach (Combatant t in Targets(effect.Scope, actor, target))
                        SetStatus(t, effect.Status, t.Status(effect.Status) + effect.Amount);
                    break;

                case EffectKind.Guard:
                    foreach (Combatant t in Targets(effect.Scope, actor, target))
                    {
                        if (t == actor) continue;
                        t.GuardedBy = actor.Id;
                        Emit(CombatEventKind.Guarded, actor.Id, t.Id);
                    }
                    break;

                case EffectKind.Draw:
                    Draw(effect.Amount, MaxHandSize);
                    break;

                case EffectKind.Heal:
                    foreach (Combatant t in Targets(effect.Scope, actor, target)) Heal(t, effect.Amount);
                    break;

                case EffectKind.Cleanse:
                    foreach (Combatant t in Targets(effect.Scope, actor, target)) SetStatus(t, effect.Status, 0);
                    break;

                case EffectKind.LoseHealth:
                    int lost = Math.Min(effect.Amount, actor.Health - 1);
                    if (lost <= 0) break;
                    actor.Health -= lost;
                    Emit(CombatEventKind.Damaged, actor.Id, actor.Id, lost);
                    break;
            }
        }

        IEnumerable<Combatant> Targets(EffectScope scope, Combatant actor, Combatant target)
        {
            switch (scope)
            {
                case EffectScope.AllEnemies: return _enemies.Where(e => !e.IsDown).ToList();
                case EffectScope.ActingHero: return new[] { actor };
                case EffectScope.AllHeroes: return _heroes.Where(h => !h.IsDown).ToList();
                default: return target == null || target.IsDown ? Enumerable.Empty<Combatant>() : new[] { target };
            }
        }

        // ---------- enemies ----------

        void ShowIntent(Combatant enemy)
        {
            IntentDef intent = enemy.Enemy.Intents[enemy.IntentStep % enemy.Enemy.Intents.Count];
            enemy.Intent = intent;
            enemy.IntentCancelled = false;
            enemy.IntentTargetId = PickTarget(intent.Target)?.Id;
            Emit(CombatEventKind.IntentShown, enemy.Id, enemy.IntentTargetId, intent.Amount, intent: intent);
        }

        Combatant PickTarget(IntentTarget rule)
        {
            List<Combatant> standing = _heroes.Where(h => !h.IsDown).ToList();
            if (standing.Count == 0) return null;
            switch (rule)
            {
                case IntentTarget.RandomHero: return standing[_rng.Next(standing.Count)];
                case IntentTarget.HealthiestHero: return Healthiest(standing);
                default: return null;
            }
        }

        static Combatant Healthiest(List<Combatant> heroes)
        {
            Combatant best = heroes[0];
            foreach (Combatant h in heroes) if (h.Health > best.Health) best = h;
            return best;
        }

        void Act(Combatant enemy)
        {
            IntentDef intent = enemy.Intent;
            enemy.Block = 0; // An enemy's Block lasts through the player's turn and drops when it acts again.
            if (enemy.IntentCancelled)
            {
                enemy.IntentCancelled = false;
                enemy.IntentStep++;
                return;
            }
            Emit(CombatEventKind.EnemyActed, enemy.Id, enemy.IntentTargetId, intent.Amount, intent: intent);

            switch (intent.Kind)
            {
                case IntentKind.Block:
                    GainBlock(enemy, intent.Amount);
                    break;

                case IntentKind.Heal:
                    Heal(enemy, intent.Amount);
                    break;

                case IntentKind.Attack:
                    foreach (Combatant hero in IntentTargets(enemy))
                    {
                        Combatant hit = Guardian(hero) ?? hero;
                        if (hit != hero) Emit(CombatEventKind.Redirected, hit.Id, hero.Id);
                        Attack(enemy, hit, intent.Amount);
                        if (IsOver) return;
                    }
                    break;

                case IntentKind.ApplyStatus:
                    IEnumerable<Combatant> targets = intent.Target == IntentTarget.Self ? new[] { enemy } : IntentTargets(enemy);
                    foreach (Combatant target in targets)
                        SetStatus(target, intent.Status, target.Status(intent.Status) + intent.Amount);
                    break;
            }

            enemy.IntentStep++;
        }

        IEnumerable<Combatant> IntentTargets(Combatant enemy)
        {
            if (enemy.Intent.Target == IntentTarget.AllHeroes) return _heroes.Where(h => !h.IsDown).ToList();
            if (enemy.Intent.Target == IntentTarget.Self) return Enumerable.Empty<Combatant>();
            Combatant chosen = _heroes.FirstOrDefault(h => h.Id == enemy.IntentTargetId);
            if (chosen != null && !chosen.IsDown) return new[] { chosen };
            // The shown target went down before the enemy acted: it turns on the healthiest hero still standing.
            List<Combatant> standing = _heroes.Where(h => !h.IsDown).ToList();
            return standing.Count == 0 ? Enumerable.Empty<Combatant>() : new[] { Healthiest(standing) };
        }

        Combatant Guardian(Combatant hero)
        {
            if (hero.GuardedBy == null) return null;
            Combatant guardian = _heroes.FirstOrDefault(h => h.Id == hero.GuardedBy);
            return guardian != null && !guardian.IsDown && guardian != hero ? guardian : null;
        }

        // ---------- shared rules ----------

        void Attack(Combatant attacker, Combatant defender, int baseDamage)
        {
            if (attacker.IsHero && defender.Enemy != null && _modifiers.BonusDamageTo.TryGetValue(defender.Enemy.Id, out int bonus))
                baseDamage += bonus;
            baseDamage += attacker.Status(StatusKind.Fury);
            int damage = DamageRules.Attack(baseDamage, attacker.Status(StatusKind.Weak) > 0, defender.Status(StatusKind.Exposed) > 0);
            TakeDamage(attacker.Id, defender, damage, ignoresBlock: false);

            // A hero with Riposte who is still standing strikes back: flat damage, through the attacker's Block.
            int riposte = defender.Status(StatusKind.Riposte);
            if (riposte > 0 && defender.IsHero && !attacker.IsHero && !defender.IsDown && !IsOver)
                TakeDamage(defender.Id, attacker, riposte, ignoresBlock: false);
        }

        void Heal(Combatant target, int amount)
        {
            if (target.IsDown) return;
            int healed = Math.Min(amount, target.MaxHealth - target.Health);
            target.Health += healed;
            Emit(CombatEventKind.Healed, target.Id, target.Id, healed);
        }

        void TakeDamage(string sourceId, Combatant target, int damage, bool ignoresBlock)
        {
            if (target.IsDown) return;
            int blocked = ignoresBlock ? 0 : Math.Min(target.Block, damage);
            target.Block -= blocked;
            int taken = Math.Min(target.Health, damage - blocked);
            target.Health -= taken;
            Emit(CombatEventKind.Damaged, sourceId, target.Id, taken, blocked);

            if (!target.IsDown) return;
            if (target.IsHero) KnockDown(target);
            else
            {
                target.Block = 0;
                Emit(CombatEventKind.EnemyDefeated, target.Id);
            }
            CheckEnd();
        }

        void KnockDown(Combatant hero)
        {
            hero.Block = 0;
            hero.GuardedBy = null;
            foreach (Combatant other in _heroes.Where(h => h.GuardedBy == hero.Id)) other.GuardedBy = null;
            Emit(CombatEventKind.HeroDowned, hero.Id);

            int before = _setAside.Count;
            foreach (List<CardInstance> pile in new[] { _draw, _hand, _discard })
            {
                List<CardInstance> dead = pile.Where(c => OwnersAllDown(CardOf(c))).ToList();
                foreach (CardInstance c in dead) pile.Remove(c);
                _setAside.AddRange(dead);
            }
            if (_setAside.Count > before) Emit(CombatEventKind.CardsSetAside, hero.Id, amount: _setAside.Count - before);
        }

        /// <summary>True when no owner of the card is standing. An owner who isn't in the party counts as down.</summary>
        bool OwnersAllDown(CardDef def) => def.Owners.All(o => _heroes.FirstOrDefault(h => h.Id == o)?.IsDown ?? true);

        void GainBlock(Combatant target, int amount)
        {
            if (amount <= 0 || target.IsDown) return;
            target.Block += amount;
            Emit(CombatEventKind.BlockGained, null, target.Id, amount);
        }

        void SetStatus(Combatant target, StatusKind status, int stacks)
        {
            if (target.IsDown) return;
            stacks = Math.Max(0, stacks);
            if (target.Status(status) == stacks) return;
            target.SetStatus(status, stacks);
            Emit(CombatEventKind.StatusChanged, null, target.Id, stacks, status: status);
        }

        // ---------- rounds ----------

        void BeginRound()
        {
            Round++;
            Phase = CombatPhase.ChooseHero;
            Emit(CombatEventKind.RoundStarted, amount: Round);
            foreach (Combatant hero in _heroes)
            {
                hero.Block = 0;
                hero.GuardedBy = null;
                SetStatus(hero, StatusKind.Riposte, 0);
            }
            Draw(HandLimit - _hand.Count, HandLimit);
        }

        /// <summary>The Keepsakes that act once, as the fight starts: Block for the heroes, statuses on the enemies, extra cards.</summary>
        void ApplyStartingKeepsakes()
        {
            int block = _modifiers.Keepsake(KeepsakeEffectKind.StartBlock);
            if (block > 0)
                foreach (Combatant hero in _heroes.Where(h => !h.IsDown)) GainBlock(hero, block);
            foreach (KeepsakeDef k in _modifiers.Keepsakes.Where(k => k.Kind == KeepsakeEffectKind.EnemyStartStatus))
                foreach (Combatant enemy in _enemies.Where(e => !e.IsDown)) SetStatus(enemy, k.Status, enemy.Status(k.Status) + k.Amount);
            int draw = _modifiers.Keepsake(KeepsakeEffectKind.FirstTurnDraw);
            if (draw > 0) Draw(draw, MaxHandSize);
        }

        /// <summary>Bleed hurts, then Bleed, Exposed and Weak each lose a stack. Hex never fades by itself.</summary>
        void EndRound()
        {
            foreach (Combatant c in _heroes.Concat(_enemies).Where(c => !c.IsDown).ToList())
            {
                int bleed = c.Status(StatusKind.Bleed);
                if (bleed > 0)
                {
                    TakeDamage(null, c, bleed, ignoresBlock: true);
                    if (IsOver) return;
                    SetStatus(c, StatusKind.Bleed, bleed - 1);
                }
                SetStatus(c, StatusKind.Exposed, c.Status(StatusKind.Exposed) - 1);
                SetStatus(c, StatusKind.Weak, c.Status(StatusKind.Weak) - 1);
            }
        }

        void Draw(int count, int handLimit)
        {
            int drawn = 0;
            while (drawn < count && _hand.Count < handLimit)
            {
                if (_draw.Count == 0)
                {
                    if (_discard.Count == 0) break;
                    _draw.AddRange(_discard);
                    _discard.Clear();
                    _rng.Shuffle(_draw);
                    Emit(CombatEventKind.DeckShuffled, amount: _draw.Count);
                }
                _hand.Add(_draw[_draw.Count - 1]);
                _draw.RemoveAt(_draw.Count - 1);
                drawn++;
            }
            if (drawn > 0) Emit(CombatEventKind.CardsDrawn, amount: drawn);
        }

        void CheckEnd()
        {
            if (IsOver) return;
            if (_enemies.All(e => e.IsDown))
            {
                Phase = CombatPhase.Won;
                ActiveHeroId = null;
                Emit(CombatEventKind.CombatWon);
            }
            else if (_heroes.All(h => h.IsDown)) Lose();
        }

        void Lose()
        {
            Phase = CombatPhase.Lost;
            ActiveHeroId = null;
            Emit(CombatEventKind.CombatLost);
        }

        // ---------- plumbing ----------

        CombatResult Run(Action command)
        {
            _pending = new List<CombatEvent>();
            command();
            List<CombatEvent> events = _pending;
            _pending = null;
            _log.AddRange(events);
            return CombatResult.Success(events);
        }

        void Emit(CombatEventKind kind, string sourceId = null, string targetId = null, int amount = 0, int blocked = 0,
            StatusKind status = default, int cardInstanceId = 0, IntentDef intent = null) =>
            _pending.Add(new CombatEvent(kind, sourceId, targetId, amount, blocked, status, cardInstanceId, intent));

        string Over()
        {
            switch (Phase)
            {
                case CombatPhase.NotStarted: return "The fight hasn't started.";
                case CombatPhase.Won:
                case CombatPhase.Lost: return "The fight is over.";
                default: return null;
            }
        }

        string Name(string heroId) => _heroes.FirstOrDefault(h => h.Id == heroId)?.Name ?? heroId;

        string OwnerNames(CardDef def) => string.Join(" or ", def.Owners.Select(Name));
    }
}
