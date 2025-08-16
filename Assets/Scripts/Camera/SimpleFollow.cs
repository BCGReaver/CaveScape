using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    public float smooth = 10f;

    void LateUpdate()
    {
        if (!target) return;
        transform.position = Vector3.Lerp(
            transform.position,
            target.position + offset,
            smooth * Time.deltaTime
        );
    }
}
