using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaiyakiKun
{
    [DisallowMultipleComponent]
    public sealed class GameOverController : MonoBehaviour
    {
        [Header("参照設定")]
        [Tooltip("監視対象のSunburnスクリプトを割り当ててください")]
        [SerializeField] private Sunburn sunburnScript;

        [Header("見た目の設定")]
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 1f);
        [SerializeField] private Color textColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField, Range(10, 200)] private int baseFontSize = 72;

        [Header("シーン遷移設定")]
        [Tooltip("戻る先のシーン名")]
        [SerializeField] private string titleSceneName = "Title";

        private bool isGameOver = false;
        private Texture2D pixelTexture;
        private Font legacyFont;

        private void Awake()
        {
            // OnGUIで単色を塗りつぶすための1x1ピクセルのテクスチャを生成
            pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            pixelTexture.SetPixel(0, 0, Color.white);
            pixelTexture.Apply();

            // フォントの取得
            legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void Update()
        {
            if (isGameOver || sunburnScript == null) return;

            if (sunburnScript.SunburnHealthNormalized <= 0f)
            {
                isGameOver = true;
                
                Time.timeScale = 0f;
            }
        }

        private void OnGUI()
        {
            if (!isGameOver) return;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // 1. 画面全体を黒で塗りつぶす
            Color oldColor = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(new Rect(0, 0, screenWidth, screenHeight), pixelTexture);
            GUI.color = oldColor;

            // 画面解像度に合わせてスケールを計算
            float scale = Mathf.Clamp(Mathf.Min(screenWidth / 1440f, screenHeight / 900f), 0.4f, 1.5f);
            
            // 2. 画面中央に赤文字で "Game Over" と表示
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                font = legacyFont,
                fontSize = Mathf.RoundToInt(baseFontSize * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = this.textColor },
                hover = { textColor = this.textColor },
                active = { textColor = this.textColor }
            };
            GUI.Label(new Rect(0, 0, screenWidth, screenHeight), "Game Over", textStyle);

            // 3. タイトルへ戻るボタンの描画とクリック判定
            float buttonWidth = 240f * scale;
            float buttonHeight = 60f * scale;
            float buttonX = (screenWidth - buttonWidth) * 0.5f;
            // Game Over の文字の少し下に配置
            float buttonY = (screenHeight * 0.5f) + (100f * scale); 

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = legacyFont,
                fontSize = Mathf.RoundToInt(24 * scale)
            };

            // GUI.Button はボタンが描画され、かつクリックされた瞬間に true を返します
            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "タイトルに戻る", buttonStyle))
            {
                Time.timeScale = 1f;

                SceneManager.LoadScene(titleSceneName);
            }
        }

        private void OnDestroy()
        {
            if (pixelTexture != null)
            {
                Destroy(pixelTexture);
            }
        }
    }
}