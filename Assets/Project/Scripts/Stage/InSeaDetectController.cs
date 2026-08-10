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
        

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player")){
                Debug.Log("プレイヤーが海に到達しました！");

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

                Time.timeScale = 1f;
                SceneManager.LoadScene(ResultSceneName);
            }
        }
    }
}