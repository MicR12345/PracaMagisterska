using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

public unsafe class InverseKinematics : MonoBehaviour
{
    public Transform[] transforms;
    public Transform ikTarget;
    public int iterations = 10;
    public float tolerance = 0.05f;
    private Transform[] jointTransforms;
    private Vector3 startPosition;
    private Vector3[] jointPositions;
    private float[] boneLength;
    private float jointChainLength;
    private float distanceToTarget;
    private Quaternion[] startRotation;
    private Vector3[] jointStartDirection;
    private Quaternion ikTargetStartRot;
    private Quaternion lastJointStartRot;
    public GameObject CubeBorders;
    public List<float?[,]> MeshVerticesAssignations = new List<float?[,]>();
    private SkinnedMeshRenderer[] skinnedMeshes;
    public float fuzziness = (float)0.3;
    private Vector3[] OriginalPositionsOfJoints;
    private Quaternion[] OriginalRotationsOfJoints;
    ComputeBuffer trianglesBuffer;
    ComputeBuffer startingVerticsPositionsBuffer;
    ComputeBuffer finishVerticesPositionBuffer;
    private int numberOfAllVertices = 0;
    private int numberOfAllTriangles = 0;
    public List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();

    [Header("Pole target (3 joint chain)")]
    public Transform poleTarget;

    [Header("Debug")]
    public bool debugJoints = true;
    public bool localRotationAxis = false;

    // Remove this if you need bigger gizmos:
    [Range(0.0f, 1.0f)]
    public float gizmoSize = 0.05f;
    public bool poleDirection = false;
    public bool poleRotationAxis = false;


    void OnEnable()
    {
        skinnedMeshes = skinnedMeshRenderers.ToArray();
        MeshRenderer cubeBordersMesh = CubeBorders.GetComponent<MeshRenderer>();
        Bounds bounds = cubeBordersMesh.bounds;

        //IK
        int numberOfJoints = transforms.Length + 1;
        jointChainLength = 0;
        jointTransforms = new Transform[numberOfJoints];
        jointPositions = new Vector3[numberOfJoints];
        boneLength = new float[numberOfJoints - 1];
        jointStartDirection = new Vector3[numberOfJoints];
        startRotation = new Quaternion[numberOfJoints];
        ikTargetStartRot = ikTarget.rotation;

        var current = transform;

        AssignVerticesToCentroids(bounds);

        int sizeOfStruct = sizeof(SimpleTriangle);
        trianglesBuffer = new ComputeBuffer(numberOfAllTriangles / 3, sizeOfStruct, ComputeBufferType.Structured);
        startingVerticsPositionsBuffer = new ComputeBuffer(numberOfAllVertices, sizeof(Vector3), ComputeBufferType.Structured);
        finishVerticesPositionBuffer = new ComputeBuffer(numberOfAllVertices, sizeof(Vector3), ComputeBufferType.Structured);

        // For each bone calculate and store the lenght of the bone
        for (var i = 0; i <= jointTransforms.Length; i += 1)
        {
            jointTransforms[i] = current;
            // If the bones lenght equals the max lenght, we are on the last joint in the chain
            if (i == jointTransforms.Length - 1)
            {
                lastJointStartRot = current.rotation;
                return;
            }
            // Store length and add the sum of the bone lengths
            else
            {
                boneLength[i] = Vector3.Distance(current.position, transforms[i].position);
                jointChainLength += boneLength[i];

                jointStartDirection[i] = transforms[i].position - current.position;
                startRotation[i] = current.rotation;
            }
            // Move the iteration to next joint in the chain
            current = transforms[i];
        }
        
    }

    public void AssignVerticesToCentroids(Bounds bounds)
    {
        for(int i=0;i< skinnedMeshes.Length; i++)
        {
            numberOfAllVertices += skinnedMeshes[i].sharedMesh.vertices.Count();
            numberOfAllTriangles += skinnedMeshes[i].sharedMesh.triangles.Count();
            var vertices = skinnedMeshes[i].sharedMesh.vertices;
            float?[,] tableofAssignations = new float?[vertices.Count(), transforms.Count()];
            for(int j=0; j < vertices.Count(); j++)
            {
                if(bounds.Contains(vertices[j]))
                {
                    var vertice = vertices[j];
                    var proportions = CalculateMembership(vertice);
                    for(int x = 0; x < proportions.Count(); x++)
                    {
                        tableofAssignations[j, x] = proportions[x];
                    }

                }
                else
                {
                    for(int z=0; z < transforms.Count(); z++)
                    {
                        tableofAssignations[j, z] = null;
                    }
                }
            }
            MeshVerticesAssignations.Add(tableofAssignations);
        }
    }

    private float[] CalculateMembership(Vector3 vertice)
    {
        float[] proportions = new float[transforms.Count()];
        for (int i=0; i< transforms.Count(); i++)
        {
            float distance = Vector3.Distance(vertice, transforms[i].position);
            float sum = 0f;
            for(int j=0; j < transforms.Count(); j++)
            {
                float zyx = distance / Vector3.Distance(vertice, transforms[j].position);
                float xyz = 2f / (transforms.Count() - 1);
                sum += (float)Math.Pow(zyx, xyz);
            }

            float newMembership = 1f / sum;
            proportions[i] = newMembership;
        }

        return proportions;
    }

