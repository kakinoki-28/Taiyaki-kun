using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TaiyakiKun
{
    public class TitleManager : MonoBehaviour
    {
        [Header("参照設定")]
        public PlayerEntryController playerEntry;

        [Header("見た目の設定")]
        public string gameTitle = "飛べ！たい焼きくん！";
        public Color titleColor = new Color(1f, 0.6f, 0f, 1f);

        private bool isTitle = true;
        private Font legacyFont;

        private void Awake()
        {
            // 以前プレイしたスコアデータリセット
            ResultScoreData.Clear();
            
            // ゲームオーバー時などに時間を止めていた場合、通常速度に戻す
            Time.timeScale = 1f;
        }

        private void Start()
        {
            legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void Update()
        {
            if (!isTitle) return;

            bool isKeyPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                isKeyPressed = true;
            }
#else
            if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            {
                isKeyPressed = true;
            }
#endif

            if (isKeyPressed)
            {
                isTitle = false;

                if (playerEntry != null)
                {
                    playerEntry.StartEntryJump();
                }
            }
        }

        private void OnGUI()
        {
            if (!isTitle) return;

            float sw = Screen.width;
            float sh = Screen.height;
            float scale = Mathf.Clamp(Mathf.Min(sw / 1440f, sh / 900f), 0.4f, 1.5f);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = legacyFont,
                fontSize = Mathf.RoundToInt(100 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = titleColor }
            };
            GUI.Label(new Rect(0, -60 * scale, sw, sh), gameTitle, titleStyle);

            GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
            {
                font = legacyFont,
                fontSize = Mathf.RoundToInt(30 * scale),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            
            float alpha = Mathf.PingPong(Time.time * 1.5f, 1f);
            promptStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
            
            GUI.Label(new Rect(0, 100 * scale, sw, sh), "何かキーを押してスタート", promptStyle);
        }

        public void ReturnToTitleScene()
        {
            // 現在のシーンを丸ごと再読み込みすることで、完全な初期状態にする
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}