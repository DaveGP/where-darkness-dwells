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
    /// Builds the combat test scene and its panel settings from the assets in the project, so they can be created
    /// the same way on any machine. Run it again to start the scene over; it asks before replacing it.
    /// </summary>
    public static class CombatSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Combat.unity";
        const string PanelSettingsPath = "Assets/UI/CombatPanelSettings.asset";
        const string ThemePath = "Assets/UI/WddRuntimeTheme.tss";
        const string DatabasePath = "Assets/Content/ContentDatabase.asset";

        [MenuItem("Where Darkness Dwells/Build Combat Scene")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Rebuild the combat scene?", $"This replaces {ScenePath} and loses any changes made to it.", "Replace", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var database = Load<ContentDatabase>(DatabasePath);
            PanelSettings panel = PanelSettingsAsset();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            var screenObject = new GameObject("Combat Screen");
            var document = screenObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = Load<VisualTreeAsset>("Assets/UI/CombatScreen.uxml");

            var screen = new SerializedObject(screenObject.AddComponent<CombatScreen>());
            screen.FindProperty("content").objectReferenceValue = database;
            SetList(screen, "party", database.Heroes);
            SetList(screen, "deck", database.Cards);
            SetList(screen, "enemies", StarterContent.FirstEncounter.Select(id => database.Enemies.First(e => e.Id == id)).ToList());
            screen.FindProperty("heroPanel").objectReferenceValue = Load<VisualTreeAsset>("Assets/UI/HeroPanel.uxml");
            screen.FindProperty("enemyPanel").objectReferenceValue = Load<VisualTreeAsset>("Assets/UI/EnemyPanel.uxml");
            screen.FindProperty("cardView").objectReferenceValue = Load<VisualTreeAsset>("Assets/UI/Card.uxml");
            screen.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log($"[Editor] Built {ScenePath} with {database.Heroes.Count} heroes, {database.Cards.Count} cards and the first encounter.");
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

        static void SetList<T>(SerializedObject target, string field, IReadOnlyList<T> assets) where T : Object
        {
            SerializedProperty list = target.FindProperty(field);
            list.arraySize = assets.Count;
            for (int i = 0; i < assets.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        }

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new System.IO.FileNotFoundException($"Couldn't load {typeof(T).Name} at {path}.");
            return asset;
        }
    }
}
