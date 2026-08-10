using System.Collections;
using UnityEngine;

public class CrowAlertController : MonoBehaviour
{
    [SerializeField] private GameObject crow;
    [SerializeField] private Vector3 destinationPosition;

    [SerializeField] private float moveSpeed = 5.0f;

    private bool hasTriggered = false;
    private Animator animator;
    private static readonly int FlyingHash = Animator.StringToHash("flying");
    private static readonly int FlyingDirectionHash = Animator.StringToHash("flyingDirectionX");

    private void Start()
    {
        if (crow != null)
        {
            animator = crow.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.SetBool(FlyingHash, true);
                animator.SetFloat(FlyingDirectionHash, 0.15f);
                animator.CrossFade("Base Layer.fly", 0.05f);
            }
        }
    }
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
        while (Vector3.Distance(crow.transform.position, destinationPosition) > 0.001f)
        {
            // 現在地から目的地に向かって一定速度で近づく
            crow.transform.position = Vector3.MoveTowards(
                crow.transform.position, 
                destinationPosition, 
                moveSpeed * Time.deltaTime
            );

            // 次のフレームまで待つ
            yield return null; 
        }

        Destroy(gameObject); 
    }
}