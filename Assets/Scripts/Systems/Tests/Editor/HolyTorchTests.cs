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

            GameObject pointLightObject = new GameObject("PointLight");
            pointLightObject.transform.SetParent(torchObject.transform);
            Light point = pointLightObject.AddComponent<Light>();
            point.type = LightType.Point;

            GameObject spotLightObject = new GameObject("SpotLight");
            spotLightObject.transform.SetParent(torchObject.transform);
            Light spot = spotLightObject.AddComponent<Light>();
            spot.type = LightType.Spot;

            HolyTorch torch = torchObject.AddComponent<HolyTorch>();

            typeof(HolyTorch).GetField(
                "maxFuel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(torch, maxFuel);

            typeof(HolyTorch).GetField(
                "_currentFuel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(torch, currentFuel);

            typeof(HolyTorch).GetField(
                "pointLight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(torch, point);

            typeof(HolyTorch).GetField(
                "spotLight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(torch, spot);

            return torchObject;
        }
    }
}
