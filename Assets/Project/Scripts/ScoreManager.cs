using System;
using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>
    /// Stores the number of collected anko items for the GameObject that owns it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScoreManager : MonoBehaviour
    {
        [SerializeField]
        [Min(0)]
        private int ankoCount;

        public int AnkoCount => ankoCount;

        public event Action<int> AnkoCountChanged;

        public void AddAnko(int amount = 1)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("Anko amount must be greater than zero.", this);
                return;
            }

            ankoCount += amount;
            AnkoCountChanged?.Invoke(ankoCount);
            Debug.Log($"[ScoreManager] Anko count: {ankoCount}", this);
        }

        [ContextMenu("Reset Anko Count")]
        public void ResetAnkoCount()
        {
            ankoCount = 0;
            AnkoCountChanged?.Invoke(ankoCount);
        }
    }
}
