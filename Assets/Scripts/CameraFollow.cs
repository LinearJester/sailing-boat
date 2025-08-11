using UnityEngine;
using UnityEngine.Serialization;

public class CameraFollow : MonoBehaviour
{
    public Transform Follow { get; set; }

    public float speed;
    public float offsetY;
    public float offsetZ;

    private void Update()
    {
        if (Follow != null)
        {
            transform.position = Vector3.Lerp(transform.position,
                new Vector3(Follow.transform.position.x, offsetY, Follow.transform.position.z + offsetZ),
                0.9f * (Time.deltaTime * speed));
        }
    }
}