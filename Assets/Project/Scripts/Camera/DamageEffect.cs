using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class DamageEffect : MonoBehaviour
{
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float duration = 0.5f; // 赤みが消えるまでの時間
    [SerializeField] private float maxIntensity = 0.45f; // 最大の赤み強度

    private Vignette vignette;
    private bool isEffectActive = false;

    void Start()
    {
        // VolumeからVignetteコンポーネントを取得
        if (globalVolume.profile.TryGet<Vignette>(out var tmpVignette))
        {
            vignette = tmpVignette;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0f;
        }
    }

    public void PlayDamageEffect()
    {
        if (vignette != null)
        {
            StopAllCoroutines();
            vignette.intensity.value = maxIntensity;
            isEffectActive = true;
        }
    }
    public void StopDamageEffect()
    {
        if (vignette != null)
        {
            if(isEffectActive)
            {
                isEffectActive = false;
                StartCoroutine(FadeOutVignette());
            }
        }
    }

    public void Reset()
    {
        StopAllCoroutines();
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
            isEffectActive = false;
        }
    }

    private IEnumerator FadeOutVignette()
    {
        float elapsedTime = 0f;

        // 時間をかけてじわじわと消していく
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            vignette.intensity.value = Mathf.Lerp(maxIntensity, 0f, elapsedTime / duration);
            yield return null;
        }

        vignette.intensity.value = 0f;
    }
}