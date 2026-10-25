using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// The title, pause and settings menus, drawn over everything else. They hold no game state: the run screen
    /// listens for the choices and acts on them. Esc opens the pause menu during a run and closes whatever is open.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameMenus : MonoBehaviour
    {
        public event Action ContinueChosen;
        public event Action NewExpeditionChosen;
        public event Action QuitToTitleChosen;

        /// <summary>True during a run, when Esc may pause. The run screen sets it.</summary>
        public bool CanPause { get; set; }
        /// <summary>Asked before pausing, so Esc can still cancel a card's targeting in a fight.</summary>
        public Func<bool> EscapeTaken { get; set; }

        VisualElement _root, _titleView, _pauseView, _settingsView;
        Button _continue;
        Label _titleNote;
        Toggle _fullscreen, _fastAnimations, _chartMap;
        // Where Back on the settings menu returns to.
        VisualElement _settingsFrom;

        void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _titleView = _root.Q("title-view");
            _pauseView = _root.Q("pause-view");
            _settingsView = _root.Q("settings-view");
            _continue = _root.Q<Button>("continue");
            _titleNote = _root.Q<Label>("title-note");
            _fullscreen = _root.Q<Toggle>("fullscreen");
            _fastAnimations = _root.Q<Toggle>("fast-animations");
            _chartMap = _root.Q<Toggle>("chart-map");

            // The backdrop is rendered from licensed art that is never committed, so a fresh clone won't have it
            // and the title keeps its plain background.
            var backdrop = Resources.Load<Texture2D>("TitleBackdrop");
            if (backdrop != null)
            {
                _titleView.style.backgroundImage = new StyleBackground(backdrop);
                _titleView.AddToClassList("has-backdrop");
            }

            _continue.clicked += () => Choose(ContinueChosen);
            _root.Q<Button>("new-expedition").clicked += () => Choose(NewExpeditionChosen);
            _root.Q<Button>("title-settings").clicked += () => ShowSettings(_titleView);
            var quit = _root.Q<Button>("quit-game");
            quit.clicked += Quit;
            // A browser tab can't be closed by the game, so the web build has no Quit.
            if (Application.platform == RuntimePlatform.WebGLPlayer) quit.style.display = DisplayStyle.None;
            _root.Q<Button>("resume").clicked += Close;
            _root.Q<Button>("pause-settings").clicked += () => ShowSettings(_pauseView);
            _root.Q<Button>("quit-to-title").clicked += () => Choose(QuitToTitleChosen);
            _root.Q<Button>("settings-back").clicked += () => Show(_settingsFrom);
            _fullscreen.RegisterValueChangedCallback(e =>
            {
                GameSettings.Fullscreen = e.newValue;
                Debug.Log($"[Settings] Fullscreen {(e.newValue ? "on" : "off")}.");
            });
            _fastAnimations.RegisterValueChangedCallback(e =>
            {
                GameSettings.FastAnimations = e.newValue;
                Debug.Log($"[Settings] Fast animations {(e.newValue ? "on" : "off")}.");
            });
            _chartMap.RegisterValueChangedCallback(e =>
            {
                GameSettings.ChartMap = e.newValue;
                Debug.Log($"[Settings] Map drawn as {(e.newValue ? "the chart" : "the floor plan")}.");
            });

            GameSettings.Apply();
            Close();
        }

        public bool IsOpen => _root.style.display != DisplayStyle.None;

        /// <summary>The title screen. Continue shows only when there is a run to carry on; <paramref name="note"/> says why one couldn't load.</summary>
        public void ShowTitle(bool canContinue, string note = null)
        {
            CanPause = false;
            _continue.EnableInClassList("hidden", !canContinue);
            _titleNote.text = note ?? "";
            Show(_titleView);
        }

        public void Close() => Show(null);

        void Show(VisualElement view)
        {
            _root.style.display = view == null ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (VisualElement v in new[] { _titleView, _pauseView, _settingsView }) v.EnableInClassList("hidden", v != view);
        }

        void ShowSettings(VisualElement from)
        {
            _settingsFrom = from;
            _fullscreen.SetValueWithoutNotify(GameSettings.Fullscreen);
            _fastAnimations.SetValueWithoutNotify(GameSettings.FastAnimations);
            _chartMap.SetValueWithoutNotify(GameSettings.ChartMap);
            Show(_settingsView);
        }

        void Choose(Action choice)
        {
            Close();
            choice?.Invoke();
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (!_settingsView.ClassListContains("hidden")) Show(_settingsFrom);
            else if (!_pauseView.ClassListContains("hidden")) Close();
            else if (!IsOpen && CanPause && !(EscapeTaken?.Invoke() ?? false))
            {
                Debug.Log("[Menus] Paused.");
                Show(_pauseView);
            }
        }

        static void Quit()
        {
            Debug.Log("[Menus] Quit.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
