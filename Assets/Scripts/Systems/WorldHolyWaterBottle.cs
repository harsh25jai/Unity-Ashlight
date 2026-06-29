using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// World pickup that adds one Holy Water bottle with bob, glow, and rotation presentation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class WorldHolyWaterBottle : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip inventoryFullSound;
        [SerializeField] private ParticleSystem pickupEffect;
        [SerializeField] private float bobAmplitude = 0.25f;
        [SerializeField] private float bobFrequency = 2f;
        [SerializeField] private float rotationSpeed = 45f;
        [SerializeField] private float glowPulseSpeed = 3f;
        [SerializeField] private float glowPulseStrength = 1.5f;
        [SerializeField] private string playerTag = "Player";

        private AudioSource _audioSource;
        private MeshRenderer _meshRenderer;
        private Material _glowMaterial;
        private Color _baseEmissionColor;
        private Vector3 _anchorPosition;

        private void Awake()
        {
            Collider pickupCollider = GetComponent<Collider>();
            if (pickupCollider == null)
            {
                Debug.LogError($"{nameof(WorldHolyWaterBottle)} requires a {nameof(Collider)}.", this);
                enabled = false;
                return;
            }

            pickupCollider.isTrigger = true;

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            if (holyWaterInventory == null)
            {
                Debug.LogError($"{nameof(WorldHolyWaterBottle)} requires a {nameof(HolyWaterInventory)} reference.", this);
                enabled = false;
                return;
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;

            _meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (_meshRenderer != null)
            {
                _glowMaterial = _meshRenderer.material;
                if (_glowMaterial.HasProperty(EmissionColorId))
                {
                    _baseEmissionColor = _glowMaterial.GetColor(EmissionColorId);
                    _glowMaterial.EnableKeyword("_EMISSION");
                }
            }

            _anchorPosition = transform.position;
        }

        private void Update()
        {
            AnimateBob();
            AnimateRotation();
            AnimateGlowPulse();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (holyWaterInventory == null || !other.CompareTag(playerTag))
            {
                return;
            }

            if (holyWaterInventory.AddBottle())
            {
                PlaySound(pickupSound);

                if (pickupEffect != null)
                {
                    pickupEffect.Play();
                }

                Destroy(gameObject);
                return;
            }

            PlaySound(inventoryFullSound);
        }

        private void AnimateBob()
        {
            Vector3 position = _anchorPosition;
            position.y += Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = position;
        }

        private void AnimateRotation()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
        }

        private void AnimateGlowPulse()
        {
            if (_glowMaterial == null || !_glowMaterial.HasProperty(EmissionColorId))
            {
                return;
            }

            float pulse = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;
            float intensity = 1f + pulse * glowPulseStrength;
            _glowMaterial.SetColor(EmissionColorId, _baseEmissionColor * intensity);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || _audioSource == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip);
        }
    }
}
