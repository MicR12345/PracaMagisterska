using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Joint : PhysicsPart
{
    public Vector3 start;
    public Vector3 end;
    public override List<Vector3> Position()
    {
        List<Vector3> list = new List<Vector3>();
        list.Add(start);
        list.Add(end);
        return list;
    }
    public Joint(Vector3 start, Vector3 end)
    {
        PhysicsManager.activePhysicsParts.Add(this);
        this.end = end;
        this.start = start;
    }
    public override void ChangePosition()
    {
        start = start + movement * Time.deltaTime;
        end = end + movement * Time.deltaTime;
    }
}
