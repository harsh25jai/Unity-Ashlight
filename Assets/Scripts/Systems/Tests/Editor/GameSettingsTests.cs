using NUnit.Framework;

namespace Ashlight.Systems.Tests
{
    public class GameSettingsTests
    {
        [Test]
        public void GameSettings_TypeExists()
        {
            Assert.NotNull(typeof(GameSettings));
        }

        [Test]
        public void AutoSaveEnabled_DefaultsToTrue()
        {
            GameSettings settings = UnityEngine.ScriptableObject.CreateInstance<GameSettings>();

            try
            {
                Assert.IsTrue(settings.AutoSaveEnabled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
