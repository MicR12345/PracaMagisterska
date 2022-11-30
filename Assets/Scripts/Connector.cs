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
    public ConnectorObject connectorScript;
    public void RecalculateForces()
    {
        foreach (Force f in forces)
        {
            (Effector e1, Effector e2) = f.GenerateEffectors();
            startPart.effectors.Add(e1);
            endPart.effectors.Add(e2);
        }
    }
    public Connector(PhysicsPart physicsPart1, PhysicsPart physicsPart2, int startConnectionNumber = 0,
        int endConnectionNumber = 0)
    {
        this.startPart = physicsPart1;
        this.endPart = physicsPart2;
        this.forces = new List<Force>();
        this.startHandleNumber = startConnectionNumber;
        this.endHandleNumber = endConnectionNumber;
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
        isInitialized = true;
    }
    private void FixedUpdate()
    {
        connectorHandle.RecalculateForces();
    }
    private void OnDrawGizmos()
    {
        if (isInitialized)
        {
            foreach (Vector3 item in connectorHandle.startPart.Position())
            {
                Gizmos.DrawSphere(item, 0.3f);
            }
            foreach (Vector3 item in connectorHandle.endPart.Position())
            {
                Gizmos.DrawSphere(item, 0.3f);
            }
            Gizmos.DrawLine(connectorHandle.startPart.Position()[0], connectorHandle.endPart.Position()[0]);
        }
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
    public List<Effector> effectors = new List<Effector>();
    public Vector3 forceDirection;
    public Vector3 movement = Vector3.zero;
    public List<PhysicsPart> connected = new List<PhysicsPart>();
    public void ApplyStaticEffectors()
    {
        foreach (Force force in staticForces)
        {
            effectors.Add(force.GenerateEffectors().Item1);
        }
    }
    /*public void CalculatePhysics()
    {
        if(effectors.Count > 1)
        {
            Vector3 force = Vector3.zero;
            for (int i = 1; i < effectors.Count; i++)
            {
                float angle = Vector3.Angle(Vector3.up,
                effectors[i].direction.normalized * effectors[i].strenght);
                force = force + effectors[i].strenght * Mathf.Cos(angle); 
            }
            for (int i = 2; i < effectors.Count; i++)
            {
                crossProduct = Vector3.Cross(crossProduct,
                effectors[i].direction.normalized * effectors[i].strenght);
            }
            forceDirection = crossProduct;
        }
        else if(effectors.Count == 1)
        {
            forceDirection = effectors[0].direction.normalized * effectors[0].strenght;
        }
        else
        {
            forceDirection = Vector3.zero;
        }
    }
    */

    public abstract List<Vector3> Position();
    public void CalculateMovement()
    {
        if (!debugDisableMovement)
        {
            foreach (Effector effector in effectors)
            {
                movement = movement * PhysicsPart.friction + effector.direction.normalized * effector.strenght;
            }
        }
        else
        {
            movement = Vector3.zero;
        }
        effectors.Clear();
    }
    /*public void CalculateMovement()
    {
        movement = movement * PhysicsPart.friction + forceDirection;
    }*/
    public abstract void ChangePosition();
}
