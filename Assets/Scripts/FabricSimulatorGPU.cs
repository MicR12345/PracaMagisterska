using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public unsafe class FabricSimulatorGPU : MonoBehaviour
{
    Mesh mesh;
    List<SimplePoint> points = new List<SimplePoint>();
    int[] vertMap;
    int[] triangleMap;
    int[] pointMap;
    Vector3[] vertices;
    int[] triangles;
    int pointCount;
    ComputeBuffer pointBuffer;
    ComputeBuffer pointBufferOut;
    MeshFilter meshFilter;
    SkinnedMeshRenderer skinnedMeshRenderer;
    bool meshFilterFlag;
    public SimplePointStr[] pointData;
    bool FinishedInitializing = false;
    List<int> anchoredPoints = new List<int>();
    [SerializeField]
    public float ro = 1;
    public float k = 1;
    public float friction = 0.1f;
    public ComputeShader computeShader;
    public List<Anchor> anchors = new List<Anchor>();
    private void OnEnable()
    {
        meshFilterFlag = TryGetComponent<MeshFilter>(out meshFilter);
        if (!meshFilterFlag)
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        }
        if (meshFilterFlag)
        {
            mesh = meshFilter.sharedMesh;
        }
        else
        {
            mesh = skinnedMeshRenderer.sharedMesh;
        }
        Mesh meshCloned;
        meshCloned = new Mesh();
        meshCloned.name = "clone";
        meshCloned.vertices = mesh.vertices;
        meshCloned.triangles = mesh.triangles;
        meshCloned.normals = mesh.normals;
        meshCloned.uv = mesh.uv;
        mesh = meshCloned;
        vertices = mesh.vertices;
        triangles = mesh.triangles;
        CalculatePoints();
        pointCount = points.Count;

        pointData = PrepareData().ToArray();
        foreach (Anchor item in anchors)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (Vector3.Distance(pointData[i].position,item.gameObject.transform.position)<= item.range)
                {
                    Vector3 v = pointData[i].position - item.gameObject.transform.position;
                    pointData[i].ignorePhysics = 1;
                    pointData[i].movePosition = item.gameObject.transform.position + v;
                    item.managedPoints.Add(i);
                    item.pointVector.Add(v);
                }
            }
        }
        int sizeOfStruct = sizeof(SimplePointStr);


        computeShader.SetFloat("k", k);
        computeShader.SetFloat("friction", friction);
        if (pointCount > 4096)
        {
            pointBuffer = new ComputeBuffer(4096, sizeOfStruct);
            pointBufferOut = new ComputeBuffer(4096, sizeOfStruct);
            computeShader.SetBuffer(0, "SimplePoints", pointBuffer);
            computeShader.SetBuffer(0, "SimplePointsOut", pointBufferOut);
            for (int i = 0; i < (pointCount/4096) + 1; i++)
            {
                int m = Mathf.Min(pointCount - i*4096,4096);
                SimplePointStr[] pointData2 = new SimplePointStr[4096];
                for (int j = 0; j < m; j++)
                {
                    pointData2[j] = pointData[i + j];
                }
                pointBuffer.SetData(pointData2);
                computeShader.Dispatch(0, (m / 32) + 1, 1, 1);
                pointBufferOut.GetData(pointData2);
                for (int j = 0; j < m; j++)
                {
                    pointData[i + j] = pointData2[j];
                }
            }
        }
        else
        {
            pointBuffer = new ComputeBuffer(pointCount, sizeOfStruct);
            pointBufferOut = new ComputeBuffer(pointCount, sizeOfStruct);
            computeShader.SetBuffer(0, "SimplePoints", pointBuffer);
            computeShader.SetBuffer(0, "SimplePointsOut", pointBufferOut);
            pointBuffer.SetData(pointData);
            computeShader.Dispatch(0, (pointCount / 32) + 1, 1, 1);
        }
        
        
        FinishedInitializing = true;
    }
    private void OnDisable()
    {
        pointBuffer.Release();
        pointBuffer = null;
        pointBufferOut.Release();
        pointBufferOut=null;
    }
    private void FixedUpdate()
    {
        if (FinishedInitializing)
        {
            computeShader.SetFloat("Time", Time.deltaTime);
            if (pointCount > 4096)
            {
                for (int i = 0; i < (pointCount / 4096) + 1; i++)
                {
                    int m = Mathf.Min(pointCount - i * 4096, 4096);
                    SimplePointStr[] pointData2 = new SimplePointStr[4096];
                    for (int j = 0; j < m; j++)
                    {
                        pointData2[j] = pointData[i + j];
                    }
                    pointBuffer.SetData(pointData2);
                    computeShader.Dispatch(0, (m / 32) + 1, 1, 1);
                    pointBufferOut.GetData(pointData2);
                    for (int j = 0; j < m; j++)
                    {
                        pointData[i + j] = pointData2[j];
                    }
                }
            }
            else
            {
                pointBuffer.SetData(pointData);
                computeShader.Dispatch(0, (pointCount / 32) + 1, 1, 1);
                pointBufferOut.GetData(pointData);
            }
            foreach (Anchor item in anchors)
            {
                for (int i = 0; i < item.managedPoints.Count; i++)
                {
                    pointData[item.managedPoints[i]].movePosition = item.gameObject.transform.position + item.pointVector[i];
                }
            }
            CreateNewMesh();
        }
    }
    public void CreateNewMesh()
    {
        Mesh meshNew = new Mesh();
        int l = vertices.Length;
        Vector3[] newPoints = new Vector3[l];
        for (int i = 0; i < l; i++)
        {
            newPoints[i] = pointData[i].position;
        }
        meshNew.vertices = newPoints;
        vertices = newPoints;
        meshNew.triangles = mesh.triangles;
        meshNew.uv = mesh.uv;
        if (meshFilterFlag)
        {
            meshFilter.sharedMesh = meshNew;
        }
        else
        {
            skinnedMeshRenderer.sharedMesh = meshNew;
        }
        mesh = meshNew;
    }
    public void CalculatePoints()
    {
        //Merge simmilar points, map them
        vertMap = new int[vertices.Length];
        for (int i = 0; i < vertMap.Length; i++)
        {
            vertMap[i] = i;
        }
        for (int i = 0; i < vertMap.Length; i++)
        {
            if (vertMap[i]==i)
            {
                for (int j = i + 1; j < vertMap.Length; j++)
                {
                    if (IsSamePoint(vertices[vertMap[i]], vertices[vertMap[j]]))
                    {
                        vertMap[j] = i;
                    }
                }
            }
        }
        //Map triangles
        triangleMap = new int[triangles.Length];
        for (int i = 0; i < triangleMap.Length; i++)
        {
            triangleMap[i] = vertMap[triangles[i]];
        }
        //Create points
        pointMap = new int[vertices.Length];
        for (int i = 0; i < vertMap.Length; i++)
        {
            if (vertMap[i] == i)
            {
                pointMap[i] = points.Count;
                points.Add(new SimplePoint(vertMap[i], vertices[vertMap[i]],0));
            }
        }
        //Connect points within triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (!points[pointMap[triangleMap[i]]].connections.Contains(points[pointMap[triangleMap[i+1]]]))
            {
                points[pointMap[triangleMap[i]]].connections.Add(points[pointMap[triangleMap[i + 1]]]);
                points[pointMap[triangleMap[i + 1]]].connections.Add(points[pointMap[triangleMap[i]]]);
            }
            if (!points[pointMap[triangleMap[i + 1]]].connections.Contains(points[pointMap[triangleMap[i + 2]]]))
            {
                points[pointMap[triangleMap[i + 1]]].connections.Add(points[pointMap[triangleMap[i + 2]]]);
                points[pointMap[triangleMap[i + 2]]].connections.Add(points[pointMap[triangleMap[i + 1]]]);
            }
            if (!points[pointMap[triangleMap[i + 2]]].connections.Contains(points[pointMap[triangleMap[i]]]))
            {
                points[pointMap[triangleMap[i + 2]]].connections.Add(points[pointMap[triangleMap[i]]]);
                points[pointMap[triangleMap[i]]].connections.Add(points[pointMap[triangleMap[i + 2]]]);
            }
        }
        int pointCount = points.Count;
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {

            SimplePoint p = new SimplePoint(
                pointCount + i / 3,
                GetBarycentricPoint(vertices[triangleMap[i]], vertices[triangleMap[i + 1]], vertices[triangleMap[i + 2]]),
                GetTriangleSurfaceArea(vertices[triangleMap[i]], vertices[triangleMap[i + 1]], vertices[triangleMap[i + 2]]),
                i/3);
            points.Add(p);
            points[triangleMap[i]].connections.Add(p);
            points[p.id].connections.Add(points[triangleMap[i]]);
            points[triangleMap[i + 1]].connections.Add(p);
            points[p.id].connections.Add(points[triangleMap[i+1]]);
            points[triangleMap[i + 2]].connections.Add(p);
            points[p.id].connections.Add(points[triangleMap[i+2]]);
        }
        for (int i = pointCount; i < points.Count; i++)
        {
            for (int j = i+1; j < points.Count; j++)
            {

                if (points[i].DoesShareNonBarycentricConnections(points[j]) && !points[i].connections.Contains(points[j]))
                {
                    points[i].connections.Add(points[j]);
                    points[j].connections.Add(points[i]);
                }
            }
        }
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            points[triangleMap[i]].mass = points[triangleMap[i]].mass + (points[pointCount + i / 3].mass * 2f) / 9f;
            points[triangleMap[i + 1]].mass = points[triangleMap[i + 1]].mass + (points[pointCount + i / 3].mass * 2f) / 9f;
            points[triangleMap[i + 2]].mass = points[triangleMap[i + 2]].mass + (points[pointCount + i / 3].mass * 2f) / 9f;
            points[pointCount + i / 3].mass = points[pointCount + i / 3].mass / 3f;
        }
    }
    public bool IsSamePoint(Vector3 a,Vector3 b, float tolerance = 0.00001f)
    {
        if (a.x + tolerance>= b.x && a.x - tolerance <= b.x 
            && a.y + tolerance >= b.y && a.y - tolerance <= b.y 
            && a.z + tolerance >= b.z && a.z - tolerance <= b.z)
        {
            return true;
        }
        return false;
    }
    public Vector3 GetBarycentricPoint(Vector3 t1,Vector3 t2, Vector3 t3)
    {
        return (t1 + t2 + t3) / 3f;
    }
    public float GetTriangleSurfaceArea(Vector3 t1, Vector3 t2, Vector3 t3)
    {
        Vector3 a = t1 - t2;
        Vector3 b = t2 - t3;
        Vector3 c = t3 - t1;
        float s = (a.magnitude + b.magnitude + c.magnitude) / 2f;
        return Mathf.Sqrt(s * ((s - a.magnitude) * (s - b.magnitude) * (s - c.magnitude)));
    }
    public List<SimplePointStr> PrepareData()
    {
        List<SimplePointStr> data = new List<SimplePointStr>();
        for (int i = 0; i < points.Count; i++)
        {
            SimplePointStr p = new SimplePointStr();
            p.position = points[i].position;
            p.mass = points[i].mass;
            p.velocity = Vector3.zero;
            p.connectorCount = points[i].connections.Count;
            p.ignorePhysics = 0;
            p.movePosition = Vector3.zero;
            Connectors c = new Connectors();
            for (int j = 0; j < 12; j++)
            {
                if (j<points[i].connections.Count)
                {
                    c.connectionL[j] = Vector3.Distance(points[i].position, points[i].connections[j].position);
                    c.number[j] = points[i].connections[j].id;
                }
                
            }
            p.connectorPositions = c;
            data.Add(p);
        }
        return data;
    }
}
//Class to prepare data
public class SimplePoint
{
    public int id;
    public Vector3 position;
    public List<SimplePoint> connections = new List<SimplePoint>();
    public int isBarycentric;
    public float mass = 0;
    public SimplePoint(int id,Vector3 position,float mass, int isBarycentric = -1)
    {
        this.id = id;
        this.position = position;
        this.isBarycentric= isBarycentric;
        this.mass = mass;
    }
    public bool DoesShareNonBarycentricConnections(SimplePoint p)
    {
        int sum = 0;
        for (int i = 0; i < p.connections.Count; i++)
        {
            if (connections.Contains(p.connections[i]) && p.connections[i].isBarycentric<=0)
            {
                sum++;
            }
            if (sum >= 2)
            {
                return true;
            }
        }
        return false;
    }
}
public struct SimplePointStr
{
    public Vector3 position;
    public float mass;
    public int connectorCount;
    public int ignorePhysics;
    public Vector3 movePosition;
    public Vector3 velocity;
    public Connectors connectorPositions;
}
//Size of buffer has to be constant
//Only broken mesh can have more than 12 connections
public unsafe struct Connectors
{
    public fixed float connectionL[12];
    public fixed int number[12];
}
