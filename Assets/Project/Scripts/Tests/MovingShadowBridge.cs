using UnityEngine;

namespace TaiyakiKun.Tests
{
    /// <summary>
    /// Moves an overhead shadow caster back and forth between two safe areas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MovingShadowBridge : MonoBehaviour
    {
        [SerializeField]
        private bool moveBetweenPoints;

        [SerializeField]
        private Vector3 pointA = new Vector3(-2f, 14.5f, -8f);

        [SerializeField]
        private Vector3 pointB = new Vector3(-2f, 14.5f, 12f);

        [SerializeField]
        [Min(0f)]
        private float travelSpeed = 3.5f;

        [SerializeField]
        private bool smoothMovement = true;

        private float routeLength;
        private float travelledDistance;

        private void OnEnable()
        {
            if (!moveBetweenPoints)
            {
                routeLength = 0f;
                return;
            }

            routeLength = Vector3.Distance(pointA, pointB);
            travelledDistance = 0f;
            transform.position = pointA;
        }

        private void Update()
        {
            if (!moveBetweenPoints
                || routeLength <= Mathf.Epsilon
                || travelSpeed <= 0f)
            {
                return;
            }

            travelledDistance += travelSpeed * Time.deltaTime;
            float progress = Mathf.PingPong(travelledDistance / routeLength, 1f);
            if (smoothMovement)
            {
                progress = Mathf.SmoothStep(0f, 1f, progress);
            }

            transform.position = Vector3.Lerp(pointA, pointB, progress);
        }

        private void OnValidate()
        {
            travelSpeed = Mathf.Max(0f, travelSpeed);
            routeLength = Vector3.Distance(pointA, pointB);
        }
    }
}
