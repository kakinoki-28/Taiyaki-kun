using UnityEngine;
using UnityEngine.SceneManagement;

public class InSeaDetectController : MonoBehaviour
{
    [SerializeField] private string ResultSceneName = "Result";
    [SerializeField] private Sunburn sunburnScript;
    

   void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("プレイヤーが海に到達しました！");
            SceneManager.LoadScene(ResultSceneName);
        }
    }
}
