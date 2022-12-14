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
    public static float friction = 0.99f;
    public List<Force> staticForces = new List<Force>();
    public Vector3 forceDirection;
    public Vector3 movement = Vector3.zero;
    public List<PhysicsPart> connected = new List<PhysicsPart>();
    public GameObject gameObject;
    public void ApplyStaticEffectors()
    {
        foreach (Force force in staticForces)
        {
            Effector e = force.GenerateEffectors().Item1;
            movement = movement * PhysicsPart.friction + e.direction.normalized * e.strenght;
        }
    }
    public abstract List<Vector3> Position();
    public abstract void ChangePosition();
}
