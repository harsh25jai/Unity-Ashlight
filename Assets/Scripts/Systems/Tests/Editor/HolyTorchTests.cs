using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Systems.Tests
{
    public class HolyTorchTests
    {
        [Test]
        public void Refuel_IncreasesFuelPercent()
        {
            GameObject torchObject = CreateTorchWithFuel(maxFuel: 100f, currentFuel: 50f);
            HolyTorch torch = torchObject.GetComponent<HolyTorch>();

            torch.Refuel(25f);

            Assert.AreEqual(0.75f, torch.FuelPercent, 0.001f);

            Object.DestroyImmediate(torchObject);
        }

        private static GameObject CreateTorchWithFuel(float maxFuel, float currentFuel)
        {
            GameObject torchObject = new GameObject("Torch");
            GameObject lightObject = new GameObject("TorchLight");
            lightObject.transform.SetParent(torchObject.transform);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;

            HolyTorch torch = torchObject.AddComponent<HolyTorch>();

            var maxFuelField = typeof(HolyTorch).GetField(
                "maxFuel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxFuelField?.SetValue(torch, maxFuel);

            var currentFuelField = typeof(HolyTorch).GetField(
                "_currentFuel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            currentFuelField?.SetValue(torch, currentFuel);

            var lightField = typeof(HolyTorch).GetField(
                "_torchLight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lightField?.SetValue(torch, light);

            return torchObject;
        }
    }
}
