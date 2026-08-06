using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>
    /// ScoreManagerの取得数をFishHopperのあんこ量（0～1）へ同期します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScoreManager))]
    [RequireComponent(typeof(global::FishHopper))]
    public sealed class FishAnkoProgression : MonoBehaviour
    {
        [SerializeField]
        [Min(1)]
        private int ankoForFull = 6;

        private ScoreManager scoreManager;
        private global::FishHopper fishHopper;

        public int AnkoForFull => ankoForFull;

        private void Awake()
        {
            scoreManager = GetComponent<ScoreManager>();
            fishHopper = GetComponent<global::FishHopper>();
        }

        private void OnEnable()
        {
            scoreManager.AnkoCountChanged += HandleAnkoCountChanged;
            ApplyAnkoAmount(scoreManager.AnkoCount);
        }

        private void OnDisable()
        {
            if (scoreManager != null)
            {
                scoreManager.AnkoCountChanged -= HandleAnkoCountChanged;
            }
        }

        private void OnValidate()
        {
            ankoForFull = Mathf.Max(1, ankoForFull);
        }

        private void HandleAnkoCountChanged(int newCount)
        {
            ApplyAnkoAmount(newCount);
        }

        private void ApplyAnkoAmount(int count)
        {
            if (fishHopper == null)
            {
                return;
            }

            fishHopper.SetAnkoAmount(Mathf.Clamp01((float)count / ankoForFull));
        }
    }
}
