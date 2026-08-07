using UnityEngine;
using UnityEngine.Rendering;

namespace TaiyakiKun
{
    [DisallowMultipleComponent]
    public sealed class ResultScreenController : MonoBehaviour
    {
        [Header("Preview Values")]
        [SerializeField, Min(0)] private int previewAnkoGrams = 120;
        [SerializeField, Range(0f, 100f)] private float previewSunburnPercent = 72f;
        [SerializeField, Min(0f)] private float previewElapsedSeconds = 154f;
        [SerializeField, Min(0.1f)] private float countUpDuration = 1.5f;

        [Header("Sea Background")]
        [SerializeField] private Texture2D oceanBackgroundTexture;
        [SerializeField] private GameObject backgroundTaiyakiPrefab;

        private static readonly Color Ink = new Color32(27, 54, 67, 255);
        private static readonly Color Muted = new Color32(75, 103, 113, 255);
        private static readonly Color Anko = new Color32(130, 66, 52, 255);
        private static readonly Color Sunburn = new Color32(235, 139, 54, 255);
        private static readonly Color TimeColor = new Color32(39, 143, 177, 255);
        private static readonly Color Total = new Color32(15, 59, 78, 240);

        private Texture2D pixel;
        private Texture2D roundMask;
        private GUIStyle roundStyle;
        private Font font;
        private int ankoGrams;
        private float sunburnPercent;
        private float elapsedSeconds;
        private float shownAt;

        public int TotalScore => ResultScoreData.CalculateTotalScore(ankoGrams, sunburnPercent, elapsedSeconds);

        private void Awake()
        {
            if (ResultScoreData.HasResult)
            {
                SetResults(ResultScoreData.AnkoGrams, ResultScoreData.SunburnPercent, ResultScoreData.ElapsedSeconds);
            }
            else
            {
                SetResults(previewAnkoGrams, previewSunburnPercent, previewElapsedSeconds);
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pixel = MakePixel();
            roundMask = MakeRoundMask(64, 14f);
            roundStyle = new GUIStyle
            {
                normal = { background = roundMask },
                border = new RectOffset(18, 18, 18, 18)
            };
            BuildSeaScene();
        }

        private void OnDestroy()
        {
            if (pixel != null) Destroy(pixel);
            if (roundMask != null) Destroy(roundMask);
        }

        public void SetResults(int grams, float percent, float seconds)
        {
            ankoGrams = Mathf.Max(0, grams);
            sunburnPercent = Mathf.Clamp(percent, 0f, 100f);
            elapsedSeconds = Mathf.Max(0f, seconds);
            shownAt = Time.unscaledTime;
        }

        public void SetScores(int grams, int percent, int seconds) => SetResults(grams, percent, seconds);

        private void BuildSeaScene()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.2f, 0.29f, 1f);
                CreateOceanQuad(camera);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.025f;
            RenderSettings.fogColor = new Color(0.04f, 0.3f, 0.38f, 1f);

            GameObject sunObject = new GameObject("Underwater Sunlight");
            Light sunlight = sunObject.AddComponent<Light>();
            sunlight.type = LightType.Directional;
            sunlight.color = new Color(0.72f, 0.96f, 0.93f, 1f);
            sunlight.intensity = 1.4f;
            sunlight.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

            if (backgroundTaiyakiPrefab == null) return;
            Transform school = new GameObject("Static Taiyaki School").transform;
            AddTaiyaki(school, "Taiyaki Left", new Vector3(-4.2f, 1.3f, 2f), new Vector3(-5f, 102f, -7f), 8.5f);
            AddTaiyaki(school, "Taiyaki Right", new Vector3(3.8f, -1.4f, 3.2f), new Vector3(8f, -94f, 5f), 7f);
            AddTaiyaki(school, "Taiyaki Far Right", new Vector3(5.1f, 2.7f, 7f), new Vector3(0f, -102f, -10f), 4.6f);
            AddTaiyaki(school, "Taiyaki Far Left", new Vector3(-5.4f, -2.8f, 8.5f), new Vector3(10f, 88f, 8f), 3.8f);
        }

        private void CreateOceanQuad(Camera camera)
        {
            if (oceanBackgroundTexture == null) return;
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Ocean Background";
            quad.transform.position = camera.transform.position + camera.transform.forward * 25f;
            quad.transform.rotation = camera.transform.rotation;
            Collider collider = quad.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            Material material = new Material(shader) { mainTexture = oceanBackgroundTexture };
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            float height = 2f * 25f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            quad.transform.localScale = new Vector3(height * camera.aspect * 1.02f, height * 1.02f, 1f);
        }

        private void AddTaiyaki(Transform parent, string name, Vector3 position, Vector3 angles, float scale)
        {
            GameObject fish = Instantiate(backgroundTaiyakiPrefab, position, Quaternion.Euler(angles), parent);
            fish.name = name;
            fish.transform.localScale = Vector3.one * scale;
            Animator animator = fish.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
        }

