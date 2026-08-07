using UnityEngine;

namespace TaiyakiKun
{
    /// <summary>Responsive result screen for anko, sunburn, time, and their total.</summary>
    [DisallowMultipleComponent]
    public sealed class ResultScreenController : MonoBehaviour
    {
        [Header("Preview Values (used when the scene is played directly)")]
        [SerializeField, Min(0)] private int previewAnkoScore = 1250;
        [SerializeField, Min(0)] private int previewSunburnScore = 820;
        [SerializeField, Min(0)] private int previewTimeScore = 930;

        [Header("Presentation")]
        [SerializeField, Min(0.1f)] private float countUpDuration = 1.5f;
        [SerializeField, Min(1)] private int categoryGaugeMaximum = 1500;

        private static readonly Color BackgroundColor = new Color32(246, 240, 229, 255);
        private static readonly Color InkColor = new Color32(41, 37, 45, 255);
        private static readonly Color MutedColor = new Color32(116, 107, 104, 255);
        private static readonly Color AnkoColor = new Color32(121, 54, 44, 255);
        private static readonly Color SunburnColor = new Color32(232, 133, 55, 255);
        private static readonly Color TimeColor = new Color32(69, 132, 157, 255);
        private static readonly Color TotalColor = new Color32(43, 48, 61, 255);

        private Texture2D whiteTexture;
        private Font uiFont;
        private GUIStyle titleStyle;
        private GUIStyle eyebrowStyle;
        private GUIStyle cardLabelStyle;
        private GUIStyle scoreStyle;
        private GUIStyle unitStyle;
        private GUIStyle totalLabelStyle;
        private GUIStyle totalScoreStyle;
        private GUIStyle footerStyle;

        private int ankoScore;
        private int sunburnScore;
        private int timeScore;
        private float shownAt;

        public int TotalScore => ankoScore + sunburnScore + timeScore;

        private void Awake()
        {
            if (ResultScoreData.HasResult)
            {
                SetScores(ResultScoreData.AnkoScore, ResultScoreData.SunburnScore, ResultScoreData.TimeScore);
            }
            else
            {
                SetScores(previewAnkoScore, previewSunburnScore, previewTimeScore);
            }

            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Result Screen Pixel",
                hideFlags = HideFlags.HideAndDontSave
            };
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void OnDestroy()
        {
            if (whiteTexture != null)
            {
                Destroy(whiteTexture);
            }
        }

        /// <summary>Updates all result values and restarts the count-up animation.</summary>
        public void SetScores(int newAnkoScore, int newSunburnScore, int newTimeScore)
        {
            ankoScore = Mathf.Max(0, newAnkoScore);
            sunburnScore = Mathf.Max(0, newSunburnScore);
            timeScore = Mathf.Max(0, newTimeScore);
            shownAt = Time.unscaledTime;
        }

        private void OnGUI()
        {
            if (whiteTexture == null)
            {
                return;
            }

            BuildStyles();

            float width = Screen.width;
            float height = Screen.height;
            float scale = Mathf.Clamp(Mathf.Min(width / 1440f, height / 900f), 0.4f, 1.45f);
            float contentWidth = Mathf.Min(width - 64f * scale, 1180f * scale);
            float left = (width - contentWidth) * 0.5f;
            float elapsed = Mathf.Max(0f, Time.unscaledTime - shownAt);
            float countProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / countUpDuration));

            DrawRect(new Rect(0f, 0f, width, height), BackgroundColor);
            DrawDecorations(width, height, scale);

            Rect eyebrowRect = new Rect(left, 70f * scale, contentWidth, 28f * scale);
            GUI.Label(eyebrowRect, "TAIYAKI RESULT", eyebrowStyle);

            Rect titleRect = new Rect(left, 100f * scale, contentWidth, 82f * scale);
            GUI.Label(titleRect, "今回のスコア", titleStyle);

            float cardGap = 22f * scale;
            float cardWidth = (contentWidth - cardGap * 2f) / 3f;
            float cardTop = 222f * scale;
            float cardHeight = 286f * scale;

            DrawScoreCard(
                new Rect(left, cardTop, cardWidth, cardHeight),
                "あんこの量",
                Mathf.RoundToInt(ankoScore * countProgress),
                ankoScore,
                AnkoColor,
                scale,
                Mathf.Clamp01(elapsed / 0.35f));

            DrawScoreCard(
                new Rect(left + cardWidth + cardGap, cardTop, cardWidth, cardHeight),
                "日焼け度合",
                Mathf.RoundToInt(sunburnScore * countProgress),
                sunburnScore,
                SunburnColor,
                scale,
                Mathf.Clamp01((elapsed - 0.12f) / 0.35f));

            DrawScoreCard(
                new Rect(left + (cardWidth + cardGap) * 2f, cardTop, cardWidth, cardHeight),
                "時間",
                Mathf.RoundToInt(timeScore * countProgress),
                timeScore,
                TimeColor,
                scale,
                Mathf.Clamp01((elapsed - 0.24f) / 0.35f));

            float totalTop = cardTop + cardHeight + 34f * scale;
            float totalHeight = 184f * scale;
            DrawTotalPanel(
                new Rect(left, totalTop, contentWidth, totalHeight),
                Mathf.RoundToInt(TotalScore * countProgress),
                scale,
                Mathf.Clamp01((elapsed - 0.4f) / 0.45f));

