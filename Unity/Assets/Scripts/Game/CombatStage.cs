using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using WhereDarknessDwells.Content;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// A 3D set behind the combat screen: an environment with a place for each hero and enemy, its own camera, lights
    /// and idle animation. Its art comes from licensed packs that aren't in the repository, so the combat screen uses a
    /// stage only when one is found in Resources (Unity/Assets/LocalArt, see .gitignore). It shows the fight's state and
    /// never changes it.
    /// </summary>
    public sealed class CombatStage : MonoBehaviour
    {
        [SerializeField] Transform[] heroSlots = new Transform[0];
        [SerializeField] Transform[] enemySlots = new Transform[0];
        [Tooltip("Played on every hero and enemy model that has an Animator. Optional.")]
        [SerializeField] RuntimeAnimatorController idle;

        [Header("Lighting the set needs, applied while it is shown")]
        [SerializeField] Color ambientSky = new Color(1f, 0.82f, 0.67f);
        [SerializeField] Color ambientEquator = new Color(0.07f, 0.15f, 0.48f);
        [SerializeField] Color ambientGround = new Color(0.07f, 0.03f, 0.21f);
        [SerializeField] bool fog = true;
        [SerializeField] Color fogColor = new Color(0.35f, 0.63f, 0.71f);
        [SerializeField] float fogStart;
        [SerializeField] float fogEnd = 150f;

        readonly Dictionary<string, GameObject> _fighters = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Transform> _places = new Dictionary<string, Transform>();
        Action _restoreLighting;
        Camera _camera;

        void Awake()
        {
            _camera = GetComponentInChildren<Camera>();
            // In a browser, a dozen shadow-casting lights cost too much, so the stage keeps its lights and drops their shadows.
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                foreach (Light stageLight in GetComponentsInChildren<Light>(true)) stageLight.shadows = LightShadows.None;
        }

        /// <summary>Draws the stage only into this part of the screen (0 to 1, from the bottom left).</summary>
        public void SetViewport(Rect normalized)
        {
            if (_camera != null) _camera.rect = normalized;
        }

        /// <summary>
        /// Where the point just above a fighter's head is in the stage's view, from (0, 0) at its bottom left to (1, 1)
        /// at its top right, or null when the fighter has no place on the stage. It uses the place, not the model, so a
        /// fallen fighter's panel stays put.
        /// </summary>
        public Vector2? HeadInView(string fighterId)
        {
            if (_camera == null || !_places.TryGetValue(fighterId, out Transform place)) return null;
            Vector3 point = _camera.WorldToViewportPoint(place.position + Vector3.up * 2.1f);
            return point.z > 0f ? new Vector2(point.x, point.y) : (Vector2?)null;
        }

        void OnEnable()
        {
            AmbientMode mode = RenderSettings.ambientMode;
            Color sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor;
            bool oldFog = RenderSettings.fog;
            FogMode fogMode = RenderSettings.fogMode;
            Color oldFogColor = RenderSettings.fogColor;
            float start = RenderSettings.fogStartDistance, end = RenderSettings.fogEndDistance;
            _restoreLighting = () =>
            {
                RenderSettings.ambientMode = mode;
                RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground;
                RenderSettings.fog = oldFog;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = oldFogColor;
                RenderSettings.fogStartDistance = start;
                RenderSettings.fogEndDistance = end;
            };

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.fog = fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }

        void OnDisable() => _restoreLighting?.Invoke();

        /// <summary>Places a model for each fighter the first time it is seen, and hides the ones that are down.</summary>
        public void Show(CombatState combat, ContentDatabase content)
        {
            for (int i = 0; i < combat.Heroes.Count; i++)
            {
                Combatant hero = combat.Heroes[i];
                Place(hero, i, heroSlots, content.Heroes.FirstOrDefault(h => h.Id == hero.Hero.Id)?.Model);
            }
            for (int i = 0; i < combat.Enemies.Count; i++)
            {
                Combatant enemy = combat.Enemies[i];
                Place(enemy, i, enemySlots, content.Enemies.FirstOrDefault(e => e.Id == enemy.Enemy.Id)?.Model);
            }
        }

        void Place(Combatant fighter, int index, Transform[] slots, GameObject model)
        {
            if (!_fighters.TryGetValue(fighter.Id, out GameObject placed))
            {
                if (index >= slots.Length || slots[index] == null) return;
                Transform slot = slots[index];
                _places[fighter.Id] = slot;
                placed = model != null ? Instantiate(model, slot.position, slot.rotation, transform) : Placeholder(slot);
                placed.name = fighter.Id;
                if (placed.GetComponentInChildren<Animator>() is Animator animator && idle != null)
                {
                    animator.runtimeAnimatorController = idle;
                    animator.applyRootMotion = false;
                    // Start each fighter at a different point of the loop, so they don't breathe in step.
                    animator.Play(0, 0, (index * 0.37f + (fighter.IsHero ? 0f : 0.5f)) % 1f);
                }
                _fighters[fighter.Id] = placed;
            }
            placed.SetActive(!fighter.IsDown);
        }

        // Stands in for a fighter whose model isn't on this machine, so no slot is left empty.
        GameObject Placeholder(Transform slot)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(capsule.GetComponent<Collider>());
            capsule.transform.SetParent(transform, false);
            capsule.transform.SetPositionAndRotation(slot.position + Vector3.up, slot.rotation);
            capsule.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            return capsule;
        }
    }
}
