using UnityEngine;
public class SpawnMarker : MonoBehaviour
{
    public Color color = Color.green;
    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, new Vector3(.65f, 1.25f, 0));
        Gizmos.DrawLine(transform.position + Vector3.left * .4f, transform.position + Vector3.right * .4f);
    }
}
