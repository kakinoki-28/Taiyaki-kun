using System;
using UnityEngine;

public class Sunburn : MonoBehaviour
{
    [Header("参照設定")]
    public ShadowDetector shadowDetector;

    [Header("マテリアル・色の設定")]
    public Material targetMaterial;
    public string colorPropertyName = "_BaseColor";
    public string emissionPropertyName = "_EmissionColor";
    public float vDecreaseRate = 0.2f;
    public float minV = 0.0f;

    public event Action UvProtectionActivated;
    public event Action UvProtectionEnded;

    public bool IsUvProtected => uvProtectionRemaining > 0f;
    public float UvProtectionRemaining => Mathf.Max(0f, uvProtectionRemaining);
    public float UvProtectionNormalized => activeUvProtectionDuration > 0f
        ? Mathf.Clamp01(uvProtectionRemaining / activeUvProtectionDuration)
        : 0f;

    public float SunburnHealthNormalized
    {
        get
        {
            float brightnessRange = originalBrightness - minV;
            if (brightnessRange <= Mathf.Epsilon)
            {
                return currentBrightness > minV ? 1f : 0f;
            }
            return Mathf.Clamp01((currentBrightness - minV) / brightnessRange);
        }
    }

    private Color originalColor;
    private Color originalEmissionColor;
    private float originalBrightness = 1f;

    private float currentBrightness = 1f;
    private float currentEmissionBrightness = 1f;

    private float uvProtectionRemaining;
    private float activeUvProtectionDuration;

    private void Start()
    {
        if (shadowDetector == null)
        {
            shadowDetector = GetComponent<ShadowDetector>();
        }

        if (targetMaterial != null)
        {
            if (targetMaterial.HasProperty(colorPropertyName))
            {
                originalColor = targetMaterial.GetColor(colorPropertyName);
                Color.RGBToHSV(originalColor, out _, out _, out originalBrightness);
                currentBrightness = originalBrightness;
            }

            if (targetMaterial.HasProperty(emissionPropertyName))
            {
                originalEmissionColor = targetMaterial.GetColor(emissionPropertyName);
                Color.RGBToHSV(originalEmissionColor, out _, out _, out currentEmissionBrightness);
            }
        }
    }

    private void Update()
    {
        if (IsUvProtected)
        {
            uvProtectionRemaining -= Time.deltaTime;
            if (uvProtectionRemaining <= 0f)
            {
                uvProtectionRemaining = 0f;
                UvProtectionEnded?.Invoke();
            }
            return;
        }

        if (shadowDetector == null || targetMaterial == null) return;

        if (!shadowDetector.isInShadow)
        {
            DecreaseBrightness();
        }
    }

    public void ActivateUvProtection(float durationSeconds)
    {
        activeUvProtectionDuration = Mathf.Max(0.1f, durationSeconds);
        uvProtectionRemaining = activeUvProtectionDuration;
        UvProtectionActivated?.Invoke();
    }

    private void DecreaseBrightness()
    {
        if (targetMaterial.HasProperty(colorPropertyName))
        {
            currentBrightness -= vDecreaseRate * Time.deltaTime;
            currentBrightness = Mathf.Max(currentBrightness, minV);

            Color.RGBToHSV(originalColor, out float h, out float s, out _);
            Color newColor = Color.HSVToRGB(h, s, currentBrightness);
            targetMaterial.SetColor(colorPropertyName, newColor);
        }

        if (targetMaterial.HasProperty(emissionPropertyName))
        {
            currentEmissionBrightness -= vDecreaseRate * Time.deltaTime;
            currentEmissionBrightness = Mathf.Max(currentEmissionBrightness, minV);

            Color.RGBToHSV(originalEmissionColor, out float eh, out float es, out _);
            Color newEmission = Color.HSVToRGB(eh, es, currentEmissionBrightness);
            targetMaterial.SetColor(emissionPropertyName, newEmission);
        }
    }

    private void OnDestroy()
    {
        if (targetMaterial != null)
        {
            if (targetMaterial.HasProperty(colorPropertyName))
            {
                targetMaterial.SetColor(colorPropertyName, originalColor);
            }
            
            if (targetMaterial.HasProperty(emissionPropertyName))
            {
                targetMaterial.SetColor(emissionPropertyName, originalEmissionColor);
            }
        }
    }
}