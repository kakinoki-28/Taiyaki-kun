using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
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

        [Header("Attack Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 fallbackGroundPoint = new Vector3(0f, 0.25f, 2f);

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

        [Header("Attack")]
        [SerializeField, Min(0.1f)] private float diveDuration = 1.15f;
        [SerializeField, Min(0f)] private float diveWindup = 0.25f;
        [SerializeField, Min(0.1f)] private float lowHoverHeight = 1.1f;
        [SerializeField, Min(0f)] private float lowHoverDuration = 1.8f;
        [SerializeField, Min(0f)] private float lowHoverBobAmount = 0.12f;
        [SerializeField, Min(0.1f)] private float ascentDuration = 1.35f;
        [SerializeField] private AudioClip attackCall;
        [SerializeField, Range(0f, 1f)] private float attackCallVolume = 0.7f;

        [Header("Demo Scene")]
        [SerializeField] private bool autoTriggerOnStart = true;
        [SerializeField, Min(0f)] private float autoTriggerDelay = 4f;
        [SerializeField] private bool showDemoInstructions = true;

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

        public bool IsAttacking => attacking;
        public bool IsFinished => finished;

        private Vector3 GroundPoint => target != null ? target.position : fallbackGroundPoint;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody>();

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

            foreach (Renderer birdRenderer in GetComponentsInChildren<Renderer>(true))
            {
                birdRenderer.shadowCastingMode = ShadowCastingMode.On;
                birdRenderer.receiveShadows = true;
            }

            CacheShadowTriggerCollider();
            EnterWarningState();
        }

        private void Start()
        {
            autoTriggerAt = Time.time + autoTriggerDelay;
            // Start unlatched so an object already overlapping the shadow can still fire once.
            triggerObjectWasInShadow = false;
        }

        private void Update()
        {
            if (!attacking && !finished)
            {
                UpdateWarningOrbit();
                UpdateShadowTrigger();

                if ((autoTriggerOnStart && Time.time >= autoTriggerAt) || AttackKeyPressed())
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
            if (attacking || finished)
            {
                return;
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

            attacking = false;
            finished = false;
            autoTriggerOnStart = false;
            EnterWarningState();
            triggerObjectWasInShadow = false;
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
            transform.position = GroundPoint + offset + Vector3.up * hoverHeight;
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
            transform.position = GroundPoint + offset + Vector3.up * hoverHeight;
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
                onShadowEntered?.Invoke();
                if (attackOnShadowEnter)
                {
                    TriggerAttack();
                }
            }

            triggerObjectWasInShadow = isInShadow;
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

            float groundHeight = GroundPoint.y;
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
            onAttackStarted?.Invoke();

            if (attackCall != null)
            {
                AudioSource.PlayClipAtPoint(attackCall, GroundPoint + Vector3.up * 2f, attackCallVolume);
            }

            // A short pull-up makes the shadow pause before the dive begins.
            Vector3 windupStart = transform.position;
            Vector3 windupEnd = windupStart + Vector3.up * 0.45f;
            yield return MoveBetween(windupStart, windupEnd, diveWindup, true);

            Vector3 diveStart = transform.position;
            Vector3 lowHoverPoint = GroundPoint + Vector3.up * lowHoverHeight;
            Vector3 horizontalDirection = lowHoverPoint - diveStart;
            horizontalDirection.y = 0f;
            FaceHorizontalDirection(horizontalDirection);

            float elapsed = 0f;
            while (elapsed < diveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / diveDuration);
                float eased = t * t * (3f - 2f * t);

                // The shallow forward arc reads as a deliberate attack rather than a fall.
                Vector3 position = Vector3.LerpUnclamped(diveStart, lowHoverPoint, eased);
                position += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.55f;
                transform.position = position;

                yield return null;
            }

            transform.position = lowHoverPoint;
            FaceHorizontalDirection(Camera.main != null
                ? Camera.main.transform.position - transform.position
                : Vector3.back);

            if (animator != null)
            {
                animator.SetBool(FlyingHash, true);
                animator.SetFloat(FlyingDirectionHash, 0f);
            }

            // Hold close to the floor with a small wing-driven bob instead of landing.
            elapsed = 0f;
            while (elapsed < lowHoverDuration)
            {
                elapsed += Time.deltaTime;
                float hoverPhase = elapsed * Mathf.PI * 2.4f;
                transform.position = lowHoverPoint + Vector3.up * (Mathf.Sin(hoverPhase) * lowHoverBobAmount);
                yield return null;
            }

            transform.position = lowHoverPoint;

            // Return to the same overhead orbit so the shadow visibly pulls away again.
            Vector3 ascentTarget = GroundPoint + OrbitOffset(orbitAngle) + Vector3.up * hoverHeight;
            FaceHorizontalDirection(ascentTarget - transform.position);
            yield return MoveBetween(transform.position, ascentTarget, ascentDuration, false);

            Vector3 orbitOffset = OrbitOffset(orbitAngle);
            FaceHorizontalDirection(new Vector3(-orbitOffset.z, 0f, orbitOffset.x));

            attacking = false;
            finished = true;
            attackRoutine = null;
            onAttackFinished?.Invoke();
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
                elapsed += Time.deltaTime;
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

        private static bool AttackKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
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

        private void OnGUI()
        {
            if (!showDemoInstructions)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(16, Screen.height / 36),
                normal = { textColor = Color.white }
            };

            string message;
            if (!attacking && !finished)
            {
                float remaining = Mathf.Max(0f, autoTriggerAt - Time.time);
                if (shadowTriggerObject != null)
                {
                    message = "Move the assigned object into the crow shadow  |  SPACE: trigger manually";
                }
                else if (autoTriggerOnStart)
                {
                    message =
                        $"Crow shadow overhead  |  SPACE: attack now  |  auto attack in {remaining:0.0}s";
                }
                else
                {
                    message = "Assign Shadow Trigger Object  |  SPACE: trigger manually";
                }
            }
            else if (attacking)
            {
                message = "Crow attack!";
            }
            else
            {
                message = "Crow returned overhead  |  R: reset";
            }

            GUI.Box(new Rect(Screen.width * 0.15f, 18f, Screen.width * 0.7f, 46f), message, style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.9f);
            Gizmos.DrawWireSphere(GroundPoint, 0.25f);
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.65f);
            Gizmos.DrawWireSphere(GroundPoint + Vector3.up * hoverHeight, orbitRadius);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(GroundPoint + Vector3.up * lowHoverHeight, 0.2f);
            Gizmos.DrawLine(GroundPoint + Vector3.up * hoverHeight, GroundPoint);

            Gizmos.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            Vector3 shadowPoint = GetProjectedShadowPoint();
            Gizmos.DrawWireSphere(shadowPoint, shadowTriggerRadius);
        }
    }
}
