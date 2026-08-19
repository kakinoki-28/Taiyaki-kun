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

        [Header("オーディオ設定")]
        [SerializeField] private AudioClip gameoverBgmClip; // gameover_bgm用
        [SerializeField] private AudioClip deathClip;       // death用

        private AudioSource bgmSource;
        private AudioSource seSource;

        private bool isGameOver = false;
        private Texture2D pixelTexture;
        [SerializeField] private Font GameoverFont;

        private void Awake()
        {
            // OnGUIで単色を塗りつぶすための1x1ピクセルのテクスチャを生成
            pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            pixelTexture.SetPixel(0, 0, Color.white);
            pixelTexture.Apply();

            // オーディオソースの初期化
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.clip = gameoverBgmClip;

            seSource = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            if (isGameOver || sunburnScript == null) return;

            if (sunburnScript.SunburnHealthNormalized <= 0f)
            {
                isGameOver = true;
                
                // ゲームオーバー時に音楽とSEを再生
                if (gameoverBgmClip != null) bgmSource.Play();
                if (deathClip != null) seSource.PlayOneShot(deathClip);

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
                font = GameoverFont,
                fontSize = Mathf.RoundToInt(baseFontSize * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = this.textColor },
                hover = { textColor = this.textColor },
                active = { textColor = this.textColor }
            };
            GUI.Label(new Rect(0, -30f * scale, screenWidth, screenHeight), "Game Over", textStyle);

            GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
            {
                font = GameoverFont,
                fontSize = Mathf.RoundToInt(28 * scale),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };
            // Game Over の文字の下に配置
            GUI.Label(new Rect(0, 60f * scale, screenWidth, screenHeight), "たい焼きくんは焦げてしまいました...\n日差しをよけながら移動しよう！", messageStyle);

            // 3. タイトルへ戻るボタンの描画とクリック判定
            float buttonWidth = 240f * scale;
            float buttonHeight = 60f * scale;
            float buttonX = (screenWidth - buttonWidth) * 0.5f;
            // メッセージを追加した分、ボタンをさらに少し下へ配置
            float buttonY = (screenHeight * 0.5f) + (160f * scale); 

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = GameoverFont,
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