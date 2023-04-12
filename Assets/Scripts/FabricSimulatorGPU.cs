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
    public int batycentricPointsIdx;
    ComputeBuffer pointBuffer;
    ComputeBuffer pointBufferOut;
    ComputeBuffer triangleBuffer;

    ComputeBuffer internalCollisionVelocitiesBuffer;

    ComputeBuffer debugBuffer;
    ComputeBuffer debugBuffer2;
    ComputeBuffer externalTrianglesBuffer;

    public List<InverseKinematics> IKs = new List<InverseKinematics>();

    Vector3[] debug;
    Vector3[] debug2;
    TriVert[] externalTriangles;
    List<TriVert> externalTrianglesFull;
    MeshFilter meshFilter;
    SkinnedMeshRenderer skinnedMeshRenderer;
    bool meshFilterFlag;
    public SimplePointStr[] pointData;
    public SimpleTriangle[] triangleData;
    bool FinishedInitializing = false;
    List<int> anchoredPoints = new List<int>();
    [SerializeField]
    public float ro = 1;
    public float k = 1;
    public float friction = 0.1f;
    public float timeScale = 1f;
    public ComputeShader forceComputeShader;
    public ComputeShader collisionComputeShader;
    public ComputeShader externalCollisionShader;
    public ComputeShader dynamicCollisionShader;
    public List<Anchor> anchors = new List<Anchor>();
    public List<MeshFilter> externalObjects = new List<MeshFilter> ();
    private void OnEnable()
    {
        externalTrianglesFull = new List<TriVert>();
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
        GenerateTriangleData();
        pointData = PrepareData().ToArray();
        
        int[] tri2 = new int[triangles.Length * 2];
        for (int i = 0; i < tri2.Length; i += 3)
        {
            if (i< triangles.Length)
            {
                tri2[i] = triangles[i];
                tri2[i+1] = triangles[i+1];
                tri2[i+2] = triangles[i+2];
            }
            else
            {
                int j = i - triangles.Length;
                tri2[i] = triangles[j + 2];
                tri2[i+1] = triangles[j+1];
                tri2[i+2] = triangles[j];
            }
        }
        mesh.triangles = tri2;
        
        foreach (Anchor item in anchors)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (Vector3.Distance(pointData[i].position + transform.position,item.gameObject.transform.position)<= item.range)
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
        int xd = sizeof(SimpleTriangle);
        RegisterExternalTriangles();
        externalTriangles = externalTrianglesFull.ToArray();
        forceComputeShader.SetFloat("k", k);
        forceComputeShader.SetFloat("friction", friction);
        pointBuffer = new ComputeBuffer(pointCount, sizeOfStruct,ComputeBufferType.Structured);
        pointBufferOut = new ComputeBuffer(pointCount, sizeOfStruct, ComputeBufferType.Structured);
        triangleBuffer = new ComputeBuffer(triangleData.Length,sizeof(SimpleTriangle),ComputeBufferType.Structured);
        debugBuffer = new ComputeBuffer(pointCount,sizeof(Vector3), ComputeBufferType.Structured);
        debugBuffer2 = new ComputeBuffer(pointCount, sizeof(Vector3), ComputeBufferType.Structured);
        externalTrianglesBuffer = new ComputeBuffer(Mathf.Max(externalTriangles.Length,1),sizeof(TriVert),ComputeBufferType.Structured);
        internalCollisionVelocitiesBuffer = new ComputeBuffer(pointCount, sizeof(Vector3), ComputeBufferType.Structured);
        forceComputeShader.SetBuffer(0, "SimplePoints", pointBuffer);
        forceComputeShader.SetBuffer(0, "SimplePointsOut", pointBufferOut);
        forceComputeShader.SetBuffer(0, "internalCollisions", internalCollisionVelocitiesBuffer);

        collisionComputeShader.SetBuffer(0, "SimplePoints", pointBufferOut);
        collisionComputeShader.SetBuffer(0, "SimplePointsOut", pointBuffer);
        collisionComputeShader.SetBuffer(0, "internalCollisions", internalCollisionVelocitiesBuffer);
        collisionComputeShader.SetBuffer(0, "Triangles", triangleBuffer);
        collisionComputeShader.SetInt("TriangleCount", triangles.Length);
        collisionComputeShader.SetBuffer(0, "Debug", debugBuffer);
        collisionComputeShader.SetBuffer(0, "Debug2", debugBuffer2);

        externalCollisionShader.SetBuffer(0, "SimplePoints", pointBuffer);
        externalCollisionShader.SetBuffer(0, "SimplePointsOut", pointBufferOut);
        externalCollisionShader.SetBuffer(0, "ExternalTriangles", externalTrianglesBuffer);
        externalCollisionShader.SetInt("ExtTriangleCount", externalTriangles.Length);
        externalTrianglesBuffer.SetData(externalTriangles);

        dynamicCollisionShader.SetBuffer(0, "SimplePoints", pointBufferOut);

        triangleBuffer.SetData(triangleData);
        pointBuffer.SetData(pointData);
        Vector3[] velocities = new Vector3[pointCount];
        internalCollisionVelocitiesBuffer.SetData(velocities);
        debug = new Vector3[pointCount];
        debug2 = new Vector3[pointCount];
        
        FinishedInitializing = true;
    }
    private void OnDisable()
    {
        pointBuffer.Release();
        pointBuffer = null;
        pointBufferOut.Release();
        pointBufferOut=null;
        triangleBuffer.Release();
        triangleBuffer = null;
        debugBuffer.Release();
        debugBuffer = null;
        debugBuffer2.Release();
        debugBuffer2 = null;
        externalTrianglesBuffer.Release();
        externalTriangles = null;
        internalCollisionVelocitiesBuffer.Release();
        internalCollisionVelocitiesBuffer = null;

    }
    private void FixedUpdate()
    {
        if (FinishedInitializing) 
        { 
            float t = Time.deltaTime * timeScale;
            externalCollisionShader.SetFloat("Time", t);
            collisionComputeShader.SetFloat("Time", t);
            forceComputeShader.SetFloat("Time", t);
            foreach (Anchor item in anchors)
            {
                for (int i = 0; i < item.managedPoints.Count; i++)
                {
                    pointData[item.managedPoints[i]].velocity = (item.gameObject.transform.position + item.pointVector[i]) - pointData[item.managedPoints[i]].position;
                }
            }
            pointBuffer.SetData(pointData);
            forceComputeShader.Dispatch(0, (pointCount / 128) + 1, 1, 1);
            collisionComputeShader.Dispatch(0, (pointCount / 128) + 1, 1, 1);
            externalCollisionShader.Dispatch(0, (pointCount / 128) + 1, 1, 1);
            foreach (InverseKinematics ik in IKs)
            {
                dynamicCollisionShader.SetBuffer(0, "triangles", ik.trianglesBuffer);
                dynamicCollisionShader.SetBuffer(0, "startPoints", ik.startingVerticsPositionsBuffer);
                dynamicCollisionShader.SetBuffer(0, "endPoints", ik.finishVerticesPositionBuffer);
                dynamicCollisionShader.Dispatch(0, (ik.numberOfAllTriangles / 128) + 1, 1, 1);
            }
            pointBufferOut.GetData(pointData);
            CreateNewMesh();
        }
    }/*
    private void OnDrawGizmos()
    {
        if (FinishedInitializing)
        {
            for (int i = 0; i < pointData.Length; i++)
            {
                for (int j = 0; j < pointData[i].connectorCount; j++)
                {
                    Gizmos.DrawLine(pointData[i].position, pointData[pointData[i].connectorPositions.number[j]].position);
                }
            }

        }
    }*/
    public void CreateNewMesh()
    {
        Mesh meshNew = new Mesh();
        int l = vertices.Length;
        Vector3[] newPoints = new Vector3[l];
        for (int i = 0; i < l; i++)
        {
            newPoints[i] = pointData[pointMap[i]].position - transform.position;
        }
        meshNew.vertices = newPoints;
        vertices = newPoints;
        meshNew.triangles = mesh.triangles;
        meshNew.uv = mesh.uv;
        meshNew.normals = mesh.normals;
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
                points.Add(new SimplePoint(this.gameObject, pointMap[vertMap[i]], vertices[vertMap[i]],0));
            }
            else
            {
                pointMap[i] = pointMap[vertMap[i]];
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
                this.gameObject,
                pointCount + i / 3,
                GetBarycentricPoint(vertices[triangleMap[i]], vertices[triangleMap[i + 1]], vertices[triangleMap[i + 2]]),
                GetTriangleSurfaceArea(vertices[triangleMap[i]], vertices[triangleMap[i + 1]], vertices[triangleMap[i + 2]]) * ro,
                i/3);
            points[pointMap[triangleMap[i]]].connections.Add(p);
            p.connections.Add(points[pointMap[triangleMap[i]]]);
            points[pointMap[triangleMap[i + 1]]].connections.Add(p);
            p.connections.Add(points[pointMap[triangleMap[i + 1]]]);
            points[pointMap[triangleMap[i + 2]]].connections.Add(p);
            p.connections.Add(points[pointMap[triangleMap[i + 2]]]);
            points.Add(p);

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
            points[pointMap[triangleMap[i]]].mass = points[pointMap[triangleMap[i]]].mass + (points[pointCount + i / 3].mass * 2f) / 9f;
            points[pointMap[triangleMap[i + 1]]].mass = points[pointMap[triangleMap[i + 1]]].mass + (points[pointCount + i / 3].mass * 2f) / 9f;
            points[pointMap[triangleMap[i + 2]]].mass = points[pointMap[triangleMap[i + 2]]].mass + (points[pointCount + i / 3].mass * 2f) / 9f;
            points[pointCount + i / 3].mass = points[pointCount + i / 3].mass / 3f;
        }
        batycentricPointsIdx = pointCount;
    }
    public void GenerateTriangleData()
    {
        triangleData = new SimpleTriangle[(pointCount- batycentricPointsIdx) * 3];
        for (int i = batycentricPointsIdx; i < pointCount; i ++)
        {
            int j = i - batycentricPointsIdx;
            triangleData[j*3] = new SimpleTriangle();
            triangleData[j * 3 + 1] = new SimpleTriangle();
            triangleData[j * 3 + 2] = new SimpleTriangle();
            triangleData[j * 3].t1 = points[i].connections[0].id;
            triangleData[j * 3].t2 = points[i].connections[1].id;
            triangleData[j * 3].t3 = i;
            triangleData[j * 3 + 1].t1 = points[i].connections[1].id;
            triangleData[j * 3 + 1].t2 = points[i].connections[2].id;
            triangleData[j * 3 + 1].t3 = i;
            triangleData[j * 3 + 2].t1 = points[i].connections[2].id;
            triangleData[j * 3 + 2].t2 = points[i].connections[0].id;
            triangleData[j * 3 + 2].t3 = i;
        }
    }
    public bool IsSamePoint(Vector3 a,Vector3 b, float tolerance = 0f)
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
            p.position = points[i].position + transform.position;
            p.mass = points[i].mass;
            p.velocity = Vector3.zero;
            p.connectorCount = points[i].connections.Count;
            p.ignorePhysics = 0;
            p.movePosition = Vector3.zero;
            Connectors c = new Connectors();
            for (int j = 0; j < 24; j++)
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
    void RegisterExternalTriangles()
    {
        
        foreach (MeshFilter item in externalObjects)
        {
            Vector3 dp = item.gameObject.transform.position;
            Vector3[] verts = item.mesh.vertices;
            int[] triangles = item.mesh.triangles;
            for (int i = 0; i < triangles.Length; i = i + 3)
            {
                TriVert triVert = new TriVert();
                triVert.t1 = verts[triangles[i]] + dp;
                triVert.t2 = verts[triangles[i+1]] + dp;
                triVert.t3 = verts[triangles[i+2]] + dp;
                externalTrianglesFull.Add(triVert);
            }
        }
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
    public GameObject gameObject;
    public SimplePoint(GameObject gameObject,int id,Vector3 position,float mass, int isBarycentric = -1)
    {
        this.gameObject = gameObject;
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
public unsafe struct Connectors
{
    public fixed float connectionL[24];
    public fixed int number[24];
}
public struct SimpleTriangle
{
    public int t1;
    public int t2;
    public int t3;
}
public struct TriVert
{
    public Vector3 t1;
    public Vector3 t2;
    public Vector3 t3;
}