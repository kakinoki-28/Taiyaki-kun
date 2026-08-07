using System.Collections;
using TMPro;
using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>
    /// Counts the three result values up from zero and writes them to the
    /// TextMeshPro labels that are placed in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultScoreCounter : MonoBehaviour
    {
        [Header("Value Labels")]
        [SerializeField] private TMP_Text ankoValue;
        [SerializeField] private TMP_Text sunburnValue;
        [SerializeField] private TMP_Text timeValue;
        [SerializeField] private TMP_Text totalScoreValue;
        [SerializeField] private GameObject returnToTitleButton;

        [Header("Data Source")]
        [Tooltip("When enabled, the preview values below are used instead of gameplay results.")]
        [SerializeField] private bool useSampleValues = true;
        [SerializeField, Min(0)] private int sampleAnkoGrams = 120;
        [SerializeField, Range(0, 100)] private int sampleSunburnPercent = 72;
        [SerializeField, Min(0)] private int sampleTimeSeconds = 154;

        [Header("Animation")]
        [Tooltip("Seconds to wait after the result scene appears.")]
        [SerializeField, Min(0f)] private float startDelay = 2f;
        [Tooltip("Count-up duration for each score. Scores play from left to right.")]
        [SerializeField, Min(0.01f)] private float countDuration = 1.5f;
        [Tooltip("Seconds to wait after the last category before revealing the total score.")]
        [SerializeField, Min(0f)] private float totalScoreDelay = 0.6f;
        [Tooltip("Seconds to wait after the total score appears before showing the title button.")]
        [SerializeField, Min(0f)] private float returnButtonDelay = 0.5f;

        private Coroutine countRoutine;

        private void Start()
        {
            RestartCountUp();
        }

        public void RestartCountUp()
        {
            if (ankoValue == null || sunburnValue == null || timeValue == null || totalScoreValue == null)
            {
                Debug.LogError("ResultScoreCounter: Assign all score labels in the Inspector.", this);
                return;
            }

            if (countRoutine != null)
            {
                StopCoroutine(countRoutine);
            }

            int targetAnko = sampleAnkoGrams;
            int targetSunburn = sampleSunburnPercent;
            int targetTime = sampleTimeSeconds;

            if (!useSampleValues && ResultScoreData.HasResult)
            {
                targetAnko = ResultScoreData.AnkoGrams;
                targetSunburn = Mathf.RoundToInt(ResultScoreData.SunburnPercent);
                targetTime = Mathf.RoundToInt(ResultScoreData.ElapsedSeconds);
            }

            totalScoreValue.gameObject.SetActive(false);
            if (returnToTitleButton != null)
            {
                returnToTitleButton.SetActive(false);
            }

            countRoutine = StartCoroutine(CountUp(targetAnko, targetSunburn, targetTime));
        }

        private IEnumerator CountUp(int targetAnko, int targetSunburn, int targetTime)
        {
            targetAnko = Mathf.Max(0, targetAnko);
            targetSunburn = Mathf.Clamp(targetSunburn, 0, 100);
            targetTime = Mathf.Max(0, targetTime);

            SetTexts(0, 0, 0);

            if (startDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }

            // Play in the same order as the panels: Anko, Sunburn, then Time.
            yield return CountText(ankoValue, targetAnko, " g");
            yield return CountText(sunburnValue, targetSunburn, " %");
            yield return CountText(timeValue, targetTime, " s");

            if (totalScoreDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(totalScoreDelay);
            }

            int totalScore = ResultScoreData.CalculateTotalScore(targetAnko, targetSunburn, targetTime);
            totalScoreValue.text = $"スコア {totalScore:N0} pt";
            totalScoreValue.gameObject.SetActive(true);

            if (returnToTitleButton != null)
            {
                if (returnButtonDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(returnButtonDelay);
                }

                returnToTitleButton.SetActive(true);
            }

            countRoutine = null;
        }

        private IEnumerator CountText(TMP_Text label, int targetValue, string unit)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, countDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                label.text = $"{Mathf.RoundToInt(targetValue * progress)}{unit}";
                yield return null;
            }

            label.text = $"{targetValue}{unit}";
        }

        private void SetTexts(int anko, int sunburn, int seconds)
        {
            ankoValue.text = $"{anko} g";
            sunburnValue.text = $"{sunburn} %";
            timeValue.text = $"{seconds} s";
        }
    }
}
