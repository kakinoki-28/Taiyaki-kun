using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TaiyakiKun
{
    /// <summary>
    /// Keeps a crow circling above its target so its shadow telegraphs the attack,
    /// then dives close to the floor, hovers briefly, and climbs back overhead.
    /// TriggerAttack can be connected
    /// directly to a UnityEvent from another gameplay system.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrowAttackController : MonoBehaviour
    {
        private static readonly int FlyingHash = Animator.StringToHash("flying");
        private static readonly int FlyingDirectionHash = Animator.StringToHash("flyingDirectionX");
        private const string DefaultFightDustResourcePath = "UI/crow-fight-dust";
        private const string FightDustOverlayShaderName = "TaiyakiKun/CrowFightDustOverlay";
        private const string FightDustMaskShaderName = "TaiyakiKun/CrowFightDustMask";
        private const int CrowMaskRenderQueue = 4998;
        private const int FightDustRenderQueue = 4999;
        private static int activeAttackTimeStops;
        private static float timeScaleBeforeAttackStop = 1f;

        [Header("Attack Target")]
        [SerializeField] private Transform target;

        [Header("Orbit Center")]
        [FormerlySerializedAs("fallbackGroundPoint")]
        [Tooltip("World-space center of the crow's warning orbit.")]
        [SerializeField] private Vector3 orbitCenter = new Vector3(0f, 0.25f, 2f);

        [Header("Shadow Warning")]
        [SerializeField, Min(1f)] private float hoverHeight = 7f;
        [SerializeField, Min(0f)] private float orbitRadius = 1.35f;
        [SerializeField, Min(0f)] private float orbitSpeed = 50f;

        [Header("Shadow Trigger")]
        [Tooltip("Only this object can activate the shadow event.")]
        [SerializeField] private Transform shadowTriggerObject;
        [Tooltip("Directional Light used to project the crow onto the ground. Down is used when unassigned.")]
        [SerializeField] private Transform directionalLight;
        [SerializeField, Min(0.05f)] private float shadowTriggerRadius = 1.25f;
        [SerializeField] private bool attackOnShadowEnter = true;
        [Tooltip("Stops the rest of the game during a shadow-triggered attack.")]
        [SerializeField] private bool stopTimeDuringShadowAttack = true;
        [FormerlySerializedAs("attackCall")]
        [SerializeField] private AudioClip shadowContactSound;
        [FormerlySerializedAs("attackCallVolume")]
        [SerializeField, Range(0f, 1f)] private float shadowContactSoundVolume = 0.7f;

        [Header("Attack")]
        [SerializeField, Min(0.01f)] private float diveSpeed = 5f;
        [SerializeField, Min(0f)] private float diveWindup = 0.25f;
        [SerializeField, Min(0.1f)] private float lowHoverHeight = 1.1f;
        [SerializeField, Min(0f)] private float lowHoverDuration = 1.8f;
        [SerializeField, Min(0f)] private float lowHoverBobAmount = 0.12f;
        [SerializeField, Min(0.01f)] private float ascentSpeed = 4.5f;
        [Tooltip("Number of anko items removed when a shadow-triggered attack begins. Set to 0 to disable.")]
        [SerializeField, Min(0)] private int ankoLossOnAttack = 1;
        [SerializeField] private bool disappearAfterAscent = true;
        [Tooltip("Additional height above the crow's position at attack start before it disappears.")]
        [SerializeField, Min(0.01f)] private float disappearHeightAboveStart = 3f;

        [Header("Attack World Effect")]
        [Tooltip("Uses Resources/UI/crow-fight-dust when unassigned.")]
        [SerializeField] private Texture2D fightDustTexture;
        [SerializeField, Min(0.1f)] private float fightDustWorldSize = 4f;
        [SerializeField] private Vector3 fightDustWorldOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField, Range(0f, 1f)] private float fightDustOpacity = 1f;
        [SerializeField, Range(0f, 0.2f)] private float fightDustPulseAmount = 0.04f;
        [SerializeField, Min(0f)] private float fightDustPulseSpeed = 6f;
        [SerializeField] private int fightDustSortingOrder = 100;
        [SerializeField] private AudioClip fightDustSound;
        [SerializeField, Range(0f, 1f)] private float fightDustSoundVolume = 1f;
        [Tooltip("Playback speed. 1 is the clip's original speed.")]
        [SerializeField, Range(0.1f, 3f)] private float fightDustSoundPitch = 1f;
        [SerializeField] private bool loopFightDustSound = true;

        [Header("Demo Scene")]
        [SerializeField] private bool autoTriggerOnStart = true;
        [SerializeField, Min(0f)] private float autoTriggerDelay = 4f;

        [Header("Events")]
        [SerializeField] private UnityEvent onShadowEntered;
        [SerializeField] private UnityEvent onAttackStarted;
        [SerializeField] private UnityEvent onAttackFinished;

        private Animator animator;
        private Rigidbody body;
        private Coroutine attackRoutine;
        private float orbitAngle;
        private float autoTriggerAt;
        private bool attacking;
        private bool finished;
        private bool triggerObjectWasInShadow;
        private Collider shadowTriggerCollider;
        private bool ownsAttackTimeStop;
        private AnimatorUpdateMode animatorUpdateModeBeforeStop;
        private GameObject fightDustObject;
        private SpriteRenderer fightDustRenderer;
        private AudioSource fightDustAudioSource;
        private Sprite runtimeFightDustSprite;
        private Material fightDustOverlayMaterial;
        private Vector3 fightDustBaseScale = Vector3.one;
        private bool fightDustVisible;
        private Renderer[] crowRenderers;
        private Renderer[] crowMaskRenderers;
        private GameObject[] crowMaskObjects;
        private Material fightDustMaskMaterial;

        public bool IsAttacking => attacking;
        public bool IsFinished => finished;

        private Vector3 AttackGroundPoint => target != null
            ? target.position
            : orbitCenter;
        private float AttackDeltaTime => ownsAttackTimeStop ? Time.unscaledDeltaTime : Time.deltaTime;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody>();

            if (fightDustTexture == null)
            {
                fightDustTexture = Resources.Load<Texture2D>(DefaultFightDustResourcePath);
            }

            CreateFightDustEffect();

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            crowRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer birdRenderer in crowRenderers)
            {
                birdRenderer.shadowCastingMode = ShadowCastingMode.On;
                birdRenderer.receiveShadows = true;
            }

            CreateCrowMasks();

            CacheShadowTriggerCollider();
            EnterWarningState();
        }

        private void Start()
        {
            autoTriggerAt = Time.time + autoTriggerDelay;
            // Start unlatched so an object already overlapping the shadow can still fire once.
            triggerObjectWasInShadow = false;
        }

        private void OnEnable()
        {
            if (fightDustObject == null)
            {
                CreateFightDustEffect();
            }
        }

        private void Update()
        {
            if (!attacking && !finished)
            {
                UpdateWarningOrbit();
                UpdateShadowTrigger();

                if (autoTriggerOnStart && Time.time >= autoTriggerAt)
                {
                    TriggerAttack();
                }
            }
            else if (finished && ResetKeyPressed())
            {
                ResetAttack();
            }
        }

        /// <summary>Starts the dive. Safe to call from a UnityEvent or SendMessage.</summary>
        [ContextMenu("Trigger Attack")]
        public void TriggerAttack()
        {
            TryTriggerAttack(false);
        }

        private void TryTriggerAttack(bool stopTime)
        {
            if (attacking || finished)
            {
                return;
            }

            if (stopTime)
            {
                BeginAttackTimeStop();
            }

            attackRoutine = StartCoroutine(AttackSequence());
        }

        /// <summary>Returns the crow to the overhead warning state for another attack.</summary>
        [ContextMenu("Reset Attack")]
        public void ResetAttack()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            EndAttackTimeStop();
            SetFightDustVisible(false);
            attacking = false;
            finished = false;
            autoTriggerOnStart = false;
            EnterWarningState();
            triggerObjectWasInShadow = false;
        }

        private void OnDisable()
        {
            SetFightDustVisible(false);
            EndAttackTimeStop();
        }

        private void OnDestroy()
        {
            SetCrowMasksVisible(false);

            if (fightDustObject != null)
            {
                Destroy(fightDustObject);
            }

            if (runtimeFightDustSprite != null)
            {
                Destroy(runtimeFightDustSprite);
            }

            if (fightDustOverlayMaterial != null)
            {
                Destroy(fightDustOverlayMaterial);
            }

            DestroyCrowMasks();
        }

        private void BeginAttackTimeStop()
        {
            if (!stopTimeDuringShadowAttack || ownsAttackTimeStop)
            {
                return;
            }

            ownsAttackTimeStop = true;
            if (animator != null)
            {
                animatorUpdateModeBeforeStop = animator.updateMode;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            if (activeAttackTimeStops == 0)
            {
                timeScaleBeforeAttackStop = Time.timeScale;
                Time.timeScale = 0f;
            }

            activeAttackTimeStops++;
        }

        private void EndAttackTimeStop()
        {
            if (!ownsAttackTimeStop)
            {
                return;
            }

            ownsAttackTimeStop = false;
            if (animator != null)
            {
                animator.updateMode = animatorUpdateModeBeforeStop;
            }

            activeAttackTimeStops = Mathf.Max(0, activeAttackTimeStops - 1);
            if (activeAttackTimeStops == 0)
            {
                Time.timeScale = timeScaleBeforeAttackStop;
            }
        }

        /// <summary>Changes the only object that is allowed to activate the shadow event.</summary>
        public void SetShadowTriggerObject(Transform newTriggerObject)
        {
            shadowTriggerObject = newTriggerObject;
            CacheShadowTriggerCollider();
            triggerObjectWasInShadow = false;
        }

        private void CacheShadowTriggerCollider()
        {
            if (shadowTriggerObject == null)
            {
                shadowTriggerCollider = null;
                return;
            }

            shadowTriggerCollider = shadowTriggerObject.GetComponentInChildren<Collider>();
            if (shadowTriggerCollider == null)
            {
                shadowTriggerCollider = shadowTriggerObject.GetComponentInParent<Collider>();
            }
        }

        private void EnterWarningState()
        {
            orbitAngle = 220f;
            Vector3 offset = OrbitOffset(orbitAngle);
            transform.position = orbitCenter + offset + Vector3.up * hoverHeight;
            FaceHorizontalDirection(new Vector3(-offset.z, 0f, offset.x));

            if (animator != null)
            {
                animator.SetBool(FlyingHash, true);
                animator.SetFloat(FlyingDirectionHash, 0.15f);
                animator.CrossFade("Base Layer.fly", 0.05f);
            }
        }

        private void UpdateWarningOrbit()
        {
            orbitAngle += orbitSpeed * Time.deltaTime;
            Vector3 offset = OrbitOffset(orbitAngle);
            transform.position = orbitCenter + offset + Vector3.up * hoverHeight;
            FaceHorizontalDirection(new Vector3(-offset.z, 0f, offset.x));
        }

        private void UpdateShadowTrigger()
        {
            if (shadowTriggerObject == null)
            {
                triggerObjectWasInShadow = false;
                return;
            }

            bool isInShadow = IsTriggerObjectInShadow();
            if (isInShadow && !triggerObjectWasInShadow)
            {
                triggerObjectWasInShadow = true;
                target = shadowTriggerObject;
                if (attackOnShadowEnter)
                {
                    TryTriggerAttack(true);
                    RemoveAnkoFromAttackTarget();
                }

                PlayShadowContactSound();
                onShadowEntered?.Invoke();
                return;
            }

            triggerObjectWasInShadow = isInShadow;
        }

        private void RemoveAnkoFromAttackTarget()
        {
            if (ankoLossOnAttack <= 0 || shadowTriggerObject == null)
            {
                return;
            }

            ScoreManager scoreManager = shadowTriggerObject.GetComponentInParent<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.RemoveAnko(ankoLossOnAttack);
            }
        }

        private void PlayShadowContactSound()
        {
            if (shadowContactSound == null)
            {
                return;
            }

            Vector3 soundPosition = shadowTriggerObject != null
                ? shadowTriggerObject.position
                : AttackGroundPoint;
            AudioSource.PlayClipAtPoint(
                shadowContactSound,
                soundPosition,
                shadowContactSoundVolume);
        }

        private bool IsTriggerObjectInShadow()
        {
            if (shadowTriggerObject == null)
            {
                return false;
            }

            Vector3 shadowPoint = GetProjectedShadowPoint();
            Vector2 shadowXZ = new Vector2(shadowPoint.x, shadowPoint.z);

            // Use the assigned object's collider footprint when available; otherwise use its pivot.
            Vector2 closestObjectPoint;
            if (shadowTriggerCollider != null)
            {
                Bounds bounds = shadowTriggerCollider.bounds;
                closestObjectPoint = new Vector2(
                    Mathf.Clamp(shadowPoint.x, bounds.min.x, bounds.max.x),
                    Mathf.Clamp(shadowPoint.z, bounds.min.z, bounds.max.z));
            }
            else
            {
                closestObjectPoint = new Vector2(shadowTriggerObject.position.x, shadowTriggerObject.position.z);
            }

            return (shadowXZ - closestObjectPoint).sqrMagnitude <= shadowTriggerRadius * shadowTriggerRadius;
        }

        private Vector3 GetProjectedShadowPoint()
        {
            Vector3 rayDirection = directionalLight != null ? directionalLight.forward : Vector3.down;
            if (Mathf.Abs(rayDirection.y) < 0.001f)
            {
                rayDirection = Vector3.down;
            }

            float groundHeight = orbitCenter.y;
            float distanceToGround = (groundHeight - transform.position.y) / rayDirection.y;
            if (distanceToGround < 0f)
            {
                rayDirection = -rayDirection;
                distanceToGround = (groundHeight - transform.position.y) / rayDirection.y;
            }

            Vector3 projectedPoint = transform.position + rayDirection * distanceToGround;
            projectedPoint.y = groundHeight;
            return projectedPoint;
        }

        private IEnumerator AttackSequence()
        {
            attacking = true;
            float attackStartY = transform.position.y;
            SetFightDustVisible(false);
            onAttackStarted?.Invoke();

            // A short pull-up makes the shadow pause before the dive begins.
            Vector3 windupStart = transform.position;
            Vector3 windupEnd = windupStart + Vector3.up * 0.45f;
            yield return MoveBetween(windupStart, windupEnd, diveWindup, true);

            Vector3 diveStart = transform.position;
            Vector3 lowHoverPoint = AttackGroundPoint + Vector3.up * lowHoverHeight;
            Vector3 horizontalDirection = lowHoverPoint - diveStart;
            horizontalDirection.y = 0f;
            FaceHorizontalDirection(horizontalDirection);

            float diveDistance = Mathf.Max(Vector3.Distance(diveStart, lowHoverPoint), 0.001f);
            float diveProgress = 0f;
            while (diveProgress < 1f)
            {
                diveProgress += AttackDeltaTime * Mathf.Max(diveSpeed, 0.01f) / diveDistance;
                float t = Mathf.Clamp01(diveProgress);
                float eased = t * t * (3f - 2f * t);

                // The shallow forward arc reads as a deliberate attack rather than a fall.
                Vector3 position = Vector3.LerpUnclamped(diveStart, lowHoverPoint, eased);
                position += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.55f;
                transform.position = position;

                yield return null;
            }

            transform.position = lowHoverPoint;
            SetFightDustVisible(true);
            FaceHorizontalDirection(Camera.main != null
                ? Camera.main.transform.position - transform.position
                : Vector3.back);

            if (animator != null)
            {
                animator.SetBool(FlyingHash, true);
                animator.SetFloat(FlyingDirectionHash, 0f);
            }

            // Hold close to the floor with a small wing-driven bob instead of landing.
            float elapsed = 0f;
            while (elapsed < lowHoverDuration)
            {
                elapsed += AttackDeltaTime;
                float hoverPhase = elapsed * Mathf.PI * 2.4f;
                transform.position = lowHoverPoint + Vector3.up * (Mathf.Sin(hoverPhase) * lowHoverBobAmount);
                yield return null;
            }

            transform.position = lowHoverPoint;

            // Return to the same overhead orbit so the shadow visibly pulls away again.
            SetFightDustVisible(false);
            EndAttackTimeStop();
            Vector3 ascentTarget = orbitCenter + OrbitOffset(orbitAngle) + Vector3.up * hoverHeight;
            if (disappearAfterAscent)
            {
                ascentTarget.y = attackStartY + Mathf.Max(disappearHeightAboveStart, 0.01f);
            }

            FaceHorizontalDirection(ascentTarget - transform.position);
            float ascentDistance = Vector3.Distance(transform.position, ascentTarget);
            float ascentDuration = ascentDistance / Mathf.Max(ascentSpeed, 0.01f);
            yield return MoveBetween(transform.position, ascentTarget, ascentDuration, false);

            Vector3 orbitOffset = OrbitOffset(orbitAngle);
            FaceHorizontalDirection(new Vector3(-orbitOffset.z, 0f, orbitOffset.x));

            attacking = false;
            finished = true;
            attackRoutine = null;
            onAttackFinished?.Invoke();

            if (disappearAfterAscent)
            {
                gameObject.SetActive(false);
            }
        }

        private void CreateCrowMasks()
        {
            if (crowRenderers == null)
            {
                return;
            }

            Shader maskShader = Shader.Find(FightDustMaskShaderName);
            if (maskShader == null)
            {
                Debug.LogWarning(
                    $"CrowAttackController: Shader '{FightDustMaskShaderName}' was not found.",
                    this);
                return;
            }

            fightDustMaskMaterial = new Material(maskShader)
            {
                hideFlags = HideFlags.DontSave,
                renderQueue = CrowMaskRenderQueue
            };
            crowMaskRenderers = new Renderer[crowRenderers.Length];
            crowMaskObjects = new GameObject[crowRenderers.Length];

            for (int rendererIndex = 0; rendererIndex < crowRenderers.Length; rendererIndex++)
            {
                Renderer sourceRenderer = crowRenderers[rendererIndex];
                GameObject maskObject = new GameObject($"{sourceRenderer.name} Fight Dust Mask")
                {
                    hideFlags = HideFlags.DontSave
                };
                maskObject.transform.SetParent(sourceRenderer.transform, false);

                Renderer maskRenderer = CreateMaskRenderer(sourceRenderer, maskObject);
                if (maskRenderer == null)
                {
                    Destroy(maskObject);
                    continue;
                }

                int materialCount = Mathf.Max(1, sourceRenderer.sharedMaterials.Length);
                Material[] maskMaterials = new Material[materialCount];
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    maskMaterials[materialIndex] = fightDustMaskMaterial;
                }

                maskRenderer.sharedMaterials = maskMaterials;
                maskRenderer.shadowCastingMode = ShadowCastingMode.Off;
                maskRenderer.receiveShadows = false;
                maskRenderer.enabled = false;
                crowMaskObjects[rendererIndex] = maskObject;
                crowMaskRenderers[rendererIndex] = maskRenderer;
            }
        }

        private static Renderer CreateMaskRenderer(Renderer sourceRenderer, GameObject maskObject)
        {
            if (sourceRenderer is SkinnedMeshRenderer sourceSkinnedRenderer)
            {
                SkinnedMeshRenderer maskRenderer = maskObject.AddComponent<SkinnedMeshRenderer>();
                maskRenderer.sharedMesh = sourceSkinnedRenderer.sharedMesh;
                maskRenderer.bones = sourceSkinnedRenderer.bones;
                maskRenderer.rootBone = sourceSkinnedRenderer.rootBone;
                maskRenderer.localBounds = sourceSkinnedRenderer.localBounds;
                maskRenderer.updateWhenOffscreen = true;
                return maskRenderer;
            }

            if (sourceRenderer is MeshRenderer)
            {
                MeshFilter sourceMeshFilter = sourceRenderer.GetComponent<MeshFilter>();
                if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
                {
                    return null;
                }

                MeshFilter maskMeshFilter = maskObject.AddComponent<MeshFilter>();
                maskMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
                return maskObject.AddComponent<MeshRenderer>();
            }

            return null;
        }

        private void SetCrowMasksVisible(bool visible)
        {
            if (crowMaskRenderers == null)
            {
                return;
            }

            for (int rendererIndex = 0; rendererIndex < crowMaskRenderers.Length; rendererIndex++)
            {
                Renderer maskRenderer = crowMaskRenderers[rendererIndex];
                Renderer sourceRenderer = crowRenderers[rendererIndex];
                if (maskRenderer != null)
                {
                    maskRenderer.enabled = visible
                        && sourceRenderer != null
                        && sourceRenderer.enabled;
                }
            }
        }

        private void DestroyCrowMasks()
        {
            if (crowMaskObjects != null)
            {
                foreach (GameObject maskObject in crowMaskObjects)
                {
                    if (maskObject != null)
                    {
                        Destroy(maskObject);
                    }
                }
            }

            if (fightDustMaskMaterial != null)
            {
                Destroy(fightDustMaskMaterial);
            }
        }

        private void CreateFightDustEffect()
        {
            if (fightDustTexture == null || fightDustObject != null)
            {
                return;
            }

            runtimeFightDustSprite = Sprite.Create(
                fightDustTexture,
                new Rect(0f, 0f, fightDustTexture.width, fightDustTexture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            runtimeFightDustSprite.name = "Crow Fight Dust Sprite";

            fightDustObject = new GameObject("Crow Fight Dust Effect");
            fightDustObject.hideFlags = HideFlags.DontSave;
            fightDustRenderer = fightDustObject.AddComponent<SpriteRenderer>();
            fightDustRenderer.sprite = runtimeFightDustSprite;
            fightDustRenderer.color = new Color(1f, 1f, 1f, fightDustOpacity);
            fightDustRenderer.sortingOrder = fightDustSortingOrder;
            fightDustAudioSource = fightDustObject.AddComponent<AudioSource>();
            fightDustAudioSource.playOnAwake = false;
            fightDustAudioSource.spatialBlend = 0f;

            Shader overlayShader = Shader.Find(FightDustOverlayShaderName);
            if (overlayShader != null)
            {
                fightDustOverlayMaterial = new Material(overlayShader)
                {
                    hideFlags = HideFlags.DontSave,
                    renderQueue = FightDustRenderQueue
                };
                fightDustRenderer.sharedMaterial = fightDustOverlayMaterial;
            }
            else
            {
                Debug.LogWarning(
                    $"CrowAttackController: Shader '{FightDustOverlayShaderName}' was not found.",
                    this);
            }

            float nativeHeight = Mathf.Max(runtimeFightDustSprite.bounds.size.y, 0.001f);
            fightDustBaseScale = Vector3.one * (fightDustWorldSize / nativeHeight);
            fightDustObject.SetActive(false);
        }

        private void SetFightDustVisible(bool visible)
        {
            fightDustVisible = visible && fightDustObject != null;
            SetCrowMasksVisible(fightDustVisible);
            if (fightDustObject == null)
            {
                return;
            }

            if (!fightDustVisible && fightDustAudioSource != null)
            {
                fightDustAudioSource.Stop();
            }

            fightDustObject.SetActive(fightDustVisible);
            if (fightDustVisible)
            {
                UpdateFightDustTransform();
                PlayFightDustSound();
            }
        }

        private void PlayFightDustSound()
        {
            if (fightDustAudioSource == null || fightDustSound == null)
            {
                return;
            }

            fightDustAudioSource.clip = fightDustSound;
            fightDustAudioSource.volume = fightDustSoundVolume;
            fightDustAudioSource.pitch = fightDustSoundPitch;
            fightDustAudioSource.loop = loopFightDustSound;
            fightDustAudioSource.Play();
        }

        private void LateUpdate()
        {
            if (fightDustVisible)
            {
                SetCrowMasksVisible(true);
                UpdateFightDustTransform();
            }
        }

        private void UpdateFightDustTransform()
        {
            if (fightDustObject == null)
            {
                return;
            }

            Transform effectTransform = fightDustObject.transform;
            effectTransform.position = AttackGroundPoint + fightDustWorldOffset;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                effectTransform.rotation = mainCamera.transform.rotation;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * fightDustPulseSpeed)
                * fightDustPulseAmount;
            effectTransform.localScale = fightDustBaseScale * pulse;
        }

        private IEnumerator MoveBetween(Vector3 from, Vector3 to, float duration, bool easeOut)
        {
            if (duration <= 0f)
            {
                transform.position = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += AttackDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (easeOut)
                {
                    t = 1f - (1f - t) * (1f - t);
                }

                transform.position = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            transform.position = to;
        }

        private Vector3 OrbitOffset(float angleDegrees)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
        }

        private void FaceHorizontalDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        private static bool ResetKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(AttackGroundPoint, 0.25f);
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.65f);
            Gizmos.DrawWireSphere(orbitCenter + Vector3.up * hoverHeight, orbitRadius);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(AttackGroundPoint + Vector3.up * lowHoverHeight, 0.2f);
            Gizmos.DrawLine(orbitCenter + Vector3.up * hoverHeight, orbitCenter);

            Gizmos.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            Vector3 shadowPoint = GetProjectedShadowPoint();
            Gizmos.DrawWireSphere(shadowPoint, shadowTriggerRadius);
        }
    }
}
