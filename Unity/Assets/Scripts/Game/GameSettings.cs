using UnityEngine;

namespace WhereDarknessDwells.Game
{
    /// <summary>The player's settings, kept in PlayerPrefs between sessions. Volume arrives with sound in Milestone 6.5.</summary>
    static class GameSettings
    {
        const string FullscreenKey = "settings.fullscreen";
        const string FastAnimationsKey = "settings.fastAnimations";

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

        /// <summary>What combat's floaters and pauses are multiplied by: 1 at normal speed, less when fast.</summary>
        public static float AnimationScale => FastAnimations ? 0.5f : 1f;

        /// <summary>Puts the saved window mode back when the game starts.</summary>
        public static void Apply()
        {
            if (PlayerPrefs.HasKey(FullscreenKey)) Screen.fullScreen = Fullscreen;
        }
    }
}