        private void OnGUI()
        {
            if (pixel == null || roundStyle == null) return;
            float w = Screen.width;
            float h = Screen.height;
            float s = Mathf.Clamp(Mathf.Min(w / 1440f, h / 900f), 0.4f, 1.45f);
            float contentW = Mathf.Min(w - 64f * s, 1180f * s);
            float left = (w - contentW) * 0.5f;
            float elapsed = Mathf.Max(0f, Time.unscaledTime - shownAt);
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / countUpDuration));

            RectFill(new Rect(0, 0, w, h), new Color(0f, 0.15f, 0.22f, 0.18f));
            RectFill(new Rect(0, 0, w, 10f * s), new Color(0.5f, 0.9f, 0.94f, 0.9f));
            Label(new Rect(left, 55f * s, contentW, 30f * s), "UNDERWATER RESULT", 17, FontStyle.Bold,
                new Color32(177, 239, 241, 255), TextAnchor.MiddleLeft, s);
            Label(new Rect(left, 86f * s, contentW, 75f * s), "今回のスコア", 56, FontStyle.Bold,
                Color.white, TextAnchor.MiddleLeft, s);

            float gap = 22f * s;
            float cardW = (contentW - gap * 2f) / 3f;
            float top = 205f * s;
            float cardH = 290f * s;
            Card(new Rect(left, top, cardW, cardH), "あんこの量",
                $"{Mathf.RoundToInt(ankoGrams * progress):N0}", "g", ankoGrams / 200f, Anko, s);
            Card(new Rect(left + cardW + gap, top, cardW, cardH), "日焼け度合",
                $"{sunburnPercent * progress:0}", "%", sunburnPercent / 100f, Sunburn, s);
            Card(new Rect(left + (cardW + gap) * 2f, top, cardW, cardH), "時間",
                FormatTime(elapsedSeconds * progress), "MIN / SEC", 1f - elapsedSeconds / 450f, TimeColor, s);

            float totalTop = top + cardH + 34f * s;
            Rect totalRect = new Rect(left, totalTop, contentW, 178f * s);
            Round(new Rect(totalRect.x + 8f * s, totalRect.y + 10f * s, totalRect.width, totalRect.height),
                new Color(0f, 0.05f, 0.08f, 0.25f));
            Round(totalRect, Total);
            Label(new Rect(totalRect.x + 52f * s, totalRect.y + 34f * s, totalRect.width * .38f, 50f * s),
                "最終スコア", 31, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, s);
            Label(new Rect(totalRect.x + totalRect.width * .42f, totalRect.y + 25f * s,
                    totalRect.width * .51f, 92f * s),
                $"{Mathf.RoundToInt(TotalScore * progress):N0}", 64, FontStyle.Bold, Color.white,
                TextAnchor.MiddleRight, s);
            Label(new Rect(totalRect.x + 54f * s, totalRect.y + 92f * s, totalRect.width * .38f, 30f * s),
                "TOTAL SCORE", 16, FontStyle.Bold, new Color32(184, 220, 226, 255), TextAnchor.MiddleCenter, s);
            Label(new Rect(left, totalTop + totalRect.height + 16f * s, contentW, 32f * s),
                "あんこ × 10 ＋ 日焼け × 15 ＋ タイムボーナス", 16, FontStyle.Normal,
                new Color32(224, 246, 248, 255), TextAnchor.MiddleCenter, s);
        }

        private void Card(Rect rect, string title, string value, string unit, float ratio, Color accent, float s)
        {
            Round(new Rect(rect.x + 7f * s, rect.y + 10f * s, rect.width, rect.height),
                new Color(0f, .08f, .12f, .22f));
            Round(rect, new Color(1f, 1f, 1f, .78f));
            Round(new Rect(rect.x + 26f * s, rect.y + 23f * s, rect.width - 52f * s, 8f * s), accent);
            Label(new Rect(rect.x + 20f * s, rect.y + 46f * s, rect.width - 40f * s, 42f * s),
                title, 25, FontStyle.Bold, Ink, TextAnchor.MiddleCenter, s);
            Label(new Rect(rect.x + 16f * s, rect.y + 98f * s, rect.width - 32f * s, 80f * s),
                value, 49, FontStyle.Bold, Ink, TextAnchor.MiddleCenter, s);
            Label(new Rect(rect.x + 20f * s, rect.y + 174f * s, rect.width - 40f * s, 30f * s),
                unit, 17, FontStyle.Bold, Muted, TextAnchor.MiddleCenter, s);
            Rect bar = new Rect(rect.x + 32f * s, rect.y + rect.height - 52f * s, rect.width - 64f * s, 9f * s);
            Round(bar, new Color(.1f, .22f, .28f, .15f));
            Round(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(ratio), bar.height), accent);
        }

        private void Label(Rect rect, string text, int size, FontStyle style, Color color, TextAnchor anchor, float s)
        {
            GUIStyle guiStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(size * s)),
                fontStyle = style,
                alignment = anchor,
                normal = { textColor = color },
                clipping = TextClipping.Clip
            };
            GUI.Label(rect, text, guiStyle);
        }

        private static string FormatTime(float value)
        {
            int seconds = Mathf.Max(0, Mathf.RoundToInt(value));
            return $"{seconds / 60:00}分 {seconds % 60:00}秒";
        }

        private void RectFill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = old;
        }

        private void Round(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, roundStyle);
            GUI.color = old;
        }

        private static Texture2D MakePixel()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeRoundMask(int size, float radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color32[] colors = new Color32[size * size];
            Vector2 center = Vector2.one * size * .5f;
            Vector2 half = Vector2.one * (size * .5f - radius - 1f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + .5f, y + .5f) - center;
                Vector2 q = new Vector2(Mathf.Max(Mathf.Abs(p.x) - half.x, 0f),
                    Mathf.Max(Mathf.Abs(p.y) - half.y, 0f));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(.5f - (q.magnitude - radius)) * 255f);
                colors[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(colors);
            texture.Apply();
            return texture;
        }
    }
}