    void PoleConstraint()
    {
        int numberOfJoints = transforms.Length + 1;
        if (poleTarget != null && numberOfJoints < 4)
        {
            // Get the limb axis direction
            var limbAxis = (jointPositions[2] - jointPositions[0]).normalized;

            // Get the direction from the root joint to the pole target and mid joint
            Vector3 poleDirection = (poleTarget.position - jointPositions[0]).normalized;
            Vector3 boneDirection = (jointPositions[1] - jointPositions[0]).normalized;

            // Ortho-normalize the vectors
            Vector3.OrthoNormalize(ref limbAxis, ref poleDirection);
            Vector3.OrthoNormalize(ref limbAxis, ref boneDirection);

            // Calculate the angle between the boneDirection vector and poleDirection vector
            Quaternion angle = Quaternion.FromToRotation(boneDirection, poleDirection);

            // Rotate the middle bone using the angle
            jointPositions[1] = angle * (jointPositions[1] - jointPositions[0]) + jointPositions[0];
        }
    }

    void Backward()
    {
        // Iterate through every position in the list until we reach the start of the chain
        for (int i = jointPositions.Length - 1; i >= 0; i -= 1)
        {
            // The last bone position should have the same position as the ikTarget
            if (i == jointPositions.Length - 1)
            {
                jointPositions[i] = ikTarget.transform.position;
            }
            else
            {
                jointPositions[i] = jointPositions[i + 1] + (jointPositions[i] - jointPositions[i + 1]).normalized * boneLength[i];
            }
        }
    }

    void Forward()
    {
        // Iterate through every position in the list until we reach the end of the chain
        for (int i = 0; i < jointPositions.Length; i += 1)
        {
            // The first bone position should have the same position as the startPosition
            if (i == 0)
            {
                jointPositions[i] = startPosition;
            }
            else
            {
                jointPositions[i] = jointPositions[i - 1] + (jointPositions[i] - jointPositions[i - 1]).normalized * boneLength[i - 1];
            }
        }
    }

    // Update is called once per frame
    private void SolveIK()
    {
        // Get the jointPositions from the joints
        for (int i = 0; i < jointTransforms.Length; i += 1)
        {
            jointPositions[i] = jointTransforms[i].position;
        }
        // Distance from the root to the ikTarget
        distanceToTarget = Vector3.Distance(jointPositions[0], ikTarget.position);

        // IF THE TARGET IS NOT REACHABLE
        if (distanceToTarget > jointChainLength)
        {
            // Direction from root to ikTarget
            var direction = ikTarget.position - jointPositions[0];

            // Get the jointPositions
            for (int i = 1; i < jointPositions.Length; i += 1)
            {
                jointPositions[i] = jointPositions[i - 1] + direction.normalized * boneLength[i - 1];
            }
        }
        // IF THE TARGET IS REACHABLE
        else
        {
            // Get the distance from the leaf bone to the ikTarget
            float distToTarget = Vector3.Distance(jointPositions[jointPositions.Length - 1], ikTarget.position);
            float counter = 0;
            // While the distance to target is greater than the tolerance let's iterate until we are close enough
            while (distToTarget > tolerance)
            {
                startPosition = jointPositions[0];
                Backward();
                Forward();
                counter += 1;
                // After x iterations break the loop to avoid an infinite loop
                if (counter > iterations)
                {
                    break;
                }
            }
        }
        // Apply the pole constraint
        PoleConstraint();

        // Apply the jointPositions and rotations to the joints
        for (int i = 0; i < jointPositions.Length; i += 1)
        {
            jointTransforms[i].position = jointPositions[i];
            Quaternion targetRotation;
            if (i == jointPositions.Length - 1)
            {
                targetRotation = ikTarget.rotation;
            }
            else
            {
                targetRotation = Quaternion.FromToRotation(jointStartDirection[i], jointPositions[i + 1] - jointPositions[i]);

            }
            jointTransforms[i].rotation = targetRotation * startRotation[i];
        }
        // Let's constrain the rotation of the last joint to the IK target and maintain the offset.
        Quaternion offset = lastJointStartRot * Quaternion.Inverse(ikTargetStartRot);
        jointTransforms.Last().rotation = ikTarget.rotation * offset;
        var x = transforms[1].position;
        var y = transforms[1].rotation;
    }

    void Update()
    {
        OriginalPositionsOfJoints = transforms.Select(x => x.position).ToArray();
        OriginalRotationsOfJoints = transforms.Select(x => x.rotation).ToArray();
        SolveIK();
        Vector3[] jointPositionsDifference = new Vector3[OriginalPositionsOfJoints.Length];
        Quaternion[] jointRoationsDifference = new Quaternion[OriginalRotationsOfJoints.Length];
        for(int i = 0; i < OriginalPositionsOfJoints.Length; i++)
        {
            jointPositionsDifference[i] = transforms[i].position - OriginalPositionsOfJoints[i];
            Quaternion inverseRotationOfOriginal = Quaternion.Inverse(OriginalRotationsOfJoints[i]);
            jointRoationsDifference[i] = inverseRotationOfOriginal * transforms[i].rotation;
        }
        var meshChange = CalculateMeshChange(jointPositionsDifference, jointRoationsDifference);
        AddBufferData(meshChange);
    }

