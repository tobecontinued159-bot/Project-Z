using UnityEngine;

public class LevelMarkerGizmo : MonoBehaviour
{
    public Color gizmoColor = Color.white;
    public float gizmoRadius = 0.35f;
    public string gizmoLabel = "";

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.6f);
    }
}
