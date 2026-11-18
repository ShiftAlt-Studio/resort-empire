
using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    [SerializeField] private float speed = 0.25f;
    [SerializeField] private float leftX = -8f;
    [SerializeField] private float rightX = 8f;

    private void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;

        Vector3 pos = transform.position;

        if (speed > 0 && pos.x > rightX)
        {
            pos.x = leftX;
            transform.position = pos;
        }

        if (speed < 0 && pos.x < leftX)
        {
            pos.x = rightX;
            transform.position = pos;
        }
    }
}
