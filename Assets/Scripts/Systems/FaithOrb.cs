using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// Collectible faith orb dropped when ghosts perish.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class FaithOrb : MonoBehaviour
    {
        private const float BobSpeed = 2f;
        private const float BobHeight = 0.3f;
        private const float RotationSpeed = 45f;
        private const float GlowPulseInterval = 0.5f;
        private const float LifetimeSeconds = 30f;

        [SerializeField] private int faithValue = 10;
        [SerializeField] private UpgradeSystem upgradeSystem;
        [SerializeField] private AudioClip collectSound;

        private Vector3 _spawnPosition;
        private float _glowPulseTimer;
        private MeshRenderer _meshRenderer;
        private Material _emissionMaterial;
        private Color _baseEmissionColor;
        private bool _hasEmission;

        private void Awake()
        {
            Collider orbCollider = GetComponent<Collider>();
            if (orbCollider != null)
            {
                orbCollider.isTrigger = true;
            }

            if (upgradeSystem == null)
            {
                upgradeSystem = FindAnyObjectByType<UpgradeSystem>();
            }
        }

        private void Start()
        {
            _spawnPosition = transform.position;
            Destroy(gameObject, LifetimeSeconds);

            _meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _emissionMaterial = _meshRenderer.material;
                if (_emissionMaterial.HasProperty("_EmissionColor"))
                {
                    _hasEmission = true;
                    _baseEmissionColor = _emissionMaterial.GetColor("_EmissionColor");
                    _emissionMaterial.EnableKeyword("_EMISSION");
                }
            }
        }

        private void Update()
        {
            transform.position = _spawnPosition + Vector3.up * (Mathf.Sin(Time.time * BobSpeed) * BobHeight);
            transform.Rotate(0f, RotationSpeed * Time.deltaTime, 0f);
            UpdateGlowPulse();
        }

        /// <summary>
        /// Configures the orb faith value at spawn time.
        /// </summary>
        /// <param name="value">Faith granted on collection.</param>
        public void Initialize(int value)
        {
            faithValue = Mathf.Max(0, value);
        }

        private void UpdateGlowPulse()
        {
            if (!_hasEmission || _emissionMaterial == null)
            {
                return;
            }

            _glowPulseTimer -= Time.deltaTime;
            if (_glowPulseTimer > 0f)
            {
                return;
            }

            _glowPulseTimer = GlowPulseInterval;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
            _emissionMaterial.SetColor("_EmissionColor", _baseEmissionColor * pulse);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (upgradeSystem != null)
            {
                upgradeSystem.AddFaith(faithValue);
            }

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
