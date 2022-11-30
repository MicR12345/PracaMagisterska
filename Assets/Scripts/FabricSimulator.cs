using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FabricSimulator : MonoBehaviour
{
    public bool elastic = true;
    public float elasticity_k = 5.5f;

    Mesh mesh;
    Mesh meshCloned;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    public List<Point> points = new List<Point>();
    List<Connector> connectors = new List<Connector>();
    List<int> pointMap;
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;
        meshCloned = new Mesh();
        meshCloned.name = "clone";
        meshCloned.vertices = mesh.vertices;
        meshCloned.triangles = mesh.triangles;
        meshCloned.normals = mesh.normals;
        meshCloned.uv = mesh.uv;
        mesh = meshCloned;
        meshFilter.sharedMesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();
        Vector3[] verts = mesh.vertices;
        pointMap = new List<int>();
        TransformMeshIntoPoints(verts);
        int[] triangles = mesh.triangles;
        TransformTrianglesIntoConnectors(triangles,verts);
        AddForces();
    }
    public void TransformMeshIntoPoints(Vector3[] verts)
    {

        for (int i = 0; i < verts.Length; i++)
        {
            if (pointMap.)
            {

            }
            Point p = new Point(verts[i]);

            if (verts[i].y>0)
            {
                p.debugDisableMovement = true;
            }
            points.Add(p);
        }
    }
    public void TransformTrianglesIntoConnectors(int[] triangles,Vector3[] verts)
    {
        for (int i = 0; i < triangles.Length; i+=3)
        {
            if (!points[triangles[i]].connected.Contains(points[triangles[i+1]]))
            {
                Connector connector1 = new Connector(points[triangles[i]], points[triangles[i + 1]]);
                connectors.Add(connector1);
            }
            if (!points[triangles[i+1]].connected.Contains(points[triangles[i + 2]]))
            {
                Connector connector2 = new Connector(points[triangles[i + 1]], points[triangles[i + 2]]);
                connectors.Add(connector2);
            }
            if (!points[triangles[i]].connected.Contains(points[triangles[i + 2]]))
            {
                Connector connector3 = new Connector(points[triangles[i]], points[triangles[i + 2]]);
                connectors.Add(connector3);
            }
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
        Vector3[] newPoints = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            newPoints[i] = points[i].Position()[0];
        }
        mesh.vertices = newPoints;
    }
    private void FixedUpdate()
    {
        CreateNewMesh();
    }
}