    private void AddBufferData(List<Vector3[]> verticesPositionChange)
    {
        Vector3[] VerticesBeforeChange = new Vector3[numberOfAllVertices];
        Vector3[] VerticesAfterChange = new Vector3[numberOfAllVertices];
        SimpleTriangle[] listOfAllTriangles = new SimpleTriangle[numberOfAllTriangles/3];
        int verticeIndexTracker = 0;
        int triangleIndexTracker = 0;
        //offset will be aded because, vertices are being merged into one array so the triangle vertice indexes will 
        //need offset starting from second mesh
        int triangleVectorPositionOffset = 0;
        for(int j=0; j< skinnedMeshes.Length; j++)
        {
            for(int k=0; k < skinnedMeshes[j].sharedMesh.vertices.Count(); k++)
            {
                var vertBeforeChange = skinnedMeshes[j].sharedMesh.vertices[k];
                VerticesBeforeChange[verticeIndexTracker] = vertBeforeChange;
                var positionChange = verticesPositionChange[j][k];
                VerticesAfterChange[verticeIndexTracker] = vertBeforeChange + positionChange;
                verticeIndexTracker++;
            }
            for(int k=0;k< skinnedMeshes[j].sharedMesh.triangles.Count(); k+=3)
            {
                SimpleTriangle triangle = new SimpleTriangle();
                triangle.t1 = skinnedMeshes[j].sharedMesh.triangles[k]+triangleVectorPositionOffset;
                triangle.t2 = skinnedMeshes[j].sharedMesh.triangles[k+1]+triangleVectorPositionOffset;
                triangle.t3 = skinnedMeshes[j].sharedMesh.triangles[k+2]+triangleVectorPositionOffset;
                listOfAllTriangles[triangleIndexTracker] = triangle;
                triangleIndexTracker++;
            }
            triangleVectorPositionOffset+= skinnedMeshes[j].sharedMesh.vertices.Count();
            var minx1 = listOfAllTriangles.Min(x => x.t1);
            var max1 = listOfAllTriangles.Max(x => x.t1);
            var minx2 = listOfAllTriangles.Min(x => x.t2);
            var max2 = listOfAllTriangles.Max(x => x.t2);
            var minx3 = listOfAllTriangles.Min(x => x.t3);
            var max3 = listOfAllTriangles.Max(x => x.t3);
        }
        trianglesBuffer.SetData(listOfAllTriangles);
        startingVerticsPositionsBuffer.SetData(VerticesBeforeChange);
        finishVerticesPositionBuffer.SetData(VerticesAfterChange);
    }

    public void OnDisable()
    {
        finishVerticesPositionBuffer.Release();
        finishVerticesPositionBuffer = null;
        startingVerticsPositionsBuffer.Release();
        startingVerticsPositionsBuffer = null;
        trianglesBuffer.Release();
        trianglesBuffer = null;
    }

    private List<Vector3[]> CalculateMeshChange(Vector3[] jointPositionsChange, Quaternion[] jointRotationChange)
    {
        List<Vector3[]> meshPositionChanges = new List<Vector3[]>();
        //List<Quaternion[]> meshRotationChanges = new List<Quaternion[]>();

        for(int i=0;i < MeshVerticesAssignations.Count(); i++)
        {
            var numberOfVerts = skinnedMeshes[i].sharedMesh.vertices.Count();
            Vector3[] singleMeshChanges= new Vector3[numberOfVerts];
            Quaternion[] singleMeshRotationChanges= new Quaternion[numberOfVerts];
            for (int j=0; j< numberOfVerts; j++)
            {
                Vector3 vertChangePositionChange = Vector3.zero;
                Quaternion vertRotationChange = Quaternion.identity;
                for(int k=0; k < transforms.Count(); k++)
                {
                    //calculate mesh vertice position change
                    var vertAssignation = MeshVerticesAssignations[i][j, k];
                    float vertAssignationValue = vertAssignation == null ? 0f : vertAssignation.Value;
                    var positionChange = jointPositionsChange[k];
                    Vector3 z = positionChange * vertAssignationValue;
                    vertChangePositionChange += z;

                    //calculate mesh vertice rotation change
                    //var rotationChange = jointRotationChange[k];
                    //var partialRoationChange = Quaternion.LerpUnclamped(Quaternion.identity, rotationChange, vertAssignationValue);
                    //vertRotationChange*= partialRoationChange;

                }
                singleMeshChanges[j] = vertChangePositionChange;
                //singleMeshRotationChanges[j] = vertRotationChange;
            }
            meshPositionChanges.Add(singleMeshChanges);
            //meshRotationChanges.Add(singleMeshRotationChanges);
        }

        return meshPositionChanges;
    }
}
