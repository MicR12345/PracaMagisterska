using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Processors;

public unsafe class InverseKinematics : MonoBehaviour
{
    //ik
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
    [Header("Pole target (3 joint chain)")]
    public Transform poleTarget;

    [Header("Debug")]
    public bool debugJoints = true;
    public bool localRotationAxis = false;

    //mesh
    private SkinnedMeshRenderer[] skinnedMeshes;
    private Vector3[] OriginalPositionsOfJoints;

    public ComputeBuffer trianglesBuffer;
    public ComputeBuffer startingVerticsPositionsBuffer;
    public ComputeBuffer finishVerticesPositionBuffer;
    public int numberOfAllVertices = 0;
    public int numberOfAllTriangles = 0;
    public List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();

    private int numberOfMeshes;
    private int[] numberOfVertsInMeshes;

    public List<JointBound> Bounds = new List<JointBound>();

    List<int[]> allNumbersOfAssignations;
    List<Assignation>[] allAsignations;

    List<List<Vector3[]>> ListOfJointChangesForRegistred = new List<List<Vector3[]>>();

    // Remove this if you need bigger gizmos:
    [Range(0.0f, 1.0f)]
    public float gizmoSize = 0.05f;
    public bool poleDirection = false;
    public bool poleRotationAxis = false;

