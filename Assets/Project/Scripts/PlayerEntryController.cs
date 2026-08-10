using UnityEngine;

namespace TaiyakiKun
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerEntryController : MonoBehaviour
    {
        [Header("ジャンプ演出の設定")]
        [Tooltip("X:横, Y:上, Z:前 方向へ飛び出す速度")]
        public Vector3 jumpVelocity = new Vector3(0f, 15f, 15f); 
        
        [Tooltip("ジャンプ中の重力倍率")]
        [Min(1f)] public float entryGravityMultiplier = 3.0f;

        [Header("着地するまで止めておくスクリプト")]
        [Tooltip("Sunburnスクリプトや、FishHopperなどをここに入れます")]
        public MonoBehaviour[] scriptsToDisableUntilLand;

        private Rigidbody rb;
        private bool hasJumped = false;
        private bool hasLanded = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            // 開始直後は指定したスクリプトのみ無効化
            foreach (var script in scriptsToDisableUntilLand)
            {
                if (script != null) script.enabled = false;
            }
        }

        public void StartEntryJump()
        {
            if (hasJumped) return;
            hasJumped = true;

            // キャラクターの向いている方向を基準に、質量を無視して直接「速度」を与える
            Vector3 velocity = transform.TransformDirection(jumpVelocity);
            rb.AddForce(velocity, ForceMode.VelocityChange);
        }

        private void FixedUpdate()
        {
            // ジャンプ中かつ空中の間だけ、追加の重力をかける
            if (hasJumped && !hasLanded && rb.useGravity)
            {
                rb.AddForce(Physics.gravity * (entryGravityMultiplier - 1f), ForceMode.Acceleration);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // ジャンプした後、着地したら
            if (hasJumped && !hasLanded)
            {
                hasLanded = true;
                
                // オフにしておいたスクリプトを一斉にオンにする
                foreach (var script in scriptsToDisableUntilLand)
                {
                    if (script != null) script.enabled = true;
                }
            }
        }
    }
}