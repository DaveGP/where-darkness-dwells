using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WhereDarknessDwells.Content;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// Live portraits of the heroes for the party screen: each hero's model, standing full length and breathing
    /// through the stage's idle loop, drawn by its own small camera into a texture the screen shows. The models are
    /// licensed art that isn't in the repository, so a hero without one gets no portrait and the screen shows a
    /// placeholder. Dispose it when the party screen closes, so its cameras stop drawing.
    /// </summary>
    sealed class HeroPortraits : IDisposable
    {
        public const int Width = 256, Height = 512;
        /// <summary>Far below everything else, so no other camera sees the portraits and their lights reach nothing else.</summary>
        static readonly Vector3 Studio = new Vector3(0f, -2000f, 0f);
        const float Spacing = 25f, FieldOfView = 26f;

        readonly ContentDatabase _content;
        readonly RuntimeAnimatorController _idle;
        readonly Dictionary<string, RenderTexture> _textures = new Dictionary<string, RenderTexture>();
        GameObject _root;

        public HeroPortraits(ContentDatabase content)
        {
            _content = content;
            // The idle loop comes with the combat stage, which is local art too.
            _idle = Resources.Load<CombatStage>("CombatStage")?.Idle;
        }

        /// <summary>The hero's portrait, made the first time it is asked for, or null when the hero has no model here.</summary>
        public Texture For(string heroId)
        {
            if (_textures.TryGetValue(heroId, out RenderTexture made)) return made;
            GameObject model = _content.Heroes.FirstOrDefault(h => h.Id == heroId)?.Model;
            if (model == null) return _textures[heroId] = null;

            if (_root == null) _root = new GameObject("Hero portraits") { hideFlags = HideFlags.DontSave };
            Vector3 place = Studio + Vector3.right * (Spacing * _textures.Count);
            var sitter = new GameObject(heroId);
            sitter.transform.SetParent(_root.transform, false);
            sitter.transform.position = place;
            // Turned a little towards the light, like a painted portrait.
            GameObject figure = UnityEngine.Object.Instantiate(model, place, Quaternion.Euler(0f, 160f, 0f), sitter.transform);
            if (figure.GetComponentInChildren<Animator>() is Animator animator && _idle != null)
            {
                animator.runtimeAnimatorController = _idle;
                animator.applyRootMotion = false;
                animator.Play(0, 0, _textures.Count * 0.29f % 1f);
            }

            Bounds body = Body(figure, place);
            var focus = new Vector3(body.center.x, body.min.y + body.size.y * 0.5f, body.center.z);
            float shown = body.size.y * 1.12f;
            float distance = shown / 2f / Mathf.Tan(FieldOfView / 2f * Mathf.Deg2Rad);

            var texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { name = $"Portrait {heroId}", antiAliasing = 4 };
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(sitter.transform, false);
            cameraObject.transform.position = focus + new Vector3(0f, body.size.y * 0.08f, -distance);
            cameraObject.transform.LookAt(focus);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // Clear, so the card's own backdrop shows around the figure.
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.fieldOfView = FieldOfView;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = distance + Spacing / 2f;
            camera.targetTexture = texture;

            // A warm key light from the front left and a cold rim light from behind, both short enough to reach only this hero.
            Light(sitter.transform, focus, new Vector3(-1.6f, 1.2f, -2.2f), new Color(1f, 0.82f, 0.62f), 3.2f);
            Light(sitter.transform, focus, new Vector3(1.4f, 1.6f, 2f), new Color(0.55f, 0.7f, 1f), 3f);
            return _textures[heroId] = texture;
        }

        static void Light(Transform parent, Vector3 focus, Vector3 from, Color colour, float intensity)
        {
            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = focus + from;
            lightObject.transform.LookAt(focus);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 70f;
            light.range = 8f;
            light.color = colour;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        /// <summary>The figure's bounds, or a person-sized box at its feet when it has nothing to draw.</summary>
        static Bounds Body(GameObject figure, Vector3 feet)
        {
            Renderer[] renderers = figure.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(feet + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        public void Dispose()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            foreach (RenderTexture texture in _textures.Values.Where(t => t != null))
            {
                texture.Release();
                UnityEngine.Object.Destroy(texture);
            }
            _textures.Clear();
        }
    }
}
