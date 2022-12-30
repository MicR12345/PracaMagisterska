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
        this.mesh = mesh;
        this.gameObject = gameObject;
        this.pos = pos;
    }
    public override void ChangePosition()
    {
        if(!debugDisableMovement)
            if (Mathf.Abs(movement.x + movement.y + movement.z) > PhysicsManager.movementToleranceDampening)
            {
                Ray ray = new Ray(pos, movement);
                float distance = float.NaN;
                for (int i = 0; i < mesh.triangles.Length; i = i + 3)
                {
                    if (movement.y>=0)
                    {
                        if (mesh.vertices[mesh.triangles[i]].y>=pos.y || mesh.vertices[mesh.triangles[i+1]].y >= pos.y || mesh.vertices[mesh.triangles[i+2]].y >= pos.y)
                        {
                            float maxdist = IntersectRayTriangle(ray, mesh.vertices[mesh.triangles[i]], mesh.vertices[mesh.triangles[i + 1]], mesh.vertices[mesh.triangles[i + 2]]);
                        }
                    }
                    else
                    {
                        if (mesh.vertices[mesh.triangles[i]].y <= pos.y || mesh.vertices[mesh.triangles[i + 1]].y <= pos.y || mesh.vertices[mesh.triangles[i + 2]].y <= pos.y)
                        {
                            float maxdist = IntersectRayTriangle(ray, mesh.vertices[mesh.triangles[i]], mesh.vertices[mesh.triangles[i + 1]], mesh.vertices[mesh.triangles[i + 2]]);
                        }
                    }

                }
                if (distance< movement.magnitude * Mathf.Pow(Time.deltaTime, 2) * PhysicsPart.timeScale)
                {
                    pos = pos + movement.normalized * distance;
                }
                else
                {
                    pos = pos + movement * Mathf.Pow(Time.deltaTime, 2) * PhysicsPart.timeScale;
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
    public CombinedPoint(List<Point> pointss,ref Mesh mesh)
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
            if (Mathf.Abs(movement.x + movement.y + movement.z) > PhysicsManager.movementToleranceDampening)
            {
                if (Mathf.Abs(movement.x + movement.y + movement.z) > PhysicsManager.movementToleranceDampening)
                {
                    Ray ray = new Ray(pos, movement);
                    float distance = float.NaN;
                    for (int i = 0; i < mesh.triangles.Length; i = i + 3)
                    {
                        if (movement.y >= 0)
                        {
                            if (mesh.vertices[mesh.triangles[i]].y >= pos.y || mesh.vertices[mesh.triangles[i + 1]].y >= pos.y || mesh.vertices[mesh.triangles[i + 2]].y >= pos.y)
                            {
                                float maxdist = IntersectRayTriangle(ray, mesh.vertices[mesh.triangles[i]], mesh.vertices[mesh.triangles[i + 1]], mesh.vertices[mesh.triangles[i + 2]]);
                            }
                        }
                        else
                        {
                            if (mesh.vertices[mesh.triangles[i]].y <= pos.y || mesh.vertices[mesh.triangles[i + 1]].y <= pos.y || mesh.vertices[mesh.triangles[i + 2]].y <= pos.y)
                            {
                                float maxdist = IntersectRayTriangle(ray, mesh.vertices[mesh.triangles[i]], mesh.vertices[mesh.triangles[i + 1]], mesh.vertices[mesh.triangles[i + 2]]);
                            }
                        }

                    }
                    //if (distance < movement.magnitude * Mathf.Pow(Time.deltaTime, 2) * PhysicsPart.timeScale)
                    //{
                        //pos = pos + movement.normalized * distance;
                    //}
                    //else
                    //{
                        pos = pos + movement * Mathf.Pow(Time.deltaTime, 2) * PhysicsPart.timeScale;
                    //}

                }
            }
            foreach (Point item in points)
            {
                item.pos = pos;
            }
        }
    }
}
