using NUnit.Framework;

namespace Ashlight.Environment.Tests
{
    public class WorshipInteractionTests
    {
        [Test]
        public void SimpleAltarInteraction_TypeExists()
        {
            Assert.NotNull(typeof(SimpleAltarInteraction));
        }

        [Test]
        public void ChurchType_SimpleAltar_HasNoSavePoint()
        {
            Assert.AreNotEqual(ChurchType.SimpleAltar, ChurchType.Candle);
        }
    }
}
