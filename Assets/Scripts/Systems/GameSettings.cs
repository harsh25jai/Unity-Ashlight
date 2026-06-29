using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// Global game settings placeholder for save behavior and future options menus.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Ashlight/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        /// <summary>When true, worship at save churches triggers automatic saves.</summary>
        public bool AutoSaveEnabled = true;
    }
}
