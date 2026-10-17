using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using WhereDarknessDwells.Content;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    /// <summary>
    /// Checks the content assets in the project. These need the Editor, so they run in Unity's Test Runner only;
    /// the rules tests use <see cref="StarterContent"/>, and the last test here keeps the two the same.
    /// </summary>
    public class ContentAssetTests
    {
        const string DatabasePath = "Assets/Content/ContentDatabase.asset";

        static ContentDatabase Database()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null, $"No content database at {DatabasePath}.");
            return database;
        }

        static ContentLibrary Build(out List<string> problems) => Database().Build(out problems);

        [Test]
        public void TheContentAssetsAreValid()
        {
            Build(out List<string> problems);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void EveryContentAssetIsInTheDatabase()
        {
            ContentLibrary library = Build(out _);
            ExpeditionDef expedition = Database().Expedition.ToDef();
            foreach (string guid in AssetDatabase.FindAssets("t:HeroAsset t:CardAsset t:EnemyAsset t:ClueAsset t:KeepsakeAsset t:EventAsset", new[] { "Assets/Content" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                bool listed = asset is HeroAsset h ? library.HasHero(h.Id)
                    : asset is CardAsset c ? library.HasCard(c.Id)
                    : asset is EnemyAsset e ? library.HasEnemy(e.Id)
                    : asset is ClueAsset clue ? expedition.Villain?.Clue(clue.Id) != null
                    : asset is KeepsakeAsset k ? expedition.Keepsake(k.Id) != null
                    : asset is EventAsset ev && expedition.Events.Any(x => x.Id == ev.Id);
                Assert.That(listed, Is.True, $"{path} isn't listed in the content database (clues: in the expedition's villain; Keepsakes and events: in the expedition).");
            }
        }

        [Test]
        public void TheAssetsMatchTheContentTheRulesAreTestedWith()
        {
            ContentLibrary assets = Build(out _);
            ContentLibrary tested = StarterContent.Library();

            Assert.That(assets.Heroes.Select(Describe), Is.EquivalentTo(tested.Heroes.Select(Describe)));
            Assert.That(assets.Cards.Select(Describe), Is.EquivalentTo(tested.Cards.Select(Describe)));
            Assert.That(assets.Enemies.Select(Describe), Is.EquivalentTo(tested.Enemies.Select(Describe)));
        }

        [Test]
        public void TheExpeditionMatchesTheOneTheRulesAreTestedWith()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            Assert.That(database.Expedition, Is.Not.Null, "The content database has no expedition.");
            ExpeditionDef asset = database.Expedition.ToDef();
            ExpeditionDef tested = StarterContent.Expedition();

            Assert.That(asset.Id, Is.EqualTo(tested.Id));
            Assert.That(asset.Heroes, Is.EqualTo(tested.Heroes));
            Assert.That(asset.StartingDeck, Is.EquivalentTo(tested.StartingDeck));
            Assert.That(asset.RewardCards, Is.EquivalentTo(tested.RewardCards));
            Assert.That(asset.Encounters.Select(Describe), Is.EquivalentTo(tested.Encounters.Select(Describe)));

            Assert.That(asset.Villain, Is.Not.Null, "The expedition has no villain.");
            Assert.That(Describe(asset.Villain), Is.EqualTo(Describe(tested.Villain)));
            Assert.That(asset.Villain.Clues.Select(Describe), Is.EqualTo(tested.Villain.Clues.Select(Describe)));
            Assert.That(asset.Keepsakes.Select(Describe), Is.EqualTo(tested.Keepsakes.Select(Describe)));
            Assert.That(asset.Events.Select(Describe), Is.EqualTo(tested.Events.Select(Describe)));
        }

        static string Describe(HeroDef h) => $"{h.Id}|{h.Name}|{h.Title}|{h.Colour}|{h.MaxHealth}";

        static string Describe(CardDef c) =>
            $"{c.Id}|{c.Name}|{string.Join("+", c.Owners)}|{c.Cost}|{c.Target}|{c.Text}|" +
            string.Join(";", c.Effects.Select(e => $"{e.Kind},{e.Scope},{e.Amount},{e.Status},{e.BonusIfExposed}"));

        static string Describe(EncounterDef e) => $"{e.Id}|{e.Kind}|{e.MinLayer}|{e.MaxLayer}|{string.Join("+", e.EnemyIds)}";

        static string Describe(EnemyDef e) =>
            $"{e.Id}|{e.Name}|{e.MaxHealth}|" +
            string.Join(";", e.Intents.Select(i => $"{i.Kind},{i.Target},{i.Amount},{i.Status},{i.Description},{i.Vague}"));

        static string Describe(VillainDef v)
        {
            SecretRoomDef r = v.SecretRoom;
            return $"{v.Id}|{v.BossEnemyId}|{v.HiddenName}|{v.Ending}|{v.TrueEnding}|" +
                (r == null ? "no secret room" : $"{r.Name},{r.Layer},{r.Scene},{r.ActionLabel},{r.HealthCostPerHero},{r.BossHealthLossPercent}");
        }

        static string Describe(KeepsakeDef k) => $"{k.Id}|{k.Name}|{k.Text}|{k.Kind}|{k.Amount}|{k.Status}";

        static string Describe(EventDef e) =>
            $"{e.Id}|{e.Title}|{e.Scene}|" + string.Join(";", e.Choices.Select(c =>
                $"{c.Label},{c.Result},{c.RequiresClue},{c.HiddenByClue}:" + string.Join("+", c.Outcomes.Select(o => $"{o.Kind} {o.Amount} {o.Id}"))));

        static string Describe(ClueDef c) =>
            $"{c.Id}|{c.Name}|{c.Sources}|{c.MinLayer}|{c.MaxLayer}|{c.SearchCost}|{c.Scene}|{c.SearchLabel}|{c.Journal}|" +
            string.Join(";", c.Effects.Select(e => $"{e.Kind},{e.Amount},{e.CancelKind},{e.Label}"));
    }
}
