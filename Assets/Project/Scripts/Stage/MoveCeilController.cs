using UnityEngine;

public class MoveCeilController : MonoBehaviour
{
    private Vector3 originalPos;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float moveDistance = 20f;
    [SerializeField] private float moveDelay = 0f;

    [SerializeField] private bool moveX = false;
    private float DelayTimer = 0f;
    private float Timer = 0f;
    void Start()
    {
        originalPos = transform.position;
        DelayTimer = moveDelay;
    }

    void FixedUpdate()
    {
        if (DelayTimer > 0)
        {
            DelayTimer -= Time.fixedDeltaTime;
            return;
        }else {
            Timer += Time.fixedDeltaTime;
        }

        if (moveX)
        {
            transform.position = new Vector3(originalPos.x + (Mathf.Cos(Timer * moveSpeed) * -1 + 1) / 2 * moveDistance, originalPos.y, originalPos.z);
        }
        else
        {
            transform.position = new Vector3(originalPos.x, originalPos.y, originalPos.z +(Mathf.Cos(Timer * moveSpeed)* -1 + 1)/2 * moveDistance);            
        }

        if(Mathf.Sin(Timer * moveSpeed) * Mathf.Sin((Timer - Time.fixedDeltaTime) * moveSpeed) < 0)
        {
            DelayTimer = moveDelay;
        }
    }
}
