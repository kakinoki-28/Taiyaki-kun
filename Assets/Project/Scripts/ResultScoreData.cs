using UnityEngine;

namespace TaiyakiKun
{
    public static class ResultScoreData
    {
        public static int AnkoGrams { get; private set; }
        public static float SunburnPercent { get; private set; }
        public static float ElapsedSeconds { get; private set; }
        public static bool HasResult { get; private set; }

        public static int TotalScore => CalculateTotalScore(AnkoGrams, SunburnPercent, ElapsedSeconds);

        public static void SetResults(int ankoGrams, float sunburnPercent, float elapsedSeconds)
        {
            AnkoGrams = Mathf.Max(0, ankoGrams);
            SunburnPercent = Mathf.Clamp(sunburnPercent, 0f, 100f);
            ElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            HasResult = true;
        }

        public static void SetScores(int ankoGrams, int sunburnPercent, int elapsedSeconds)
        {
            SetResults(ankoGrams, sunburnPercent, elapsedSeconds);
        }

        public static int CalculateTotalScore(int ankoGrams, float sunburnPercent, float elapsedSeconds)
        {
            int ankoPoints = Mathf.Max(0, ankoGrams) * 10;
            int sunburnPoints = Mathf.RoundToInt(Mathf.Clamp(sunburnPercent, 0f, 100f) * 15f);
            int timePoints = Mathf.Max(0, 1800 - Mathf.RoundToInt(Mathf.Max(0f, elapsedSeconds) * 4f));
            return ankoPoints + sunburnPoints + timePoints;
        }

        public static void Clear()
        {
            AnkoGrams = 0;
            SunburnPercent = 0f;
            ElapsedSeconds = 0f;
            HasResult = false;
        }
    }
}
