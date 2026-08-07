using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TaiyakiKun
{
    /// <summary>
    /// Sunburnの日焼け止め状態を、たい焼き周囲のリングと円形UIで表示します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(global::Sunburn))]
    public sealed class UvProtectionPresentation : MonoBehaviour
    {
        private const int CircleTextureSize = 128;
        private const int RingSegments = 64;

        [Header("World Effect")]
        [SerializeField]
        private Color effectColor = new Color(0.15f, 0.95f, 1f, 1f);

        [SerializeField]
        [Min(0.1f)]
        private float ringRadius = 0.85f;

        [SerializeField]
        [Min(0.005f)]
        private float ringWidth = 0.045f;

        [SerializeField]
        [Min(0.1f)]
        private float whiteFlashFrequency = 3f;

        [Header("Timer UI")]
        [SerializeField]
        [Min(64f)]
        private float timerSize = 112f;

        [SerializeField]
        [Min(0f)]
        private float timerMargin = 24f;

        private global::Sunburn sunburn;
        private GameObject effectRoot;
        private Transform outerRingTransform;
        private Transform innerRingTransform;
        private LineRenderer outerRing;
        private LineRenderer innerRing;
        private Material ringMaterial;
        private Material whiteFlashMaterial;
        private RendererMaterialState[] taiyakiRenderers;
        private bool isTaiyakiWhite;
        private float protectionVisualStartedAt;

        private Canvas timerCanvas;
        private GameObject timerRoot;
        private Image timerFill;
        private Text timerText;
        private Sprite circleSprite;
        private Texture2D circleTexture;
        private bool wasProtectionActive;

        private void Awake()
        {
            sunburn = GetComponent<global::Sunburn>();
            CacheTaiyakiRenderers();
            CreateWhiteFlashMaterial();
            CreateWorldEffect();
            CreateTimerUi();
            SetPresentationActive(false);
        }

        private void Update()
        {
            bool isActive = sunburn != null && sunburn.IsUvProtected;
            if (isActive != wasProtectionActive)
            {
                SetPresentationActive(isActive);
                wasProtectionActive = isActive;
            }

            if (!isActive)
            {
                return;
            }

            UpdateWorldEffect();
            UpdateTimerUi();
        }

        private void OnDisable()
        {
            SetPresentationActive(false);
            wasProtectionActive = false;
        }

        private void OnDestroy()
        {
            if (timerCanvas != null)
            {
                Destroy(timerCanvas.gameObject);
            }

            if (ringMaterial != null)
            {
                Destroy(ringMaterial);
            }

            if (whiteFlashMaterial != null)
            {
                Destroy(whiteFlashMaterial);
            }

            if (circleSprite != null)
            {
                Destroy(circleSprite);
            }

            if (circleTexture != null)
            {
                Destroy(circleTexture);
            }
        }

        private void OnValidate()
        {
            ringRadius = Mathf.Max(0.1f, ringRadius);
            ringWidth = Mathf.Max(0.005f, ringWidth);
            whiteFlashFrequency = Mathf.Max(0.1f, whiteFlashFrequency);
            timerSize = Mathf.Max(64f, timerSize);
            timerMargin = Mathf.Max(0f, timerMargin);
        }

        private void CreateWorldEffect()
        {
            effectRoot = new GameObject("UV Protection Effect");
            effectRoot.transform.SetParent(transform, false);

            Shader ringShader = Shader.Find("Sprites/Default");
            if (ringShader != null)
            {
                ringMaterial = new Material(ringShader)
                {
                    name = "UV Protection Ring (Runtime)",
                    hideFlags = HideFlags.DontSave
                };
            }

            outerRing = CreateRing("Outer UV Ring", 1f, 68f, out outerRingTransform);
            innerRing = CreateRing("Inner UV Ring", 0.72f, 108f, out innerRingTransform);
        }

        private LineRenderer CreateRing(
            string objectName,
            float radiusMultiplier,
            float tilt,
            out Transform ringTransform)
        {
            GameObject ringObject = new GameObject(objectName);
            ringTransform = ringObject.transform;
            ringTransform.SetParent(effectRoot.transform, false);
            ringTransform.localPosition = Vector3.zero;
            ringTransform.localRotation = Quaternion.AngleAxis(tilt, Vector3.right);

            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = RingSegments;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.widthMultiplier = ringWidth;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = effectColor;
            line.endColor = effectColor;

            if (ringMaterial != null)
            {
                line.sharedMaterial = ringMaterial;
            }

            float radius = ringRadius * radiusMultiplier;
            for (int index = 0; index < RingSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / RingSegments;
                line.SetPosition(index, new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0f));
            }

            return line;
        }

        private void UpdateWorldEffect()
        {
            float time = Time.time;
            float pulse = 1f + Mathf.Sin(time * 5f) * 0.09f;
            float remaining = sunburn.UvProtectionRemaining;
            float endingFade = Mathf.Clamp01(remaining / 0.75f);

            outerRingTransform.localScale = Vector3.one * pulse;
            innerRingTransform.localScale = Vector3.one * (2f - pulse);
            outerRingTransform.localRotation =
                Quaternion.AngleAxis(time * 70f, Vector3.up)
                * Quaternion.AngleAxis(68f, Vector3.right);
            innerRingTransform.localRotation =
                Quaternion.AngleAxis(-time * 95f, Vector3.up)
                * Quaternion.AngleAxis(108f, Vector3.right);

            Color outerColor = effectColor;
            outerColor.a *= 0.9f * endingFade;
            Color innerColor = Color.Lerp(effectColor, Color.white, 0.45f);
            innerColor.a *= 0.75f * endingFade;
            outerRing.startColor = outerColor;
            outerRing.endColor = outerColor;
            innerRing.startColor = innerColor;
            innerRing.endColor = innerColor;

            UpdateEffectCenter();
            bool showWhite = Mathf.Repeat(
                (time - protectionVisualStartedAt) * whiteFlashFrequency,
                1f) < 0.5f;
            SetTaiyakiWhite(showWhite);
        }

        private void CreateTimerUi()
        {
            circleSprite = CreateCircleSprite();

            GameObject canvasObject = new GameObject("UV Protection Timer Overlay");
            timerCanvas = canvasObject.AddComponent<Canvas>();
            timerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            timerCanvas.sortingOrder = 110;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            timerRoot = CreateRectObject("UV Timer", canvasObject.transform, out RectTransform rootRect);
            rootRect.anchorMin = Vector2.one;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = Vector2.one;
            rootRect.anchoredPosition = new Vector2(-timerMargin, -timerMargin);
            rootRect.sizeDelta = new Vector2(timerSize, timerSize + 30f);

            GameObject backgroundObject = CreateRectObject(
                "Timer Background",
                timerRoot.transform,
                out RectTransform backgroundRect);
            SetTopCircleRect(backgroundRect, timerSize);
            Image background = backgroundObject.AddComponent<Image>();
            background.sprite = circleSprite;
            background.color = new Color(0.02f, 0.08f, 0.11f, 0.84f);
            background.raycastTarget = false;

            GameObject fillObject = CreateRectObject(
                "Remaining Time Fill",
                timerRoot.transform,
                out RectTransform fillRect);
            SetTopCircleRect(fillRect, timerSize - 10f);
            timerFill = fillObject.AddComponent<Image>();
            timerFill.sprite = circleSprite;
            timerFill.color = effectColor;
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Radial360;
            timerFill.fillOrigin = (int)Image.Origin360.Top;
            timerFill.fillClockwise = true;
            timerFill.fillAmount = 1f;
            timerFill.raycastTarget = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject timeObject = CreateRectObject(
                "Remaining Seconds",
                timerRoot.transform,
                out RectTransform timeRect);
            SetTopCircleRect(timeRect, timerSize);
            timerText = timeObject.AddComponent<Text>();
            timerText.font = font;
            timerText.fontSize = Mathf.RoundToInt(timerSize * 0.25f);
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.color = Color.white;
            timerText.raycastTarget = false;
            Outline timeOutline = timeObject.AddComponent<Outline>();
            timeOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            timeOutline.effectDistance = new Vector2(2f, -2f);

            GameObject labelObject = CreateRectObject(
                "UV Cut Label",
                timerRoot.transform,
                out RectTransform labelRect);
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -timerSize - 2f);
            labelRect.sizeDelta = new Vector2(timerSize + 30f, 28f);
            Text label = labelObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = Mathf.RoundToInt(timerSize * 0.18f);
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = "UV CUT";
            label.color = effectColor;
            label.raycastTarget = false;
            Outline labelOutline = labelObject.AddComponent<Outline>();
            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            labelOutline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void UpdateTimerUi()
        {
            timerFill.fillAmount = sunburn.UvProtectionNormalized;
            timerText.text = sunburn.UvProtectionRemaining.ToString("0.0");
        }

        private void SetPresentationActive(bool isActive)
        {
            if (isActive)
            {
                protectionVisualStartedAt = Time.time;
            }
            else
            {
                SetTaiyakiWhite(false);
            }

            if (effectRoot != null)
            {
                effectRoot.SetActive(isActive);
            }

            if (timerRoot != null)
            {
                timerRoot.SetActive(isActive);
            }
        }

        private void CacheTaiyakiRenderers()
        {
            Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
            List<RendererMaterialState> states = new List<RendererMaterialState>();
            foreach (Renderer childRenderer in childRenderers)
            {
                // プレイヤー本体に残っている非表示のテスト用Cubeは対象外にする。
                if (childRenderer.transform == transform)
                {
                    continue;
                }

                Material[] originalMaterials = childRenderer.sharedMaterials;
                if (originalMaterials.Length == 0)
                {
                    continue;
                }

                states.Add(new RendererMaterialState(childRenderer, originalMaterials));
            }

            taiyakiRenderers = states.ToArray();
        }

        private void CreateWhiteFlashMaterial()
        {
            Shader whiteShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (whiteShader == null)
            {
                whiteShader = Shader.Find("Unlit/Color");
            }

            if (whiteShader == null)
            {
                whiteShader = Shader.Find("Sprites/Default");
            }

            if (whiteShader == null)
            {
                return;
            }

            whiteFlashMaterial = new Material(whiteShader)
            {
                name = "UV Protection White Flash (Runtime)",
                hideFlags = HideFlags.DontSave
            };

            if (whiteFlashMaterial.HasProperty("_BaseColor"))
            {
                whiteFlashMaterial.SetColor("_BaseColor", Color.white);
            }

            if (whiteFlashMaterial.HasProperty("_Color"))
            {
                whiteFlashMaterial.SetColor("_Color", Color.white);
            }

            if (whiteFlashMaterial.HasProperty("_BaseMap"))
            {
                whiteFlashMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture);
            }

            if (whiteFlashMaterial.HasProperty("_MainTex"))
            {
                whiteFlashMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
            }
        }

        private void UpdateEffectCenter()
        {
            bool foundVisibleRenderer = false;
            Bounds combinedBounds = default;
            foreach (RendererMaterialState state in taiyakiRenderers)
            {
                Renderer targetRenderer = state.Renderer;
                if (targetRenderer == null
                    || !targetRenderer.enabled
                    || !targetRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!foundVisibleRenderer)
                {
                    combinedBounds = targetRenderer.bounds;
                    foundVisibleRenderer = true;
                }
                else
                {
                    combinedBounds.Encapsulate(targetRenderer.bounds);
                }
            }

            if (foundVisibleRenderer)
            {
                effectRoot.transform.position = combinedBounds.center;
            }
        }

        private void SetTaiyakiWhite(bool makeWhite)
        {
            if (isTaiyakiWhite == makeWhite)
            {
                return;
            }

            isTaiyakiWhite = makeWhite;
            foreach (RendererMaterialState state in taiyakiRenderers)
            {
                if (state.Renderer == null)
                {
                    continue;
                }

                state.Renderer.sharedMaterials = makeWhite && whiteFlashMaterial != null
                    ? state.GetWhiteMaterials(whiteFlashMaterial)
                    : state.OriginalMaterials;
            }
        }

        private sealed class RendererMaterialState
        {
            private Material[] whiteMaterials;

            public Renderer Renderer { get; }
            public Material[] OriginalMaterials { get; }

            public RendererMaterialState(Renderer renderer, Material[] originalMaterials)
            {
                Renderer = renderer;
                OriginalMaterials = originalMaterials;
            }

            public Material[] GetWhiteMaterials(Material whiteMaterial)
            {
                if (whiteMaterials == null || whiteMaterials.Length != OriginalMaterials.Length)
                {
                    whiteMaterials = new Material[OriginalMaterials.Length];
                    for (int index = 0; index < whiteMaterials.Length; index++)
                    {
                        whiteMaterials[index] = whiteMaterial;
                    }
                }

                return whiteMaterials;
            }
        }

        private Sprite CreateCircleSprite()
        {
            circleTexture = new Texture2D(
                CircleTextureSize,
                CircleTextureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = "UV Timer Circle (Runtime)",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color32[] pixels = new Color32[CircleTextureSize * CircleTextureSize];
            float center = (CircleTextureSize - 1) * 0.5f;
            float solidRadius = center - 2f;
            float outerRadius = center;
            for (int y = 0; y < CircleTextureSize; y++)
            {
                for (int x = 0; x < CircleTextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    byte alpha = distance <= solidRadius
                        ? (byte)255
                        : (byte)Mathf.RoundToInt(
                            Mathf.Clamp01((outerRadius - distance) / (outerRadius - solidRadius)) * 255f);
                    pixels[y * CircleTextureSize + x] = new Color32(255, 255, 255, alpha);
                }
            }

            circleTexture.SetPixels32(pixels);
            circleTexture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                circleTexture,
                new Rect(0f, 0f, CircleTextureSize, CircleTextureSize),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = "UV Timer Circle (Runtime)";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static GameObject CreateRectObject(
            string objectName,
            Transform parent,
            out RectTransform rectTransform)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return gameObject;
        }

        private static void SetTopCircleRect(RectTransform rectTransform, float size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(size, size);
        }
    }
}
