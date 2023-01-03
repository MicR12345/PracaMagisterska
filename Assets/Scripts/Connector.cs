using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
public class Connector
{
    public PhysicsPart startPart;
    public PhysicsPart endPart;
    int startHandleNumber = 0;
    int endHandleNumber = 0;
    public List<Force> forces;
    public GameObject connectorObject;
    public GameObject connectedObject;
    public ConnectorObject connectorScript;
    public void RecalculateForces()
    {
        foreach (Force f in forces)
        {
            (Effector e1, Effector e2) = f.GenerateEffectors();
            startPart.movement = startPart.movement * PhysicsPart.friction + e1.direction.normalized * e1.strenght;
            endPart.movement = endPart.movement * PhysicsPart.friction + e2.direction.normalized * e2.strenght;
        }
    }
    public Connector(PhysicsPart physicsPart1, PhysicsPart physicsPart2,GameObject connectedObject, int startConnectionNumber = 0,
        int endConnectionNumber = 0)
    {
        this.startPart = physicsPart1;
        this.endPart = physicsPart2;
        this.forces = new List<Force>();
        this.startHandleNumber = startConnectionNumber;
        this.endHandleNumber = endConnectionNumber;
        this.connectedObject = connectedObject;
        physicsPart1.connected.Add(physicsPart2);
        physicsPart2.connected.Add(physicsPart1);
        GameObject gameObject = new GameObject("Connector");
        connectorScript = gameObject.AddComponent<ConnectorObject>();
        connectorScript.Initialize(this);
        connectorObject = gameObject;
    }
}
public class ConnectorObject: MonoBehaviour
{
    private bool isInitialized = false;
    public Connector connectorHandle;
    public void Initialize(Connector connector)
    {
        connectorHandle = connector;
        this.transform.parent = connector.connectedObject.transform;
        isInitialized = true;
    }
    int i = 0;
    private void FixedUpdate()
    {
        
        if (i==5)
        {
            connectorHandle.RecalculateForces();
            i = 0;
        }
        else
        {
            i++;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(connectorHandle.startPart.gameObject.transform.position + connectorHandle.startPart.Position()[0],
            connectorHandle.endPart.gameObject.transform.position + connectorHandle.endPart.Position()[0]);
        Gizmos.color = Color.gray;
    }
}
public abstract class Force
{
    public abstract (Effector,Effector) GenerateEffectors();
}
public abstract class PhysicsPart
{
    public bool debugDisableMovement = false;
    public static float friction = 0.92f;
    public static float timeScale = 3f;
    public List<Force> staticForces = new List<Force>();
    public Vector3 forceDirection;
    public Vector3 movement = Vector3.zero;
    public List<PhysicsPart> connected = new List<PhysicsPart>();
    public GameObject gameObject;
    public Mesh mesh;
    public void ApplyStaticEffectors()
    {
        foreach (Force force in staticForces)
        {
            Effector e = force.GenerateEffectors().Item1;
            movement = movement * PhysicsPart.friction + e.direction.normalized * e.strenght * PhysicsManager.WaitTime;
        }
    }
    public abstract List<Vector3> Position();
    public abstract void ChangePosition();
    public virtual float IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        const float kEpsilon = 0.000001f;
        // edges from v1 & v2 to v0.     
        Vector3 e1 = v1 - v0;
        Vector3 e2 = v2 - v0;

        Vector3 h = Vector3.Cross(ray.direction, e2);
        float a = Vector3.Dot(e1, h);
        if ((a > -kEpsilon) && (a < kEpsilon))
        {
            return float.NaN;
        }

        float f = 1.0f / a;

        Vector3 s = ray.origin - v0;
        float u = f * Vector3.Dot(s, h);
        if ((u < 0.0f) || (u > 1.0f))
        {
            return float.NaN;
        }

        Vector3 q = Vector3.Cross(s, e1);
        float v = f * Vector3.Dot(ray.direction, q);
        if ((v < 0.0f) || (u + v > 1.0f))
        {
            return float.NaN;
        }

        float t = f * Vector3.Dot(e2, q);
        if (t > kEpsilon)
        {
            return t;
        }
        else
        {
            return float.NaN;
        }
    }
    public static bool TriangleGreaterZThanPoint(Vector3 pos, int triangleIdx, Mesh mesh)
    {
        if (mesh.vertices[mesh.triangles[triangleIdx]].z >= pos.z || mesh.vertices[mesh.triangles[triangleIdx + 1]].z >= pos.z || mesh.vertices[mesh.triangles[triangleIdx + 2]].z >= pos.z)
        {
            return true;
        }
        return false;
    }
    public static bool TriangleGreaterXThanPoint(Vector3 pos, int triangleIdx, Mesh mesh)
    {
        if (mesh.vertices[mesh.triangles[triangleIdx]].x >= pos.y || mesh.vertices[mesh.triangles[triangleIdx + 1]].x >= pos.y || mesh.vertices[mesh.triangles[triangleIdx + 2]].x >= pos.y)
        {
            return true;
        }
        return false;
    }
    public static bool TriangleGreaterYThanPoint(Vector3 pos, int triangleIdx, Mesh mesh)
    {
        if (mesh.vertices[mesh.triangles[triangleIdx]].y >= pos.y || mesh.vertices[mesh.triangles[triangleIdx + 1]].y >= pos.y || mesh.vertices[mesh.triangles[triangleIdx + 2]].y >= pos.y)
        {
            return true;
        }
        return false;
    }
}
