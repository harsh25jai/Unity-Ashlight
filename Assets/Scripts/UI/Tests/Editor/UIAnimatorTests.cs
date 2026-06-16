using NUnit.Framework;

namespace Ashlight.UI.Tests
{
    public class UIAnimatorTests
    {
        [Test]
        public void EvaluateProgress_ClampsToRange()
        {
            Assert.AreEqual(0f, UIAnimator.EvaluateProgress(0f, 0.35f), 0.001f);
            Assert.AreEqual(0.5f, UIAnimator.EvaluateProgress(0.175f, 0.35f), 0.001f);
            Assert.AreEqual(1f, UIAnimator.EvaluateProgress(0.35f, 0.35f), 0.001f);
            Assert.AreEqual(1f, UIAnimator.EvaluateProgress(1f, 0.35f), 0.001f);
        }

        [Test]
        public void EvaluateProgress_ReturnsOneForZeroDuration()
        {
            Assert.AreEqual(1f, UIAnimator.EvaluateProgress(0f, 0f));
        }
    }
}
