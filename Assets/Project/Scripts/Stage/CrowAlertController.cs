using System.Collections;
using UnityEngine;

public class CrowAlertController : MonoBehaviour
{
    [SerializeField] private Transform crow;
    [SerializeField] private Vector3 destinationPosition;

    [SerializeField] private float moveSpeed = 5.0f;

    private bool hasTriggered = false;
    private void OnTriggerEnter(Collider other)
    {
        // ぶつかったオブジェクトが「Player」タグを持っているか確認
        if (other.CompareTag("Player") && !hasTriggered)
        {
            ExecuteEvent();
        }
    }

    void ExecuteEvent()
    {
        Debug.Log("プレイヤーがカラス警告エリアを通過しました！");
        if (crow != null){
            hasTriggered = true;
            // コルーチンを開始して滑らかに移動させる
            StartCoroutine(SmoothMoveRoutine());
        }
        else
        {
            Debug.LogWarning("カラスのオブジェクトが設定されていません。");
        }        

    }

    // 滑らかに移動させるためのコルーチン
    IEnumerator SmoothMoveRoutine()
    {
        // 目的地の座標に到達するまで毎フレーム処理を繰り返す
        while (Vector3.Distance(crow.position, destinationPosition) > 0.001f)
        {
            Debug.Log("カラスの現在位置: " + crow.position + ", 目的地: " + destinationPosition);
            // 現在地から目的地に向かって一定速度で近づく
            crow.position = Vector3.MoveTowards(
                crow.position, 
                destinationPosition, 
                moveSpeed * Time.deltaTime
            );

            // 次のフレームまで待つ
            yield return null; 
        }

        Destroy(gameObject); 
    }
}