using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform objetive;
    public float cameraSpeed = 0.025f;
    public Vector3 glissade;

    private void LateUpdate()
    {
        Vector3 desiredPosition = objetive.position + glissade;

        Vector3 softenedPosition = Vector3.Lerp(transform.position, desiredPosition, cameraSpeed);

        transform.position = softenedPosition;
    }
}
