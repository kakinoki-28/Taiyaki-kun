using UnityEngine;

namespace TaiyakiKun.Tests
{
    /// <summary>
    /// Switches between the supplied taiyaki poses to match each phase of a hop.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TaiyakiHopPoseController : MonoBehaviour
    {
        [SerializeField]
        private global::FishHopper fishHopper;

        [SerializeField]
        private GameObject groundedVisual;

        [SerializeField]
        private GameObject risingVisual;

        [SerializeField]
        private GameObject apexVisual;

        [SerializeField]
        private GameObject fallingVisual;

        [SerializeField]
        [Min(0f)]
        private float apexVelocityThreshold = 0.8f;

        private Rigidbody body;
        private GameObject activeVisual;

        public void Configure(
            global::FishHopper hopper,
            GameObject grounded,
            GameObject rising,
            GameObject apex,
            GameObject falling)
        {
            fishHopper = hopper;
            groundedVisual = grounded;
            risingVisual = rising;
            apexVisual = apex;
            fallingVisual = falling;
            ApplyVisual(groundedVisual);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            fishHopper = fishHopper != null ? fishHopper : GetComponent<global::FishHopper>();
        }

        private void OnEnable()
        {
            ApplyVisual(groundedVisual);
        }

        private void LateUpdate()
        {
            float verticalSpeed = body.linearVelocity.y;
            GameObject nextVisual;

            if (fishHopper != null
                && fishHopper.IsGrounded
                && Mathf.Abs(verticalSpeed) <= apexVelocityThreshold)
            {
                nextVisual = groundedVisual;
            }
            else if (verticalSpeed > apexVelocityThreshold)
            {
                nextVisual = risingVisual;
            }
            else if (verticalSpeed < -apexVelocityThreshold)
            {
                nextVisual = fallingVisual;
            }
            else
            {
                nextVisual = apexVisual;
            }

            ApplyVisual(nextVisual);
        }

        private void ApplyVisual(GameObject nextVisual)
        {
            if (nextVisual == null || activeVisual == nextVisual)
            {
                return;
            }

            SetVisualActive(groundedVisual, groundedVisual == nextVisual);
            SetVisualActive(risingVisual, risingVisual == nextVisual);
            SetVisualActive(apexVisual, apexVisual == nextVisual);
            SetVisualActive(fallingVisual, fallingVisual == nextVisual);
            activeVisual = nextVisual;
        }

        private static void SetVisualActive(GameObject visual, bool active)
        {
            if (visual != null && visual.activeSelf != active)
            {
                visual.SetActive(active);
            }
        }
    }
}
