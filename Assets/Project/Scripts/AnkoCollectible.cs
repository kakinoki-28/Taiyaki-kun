using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>
    /// Adds anko to a colliding ScoreManager and then removes this collectible.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class AnkoCollectible : MonoBehaviour
    {
        [SerializeField]
        [Min(1)]
        private int collectAmount = 1;

        private bool isCollected;

        public int CollectAmount => collectAmount;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnValidate()
        {
            collectAmount = Mathf.Max(1, collectAmount);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected)
            {
                return;
            }

            ScoreManager scoreManager = other.GetComponentInParent<ScoreManager>();
            if (scoreManager == null && other.attachedRigidbody != null)
            {
                scoreManager = other.attachedRigidbody.GetComponentInParent<ScoreManager>();
            }

            if (scoreManager == null)
            {
                return;
            }

            isCollected = true;
            scoreManager.AddAnko(collectAmount);

            // Hide immediately so that multiple physics callbacks cannot collect it twice.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
