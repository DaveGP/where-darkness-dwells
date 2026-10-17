using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// Every definition the game knows about, looked up by id. Built once at startup from the content assets, or
    /// from <see cref="StarterContent"/> in tests. Call <see cref="Validate"/> before using new content.
    /// </summary>
    public sealed class ContentLibrary
    {
        readonly Dictionary<string, HeroDef> _heroes = new Dictionary<string, HeroDef>();
        readonly Dictionary<string, CardDef> _cards = new Dictionary<string, CardDef>();
        readonly Dictionary<string, EnemyDef> _enemies = new Dictionary<string, EnemyDef>();
        readonly List<string> _problems = new List<string>();

        public ContentLibrary(IEnumerable<HeroDef> heroes, IEnumerable<CardDef> cards, IEnumerable<EnemyDef> enemies)
        {
            foreach (HeroDef h in heroes) Add(_heroes, h?.Id, h, "hero");
            foreach (CardDef c in cards) Add(_cards, c?.Id, c, "card");
            foreach (EnemyDef e in enemies) Add(_enemies, e?.Id, e, "enemy");
        }

        public IEnumerable<HeroDef> Heroes => _heroes.Values;
        public IEnumerable<CardDef> Cards => _cards.Values;
        public IEnumerable<EnemyDef> Enemies => _enemies.Values;

        public HeroDef Hero(string id) => Get(_heroes, id, "hero");
        public CardDef Card(string id) => Get(_cards, id, "card");
        public EnemyDef Enemy(string id) => Get(_enemies, id, "enemy");

        public bool HasHero(string id) => id != null && _heroes.ContainsKey(id);
        public bool HasCard(string id) => id != null && _cards.ContainsKey(id);
        public bool HasEnemy(string id) => id != null && _enemies.ContainsKey(id);

        /// <summary>Everything wrong with the content, one readable line per problem. Empty means it's safe to use.</summary>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>(_problems);

            foreach (HeroDef h in _heroes.Values)
            {
                if (h.MaxHealth <= 0) problems.Add($"Hero '{h.Id}' needs more than 0 health.");
                if (string.IsNullOrEmpty(h.Colour)) problems.Add($"Hero '{h.Id}' has no card colour.");
            }

            foreach (CardDef c in _cards.Values)
            {
                if (c.Owners.Count < 1 || c.Owners.Count > 2) problems.Add($"Card '{c.Id}' needs one or two owners, not {c.Owners.Count}.");
                if (c.Owners.Distinct().Count() != c.Owners.Count) problems.Add($"Card '{c.Id}' lists the same owner twice.");
                foreach (string owner in c.Owners)
                    if (!HasHero(owner)) problems.Add($"Card '{c.Id}' belongs to unknown hero '{owner}'.");
                if (c.Cost < 0) problems.Add($"Card '{c.Id}' has a negative cost.");
                if (c.Effects.Count == 0) problems.Add($"Card '{c.Id}' does nothing.");

                foreach (EffectDef e in c.Effects)
                {
                    if (e.Amount < 0) problems.Add($"Card '{c.Id}' has a {e.Kind} effect with a negative amount.");
                    if (e.Scope != EffectScope.Target) continue;
                    bool hitsEnemy = e.Kind == EffectKind.Damage || e.Kind == EffectKind.ConsumeHex;
                    bool needsAlly = e.Kind == EffectKind.Block || e.Kind == EffectKind.Guard;
                    if (hitsEnemy && c.Target != CardTarget.Enemy) problems.Add($"Card '{c.Id}' deals damage to its target, so it must target an enemy.");
                    if (needsAlly && c.Target != CardTarget.Ally) problems.Add($"Card '{c.Id}' has a {e.Kind} effect on its target, so it must target an ally.");
                    if (c.Target == CardTarget.None) problems.Add($"Card '{c.Id}' has an effect on its target but no target.");
                }
            }

            foreach (EnemyDef e in _enemies.Values)
            {
                if (e.MaxHealth <= 0) problems.Add($"Enemy '{e.Id}' needs more than 0 health.");
                if (e.Intents.Count == 0) problems.Add($"Enemy '{e.Id}' has no actions.");
                foreach (IntentDef i in e.Intents)
                {
                    if (i.Amount < 0) problems.Add($"Enemy '{e.Id}' has an action with a negative amount.");
                    if (i.Kind == IntentKind.Block && i.Target != IntentTarget.Self) problems.Add($"Enemy '{e.Id}' blocks for someone other than itself.");
                    if (i.Kind != IntentKind.Block && i.Target == IntentTarget.Self) problems.Add($"Enemy '{e.Id}' aims an attack or status at itself.");
                }
            }

            return problems;
        }

        /// <summary>Everything wrong with an expedition, checked against this library. Empty means a run can start.</summary>
        public IReadOnlyList<string> Validate(ExpeditionDef expedition)
        {
            var problems = new List<string>();
            if (expedition == null) { problems.Add("There is no expedition."); return problems; }
            string name = $"Expedition '{expedition.Id}'";

            if (expedition.Party.Count == 0) problems.Add($"{name} has no party.");
            if (expedition.Party.Distinct().Count() != expedition.Party.Count) problems.Add($"{name} lists a hero twice.");
            foreach (string hero in expedition.Party)
                if (!HasHero(hero)) problems.Add($"{name} has unknown hero '{hero}' in its party.");

            if (expedition.StartingDeck.Count == 0) problems.Add($"{name} has an empty starting deck.");
            foreach (string card in expedition.StartingDeck) CheckCard(card, "starting deck");
            if (expedition.RewardCards.Count < 3) problems.Add($"{name} needs at least 3 reward cards to offer a choice.");
            if (expedition.RewardCards.Distinct().Count() != expedition.RewardCards.Count) problems.Add($"{name} lists a reward card twice.");
            foreach (string card in expedition.RewardCards) CheckCard(card, "reward cards");

            if (expedition.Encounters.Select(e => e.Id).Distinct().Count() != expedition.Encounters.Count) problems.Add($"{name} has two encounters with the same id.");
            foreach (EncounterDef e in expedition.Encounters)
            {
                if (e.EnemyIds.Count == 0) problems.Add($"Encounter '{e.Id}' has no enemies.");
                foreach (string enemy in e.EnemyIds)
                    if (!HasEnemy(enemy)) problems.Add($"Encounter '{e.Id}' has unknown enemy '{enemy}'.");
                if (e.Kind != EncounterKind.Boss && e.MinLayer > e.MaxLayer) problems.Add($"Encounter '{e.Id}' has its first layer after its last.");
            }
            int bosses = expedition.Encounters.Count(e => e.Kind == EncounterKind.Boss);
            if (bosses != 1) problems.Add($"{name} needs exactly one boss encounter, not {bosses}.");
            for (int layer = 0; layer < MapGenerator.Layers; layer++)
            {
                if (!expedition.Encounters.Any(e => e.Kind == EncounterKind.Normal && e.FitsLayer(layer)))
                    problems.Add($"{name} has no normal encounter for layer {layer + 1}.");
                if (layer >= MapGenerator.FirstEliteLayer && !expedition.Encounters.Any(e => e.Kind == EncounterKind.Elite && e.FitsLayer(layer)))
                    problems.Add($"{name} has no elite encounter for layer {layer + 1}.");
            }
            return problems;

            void CheckCard(string id, string list)
            {
                if (!HasCard(id)) { problems.Add($"{name} has unknown card '{id}' in its {list}."); return; }
                if (!Card(id).Owners.Any(expedition.Party.Contains)) problems.Add($"{name} has '{id}' in its {list}, but no hero in the party owns it.");
            }
        }

        void Add<T>(Dictionary<string, T> map, string id, T def, string kind)
        {
            if (def == null) { _problems.Add($"A {kind} entry is empty."); return; }
            if (string.IsNullOrEmpty(id)) { _problems.Add($"A {kind} has no id."); return; }
            if (map.ContainsKey(id)) { _problems.Add($"Two {kind}s share the id '{id}'."); return; }
            map.Add(id, def);
        }

        static T Get<T>(Dictionary<string, T> map, string id, string kind)
        {
            if (id != null && map.TryGetValue(id, out T def)) return def;
            throw new KeyNotFoundException($"No {kind} with the id '{id}'.");
        }
    }
}
