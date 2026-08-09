using UnityEngine;

public class WayPointMoveController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Transform[] waypoints; // 巡回する地点の配列
    [SerializeField] private float movespeed = 5f;      // 移動速度
    private int currentIdx = 0;

    void FixedUpdate()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentIdx];
        transform.position = Vector3.MoveTowards(transform.position, target.position, movespeed * Time.deltaTime);

        // 目的地の近くに来たら次のターゲットへ
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentIdx = (currentIdx + 1) % waypoints.Length; // ループさせる
        }
    }
}
