using NUnit.Framework;

namespace Ashlight.Environment.Tests
{
    public class LightingDebugControllerTests
    {
        [Test]
        public void LightingDebugController_TypeExists()
        {
            Assert.NotNull(typeof(LightingDebugController));
        }

        [Test]
        public void LogLightingDiagnostic_MethodExists()
        {
            Assert.NotNull(typeof(LightingDebugController).GetMethod(nameof(LightingDebugController.LogLightingDiagnostic)));
        }
    }
}
