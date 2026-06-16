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
    /// Main menu scene controller wired to UI Toolkit buttons.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour
    {
        private const string GameSceneName = "SampleScene";

        [SerializeField] private UIDocument menuDocument;

        private Button _newGameButton;
        private Button _continueButton;
        private Button _quitButton;
        private Label _noSaveLabel;

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

            _newGameButton = root.Q<Button>("new-game-button");
            _continueButton = root.Q<Button>("continue-button");
            _quitButton = root.Q<Button>("quit-button");
            _noSaveLabel = root.Q<Label>("no-save-label");

            _newGameButton?.RegisterCallback<ClickEvent>(_ => StartNewGame());
            _continueButton?.RegisterCallback<ClickEvent>(_ => ContinueGame());
            _quitButton?.RegisterCallback<ClickEvent>(_ => QuitGame());

            RefreshContinueButtonState();
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
            SaveSystem saveSystem = SaveSystem.Instance ?? FindAnyObjectByType<SaveSystem>();
            if (saveSystem != null && saveSystem.HasSave)
            {
                SceneManager.LoadScene(GameSceneName);
                return;
            }

            ShowNoSaveMessage();
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

        private void RefreshContinueButtonState()
        {
            SaveSystem saveSystem = SaveSystem.Instance ?? FindAnyObjectByType<SaveSystem>();
            bool hasSave = saveSystem != null && saveSystem.HasSave;

            if (_continueButton != null)
            {
                _continueButton.SetEnabled(hasSave);
                _continueButton.style.color = hasSave
                    ? new Color(0.92f, 0.9f, 0.85f)
                    : new Color(0.45f, 0.45f, 0.45f);
            }

            if (_noSaveLabel != null)
            {
                _noSaveLabel.style.display = hasSave ? DisplayStyle.None : DisplayStyle.Flex;
                _noSaveLabel.text = hasSave ? string.Empty : "No save found";
            }
        }

        private void ShowNoSaveMessage()
        {
            if (_noSaveLabel != null)
            {
                _noSaveLabel.style.display = DisplayStyle.Flex;
                _noSaveLabel.text = "No save found";
            }
        }
    }
}
