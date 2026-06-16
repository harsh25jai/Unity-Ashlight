using NUnit.Framework;
using UnityEngine;

namespace Ashlight.UI.Tests
{
    public class MainMenuTests
    {
        [Test]
        public void MainMenu_CanBeAddedToGameObject()
        {
            GameObject host = new GameObject("MainMenuTestHost");
            MainMenu menu = host.AddComponent<MainMenu>();
            Assert.IsNotNull(menu);
            Object.DestroyImmediate(host);
        }
    }
}
