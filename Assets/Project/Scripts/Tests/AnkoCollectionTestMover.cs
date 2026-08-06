using UnityEngine;

namespace TaiyakiKun.Tests
{
    /// <summary>
    /// Moves the test cube through the collectibles and reports when the test succeeds.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ScoreManager))]
    public sealed class AnkoCollectionTestMover : MonoBehaviour
    {
        [SerializeField]
        private Vector3 moveDirection = Vector3.right;

        [SerializeField]
        [Min(0f)]
        private float moveSpeed = 2f;

        [SerializeField]
        [Min(1)]
        private int expectedAnkoCount = 2;

        private Rigidbody body;
        private ScoreManager scoreManager;
        private bool successReported;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            scoreManager = GetComponent<ScoreManager>();
        }

        private void FixedUpdate()
        {
            if (scoreManager.AnkoCount >= expectedAnkoCount)
            {
                if (!successReported)
                {
                    successReported = true;
                    Debug.Log(
                        $"[AnkoCollectionTest] Success: collected {scoreManager.AnkoCount} anko items.",
                        this);
                }

                return;
            }

            if (moveDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 movement = moveDirection.normalized * (moveSpeed * Time.fixedDeltaTime);
            body.MovePosition(body.position + movement);
        }
    }
}
