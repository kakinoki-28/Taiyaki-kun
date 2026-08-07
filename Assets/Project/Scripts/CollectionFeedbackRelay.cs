using System;
using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>
    /// Relays collectible feedback from the player to whichever UI is presenting it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CollectionFeedbackRelay : MonoBehaviour
    {
        public event Action<string, Color> FeedbackRequested;

        public void RequestFeedback(string message, Color color)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            FeedbackRequested?.Invoke(message, color);
        }
    }
}
