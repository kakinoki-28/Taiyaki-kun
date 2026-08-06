using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>
    /// Keeps a world-space object upright and parallel to the active camera's view plane.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CameraFacingBillboard : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Optional camera override. When empty, the camera tagged MainCamera is used.")]
        private Camera targetCamera;

        [SerializeField]
        [Tooltip("Keeps the billboard vertical by rotating it only around the Y axis.")]
        private bool lockYAxis = true;

        public Camera TargetCamera
        {
            get => targetCamera;
            set => targetCamera = value;
        }

        public bool LockYAxis
        {
            get => lockYAxis;
            set => lockYAxis = value;
        }

        private void LateUpdate()
        {
            FaceCamera();
        }

        public void FaceCamera()
        {
            Camera cameraToFace = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToFace == null)
            {
                return;
            }

            // Unity sprites show their front face toward local -Z. Matching local +Z
            // to the camera's forward direction keeps every billboard parallel to the
            // camera's view plane instead of making each one converge on its position.
            Vector3 facingDirection = cameraToFace.transform.forward;
            if (lockYAxis)
            {
                facingDirection = Vector3.ProjectOnPlane(facingDirection, Vector3.up);

                // A straight-down camera has no meaningful horizontal facing direction.
                // Fall back to its projected position so the sprite remains visible.
                if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    facingDirection = transform.position - cameraToFace.transform.position;
                    facingDirection.y = 0f;
                }
            }

            if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(facingDirection, Vector3.up);
        }
    }
}