    void OnEnable()
    {
        if(transforms.Length != Bounds.Count())
        {
            return;
        }

        skinnedMeshes = skinnedMeshRenderers.ToArray();

        //IK
        int numberOfJoints = transforms.Length + 1;
        jointChainLength = 0;
        jointTransforms = new Transform[numberOfJoints];
        jointPositions = new Vector3[numberOfJoints];
        boneLength = new float[numberOfJoints - 1];
        jointStartDirection = new Vector3[numberOfJoints];
        startRotation = new Quaternion[numberOfJoints];
        ikTargetStartRot = ikTarget.rotation;

        numberOfMeshes = skinnedMeshRenderers.Count();
        numberOfVertsInMeshes = new int[numberOfMeshes];

        for(int i=0; i<numberOfMeshes; i++)
        {
            numberOfVertsInMeshes[i] = skinnedMeshRenderers[i].sharedMesh.vertices.Count();
        }
        
        AssignMeshVertices();

        int sizeOfStruct = sizeof(SimpleTriangle);
        trianglesBuffer = new ComputeBuffer(numberOfAllTriangles / 3, sizeOfStruct, ComputeBufferType.Structured);
        startingVerticsPositionsBuffer = new ComputeBuffer(numberOfAllVertices, sizeof(Vector3), ComputeBufferType.Structured);
        finishVerticesPositionBuffer = new ComputeBuffer(numberOfAllVertices, sizeof(Vector3), ComputeBufferType.Structured);

        var current = transform;

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

    void PoleConstraint()
    {
        int numberOfJoints = transforms.Length + 1;
        if (poleTarget != null && numberOfJoints < 5)
        {
            // Get the limb axis direction
            var limbAxis = (jointPositions[3] - jointPositions[1]).normalized;

            // Get the direction from the root joint to the pole target and mid joint
            Vector3 poleDirection = (poleTarget.position - jointPositions[1]).normalized;
            Vector3 boneDirection = (jointPositions[2] - jointPositions[1]).normalized;

            // Ortho-normalize the vectors
            Vector3.OrthoNormalize(ref limbAxis, ref poleDirection);
            Vector3.OrthoNormalize(ref limbAxis, ref boneDirection);

            // Calculate the angle between the boneDirection vector and poleDirection vector
            Quaternion angle = Quaternion.FromToRotation(boneDirection, poleDirection);

            // Rotate the middle bone using the angle
            jointPositions[2] = angle * (jointPositions[2] - jointPositions[1]) + jointPositions[1];
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
    }

    public bool HasAnyJointMoved(Vector3[] originalPositions)
    {
        for(int i=0;i < originalPositions.Length; i++)
        {
            if (originalPositions[i] != transforms[i].position)
            {
                return true;
            }
        }
        return false;
    }

    void FixedUpdate()
    {
        OriginalPositionsOfJoints = transforms.Select(x => x.position).ToArray();
        SolveIK();
        

        var hasAnyJointMoved = HasAnyJointMoved(OriginalPositionsOfJoints);

        if (hasAnyJointMoved)
        {
            for (int i = 0; i < ListOfJointChangesForRegistred.Count; i++)
            {
                ListOfJointChangesForRegistred[i].Add(transforms.Select(x => x.position).ToArray());
            }
            Vector3[] jointPositionsDifference = new Vector3[OriginalPositionsOfJoints.Length];
            for (int i = 0; i < OriginalPositionsOfJoints.Length; i++)
            {
                jointPositionsDifference[i] = transforms[i].position - OriginalPositionsOfJoints[i];
            }

            List<Vector3[]> meshChanges = CalculateMeshChanges();
            var positionsBeforeChange = ApplyMeshChanges(meshChanges);
            SetBufferData(positionsBeforeChange);
        }
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

    private void AssignMeshVertices()
    {
        List<int[]> numberOfAssignations = new List<int[]>();
        List<Assignation>[] asignations = new List<Assignation>[transforms.Count()];

        int numberOfMeshes = skinnedMeshRenderers.Count();

        for(int i=0; i<numberOfMeshes; i++)
        {
            numberOfAssignations.Add(new int[skinnedMeshRenderers[i].sharedMesh.vertices.Count()]);
            numberOfAllVertices += skinnedMeshes[i].sharedMesh.vertices.Count();
            numberOfAllTriangles += skinnedMeshes[i].sharedMesh.triangles.Count();
        }

        int localNumberOfJoints = transforms.Count();
            
        for(int i=0; i<localNumberOfJoints; i++)
        {
            List<Assignation> assignationSingle = new List<Assignation>();
            
            for (int j = 0; j < numberOfMeshes; j++)
            {
                Vector3[] verts = skinnedMeshRenderers[j].sharedMesh.vertices;
                Matrix4x4 localToWorld = skinnedMeshRenderers[j].transform.localToWorldMatrix;
                int numberOfVerts = verts.Count();
                JointBound bounds = Bounds[i];
                for(int k=0; k<numberOfVerts; k++)
                {
                    bool isInside = bounds.IsInBounds(localToWorld.MultiplyPoint3x4(verts[k]));

                    if (isInside)
                    {
                        numberOfAssignations[j][k]++;
                        Assignation newAssingation = new(j, k);
                        assignationSingle.Add(newAssingation);
                    }
                }
            }

            asignations[i] = assignationSingle;
        }

        allNumbersOfAssignations = numberOfAssignations;
        allAsignations = asignations;
    }
    public (List<Assignation>[],int) GenerateFabricAssignations(ref SimplePointStr[] pointData)
    {
        List<Assignation>[] asignations = new List<Assignation>[transforms.Count()];
        int localNumberOfJoints = transforms.Count();
        for (int i = 0; i < localNumberOfJoints; i++)
        {
            List<Assignation> assignationSingle = new List<Assignation>();
            int numberOfVerts = pointData.Count();
            JointBound bounds = Bounds[i];
            for (int k = 0; k < numberOfVerts; k++)
            {
                bool isInside = bounds.IsInBounds(pointData[k].position);

                if (isInside)
                {
                    Assignation newAssingation = new(0, k);
                    assignationSingle.Add(newAssingation);
                }
            }
            asignations[i] = assignationSingle;
        }
        int id = ListOfJointChangesForRegistred.Count;
        Vector3[] initial = transforms.Select(x => x.position).ToArray();
        ListOfJointChangesForRegistred.Add(new List<Vector3[]>());
        ListOfJointChangesForRegistred[id].Add(initial);
        return (asignations,id);
    }
    private List<Vector3[]> CalculateMeshChanges()
    {
        List<Vector3[]> meshChanges = new List<Vector3[]>();
        for (int i = 0; i < numberOfMeshes; i++)
        {
            meshChanges.Add((Vector3[])skinnedMeshRenderers[i].sharedMesh.vertices.Clone());
        }

        Vector3[] currentPositions = (Vector3[])OriginalPositionsOfJoints.Clone();

        for (int i = 1; i < transforms.Length; i++)
        {
            List<Assignation> assignationsToJoint = allAsignations[i];
            int numberOfAssignations = assignationsToJoint.Count();

            Vector3 desiredJointPosition = transforms[i].position;

            Vector3 currentSegmentStart = currentPositions[i-1];

            Vector3 desiredSegmentVector = desiredJointPosition - currentPositions[i-1];

            Vector3 currentSegmentVector = currentPositions[i] - currentPositions[i - 1];

            Quaternion difference = Quaternion.FromToRotation(Vector3.Normalize(currentSegmentVector), Vector3.Normalize(desiredSegmentVector));

            Matrix4x4 rotation = Matrix4x4.Rotate(difference);

            for (int j = 0; j < numberOfAssignations; j++)
            {
                Assignation singleAssignation = assignationsToJoint[j];
                Matrix4x4 localToWorld = skinnedMeshRenderers[singleAssignation.MeshNumber].localToWorldMatrix;
                Matrix4x4 worldToLocal = skinnedMeshRenderers[singleAssignation.MeshNumber].worldToLocalMatrix;
                Vector3 vert = meshChanges[singleAssignation.MeshNumber][singleAssignation.VertexNumber];
                vert = localToWorld.MultiplyPoint3x4(vert);
                Vector3 newPt = rotation.MultiplyPoint3x4(vert - currentSegmentStart) + currentSegmentStart;
                newPt = worldToLocal.MultiplyPoint3x4(newPt);
                meshChanges[singleAssignation.MeshNumber][singleAssignation.VertexNumber] = newPt;
            }
            for (int j = i; j < transforms.Length; j++)
            {
                currentPositions[j] = rotation.MultiplyPoint3x4(currentPositions[j] - currentSegmentStart) + currentSegmentStart;
            }
        }

        return meshChanges;
    }
    public void PerformMeshChanges(ref SimplePointStr[] pointData,List<Assignation>[] assignations,Transform transform,int id)
    {
        for(int n = 0;n<ListOfJointChangesForRegistred[id].Count-1;n++)
        {
            Vector3[] currentPositions = (Vector3[])ListOfJointChangesForRegistred[id][n].Clone();

            for (int i = 1; i < ListOfJointChangesForRegistred[id][n].Length; i++)
            {
                List<Assignation> assignationsToJoint = assignations[i];
                int numberOfAssignations = assignationsToJoint.Count();
                Vector3 desiredJointPosition = ListOfJointChangesForRegistred[id][n+1][i];

                Vector3 currentSegmentStart = currentPositions[i - 1];

                Vector3 desiredSegmentVector = desiredJointPosition - currentPositions[i - 1];

                Vector3 currentSegmentVector = currentPositions[i] - currentPositions[i - 1];

                Quaternion difference = Quaternion.FromToRotation(Vector3.Normalize(currentSegmentVector), Vector3.Normalize(desiredSegmentVector));

                Matrix4x4 rotation = Matrix4x4.Rotate(difference);

                for (int j = 0; j < numberOfAssignations; j++)
                {
                    Assignation singleAssignation = assignationsToJoint[j];
                    Matrix4x4 localToWorld = transform.localToWorldMatrix;
                    Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
                    Vector3 vert = pointData[singleAssignation.VertexNumber].position;
                    Vector3 newPt = rotation.MultiplyPoint3x4(vert - currentSegmentStart) + currentSegmentStart;
                    pointData[singleAssignation.VertexNumber].position = newPt;
                }
                for (int j = i; j < ListOfJointChangesForRegistred[id][n].Length; j++)
                {
                    currentPositions[j] = rotation.MultiplyPoint3x4(currentPositions[j] - currentSegmentStart) + currentSegmentStart;
                }
            }
        }
        Vector3[] last = ListOfJointChangesForRegistred[id][ListOfJointChangesForRegistred[id].Count-1];
        ListOfJointChangesForRegistred[id].Clear();
        ListOfJointChangesForRegistred[id].Add(last);
    }
    public List<Vector3[]> ApplyMeshChanges(List<Vector3[]> meshChanges)
    {
        List<Mesh> clones = new List<Mesh>();
        List<Vector3[]> positionsBeforeChange = new List<Vector3[]>();

        for(int i=0; i<numberOfMeshes; i++)
        {
            var numberOfVerts = numberOfVertsInMeshes[i];
            positionsBeforeChange.Add(new Vector3[numberOfVerts]); 
            Mesh clone = Instantiate(skinnedMeshRenderers[i].sharedMesh);
            Vector3[] clonedVertices = clone.vertices;

            for(int j=0; j < numberOfVertsInMeshes[i]; j++)
            {
                positionsBeforeChange[i][j] = clonedVertices[j];
                clonedVertices[j] = meshChanges[i][j];
            }
            clone.vertices = clonedVertices;
            clones.Add(clone);
        }

        for(int i = 0; i < numberOfMeshes; i++)
        {
            skinnedMeshRenderers[i].sharedMesh = clones[i];
        }

        return positionsBeforeChange;
    }

    public static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        // Calculate the direction vector of the segment.
        Vector2 segmentDirection = end - start;

        // Calculate the distance between the start point and the given point.
        Vector2 pointDirection = point - start;

        // Calculate the projection of the point direction onto the segment direction.
        float projection = Vector2.Dot(pointDirection, segmentDirection) / segmentDirection.sqrMagnitude;

        // If the projection is less than zero, the closest point is the start point.
        if (projection < 0f)
        {
            return Vector2.Distance(point, start);
        }

        // If the projection is greater than one, the closest point is the end point.
        if (projection > 1f)
        {
            return Vector2.Distance(point, end);
        }

        // Otherwise, the closest point is on the segment between the start and end points.
        Vector2 closestPoint = start + projection * segmentDirection;
        return Vector2.Distance(point, closestPoint);
    }

    private void SetBufferData(List<Vector3[]> positionsBeforeChange)
    {
        Vector3[] VerticesBeforeChange = new Vector3[numberOfAllVertices];
        Vector3[] VerticesAfterChange = new Vector3[numberOfAllVertices];
        SimpleTriangle[] listOfAllTriangles = new SimpleTriangle[numberOfAllTriangles / 3];
        int verticeIndexTracker = 0;
        int triangleIndexTracker = 0;
        //offset will be aded because, vertices are being merged into one array so the triangle vertice indexes will 
        //need offset starting from second mesh
        int triangleVectorPositionOffset = 0;
        for(int j=0; j<skinnedMeshRenderers.Count(); j++)
        {
            for(int k=0; k < skinnedMeshRenderers[j].sharedMesh.vertices.Count(); k++)
            {
                Matrix4x4 localToWorld = skinnedMeshRenderers[j].transform.localToWorldMatrix;
                var positionBeforeCHange = positionsBeforeChange[j][k];
                var positionAfterChange = skinnedMeshRenderers[j].sharedMesh.vertices[k];
                VerticesBeforeChange[verticeIndexTracker] = localToWorld.MultiplyPoint3x4(positionBeforeCHange);
                VerticesAfterChange[verticeIndexTracker] = localToWorld.MultiplyPoint3x4(positionAfterChange);
                verticeIndexTracker++;
            }

            for (int k = 0; k < skinnedMeshes[j].sharedMesh.triangles.Count(); k += 3)
            {
                SimpleTriangle triangle = new SimpleTriangle();
                triangle.t1 = skinnedMeshes[j].sharedMesh.triangles[k] + triangleVectorPositionOffset;
                triangle.t2 = skinnedMeshes[j].sharedMesh.triangles[k + 1] + triangleVectorPositionOffset;
                triangle.t3 = skinnedMeshes[j].sharedMesh.triangles[k + 2] + triangleVectorPositionOffset;
                listOfAllTriangles[triangleIndexTracker] = triangle;
                triangleIndexTracker++;
                
            }
            triangleVectorPositionOffset += skinnedMeshes[j].sharedMesh.vertices.Count();
        }

        trianglesBuffer.SetData(listOfAllTriangles);
        startingVerticsPositionsBuffer.SetData(VerticesBeforeChange);
        finishVerticesPositionBuffer.SetData(VerticesAfterChange);
    }

    public class Assignation
    {
        public int MeshNumber;
        public int VertexNumber;

        public Assignation(int meshNumber, int vertexNumber)
        {
            MeshNumber = meshNumber;
            VertexNumber = vertexNumber;
        }
    }
}
