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

        static ContentLibrary Build(out List<string> problems)
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DatabasePath);
            Assert.That(database, Is.Not.Null, $"No content database at {DatabasePath}.");
            return database.Build(out problems);
        }

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
            foreach (string guid in AssetDatabase.FindAssets("t:HeroAsset t:CardAsset t:EnemyAsset", new[] { "Assets/Content" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                bool listed = asset is HeroAsset h ? library.HasHero(h.Id)
                    : asset is CardAsset c ? library.HasCard(c.Id)
                    : asset is EnemyAsset e && library.HasEnemy(e.Id);
                Assert.That(listed, Is.True, $"{path} isn't listed in the content database.");
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

        static string Describe(HeroDef h) => $"{h.Id}|{h.Name}|{h.Title}|{h.Colour}|{h.MaxHealth}";

        static string Describe(CardDef c) =>
            $"{c.Id}|{c.Name}|{string.Join("+", c.Owners)}|{c.Cost}|{c.Target}|{c.Text}|" +
            string.Join(";", c.Effects.Select(e => $"{e.Kind},{e.Scope},{e.Amount},{e.Status},{e.BonusIfExposed}"));

        static string Describe(EnemyDef e) =>
            $"{e.Id}|{e.Name}|{e.MaxHealth}|" +
            string.Join(";", e.Intents.Select(i => $"{i.Kind},{i.Target},{i.Amount},{i.Status},{i.Description}"));
    }
}