            GUI.Label(
                new Rect(left, totalTop + totalHeight + 20f * scale, contentWidth, 36f * scale),
                "3つの評価を合計した最終スコアです",
                footerStyle);
        }

        private void DrawScoreCard(
            Rect rect,
            string label,
            int displayedScore,
            int finalScore,
            Color accent,
            float scale,
            float reveal)
        {
            reveal = Mathf.SmoothStep(0f, 1f, reveal);
            float slide = (1f - reveal) * 18f * scale;
            rect.y += slide;

            DrawRect(new Rect(rect.x + 7f * scale, rect.y + 10f * scale, rect.width, rect.height),
                new Color(0f, 0f, 0f, 0.09f * reveal));
            DrawRect(rect, new Color(1f, 1f, 1f, reveal));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 10f * scale), WithAlpha(accent, reveal));

            GUI.color = new Color(1f, 1f, 1f, reveal);
            GUI.Label(new Rect(rect.x + 28f * scale, rect.y + 36f * scale, rect.width - 56f * scale, 42f * scale),
                label, cardLabelStyle);
            GUI.Label(new Rect(rect.x + 24f * scale, rect.y + 92f * scale, rect.width - 48f * scale, 82f * scale),
                displayedScore.ToString("N0"), scoreStyle);
            GUI.Label(new Rect(rect.x + 24f * scale, rect.y + 166f * scale, rect.width - 48f * scale, 28f * scale),
                "SCORE", unitStyle);

            Rect gaugeBack = new Rect(rect.x + 30f * scale, rect.y + rect.height - 54f * scale,
                rect.width - 60f * scale, 9f * scale);
            DrawRect(gaugeBack, new Color(0.88f, 0.86f, 0.83f, reveal));
            float gaugeRatio = Mathf.Clamp01((float)finalScore / categoryGaugeMaximum);
            DrawRect(new Rect(gaugeBack.x, gaugeBack.y, gaugeBack.width * gaugeRatio, gaugeBack.height),
                WithAlpha(accent, reveal));
            GUI.color = Color.white;
        }

        private void DrawTotalPanel(Rect rect, int displayedTotal, float scale, float reveal)
        {
            reveal = Mathf.SmoothStep(0f, 1f, reveal);
            DrawRect(new Rect(rect.x + 8f * scale, rect.y + 11f * scale, rect.width, rect.height),
                new Color(0f, 0f, 0f, 0.13f * reveal));
            DrawRect(rect, WithAlpha(TotalColor, reveal));
            DrawRect(new Rect(rect.x, rect.y, 12f * scale, rect.height),
                WithAlpha(SunburnColor, reveal));

            GUI.color = new Color(1f, 1f, 1f, reveal);
            GUI.Label(new Rect(rect.x + 52f * scale, rect.y + 36f * scale, rect.width * 0.38f, 48f * scale),
                "合計スコア", totalLabelStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.43f, rect.y + 30f * scale,
                    rect.width * 0.5f, 94f * scale),
                displayedTotal.ToString("N0"), totalScoreStyle);
            GUI.Label(new Rect(rect.x + 54f * scale, rect.y + 96f * scale, rect.width * 0.38f, 30f * scale),
                "TOTAL SCORE", unitStyle);
            GUI.color = Color.white;
        }

        private void DrawDecorations(float width, float height, float scale)
        {
            DrawRect(new Rect(0f, 0f, width, 14f * scale), AnkoColor);
            DrawRect(new Rect(width - 290f * scale, 92f * scale, 220f * scale, 4f * scale), SunburnColor);
            DrawRect(new Rect(70f * scale, height - 82f * scale, 150f * scale, 4f * scale), TimeColor);
        }

        private void BuildStyles()
        {
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 900f), 0.4f, 1.45f);
            titleStyle = MakeStyle(Mathf.RoundToInt(58f * scale), FontStyle.Bold, InkColor, TextAnchor.MiddleLeft);
            eyebrowStyle = MakeStyle(Mathf.RoundToInt(18f * scale), FontStyle.Bold, AnkoColor, TextAnchor.MiddleLeft);
            cardLabelStyle = MakeStyle(Mathf.RoundToInt(26f * scale), FontStyle.Bold, InkColor, TextAnchor.MiddleCenter);
            scoreStyle = MakeStyle(Mathf.RoundToInt(52f * scale), FontStyle.Bold, InkColor, TextAnchor.MiddleCenter);
            unitStyle = MakeStyle(Mathf.RoundToInt(15f * scale), FontStyle.Bold, MutedColor, TextAnchor.MiddleCenter);
            totalLabelStyle = MakeStyle(Mathf.RoundToInt(32f * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            totalScoreStyle = MakeStyle(Mathf.RoundToInt(66f * scale), FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
            footerStyle = MakeStyle(Mathf.RoundToInt(17f * scale), FontStyle.Normal, MutedColor, TextAnchor.MiddleCenter);
        }

        private GUIStyle MakeStyle(int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = uiFont,
                fontSize = fontSize,
                fontStyle = fontStyle,
                normal = { textColor = color },
                alignment = alignment,
                clipping = TextClipping.Clip
            };
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }
    }
}
