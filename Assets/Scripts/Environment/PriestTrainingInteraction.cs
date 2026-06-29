using UnityEngine;

namespace Ashlight.Environment
{
    /// <summary>
    /// Priest training worship placeholder until the full task system is implemented.
    /// </summary>
    [DisallowMultipleComponent]
    public class PriestTrainingInteraction : WorshipInteraction
    {
        /// <inheritdoc />
        protected override void OnWorship()
        {
            Debug.Log(
                "Priest training interaction — task system not yet implemented (Section 8 of PRD)",
                this);
            base.OnWorship();
        }

        /// <inheritdoc />
        protected override string GetPromptText()
        {
            return "Press E to begin training (coming soon)";
        }
    }
}
