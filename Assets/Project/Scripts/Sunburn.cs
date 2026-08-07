using UnityEngine;

public class Sunburn : MonoBehaviour
{
    [Header("参照設定")]
    [Tooltip("影の判定を行っているShadowDetectorスクリプトを指定してください")]
    public ShadowDetector shadowDetector;

    [Header("マテリアル・色の設定")]
    [Tooltip("色を変化させたいマテリアルを指定してください")]
    public Material targetMaterial;

    [Tooltip("URPの場合は '_BaseColor'、Built-in(標準)の場合は '_Color' を指定")]
    public string colorPropertyName = "_BaseColor";

    [Tooltip("Emissionのプロパティ名。通常は '_EmissionColor' を指定します")]
    public string emissionPropertyName = "_EmissionColor";

    [Tooltip("1秒間にどれくらいVの値を下げるか")]
    public float vDecreaseRate = 0.2f;

    [Tooltip("Vの最小値")]
    public float minV = 0.0f;

    // 最初の色を記憶しておくための変数
    private Color originalColor;
    private Color originalEmissionColor;

    private void Start()
    {
        // インスペクターでShadowDetectorをアタッチし忘れた場合のフェイルセーフ
        if (shadowDetector == null)
        {
            shadowDetector = GetComponent<ShadowDetector>();
        }

        if (targetMaterial != null)
        {
            // BaseColorの初期値を記憶
            if (targetMaterial.HasProperty(colorPropertyName))
            {
                originalColor = targetMaterial.GetColor(colorPropertyName);
            }

            // EmissionColorの初期値を記憶
            if (targetMaterial.HasProperty(emissionPropertyName))
            {
                originalEmissionColor = targetMaterial.GetColor(emissionPropertyName);
            }
        }
    }

    private void Update()
    {
        if (shadowDetector == null || targetMaterial == null) return;

        // ShadowDetectorの判定結果を読み取り、影にいない場合のみ暗くする
        if (!shadowDetector.isInShadow)
        {
            DecreaseBrightness();
        }
    }

    private void DecreaseBrightness()
    {
        // --- 1. BaseColorの処理 ---
        if (targetMaterial.HasProperty(colorPropertyName))
        {
            Color currentColor = targetMaterial.GetColor(colorPropertyName);
            Color.RGBToHSV(currentColor, out float h, out float s, out float v);
            
            v -= vDecreaseRate * Time.deltaTime;
            v = Mathf.Max(v, minV);
            
            Color newColor = Color.HSVToRGB(h, s, v);
            targetMaterial.SetColor(colorPropertyName, newColor);
        }

        // --- 2. Emissionの処理 ---
        if (targetMaterial.HasProperty(emissionPropertyName))
        {
            Color currentEmission = targetMaterial.GetColor(emissionPropertyName);
            Color.RGBToHSV(currentEmission, out float eh, out float es, out float ev);
            
            ev -= vDecreaseRate * Time.deltaTime;
            ev = Mathf.Max(ev, minV);
            
            Color newEmission = Color.HSVToRGB(eh, es, ev);
            targetMaterial.SetColor(emissionPropertyName, newEmission);
        }
    }

    private void OnDestroy()
    {
        if (targetMaterial != null)
        {
            // プレイモード終了時に、記憶した色に戻す
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