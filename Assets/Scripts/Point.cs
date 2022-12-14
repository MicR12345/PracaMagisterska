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
    public override List<Vector3> Position()
    {
        List<Vector3> list = new List<Vector3>();
        list.Add(pos);
        return list;
    }
    public Point(Vector3 pos,GameObject gameObject)
    {
        this.gameObject = gameObject;
        this.pos = pos;
    }
    public override void ChangePosition()
    {
        if(!debugDisableMovement)
        pos = pos + movement * Time.deltaTime * 0.1f;
    }
}
public class CombinedPoint : PhysicsPart
{
    public Vector3 pos;
    public List<Point> points = new List<Point>();
    public override List<Vector3> Position()
    {
        List<Vector3> list = new List<Vector3>();
        list.Add(pos);
        return list;
    }
    public CombinedPoint(List<Point> pointss)
    {
        gameObject = pointss[0].gameObject;
        foreach (Point item in pointss)
        {
            this.points.Add(item);
            if (item.debugDisableMovement)
            {
                debugDisableMovement = true;
            }
            item.debugDisableMovement = true;
        }
        PhysicsManager.activePhysicsParts.Add(this);
        this.pos = pointss[0].pos;
    }
    public override void ChangePosition()
    {
        if (!debugDisableMovement)
        {
            foreach (Point item in points)
            {
                movement = movement + item.movement;
                item.movement = Vector3.zero;
            }

            pos = pos + movement * Time.deltaTime * 0.1f;
            foreach (Point item in points)
            {
                item.pos = pos;
            }
        }
    }
}
