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
    public Point(Vector3 pos,GameObject gameObject, ref Mesh mesh)
    {
        this.gameObject = gameObject;
        this.pos = pos;
        this.mesh = mesh;
    }
    public override void ChangePosition()
    {
        if (!debugDisableMovement)
        {
            int i = 0;
            Vector3 newPosition = pos + movement * Mathf.Pow(Time.deltaTime, 2) * 0.7f;
            while (CheckIfPointColidesWithAnyTriangle(newPosition))
            {
                newPosition = newPosition * 0.5f;
                i++;
                if (i == 10)
                    break;
            }

            if(i != 10)
            {
                pos = newPosition;
            }
        }
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
    public CombinedPoint(List<Point> pointss, ref Mesh mesh)
    {
        this.mesh = mesh;
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

            int i = 0;
            Vector3 newPosition = pos + movement * Mathf.Pow(Time.deltaTime, 2) * 0.7f;
            while (CheckIfPointColidesWithAnyTriangle(newPosition))
            {
                newPosition = newPosition * 0.5f;
                i++;
                if (i == 10)
                    break;
            }

            if (i != 10)
            {
                pos = newPosition;
            }

            foreach (Point item in points)
            {
                item.pos = pos;
            }
        }
    }
}
