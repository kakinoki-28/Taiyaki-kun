using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>
    /// Carries the three final scores across scene loads.
    /// Call SetScores before loading the Result scene.
    /// </summary>
    public static class ResultScoreData
    {
        public static int AnkoScore { get; private set; }
        public static int SunburnScore { get; private set; }
        public static int TimeScore { get; private set; }
        public static bool HasResult { get; private set; }

        public static int TotalScore => AnkoScore + SunburnScore + TimeScore;

        public static void SetScores(int ankoScore, int sunburnScore, int timeScore)
        {
            AnkoScore = Mathf.Max(0, ankoScore);
            SunburnScore = Mathf.Max(0, sunburnScore);
            TimeScore = Mathf.Max(0, timeScore);
            HasResult = true;
        }

        public static void Clear()
        {
            AnkoScore = 0;
            SunburnScore = 0;
            TimeScore = 0;
            HasResult = false;
        }
    }
}
