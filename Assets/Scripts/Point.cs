using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Effector
{
    public Vector3 direction;
    public float strenght;
    public Effector(Vector3 direction, float strenght)
    {
        this.direction = direction;
        this.strenght = strenght;
    }
}
public class Point: PhysicsPart
{
    public Vector3 pos;
    public List<int> VertsIndexes = new List<int>();
    public override List<Vector3> Position()
    {
        List<Vector3> list = new List<Vector3>();
        list.Add(pos);
        return list;
    }
    public Point(Vector3 pos)
    {
        PhysicsManager.activePhysicsParts.Add(this);
        this.pos = pos;
    }
    public override void ChangePosition()
    {
        pos = pos + movement * Time.deltaTime * 0.1f;
    }
}
