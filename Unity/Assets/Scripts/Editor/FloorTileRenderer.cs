using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace WhereDarknessDwells.Editor
{
    /// <summary>
    /// Renders the floor plan's room tiles from the Dark Fantasy pack. Each recipe below is a small diorama on a stone floor,
    /// built in a temporary scene, lit, and photographed from almost straight above. The images go to
    /// <see cref="OutputFolder"/>, which is licensed art and gitignored, so a fresh clone has none and the floor plan keeps
    /// its plain tiles. It also renders floor.png, one stone slab seen from straight above, which tiles seamlessly: the
    /// floor plan lays it under every room and sets the room's picture in the middle.
    /// </summary>
    public static class FloorTileRenderer
    {
        const string PackPrefabs = "Assets/Synty/PolygonDarkFantasy/Prefabs/";
        const string OutputFolder = "Assets/LocalArt/Resources/FloorTiles/";
        const string Floor = "Base/SM_Bld_Base_Floor_01";
        const int Size = 512;

        readonly struct Prop
        {
            public readonly string Path;
            public readonly float X, Z, Turn, Scale, Height;

            public Prop(string path, float x, float z, float turn = 0f, float scale = 1f, float height = 0f)
            {
                Path = path;
                X = x;
                Z = z;
                Turn = turn;
                Scale = scale;
                Height = height;
            }
        }

        /// <summary>A light with no shadow: a brazier, a candle or the glow of a ritual circle.</summary>
        readonly struct Glow
        {
            public readonly Vector3 At;
            public readonly Color Colour;
            public readonly float Intensity, Range;

            public Glow(float x, float y, float z, Color colour, float intensity, float range)
            {
                At = new Vector3(x, y, z);
                Colour = colour;
                Intensity = intensity;
                Range = range;
            }
        }

        sealed class Recipe
        {
            public string Name;
            /// <summary>The colour of the one soft light from above that casts the shadows, and how wide its pool is.</summary>
            public Color Key;
            public float KeyAngle = 70f;
            public Prop[] Props;
            public Glow[] Glows = new Glow[0];
        }

        static Prop P(string prop, float x, float z, float turn = 0f, float scale = 1f, float height = 0f) =>
            new Prop("Props/SM_Prop_" + prop, x, z, turn, scale, height);

        static Prop W(string weapon, float x, float z, float turn = 0f, float scale = 1f) => new Prop("Weapons/SM_Wep_" + weapon, x, z, turn, scale);

        static readonly Color Fire = new Color(1f, 0.45f, 0.15f), Candle = new Color(1f, 0.7f, 0.35f), Torchlight = new Color(1f, 0.75f, 0.55f);

        // A file is named after its room kind and a number. FloorPlanView picks a room's variant from its id.
        static readonly Recipe[] Recipes =
        {
            new Recipe
            {
                Name = "combat-1", Key = Torchlight,
                Props = new[]
                {
                    P("Gibbet_01", -1.6f, 1.4f, 30), P("Body_Skeleton_01", 0.4f, -0.2f, 30), W("Sword_01", -0.2f, -0.9f, 80, 1.3f),
                    W("Shield_01", 1.2f, 0.7f, 200, 1.3f), P("Body_Burnt_01", 1.4f, -1.2f, -70), P("Skull_Pile_01", -2f, -1.9f, 40, 1.3f),
                    P("Bone_Pile_01", 0.2f, 1.4f, 0, 1.3f), P("Brazier_01", 2.1f, 2f), P("Skull_01", -0.8f, 0.4f, 120, 1.3f),
                    W("Mace_01", -0.9f, -1.8f, 20, 1.3f), P("Rubble_Pile_Burnt_01", 2.1f, -2.1f),
                },
                Glows = new[] { new Glow(2.1f, 1.6f, 2f, Fire, 4, 5), new Glow(0.3f, 1.2f, -0.2f, new Color(1f, 0.3f, 0.1f), 1.5f, 3) },
            },
            new Recipe
            {
                Name = "combat-2", Key = Torchlight,
                Props = new[]
                {
                    P("Fire_Pit_01_Rotisserie_01", 0, 0.6f), P("Fire_Pit_01", 0, 0.6f), P("Rack_Weapon_01", 0, 2.6f, 180),
                    P("Barrel_01", -2.1f, 2.1f), P("Crate_01", -2.2f, 1.1f, 25), P("Body_Skeleton_02", -0.8f, -1.2f, -60),
                    W("Axe_01", 0.7f, -1f, 30, 1.3f), P("Bones_Rotten_01", 1.6f, -1.6f, 0, 1.3f), P("Chain_01", 2.1f, 0.9f, 90),
                    P("Skull_02", -1.9f, -1.9f, 200, 1.3f), W("Halberd_01", 1.9f, 2f, -30), P("Sack_04", -2.2f, -0.2f),
                },
                Glows = new[] { new Glow(0, 1f, 0.6f, new Color(1f, 0.5f, 0.2f), 5, 5) },
            },
            new Recipe
            {
                Name = "elite-1", Key = new Color(1f, 0.55f, 0.45f), KeyAngle = 60,
                Props = new[]
                {
                    P("Cage_Large_01", -0.4f, -0.1f), P("Brazier_01", -2.1f, -1.9f), P("Brazier_01", 2.1f, -1.9f),
                    P("Skull_Pile_02", -2f, 2f), P("Chain_02", 2.1f, 2f), P("Bone_Pile_02", 1.7f, 0.2f),
                    P("Demon_Ward_01", 0, 2.6f, 180), P("Skull_Pile_03", -2.2f, 0.2f, 90),
                },
                Glows = new[] { new Glow(-2.1f, 1.4f, -1.9f, Fire, 4, 5), new Glow(2.1f, 1.4f, -1.9f, Fire, 4, 5) },
            },
            new Recipe
            {
                Name = "investigation-1", Key = new Color(0.95f, 0.8f, 0.6f),
                Props = new[]
                {
                    P("Rug_02", 0, -0.2f), P("Desk_Reading_01", 0, 0.6f, 180), P("Chair_01", 0, -0.1f),
                    P("Bookshelf_01", -1.2f, 2.6f, 180), P("Bookshelf_01_Insert_01", -1.2f, 2.6f, 180),
                    P("Bookshelf_Double_01", 2.6f, 2.6f, 180), P("Bookshelf_Double_01_Insert_01", 2.6f, 2.6f, 180),
                    P("Candelabra_Small_01", -0.3f, 0.6f, 0, 1, 1.2f), P("Book_Open_02", 0.25f, 0.55f, 10, 1, 1.2f),
                    P("Papers_01", 1.4f, -0.9f, 20), P("Book_Open_03", -1.3f, -1.3f, -30), P("Scroll_01", 1.7f, 0.6f, 60),
                    P("Chest_01", -2.2f, -0.4f, 90), P("Book_02", -2.2f, -0.4f, 20, 1, 0.6f), P("Papers_03", 0.9f, -2f, -40),
                },
                Glows = new[] { new Glow(-0.3f, 1.8f, 0.4f, Candle, 2, 3) },
            },
            new Recipe
            {
                Name = "investigation-2", Key = new Color(0.95f, 0.8f, 0.6f),
                Props = new[]
                {
                    P("Rug_01", 0, 0.1f, 0, 1.8f), P("Lecturn_01", 0, 0.5f, 180), P("Notice_Board_01", 0, 2.7f, 180),
                    P("Candle_Rack_01", -2.1f, 1.4f, 90), P("Candle_Rack_01_Insert_01", -2.1f, 1.4f, 90), P("Crate_02", 2f, 1.6f, 10),
                    P("Book_Open_03", -1f, -1f, 20), P("Papers_02", 0.8f, -0.9f, -15), P("Papers_03", 1.6f, -0.1f, 40),
                    P("Book_02", 2f, 1.6f, 70, 1, 0.9f), P("Scroll_02", -0.3f, -1.9f), P("Book_03", -1.9f, -1.8f, 140),
                },
                Glows = new[] { new Glow(-2.1f, 1.6f, 1.4f, Candle, 2, 3) },
            },
            new Recipe
            {
                Name = "event-1", Key = new Color(0.75f, 0.85f, 1f),
                Props = new[]
                {
                    P("Well_01", 0, 0.1f), P("Well_01_Insert_01", 0, 0.1f), P("Lantern_01", -2.1f, -1.8f),
                    P("Sack_01", 2f, -1.6f, 20), P("Sack_02", 2.3f, -0.8f, -40), P("Barrel_Open_01", -2.1f, 2f),
                },
                Glows = new[] { new Glow(-2.1f, 0.8f, -1.8f, Candle, 3, 4) },
            },
            new Recipe
            {
                Name = "event-2", Key = new Color(1f, 0.8f, 0.6f),
                Props = new[]
                {
                    P("Rug_01", 0, 0.2f, 90, 2.2f), P("Table_01", 0, 0.2f), P("Chair_01", -0.6f, -0.8f), P("Chair_01", 0.6f, 1.2f, 180),
                    P("Chair_01_Damaged_01", 1.8f, -0.5f, -110), P("Goblet_01", -0.5f, 0.4f, 0, 1, 0.8f), P("Goblet_02", 0.7f, 0, 0, 1, 0.8f),
                    P("Plate_01", -0.6f, -0.1f, 0, 1, 0.8f), P("Bread_Rotten_01", -0.6f, -0.1f, 30, 1, 0.82f),
                    P("Plate_02", 0.5f, 0.5f, 0, 1, 0.8f), P("Meat_Rotten_01", 0.5f, 0.5f, 0, 1, 0.82f),
                    P("Candelabra_Small_01", 0, 0.2f, 0, 1, 0.8f), P("Bottle_01", 1f, 0.5f, 0, 1, 0.8f),
                    P("Keg_01", -2.1f, 2f), P("Bottle_02", -2.2f, -1.9f, 70),
                },
                Glows = new[] { new Glow(0, 1.8f, 0.2f, Candle, 2.5f, 3) },
            },
            new Recipe
            {
                Name = "sanctuary-1", Key = new Color(1f, 0.88f, 0.7f), KeyAngle = 80,
                Props = new[]
                {
                    P("Statue_01", 0, 2.5f, 180), P("Altar_Table_01", 0, 1.4f, 180), P("Candelabra_02", -1.4f, 1.5f), P("Candelabra_02", 1.4f, 1.5f),
                    // A pew's pivot is at one end, so turned around it reaches back the other way.
                    P("Pew_01", -2.1f, -0.2f, 180), P("Pew_01", 0.1f, -0.2f, 180), P("Pew_01", -2.1f, -1.5f, 180), P("Pew_01", 0.1f, -1.5f, 180),
                    P("Candle_Rack_01", -2.5f, 1.4f, 90), P("Candle_Rack_01_Insert_02", -2.5f, 1.4f, 90), P("Holy_Water_01", 2.3f, 1.5f),
                },
                Glows = new[] { new Glow(0, 2f, 1.4f, new Color(1f, 0.75f, 0.4f), 3, 4) },
            },
            new Recipe
            {
                Name = "boss-1", Key = new Color(1f, 0.3f, 0.25f), KeyAngle = 40,
                Props = new[]
                {
                    P("Ritual_Circle_01", 0, 0, 0, 0.8f), P("Demon_01", 0, 2.3f, 180), P("Brazier_01", -2.2f, 1.8f), P("Brazier_01", 2.2f, 1.8f),
                    P("Skull_Pile_03", -2.1f, -1.9f), P("Skull_Pile_01", 2.1f, -1.9f, 60), P("Candle_Blob_01", -1.4f, -0.2f),
                    P("Candle_Blob_02", 1.4f, -0.2f), P("Flag_Dark_01", -1.2f, 2.5f, 180), P("Flag_Dark_02", 1.2f, 2.5f, 180),
                },
                Glows = new[]
                {
                    new Glow(0, 1.2f, 0, new Color(1f, 0.15f, 0.1f), 8, 6), new Glow(-2.2f, 1.5f, 1.8f, new Color(1f, 0.4f, 0.15f), 5, 6),
                    new Glow(2.2f, 1.5f, 1.8f, new Color(1f, 0.4f, 0.15f), 5, 6),
                },
            },
            new Recipe
            {
                Name = "secret-1", Key = new Color(0.45f, 0.85f, 0.9f),
                Props = new[]
                {
                    P("Tomb_02", 0, 0.1f), P("Gargoyle_Statue_01", 0, 2.5f, 180), P("Candle_Blob_01", -1.1f, -1.3f), P("Candle_Blob_02", 1.1f, -1.3f),
                    P("Bone_Pile_02", 1.9f, 1.7f), P("Chain_01", -2f, 1.8f), new Prop("Environment/SM_Env_Moss_01", -1.6f, -0.4f),
                    new Prop("Environment/SM_Env_Moss_03", 1.7f, -0.2f, 60), P("Tomb_Stone_02", -2.1f, -2f, 20), P("Tomb_Stone_03", 2.1f, -2f, -20),
                },
                Glows = new[] { new Glow(-1.1f, 0.8f, -1.3f, Candle, 1.5f, 2.5f), new Glow(1.1f, 0.8f, -1.3f, Candle, 1.5f, 2.5f) },
            },
            new Recipe
            {
                Name = "entrance-1", Key = new Color(0.85f, 0.85f, 0.95f),
                Props = new[]
                {
                    P("Rug_02", 0, -0.3f), new Prop("Building/SM_Bld_Gates_Cemetary_01", 1.4f, -2.7f), P("Torch_01", -2.1f, -1.9f),
                    P("Torch_01", 2.1f, -1.9f), P("Statue_People_01", 0, 2.2f, 180), P("Brazier_01", -1.5f, 1.8f), P("Brazier_01", 1.5f, 1.8f),
                    new Prop("Environment/SM_Env_Rocks_Small_01", 2.2f, 0.8f), P("Sack_03", -2.2f, 0.6f, 30),
                },
                Glows = new[] { new Glow(-1.5f, 1.5f, 1.8f, new Color(1f, 0.55f, 0.25f), 3, 4), new Glow(1.5f, 1.5f, 1.8f, new Color(1f, 0.55f, 0.25f), 3, 4) },
            },
        };

        [MenuItem("Where Darkness Dwells/Render Floor Tiles")]
        public static void Render()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PackPrefabs + Floor + ".prefab") == null)
            {
                Debug.LogWarning($"[Editor] The Dark Fantasy pack isn't in {PackPrefabs}, so there are no floor tiles to render. The floor plan keeps its plain tiles.");
                return;
            }
            Directory.CreateDirectory(OutputFolder);
            var missing = new List<string>();
            Scene previous = SceneManager.GetActiveScene();
            Scene studio = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            // Lighting settings belong to the active scene, so the studio is active while it renders.
            SceneManager.SetActiveScene(studio);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.21f, 0.26f);
            RenderSettings.fog = false;
            try
            {
                for (int i = 0; i < Recipes.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Render Floor Tiles", Recipes[i].Name, (float)i / Recipes.Length);
                    // Far below everything else. Each set is removed once photographed, so none shows in another's picture.
                    RenderOne(Recipes[i], studio, new Vector3(0f, -5000f, 0f), missing);
                }
                RenderFloor(studio, new Vector3(0f, -5000f, 0f), missing);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(studio, true);
            }
            AssetDatabase.Refresh();
            foreach (string name in Recipes.Select(r => r.Name).Append("floor"))
            {
                // Mipmaps keep the small tiles on the map from shimmering. The floor repeats, so it wraps.
                if (AssetImporter.GetAtPath(OutputFolder + name + ".png") is TextureImporter importer)
                {
                    importer.mipmapEnabled = true;
                    importer.maxTextureSize = Size;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.wrapMode = name == "floor" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                }
            }
            Debug.Log($"[Editor] Rendered {Recipes.Length} room tiles and the floor into {OutputFolder}." +
                (missing.Count > 0 ? $" Missing props, left out: {string.Join(", ", missing)}." : ""));
        }

        static void RenderOne(Recipe recipe, Scene studio, Vector3 origin, List<string> missing)
        {
            var set = new GameObject("Tile " + recipe.Name);
            SceneManager.MoveGameObjectToScene(set, studio);
            set.transform.position = origin;

            // A floor of 4 by 4 slabs, 2.5 m each, around the middle. A slab's pivot is at a corner.
            for (int x = -2; x < 2; x++)
                for (int z = -2; z < 2; z++)
                    Spawn(set, studio, new Prop(Floor, x * 2.5f + 2.5f, z * 2.5f), origin, missing);
            foreach (Prop prop in recipe.Props) Spawn(set, studio, prop, origin, missing);

            // The key: one soft spot from high above and a little south, the only light that casts shadows.
            Light key = AddLight(set, origin + new Vector3(1.2f, 9f, -3f), LightType.Spot, recipe.Key, 90f, 25f);
            key.transform.LookAt(origin);
            key.spotAngle = recipe.KeyAngle;
            key.innerSpotAngle = recipe.KeyAngle * 0.4f;
            key.shadows = LightShadows.Soft;
            foreach (Glow glow in recipe.Glows) AddLight(set, origin + glow.At, LightType.Point, glow.Colour, glow.Intensity, glow.Range);

            // Almost straight down, tipped a little from the south so props read as objects rather than outlines.
            Camera camera = AddCamera(set);
            camera.transform.position = origin + new Vector3(0, 13f, -2.6f);
            camera.transform.LookAt(origin + new Vector3(0, 0, 0.1f));
            camera.fieldOfView = 25f;
            Photograph(camera, recipe.Name);
            Object.DestroyImmediate(set);
        }

        /// <summary>
        /// One slab from straight above, lit only by the dim light at the edges of the room pictures, so it tiles
        /// seamlessly and meets them without a seam in tone.
        /// </summary>
        static void RenderFloor(Scene studio, Vector3 origin, List<string> missing)
        {
            var set = new GameObject("Floor");
            SceneManager.MoveGameObjectToScene(set, studio);
            set.transform.position = origin;
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    Spawn(set, studio, new Prop(Floor, x * 2.5f + 1.25f, z * 2.5f - 1.25f), origin, missing);
            Light fill = AddLight(set, origin + Vector3.up * 5f, LightType.Directional, new Color(0.75f, 0.68f, 0.6f), 0.35f, 0f);
            fill.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Camera camera = AddCamera(set);
            camera.transform.SetPositionAndRotation(origin + Vector3.up * 10f, Quaternion.Euler(90f, 0f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 1.25f;
            Photograph(camera, "floor");
            Object.DestroyImmediate(set);
        }

        static Camera AddCamera(GameObject set)
        {
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(set.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 1f;
            camera.farClipPlane = 30f;
            return camera;
        }

        static void Photograph(Camera camera, string name)
        {
            RenderTexture active = RenderTexture.active;
            var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            camera.targetTexture = target;
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
            else camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            image.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(OutputFolder + name + ".png", image.EncodeToPNG());
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
        }

        static void Spawn(GameObject set, Scene studio, Prop prop, Vector3 origin, List<string> missing)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PackPrefabs + prop.Path + ".prefab");
            if (prefab == null)
            {
                if (!missing.Contains(prop.Path)) missing.Add(prop.Path);
                return;
            }
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, studio);
            placed.transform.SetParent(set.transform, false);
            placed.transform.SetPositionAndRotation(origin + new Vector3(prop.X, prop.Height, prop.Z), Quaternion.Euler(0, prop.Turn, 0));
            placed.transform.localScale = Vector3.one * prop.Scale;
        }

        static Light AddLight(GameObject set, Vector3 at, LightType type, Color colour, float intensity, float range)
        {
            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(set.transform, false);
            lightObject.transform.position = at;
            Light light = lightObject.AddComponent<Light>();
            light.type = type;
            light.color = colour;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
