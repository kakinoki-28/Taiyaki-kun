using UnityEngine;

public class WaterController : MonoBehaviour
{
    private Vector3 originalPos;
    [SerializeField] private float frequency = 5f;

    void Start()
    {
        originalPos = transform.position;
    }

    void FixedUpdate()
    {
        transform.position = new Vector3(originalPos.x, originalPos.y, originalPos.z + Mathf.Sin(Time.fixedTime * frequency) * 0.5f);
    }
}
