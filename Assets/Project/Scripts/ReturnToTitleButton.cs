using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TaiyakiKun
{
    /// <summary>
    /// Loads the title scene when the attached UI Button is clicked.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class ReturnToTitleButton : MonoBehaviour
    {
        [SerializeField] private string titleSceneName = "SampleScene";

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(ReturnToTitle);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(ReturnToTitle);
            }
        }

        public void ReturnToTitle()
        {
            if (string.IsNullOrWhiteSpace(titleSceneName))
            {
                Debug.LogError("ReturnToTitleButton: Title Scene Name is empty.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
            {
                Debug.LogError(
                    $"ReturnToTitleButton: Scene '{titleSceneName}' is not enabled in Build Settings.",
                    this);
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(titleSceneName);
        }
    }
}
