using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>
    /// Collects sunscreen and exposes a clean hook for its future UV-protection effect.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class HiyakedomeCollectible : MonoBehaviour
    {
        [SerializeField]
        private string feedbackMessage = "UV CUT!!";

        [SerializeField]
        private Color feedbackColor = new Color(0.2f, 0.9f, 1f);

        private bool isCollected;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
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

            // ScoreManager marks a collider as the player in the current game setup.
            if (scoreManager == null)
            {
                return;
            }

            CollectionFeedbackRelay feedbackRelay = scoreManager.GetComponent<CollectionFeedbackRelay>();
            if (feedbackRelay == null)
            {
                feedbackRelay = scoreManager.gameObject.AddComponent<CollectionFeedbackRelay>();
            }

            isCollected = true;
            feedbackRelay.RequestFeedback(feedbackMessage, feedbackColor);

            // Hide immediately so that multiple physics callbacks cannot collect it twice.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
