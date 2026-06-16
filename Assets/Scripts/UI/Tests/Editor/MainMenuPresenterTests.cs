using Ashlight.Systems;
using NUnit.Framework;

namespace Ashlight.UI.Tests
{
    public class MainMenuPresenterTests
    {
        private readonly MainMenuPresenter _presenter = new MainMenuPresenter();

        [Test]
        public void CanContinue_ReturnsFalseWhenSaveSystemMissing()
        {
            Assert.IsFalse(_presenter.CanContinue(null));
        }

        [Test]
        public void ShouldShowNoSaveHint_WhenContinueUnavailable()
        {
            Assert.IsTrue(_presenter.ShouldShowNoSaveHint(null));
        }

        [Test]
        public void SettingsUnavailableMessage_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_presenter.SettingsUnavailableMessage));
        }
    }
}
