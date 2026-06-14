using System.Reflection;
using Ashlight.Ghost;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Ashlight.UI.Tests
{
    public class GhostHealthBarTests
    {
        [Test]
        public void GhostState_CombatStates_AreDefinedForHealthBarVisibility()
        {
            Assert.AreNotEqual(GhostState.Stalk, GhostState.Idle);
            Assert.AreNotEqual(GhostState.Chase, GhostState.Wander);
            Assert.AreNotEqual(GhostState.Attack, GhostState.Idle);
            Assert.AreNotEqual(GhostState.Retreat, GhostState.Wander);
            Assert.AreNotEqual(GhostState.Recharge, GhostState.Idle);
        }

        [Test]
        public void OnGhostSpawn_HealthBarShowsFullHealth()
        {
            GameObject player = new GameObject("Player");
            PooledGhostSetup setup = CreatePooledGhost(currentHealth: 0f, sliderValue: 0f, canvasActive: false);

            try
            {
                setup.GhostRoot.SetActive(true);
                Assert.IsTrue(setup.GhostAI.enabled, "GhostAIController must be enabled after Awake.");
                setup.GhostAI.Activate(player.transform);

                Assert.AreEqual(1f, setup.GhostAI.GhostHealthPercent, 0.001f);
                Assert.AreEqual(1f, setup.HealthSlider.value, 0.001f);
                Assert.IsTrue(setup.HealthBarCanvas.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(setup.GhostRoot);
                Object.DestroyImmediate(setup.GhostType);
            }
        }

        [Test]
        public void OnGhostPoolReuse_HealthBarRespawnsWithFullHealth()
        {
            GameObject player = new GameObject("Player");
            PooledGhostSetup setup = CreatePooledGhost(currentHealth: 100f, sliderValue: 1f, canvasActive: true);

            try
            {
                setup.GhostRoot.SetActive(true);
                setup.GhostAI.Activate(player.transform);

                SetPrivateField(setup.GhostAI, "currentGhostHealth", 0f);
                InvokeUpdateHealthBar(setup.GhostAI);
                InvokeLateUpdate(setup.HealthBar);

                Assert.IsFalse(setup.HealthBarCanvas.gameObject.activeSelf);

                setup.GhostRoot.SetActive(false);
                setup.GhostRoot.SetActive(true);
                setup.GhostAI.Activate(player.transform);

                Assert.AreEqual(1f, setup.GhostAI.GhostHealthPercent, 0.001f);
                Assert.AreEqual(1f, setup.HealthSlider.value, 0.001f);
                Assert.IsTrue(setup.HealthBarCanvas.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(setup.GhostRoot);
                Object.DestroyImmediate(setup.GhostType);
            }
        }

        private static PooledGhostSetup CreatePooledGhost(float currentHealth, float sliderValue, bool canvasActive)
        {
            GhostTypeDefinition ghostType = ScriptableObject.CreateInstance<GhostTypeDefinition>();

            GameObject ghostRoot = new GameObject("Ghost");
            ghostRoot.SetActive(false);

            ghostRoot.AddComponent<NavMeshAgent>();
            GhostPerceptionSystem perception = ghostRoot.AddComponent<GhostPerceptionSystem>();
            GhostAIController ghostAI = ghostRoot.AddComponent<GhostAIController>();

            SetPrivateField(ghostAI, "ghostType", ghostType);
            SetPrivateField(ghostAI, "perception", perception);
            SetPrivateField(ghostAI, "maxGhostHealth", 100f);
            SetPrivateField(ghostAI, "currentGhostHealth", currentHealth);

            GameObject canvasObject = new GameObject("HealthBarCanvas");
            canvasObject.transform.SetParent(ghostRoot.transform, false);
            Canvas healthBarCanvas = canvasObject.AddComponent<Canvas>();

            GameObject sliderObject = new GameObject("HealthBarSlider");
            sliderObject.transform.SetParent(canvasObject.transform, false);
            Slider healthSlider = sliderObject.AddComponent<Slider>();

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(sliderObject.transform, false);
            Image fillImage = fillObject.AddComponent<Image>();

            GhostHealthBar healthBar = ghostRoot.AddComponent<GhostHealthBar>();
            SetPrivateField(healthBar, "healthSlider", healthSlider);
            SetPrivateField(healthBar, "fillImage", fillImage);
            SetPrivateField(healthBar, "healthBarCanvas", healthBarCanvas);

            healthSlider.value = sliderValue;
            healthBarCanvas.gameObject.SetActive(canvasActive);

            return new PooledGhostSetup(ghostRoot, ghostAI, healthBar, healthSlider, healthBarCanvas, ghostType);
        }

        private static void InvokeUpdateHealthBar(GhostAIController ghostAI)
        {
            MethodInfo updateHealthBar = typeof(GhostAIController).GetMethod(
                "UpdateHealthBar",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(updateHealthBar, "UpdateHealthBar method was not found.");
            updateHealthBar.Invoke(ghostAI, null);
        }

        private static void InvokeLateUpdate(GhostHealthBar healthBar)
        {
            MethodInfo lateUpdate = typeof(GhostHealthBar).GetMethod(
                "LateUpdate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(lateUpdate, "LateUpdate method was not found.");
            lateUpdate.Invoke(healthBar, null);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private sealed class PooledGhostSetup
        {
            public PooledGhostSetup(
                GameObject ghostRoot,
                GhostAIController ghostAI,
                GhostHealthBar healthBar,
                Slider healthSlider,
                Canvas healthBarCanvas,
                GhostTypeDefinition ghostType)
            {
                GhostRoot = ghostRoot;
                GhostAI = ghostAI;
                HealthBar = healthBar;
                HealthSlider = healthSlider;
                HealthBarCanvas = healthBarCanvas;
                GhostType = ghostType;
            }

            public GameObject GhostRoot { get; }

            public GhostAIController GhostAI { get; }

            public GhostHealthBar HealthBar { get; }

            public Slider HealthSlider { get; }

            public Canvas HealthBarCanvas { get; }

            public GhostTypeDefinition GhostType { get; }
        }
    }
}
