using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;                         // Boat_Agent
    public Vector3 offset = new Vector3(0f, 6f, -14f); // dietro e sopra la barca (spazio locale barca)
    public float posSmooth = 3f;                     // più alto = segue più rigido
    public float rotSmooth = 4f;
    public float lookHeight = 1f;

    void LateUpdate()
    {
        if (target == null) return;
        // offset ruotato solo sullo yaw della barca: niente rollio/beccheggio della camera
        Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        Vector3 desired = target.position + yaw * offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-posSmooth * Time.deltaTime));
        Quaternion look = Quaternion.LookRotation(target.position + Vector3.up * lookHeight - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-rotSmooth * Time.deltaTime));
    }
}