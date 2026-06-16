using Ashlight.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ashlight.UI
{
    /// <summary>
    /// Main menu view controller with presenter-driven state and UI Toolkit animations.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour
    {
        private const string GameSceneName = "SampleScene";
        private const float MenuFadeDuration = 0.35f;
        private const float MenuSlideOffset = 24f;
        private const float AmbientPulseDuration = 4f;
        private const float TitleFlickerInterval = 6f;

        [SerializeField] private UIDocument menuDocument;
        [SerializeField] private float menuFadeDuration = MenuFadeDuration;

        private readonly MainMenuPresenter _presenter = new MainMenuPresenter();

        private VisualElement _centerPanel;
        private VisualElement _ambientGlow;
        private Label _titleLabel;
        private Label _noSaveLabel;
        private Label _statusLabel;
        private Button _newGameButton;
        private Button _continueButton;
        private Button _settingsButton;
        private Button _quitButton;

        private Coroutine _ambientPulseRoutine;
        private Coroutine _titleFlickerRoutine;

        private void Awake()
        {
            if (menuDocument == null)
            {
                menuDocument = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            VisualElement root = menuDocument != null ? menuDocument.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError($"{nameof(MainMenu)} has no UI root.", this);
                return;
            }

            CacheElements(root);
            WireButtons();
            RefreshContinueButtonState();
            PlayIntroAnimation();
            StartAmbientEffects();
            FocusDefaultButton();
        }

        private void OnDisable()
        {
            if (_ambientPulseRoutine != null)
            {
                StopCoroutine(_ambientPulseRoutine);
                _ambientPulseRoutine = null;
            }

            if (_titleFlickerRoutine != null)
            {
                StopCoroutine(_titleFlickerRoutine);
                _titleFlickerRoutine = null;
            }
        }

        /// <summary>
        /// Loads the game scene for a new run.
        /// </summary>
        public void StartNewGame()
        {
            SceneManager.LoadScene(GameSceneName);
        }

        /// <summary>
        /// Loads the game scene when a save file exists.
        /// </summary>
        public void ContinueGame()
        {
            SaveSystem saveSystem = ResolveSaveSystem();
            if (_presenter.CanContinue(saveSystem))
            {
                SceneManager.LoadScene(GameSceneName);
                return;
            }

            ShowNoSaveMessage();
        }

        /// <summary>
        /// Shows a placeholder message until a settings screen is implemented.
        /// </summary>
        public void OpenSettings()
        {
            ShowStatusMessage(_presenter.SettingsUnavailableMessage);
        }

        /// <summary>
        /// Quits the application, with an editor play-mode fallback.
        /// </summary>
        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }

        private void CacheElements(VisualElement root)
        {
            _centerPanel = root.Q<VisualElement>("center-panel");
            _ambientGlow = root.Q<VisualElement>("ambient-glow");
            _titleLabel = root.Q<Label>("title-label");
            _noSaveLabel = root.Q<Label>("no-save-label");
            _statusLabel = root.Q<Label>("status-label");
            _newGameButton = root.Q<Button>("new-game-button");
            _continueButton = root.Q<Button>("continue-button");
            _settingsButton = root.Q<Button>("settings-button");
            _quitButton = root.Q<Button>("quit-button");
        }

        private void WireButtons()
        {
            _newGameButton?.RegisterCallback<ClickEvent>(_ => StartNewGame());
            _continueButton?.RegisterCallback<ClickEvent>(_ => ContinueGame());
            _settingsButton?.RegisterCallback<ClickEvent>(_ => OpenSettings());
            _quitButton?.RegisterCallback<ClickEvent>(_ => QuitGame());
        }

        private void PlayIntroAnimation()
        {
            if (_centerPanel == null)
            {
                return;
            }

            _centerPanel.style.opacity = 0f;
            _centerPanel.style.translate = new Translate(0f, MenuSlideOffset);

            UIAnimator.Fade(this, _centerPanel, 0f, 1f, menuFadeDuration);
            UIAnimator.SlideY(this, _centerPanel, MenuSlideOffset, 0f, menuFadeDuration);
        }

        private void StartAmbientEffects()
        {
            if (_ambientGlow != null)
            {
                _ambientPulseRoutine = UIAnimator.PulseOpacity(this, _ambientGlow, 0.04f, 0.12f, AmbientPulseDuration);
            }

            if (_titleLabel != null)
            {
                _titleFlickerRoutine = StartCoroutine(TitleFlickerRoutine());
            }
        }

        private System.Collections.IEnumerator TitleFlickerRoutine()
        {
            while (_titleLabel != null)
            {
                yield return new WaitForSecondsRealtime(TitleFlickerInterval + Random.Range(-1f, 1f));
                UIAnimator.Fade(this, _titleLabel, 1f, 0.82f, 0.08f, () =>
                {
                    if (_titleLabel != null)
                    {
                        UIAnimator.Fade(this, _titleLabel, 0.82f, 1f, 0.12f);
                    }
                });
            }
        }

        private void FocusDefaultButton()
        {
            if (_newGameButton != null)
            {
                _newGameButton.Focus();
            }
        }

        private void RefreshContinueButtonState()
        {
            SaveSystem saveSystem = ResolveSaveSystem();
            bool canContinue = _presenter.CanContinue(saveSystem);

            if (_continueButton != null)
            {
                _continueButton.SetEnabled(canContinue);
            }

            SetLabelVisible(_noSaveLabel, _presenter.ShouldShowNoSaveHint(saveSystem), _presenter.NoSaveMessage);
            HideStatusMessage();
        }

        private void ShowNoSaveMessage()
        {
            SetLabelVisible(_noSaveLabel, true, _presenter.NoSaveMessage);
            HideStatusMessage();
        }

        private void ShowStatusMessage(string message)
        {
            if (_statusLabel == null)
            {
                return;
            }

            _statusLabel.text = message;
            _statusLabel.AddToClassList("status-label--visible");
        }

        private void HideStatusMessage()
        {
            if (_statusLabel == null)
            {
                return;
            }

            _statusLabel.text = string.Empty;
            _statusLabel.RemoveFromClassList("status-label--visible");
        }

        private static void SetLabelVisible(Label label, bool visible, string text)
        {
            if (label == null)
            {
                return;
            }

            label.text = visible ? text : string.Empty;
            if (visible)
            {
                label.AddToClassList("no-save-label--visible");
            }
            else
            {
                label.RemoveFromClassList("no-save-label--visible");
            }
        }

        private static SaveSystem ResolveSaveSystem()
        {
            return SaveSystem.Instance ?? FindAnyObjectByType<SaveSystem>();
        }
    }
}
