using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// The game's sound: a music track for each kind of screen, crossfaded, and short effects for what happens. Clips are
    /// loaded by name from Resources/Audio (see Assets/Audio/CREDITS.md); a missing clip is simply silent. It plays what
    /// the screens ask for and never touches game state. The volumes come from <see cref="GameSettings"/>.
    /// </summary>
    sealed class GameAudio : MonoBehaviour
    {
        public enum Track { None, Title, Map, Rooms, Battle, Lair }

        const float FadeSeconds = 1.5f;

        static GameAudio _instance;
        // Two music sources, so the old track can fade out while the new one fades in.
        AudioSource _playing, _fading;
        AudioSource _effects;
        Track _track = Track.None;
        float _fade = 1f;
        readonly Dictionary<string, AudioClip[]> _clips = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<string, int> _turns = new Dictionary<string, int>();

        static GameAudio Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var holder = new GameObject("Game Audio");
                DontDestroyOnLoad(holder);
                _instance = holder.AddComponent<GameAudio>();
                return _instance;
            }
        }

        void Awake()
        {
            _instance = this;
            // The game's sound is all 2D, so the player carries the one listener. The main camera's is switched off,
            // because the combat stage turns that camera off during fights, which would leave no one listening.
            foreach (AudioListener other in FindObjectsByType<AudioListener>(FindObjectsInactive.Include)) other.enabled = false;
            gameObject.AddComponent<AudioListener>();
            _playing = NewSource(loop: true);
            _fading = NewSource(loop: true);
            _effects = NewSource(loop: false);
            ApplyVolumes();
        }

        AudioSource NewSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        /// <summary>Crossfades to the track for a screen. Asking for the track already playing changes nothing.</summary>
        public static void Play(Track track) => Instance.PlayTrack(track);

        /// <summary>
        /// Plays an effect by name, after <paramref name="delaySeconds"/>. Names with numbered variants ("hit_1", "hit_2")
        /// take turns, so a run of blows doesn't repeat one sound.
        /// </summary>
        public static void Play(string effect, float delaySeconds = 0f) => Instance.PlayEffect(effect, delaySeconds);

        /// <summary>Applies the volumes from Settings: master to everything, then music and effects on their own.</summary>
        public static void ApplyVolumes()
        {
            if (_instance == null) return;
            AudioListener.volume = GameSettings.MasterVolume;
            _instance._effects.volume = GameSettings.EffectsVolume;
            _instance.SetMusicVolumes();
        }

        /// <summary>Gives every button under <paramref name="root"/> a click.</summary>
        public static void ClickSounds(VisualElement root) =>
            root.RegisterCallback<ClickEvent>(e => { if (e.target is Button) Play("click"); }, TrickleDown.TrickleDown);

        void PlayTrack(Track track)
        {
            if (track == _track) return;
            _track = track;
            AudioClip clip = track == Track.None ? null : Resources.Load<AudioClip>("Audio/Music/" + track.ToString().ToLowerInvariant());
            (_playing, _fading) = (_fading, _playing);
            _playing.clip = clip;
            if (clip != null) _playing.Play();
            else _playing.Stop();
            _fade = 0f;
            Debug.Log($"[Audio] Music: {track}" + (clip == null && track != Track.None ? " (no clip found)." : "."));
        }

        void Update()
        {
            if (_fade >= 1f) return;
            _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / FadeSeconds);
            SetMusicVolumes();
            if (_fade >= 1f) _fading.Stop();
        }

        void SetMusicVolumes()
        {
            _playing.volume = GameSettings.MusicVolume * _fade;
            _fading.volume = GameSettings.MusicVolume * (1f - _fade);
        }

        void PlayEffect(string effect, float delaySeconds)
        {
            AudioClip[] variants = Variants(effect);
            if (variants.Length == 0) return;
            _turns.TryGetValue(effect, out int turn);
            _turns[effect] = turn + 1;
            AudioClip clip = variants[turn % variants.Length];
            if (delaySeconds <= 0f) _effects.PlayOneShot(clip);
            else StartCoroutine(PlayLater(clip, delaySeconds));
        }

        IEnumerator PlayLater(AudioClip clip, float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            _effects.PlayOneShot(clip);
        }

        AudioClip[] Variants(string effect)
        {
            if (_clips.TryGetValue(effect, out AudioClip[] found)) return found;
            var variants = new List<AudioClip>();
            for (int i = 1; ; i++)
            {
                AudioClip clip = Resources.Load<AudioClip>($"Audio/Effects/{effect}_{i}");
                if (clip == null) break;
                variants.Add(clip);
            }
            return _clips[effect] = variants.ToArray();
        }
    }
}
