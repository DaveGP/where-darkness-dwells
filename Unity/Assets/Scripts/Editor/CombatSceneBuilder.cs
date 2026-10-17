using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using WhereDarknessDwells.Content;
using WhereDarknessDwells.Core;
using WhereDarknessDwells.Game;

namespace WhereDarknessDwells.Editor
{
    /// <summary>
    /// Builds the game's scenes and their panel settings from the assets in the project, so they can be created the
    /// same way on any machine: the Run scene (the whole game) and the Combat scene (one test fight). Run either
    /// again to start that scene over; it asks before replacing it.
    /// </summary>
    public static class CombatSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Combat.unity";
        const string RunScenePath = "Assets/Scenes/Run.unity";
        const string PanelSettingsPath = "Assets/UI/CombatPanelSettings.asset";
        const string ThemePath = "Assets/UI/WddRuntimeTheme.tss";
        const string DatabasePath = "Assets/Content/ContentDatabase.asset";

        [MenuItem("Where Darkness Dwells/Build Combat Scene")]
        public static void Build()
        {
            if (!StartScene(ScenePath, "combat")) return;
            var database = Load<ContentDatabase>(DatabasePath);
            AddCamera();

            AddCombatScreen(database, PanelSettingsAsset(), out SerializedObject fields);
            SetList(fields, "party", database.Expedition.ToDef().Party.Select(id => database.Heroes.First(h => h.Id == id)).ToList());
            SetList(fields, "deck", database.Expedition.ToDef().StartingDeck.Select(id => database.Cards.First(c => c.Id == id)).ToList());
            SetList(fields, "enemies", StarterContent.FirstEncounter.Select(id => database.Enemies.First(e => e.Id == id)).ToList());
            fields.ApplyModifiedPropertiesWithoutUndo();

            Save(ScenePath, first: false);
            Debug.Log($"[Editor] Built {ScenePath} with the starting party and deck and the first encounter.");
        }

        [MenuItem("Where Darkness Dwells/Build Run Scene")]
        public static void BuildRun()
        {
            if (!StartScene(RunScenePath, "run")) return;
            var database = Load<ContentDatabase>(DatabasePath);
            PanelSettings panel = PanelSettingsAsset();
            AddCamera();

            var flowObject = new GameObject("Run Flow");
            var document = flowObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = Load<VisualTreeAsset>("Assets/UI/RunScreen.uxml");

            // The combat screen draws over the run screens and stays hidden until the run hands it a fight.
            CombatScreen combat = AddCombatScreen(database, panel, out SerializedObject combatFields);
            combat.GetComponent<UIDocument>().sortingOrder = 1;
            combatFields.FindProperty("startTestFight").boolValue = false;
            combatFields.ApplyModifiedPropertiesWithoutUndo();

            var flow = new SerializedObject(flowObject.AddComponent<RunFlow>());
            flow.FindProperty("content").objectReferenceValue = database;
            flow.FindProperty("combatScreen").objectReferenceValue = combat;
            flow.FindProperty("heroPanel").objectReferenceValue = Load<VisualTreeAsset>("Assets/UI/HeroPanel.uxml");
            flow.FindProperty("cardView").objectReferenceValue = Load<VisualTreeAsset>("Assets/UI/Card.uxml");
            flow.ApplyModifiedPropertiesWithoutUndo();

            Save(RunScenePath, first: true);
            Debug.Log($"[Editor] Built {RunScenePath}, the first scene in the build.");
        }

        /// <summary>Asks before replacing an existing scene, then opens a new empty one.</summary>
        static bool StartScene(string path, string name)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null &&
                !EditorUtility.DisplayDialog($"Rebuild the {name} scene?", $"This replaces {path} and loses any changes made to it.", "Replace", "Cancel"))
                return false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            // Create the scene before loading any assets: NewScene unloads unused assets, so anything loaded before it
            // would be saved as a missing reference.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return true;
        }

        static void AddCamera()
        {
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }

        static CombatScreen AddCombatScreen(ContentDatabase database, PanelSettings panel, out SerializedObject fields)
        {
            var screenObject = new GameObject("Combat Screen");
            var document = screenObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = Load<VisualTreeAsset>("Assets/UI/CombatScreen.uxml");

            var screen = screenObject.AddComponent<CombatScreen>();
            fields = new SerializedObject(screen);
            fields.FindProperty("content").objectReferenceValue = database;
            fields.FindProperty("heroPanel").objectReferenceValue = Load<VisualTreeAsset>("Assets/UI/HeroPanel.uxml");
            fields.FindProperty("enemyPanel").objectReferenceValue = Load<VisualTreeAsset>("Assets/UI/EnemyPanel.uxml");
            fields.FindProperty("cardView").objectReferenceValue = Load<VisualTreeAsset>("Assets/UI/Card.uxml");
            fields.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        /// <summary>Saves the open scene and adds it to Build Settings, first when <paramref name="first"/> is set.</summary>
        static void Save(string path, bool first)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(first ? 0 : Math.Min(1, scenes.Count), new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static PanelSettings PanelSettingsAsset()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }
            panel.themeStyleSheet = Load<ThemeStyleSheet>(ThemePath);
            // Lay the screen out at 1920 x 1080 and scale it to the window, so it looks the same at any 16:9 size.
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            return panel;
        }

        static void SetList<T>(SerializedObject target, string field, IReadOnlyList<T> assets) where T : UnityEngine.Object
        {
            SerializedProperty list = target.FindProperty(field);
            list.arraySize = assets.Count;
            for (int i = 0; i < assets.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        }

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new System.IO.FileNotFoundException($"Couldn't load {typeof(T).Name} at {path}.");
            return asset;
        }
    }
}
