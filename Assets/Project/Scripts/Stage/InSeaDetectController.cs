using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TaiyakiKun.Tests
{
    [DisallowMultipleComponent]
    public class InSeaDetectController : MonoBehaviour
    {
        [SerializeField] private string ResultSceneName = "Result";
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private Sunburn sunburn;
        [SerializeField] private AnkoCollectionPlaygroundController ankoCollectionPlaygroundController;
        [SerializeField] private AudioClip SplashClip;
        private AudioSource source;
        void Start() => source = GetComponent<AudioSource>();

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player")){
                Debug.Log("プレイヤーが海に到達しました！");
                source.PlayOneShot(SplashClip, 1.0f);
                StartCoroutine(SlowMotionAndTransition());
                if (scoreManager == null)
                {
                    Debug.LogError(
                        "ScoreManagerを設定してください。",
                        this);
                    return;
                }

                if (sunburn == null)
                {
                    Debug.LogError(
                        "Sunburnを設定してください。");
                    return;
                }

                if (ankoCollectionPlaygroundController == null)
                {
                    Debug.LogError(
                        "AnkoCollectionPlaygroundControllerを設定してください。");
                    return;
                }

                int ankoGrams = scoreManager.AnkoCount * 100;

                float sunburnPercent = (1f - sunburn.SunburnHealthNormalized) * 100f;

                float remainingSeconds =  ankoCollectionPlaygroundController.TimeLimitSeconds - ankoCollectionPlaygroundController.ElapsedSeconds;

                // 現在のResultScoreDataでは第3引数の名前は
                // elapsedSecondsですが、Result画面に渡す時間値として使えます。
                ResultScoreData.SetResults(
                    ankoGrams,
                    sunburnPercent,
                    remainingSeconds);

            }
        }
        private IEnumerator SlowMotionAndTransition()
        {
            Time.timeScale = 0.1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // 音の長さ分だけ待つ（Time.timeScaleの影響を受けないように WaitForSecondsRealtime を使用）
            float waitTime = (SplashClip != null) ? SplashClip.length : 1.0f;
            yield return new WaitForSecondsRealtime(waitTime);

            // タイムスケールを通常に戻す
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            SceneManager.LoadScene(ResultSceneName);
        }
    }
}