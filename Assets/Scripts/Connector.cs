using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Connector : MonoBehaviour
{
    public PhysicsPart startPart;
    public PhysicsPart endPart;
    public List<Force> forces;
    public void RecalculatePoints()
    {
        if (forces.Count > 0)
        {
            foreach (Force force in forces)
            {
                force.CalculateForce();
            }
        }
        else Debug.LogError("No forces found");
    }
}
public abstract class Force
{
    public List<Force> forces;
    public abstract void CalculateForce();
}
public abstract class PhysicsPart
{
    public List<Effectors> effectors;
    public Vector3 forceDirection;
    public float strenght;
    public void CalculatePhysics()
    {
        //TODO
    }
}
