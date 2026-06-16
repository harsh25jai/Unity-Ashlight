using Ashlight.Systems;

namespace Ashlight.UI
{
    /// <summary>
    /// Testable main menu state and messaging logic separated from UI Toolkit view code.
    /// </summary>
    public class MainMenuPresenter
    {
        /// <summary>Gets the message shown when no save file exists.</summary>
        public string NoSaveMessage => "No save found";

        /// <summary>Gets the message shown when settings are not yet implemented.</summary>
        public string SettingsUnavailableMessage => "Settings — coming soon";

        /// <summary>
        /// Determines whether the continue action should be enabled.
        /// </summary>
        /// <param name="saveSystem">Active save system, if any.</param>
        /// <returns>True when a save file exists on disk.</returns>
        public bool CanContinue(SaveSystem saveSystem)
        {
            return saveSystem != null && saveSystem.HasSave;
        }

        /// <summary>
        /// Resolves whether the no-save hint should be visible on menu load.
        /// </summary>
        /// <param name="saveSystem">Active save system, if any.</param>
        /// <returns>True when continue is unavailable.</returns>
        public bool ShouldShowNoSaveHint(SaveSystem saveSystem)
        {
            return !CanContinue(saveSystem);
        }
    }
}
