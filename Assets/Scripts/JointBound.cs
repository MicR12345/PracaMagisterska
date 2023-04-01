using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class JointBound : MonoBehaviour
{
    public Vector3 start = Vector3.zero;
    public Vector3 end = Vector3.zero;
    public GameObject startObject;
    public GameObject endObject;

    public bool isVisible = true;

    float minX = 0f;
    float maxX = 0f;

    float minY = 0f;
    float maxY = 0f;

    float minZ = 0f;
    float maxZ = 0f;
    private void OnDrawGizmos()
    {
        if (!isVisible)
        {
            return;
        }
        Gizmos.DrawLine(new Vector3(start.x, start.y, start.z),new Vector3(end.x,start.y,start.z));
        Gizmos.DrawLine(new Vector3(start.x, start.y, start.z), new Vector3(start.x, end.y, start.z));
        Gizmos.DrawLine(new Vector3(start.x, start.y, start.z), new Vector3(start.x, start.y, end.z));

        Gizmos.DrawLine(new Vector3(end.x, end.y, end.z), new Vector3(start.x, end.y, end.z));
        Gizmos.DrawLine(new Vector3(end.x, end.y, end.z), new Vector3(end.x, start.y, end.z));
        Gizmos.DrawLine(new Vector3(end.x, end.y, end.z), new Vector3(end.x, end.y, start.z));

        Gizmos.DrawLine(new Vector3(start.x, start.y, end.z), new Vector3(start.x, end.y, end.z));
        Gizmos.DrawLine(new Vector3(start.x, start.y, end.z), new Vector3(end.x, start.y, end.z));
        Gizmos.DrawLine(new Vector3(start.x, end.y, start.z), new Vector3(start.x, end.y, end.z));
        Gizmos.DrawLine(new Vector3(start.x, end.y, start.z), new Vector3(end.x, end.y, start.z));
        Gizmos.DrawLine(new Vector3(end.x, start.y, start.z), new Vector3(end.x, start.y, end.z));
        Gizmos.DrawLine(new Vector3(end.x, start.y, start.z), new Vector3(end.x, end.y, start.z));
    }
    private void Update()
    {
        if (startObject==null)
        {
            GameObject gameObject = new GameObject("Start");
            gameObject.transform.SetParent(transform);
            startObject = gameObject;
        }
        else
        {
            start = startObject.transform.position;
        }
        if (endObject == null)
        {
            GameObject gameObject = new GameObject("End");
            gameObject.transform.SetParent(transform);
            endObject = gameObject;
        }
        else
        {
            end = endObject.transform.position;
        }
        minX = Mathf.Min(start.x, end.x);
        maxX = Mathf.Max(start.x, end.x);

        minY = Mathf.Min(start.y, end.y);
        maxY = Mathf.Max(start.y, end.y);

        minZ = Mathf.Min(start.z, end.z);
        maxZ = Mathf.Max(start.z, end.z);
    }
    public bool IsInBounds(Vector3 point)
    {
        float minX = Mathf.Min(start.x,end.x);
        float maxX = Mathf.Max(start.x, end.x);

        float minY = Mathf.Min(start.y, end.y);
        float maxY = Mathf.Max(start.y, end.y);

        float minZ = Mathf.Min(start.z, end.z);
        float maxZ = Mathf.Max(start.z, end.z);
        if (point.x <= maxX && point.x >= minX && point.y <= maxY && point.y >= minY && point.z <= maxZ && point.z >= minZ)
        {
            return true;
        }
        return false;
    }
}
