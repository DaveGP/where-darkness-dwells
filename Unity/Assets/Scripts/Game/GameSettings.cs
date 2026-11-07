using UnityEngine;

namespace WhereDarknessDwells.Game
{
    /// <summary>The player's settings, kept in PlayerPrefs between sessions.</summary>
    static class GameSettings
    {
        const string FullscreenKey = "settings.fullscreen";
        const string FastAnimationsKey = "settings.fastAnimations";
        const string ChartMapKey = "settings.chartMap";
        const string MasterVolumeKey = "settings.masterVolume";
        const string MusicVolumeKey = "settings.musicVolume";
        const string EffectsVolumeKey = "settings.effectsVolume";

        /// <summary>Raised when <see cref="ChartMap"/> changes, so a map on screen can be drawn the other way.</summary>
        public static event System.Action MapStyleChanged;

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Screen.fullScreen = value;
            }
        }

        public static bool FastAnimations
        {
            get => PlayerPrefs.GetInt(FastAnimationsKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FastAnimationsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Draws the map as the branching chart instead of the floor plan. A debug view: both draw the same run, so
        /// switching changes nothing in it.
        /// </summary>
        public static bool ChartMap
        {
            get => PlayerPrefs.GetInt(ChartMapKey, 0) == 1;
            set
            {
                if (value == ChartMap) return;
                PlayerPrefs.SetInt(ChartMapKey, value ? 1 : 0);
                PlayerPrefs.Save();
                MapStyleChanged?.Invoke();
            }
        }

        /// <summary>Every sound, from 0 (silent) to 1.</summary>
        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
            set => SetVolume(MasterVolumeKey, value);
        }

        /// <summary>The music, under the master volume.</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.6f);
            set => SetVolume(MusicVolumeKey, value);
        }

        /// <summary>Sound effects, under the master volume.</summary>
        public static float EffectsVolume
        {
            get => PlayerPrefs.GetFloat(EffectsVolumeKey, 0.8f);
            set => SetVolume(EffectsVolumeKey, value);
        }

        static void SetVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            GameAudio.ApplyVolumes();
        }

        /// <summary>What combat's floaters and pauses are multiplied by: 1 at normal speed, less when fast.</summary>
        public static float AnimationScale => FastAnimations ? 0.5f : 1f;

        /// <summary>Puts the saved window mode back when the game starts.</summary>
        public static void Apply()
        {
            if (PlayerPrefs.HasKey(FullscreenKey)) Screen.fullScreen = Fullscreen;
        }
    }
}
