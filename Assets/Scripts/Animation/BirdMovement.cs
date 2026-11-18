
using UnityEngine;

public class BirdMovement : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float speed = 1.2f;
    [SerializeField] private float leftX = -8f;
    [SerializeField] private float rightX = 8f;

    [Header("Floating")]
    [SerializeField] private float floatHeight = 0.12f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Direction")]
    [SerializeField] private bool flyRight = true;

    private float baseY;
    private float phase;
    private Vector3 initialScale;

    private void Start()
    {
        baseY = transform.position.y;
        phase = Random.Range(0f, Mathf.PI * 2f);
        initialScale = transform.localScale;
        UpdateDirection();
    }

    private void Update()
    {
        float direction = flyRight ? 1f : -1f;

        Vector3 pos = transform.position;

        pos.x += direction * speed * Time.deltaTime;

        pos.y = baseY +
            Mathf.Sin(Time.time * floatSpeed + phase)
            * floatHeight;

        if (flyRight && pos.x > rightX)
            pos.x = leftX;

        if (!flyRight && pos.x < leftX)
            pos.x = rightX;

        transform.position = pos;
    }

    private void UpdateDirection()
    {
        Vector3 scale = initialScale;

        scale.x = Mathf.Abs(scale.x) *
                  (flyRight ? 1f : -1f);

        transform.localScale = scale;
    }
}
