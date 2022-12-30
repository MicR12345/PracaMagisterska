using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class FabricSimulator : MonoBehaviour
{
    public bool elastic = true;
    public float elasticity_k = 5.5f;

    Mesh mesh;
    Mesh meshCloned;
    SkinnedMeshRenderer skinnedMeshRenderer;
    public List<Point> points = new List<Point>();
    public int[] CombinedPointsMap;
    public List<Connector> connectors = new List<Connector>();
    public List<CombinedPoint> combinedPoints = new List<CombinedPoint>();
    private void Start()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        mesh = skinnedMeshRenderer.sharedMesh;
        meshCloned = new Mesh();
        meshCloned.name = "clone";
        meshCloned.vertices = mesh.vertices;
        meshCloned.triangles = mesh.triangles;
        meshCloned.normals = mesh.normals;
        meshCloned.uv = mesh.uv;
        mesh = meshCloned;
        skinnedMeshRenderer.sharedMesh = mesh;
        Vector3[] verts = mesh.vertices;
        TransformMeshIntoPoints(verts);
        int[] triangles = mesh.triangles;
        TransformTrianglesIntoConnectors(triangles, verts);
        AddForces();
    }
    public void TransformMeshIntoPoints(Vector3[] verts)
    {
        for (int i = 0; i < verts.Length; i++)
        {
            Point p = new Point(verts[i],this.gameObject,ref mesh);
            points.Add(p);
        }
        CombinedPointsMap = new int[points.Count];
        bool[] repeatedPoint = new bool[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            if (i==0)
            {
                points[i].debugDisableMovement = true;
            }
            if (!repeatedPoint[i])
            {
                CombinedPointsMap[i] = i;
                List<Point> list = new List<Point>();
                for (int j = 0; j < points.Count; j++)
                {
                    if (points[i].pos == points[j].pos && !repeatedPoint[j])
                    {
                        list.Add(points[j]);
                        repeatedPoint[j] = true;
                        CombinedPointsMap[j] = i;
                    }
                }
                CombinedPoint combinedPoint = new CombinedPoint(list,ref mesh);
                combinedPoint.staticForces.Add(new Gravity());
                combinedPoints.Add(combinedPoint);
            }
        }
        for (int i = 0; i < points.Count; i++)
        {
            if (!repeatedPoint[i])
            {
                PhysicsManager.activePhysicsParts.Add(points[i]);
            }
        }
    }
    public void TransformTrianglesIntoConnectors(int[] triangles,Vector3[] verts)
    {
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (!points[CombinedPointsMap[triangles[i]]].connected.Contains(points[CombinedPointsMap[triangles[i+1]]]))
            {
                Connector connector = new Connector(points[CombinedPointsMap[triangles[i]]], points[CombinedPointsMap[triangles[i + 1]]], this.gameObject);
                connectors.Add(connector);
            }
            if (!points[triangles[i]].connected.Contains(points[CombinedPointsMap[triangles[i + 2]]]))
            {
                Connector connector = new Connector(points[CombinedPointsMap[triangles[i]]], points[CombinedPointsMap[triangles[i + 2]]], this.gameObject);
                connectors.Add(connector);
            }
            if (!points[CombinedPointsMap[triangles[i+1]]].connected.Contains(points[CombinedPointsMap[triangles[i + 2]]]))
            {
                Connector connector = new Connector(points[CombinedPointsMap[triangles[i+1]]], points[CombinedPointsMap[triangles[i + 2]]], this.gameObject);
                connectors.Add(connector);
            }
        }
        List<(int,int)> MissingConnections = new List<(int, int)> ();
        for (int i = 0; i < triangles.Length; i+=3)
        {
            for (int j = 0; j < triangles.Length; j+=3)
            {
                if (i==j)
                {
                    continue;
                }
                else
                {
                    if (CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i +1]] == CombinedPointsMap[triangles[j +1]] 
                        || CombinedPointsMap[triangles[i + 1]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j + 1]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i + 2]], CombinedPointsMap[triangles[j + 2]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j + 2]], CombinedPointsMap[triangles[i + 2]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    else if(CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j +1]] && CombinedPointsMap[triangles[i + 1]] == CombinedPointsMap[triangles[j + 2]] 
                        || CombinedPointsMap[triangles[i+1]] == CombinedPointsMap[triangles[j + 1]] && CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j + 2]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i + 2]], CombinedPointsMap[triangles[j]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j]], CombinedPointsMap[triangles[i + 2]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    else if (CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i + 1]] == CombinedPointsMap[triangles[j + 2]] 
                        || CombinedPointsMap[triangles[i+ 1]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j + 2]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i + 2]], CombinedPointsMap[triangles[j + 1]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j + 1]], CombinedPointsMap[triangles[i + 2]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    if (CombinedPointsMap[triangles[i +1]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j + 1]] 
                        || CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i + 1]] == CombinedPointsMap[triangles[j + 1]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i]], CombinedPointsMap[triangles[j + 2]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j + 2]], CombinedPointsMap[triangles[i]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    else if (CombinedPointsMap[triangles[i +1]] == CombinedPointsMap[triangles[j + 1]] && CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j + 2]] 
                        || CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j + 1]] && CombinedPointsMap[triangles[i + 1]] == CombinedPointsMap[triangles[j + 2]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i]], CombinedPointsMap[triangles[j]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j]], CombinedPointsMap[triangles[i]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    else if (CombinedPointsMap[triangles[i +1]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j + 2]] 
                        || CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i + 1]] == CombinedPointsMap[triangles[j + 2]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i]], CombinedPointsMap[triangles[j + 1]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j + 1]], CombinedPointsMap[triangles[i]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    
                    if (CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j + 1]] 
                        || CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j + 1]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i + 1]], CombinedPointsMap[triangles[j + 2]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j + 2]], CombinedPointsMap[triangles[i + 1]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    else if (CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j + 1]] && CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j + 2]]
                        || CombinedPointsMap[triangles[i+ 2]] == CombinedPointsMap[triangles[j + 1]] && CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j + 2]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i + 1]], CombinedPointsMap[triangles[j]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j]], CombinedPointsMap[triangles[i + 1]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    else if (CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j + 2]]
                        || CombinedPointsMap[triangles[i + 2]] == CombinedPointsMap[triangles[j]] && CombinedPointsMap[triangles[i]] == CombinedPointsMap[triangles[j + 2]])
                    {
                        (int, int) c = (CombinedPointsMap[triangles[i + 1]], CombinedPointsMap[triangles[j + 1]]);
                        (int, int) c2 = (CombinedPointsMap[triangles[j + 1]], CombinedPointsMap[triangles[i + 1]]);
                        if (!MissingConnections.Contains(c))
                        {
                            MissingConnections.Add(c);
                            MissingConnections.Add(c2);
                        }
                    }
                    
                }
            }
        }
        for (int i = 0; i < MissingConnections.Count; i+=2)
        {
            Connector connector = new Connector(points[MissingConnections[i].Item1], points[MissingConnections[i].Item2], this.gameObject);
            connectors.Add(connector);
        }
    }
    public void AddForces()
    {
        foreach (Point p in points)
        {
            p.staticForces.Add(new Gravity());
        }
        foreach (Connector c in connectors)
        {
            c.forces.Add(new Elasticity(c.startPart, c.endPart, elasticity_k, Vector3.Distance(c.startPart.Position()[0], c.endPart.Position()[0])));
        }
    }
    public void CreateNewMesh()
    {
        int l = mesh.vertices.Length;
        Vector3[] newPoints = new Vector3[l];
        for (int i = 0; i < l; i++)
        {
            newPoints[i] = points[i].Position()[0];
        }
        mesh.vertices = newPoints;
    }
    private void FixedUpdate()
    {
        CreateNewMesh();
        float maxDist = 0;
        float minDist = 0;
        Vector3 transformVec = Vector3.zero;
        Vector3 transformVec2 = Vector3.zero;
        for (int i = 0; i < points.Count; i++)
        {
            if (i==0)
            {
                maxDist = points[0].Position()[0].x + points[0].Position()[0].y + points[0].Position()[0].z;
                minDist = points[0].Position()[0].x + points[0].Position()[0].y + points[0].Position()[0].z;
                transformVec = points[0].Position()[0];
                transformVec2 = points[0].Position()[0];
            }
            float thisDist = points[i].Position()[0].x + points[i].Position()[0].y + points[i].Position()[0].z;
            if (thisDist < minDist)
            {
                minDist = thisDist;
                transformVec = points[i].Position()[0];
            }
            if (thisDist > maxDist)
            {
                maxDist = thisDist;
                transformVec2 = points[i].Position()[0];
            }
        }
        Vector3 moveVector = (transformVec + transformVec2)/2;
        foreach (Point item in points)
        {
            item.pos = item.pos - moveVector;
        }
        foreach (CombinedPoint item in combinedPoints)
        {
            item.pos = item.pos - moveVector;
        }
        transform.position = transform.position + moveVector;
        Bounds bounds = new Bounds();
        bounds.center = moveVector;
        bounds.size = transformVec - transformVec2;
        skinnedMeshRenderer.localBounds = bounds;
    }
}
