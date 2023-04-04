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
    ComputeBuffer trianglesBuffer;
    ComputeBuffer startingVerticsPositionsBuffer;
    ComputeBuffer finishVerticesPositionBuffer;
    private int numberOfAllVertices = 0;
    private int numberOfAllTriangles = 0;
    public List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();

    private int numberOfMeshes;
    private int[] numberOfVertsInMeshes;

    public List<JointBound> Bounds = new List<JointBound>();

    List<int[]> allNumbersOfAssignations;
    List<Assignation>[] allAsignations;

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

        /*
        int sizeOfStruct = sizeof(SimpleTriangle);
        trianglesBuffer = new ComputeBuffer(numberOfAllTriangles / 3, sizeOfStruct, ComputeBufferType.Structured);
        startingVerticsPositionsBuffer = new ComputeBuffer(numberOfAllVertices, sizeof(Vector3), ComputeBufferType.Structured);
        finishVerticesPositionBuffer = new ComputeBuffer(numberOfAllVertices, sizeof(Vector3), ComputeBufferType.Structured);
        */
        AssignMeshVertices();

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
            Vector3[] jointPositionsDifference = new Vector3[OriginalPositionsOfJoints.Length];
            for (int i = 0; i < OriginalPositionsOfJoints.Length; i++)
            {
                jointPositionsDifference[i] = transforms[i].position - OriginalPositionsOfJoints[i];
            }

            List<Vector3[]> meshChanges = CalculateMeshChanges(jointPositionsDifference);
            ApplyMeshChanges(meshChanges);
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
                int numberOfVerts = verts.Count();
                JointBound bounds = Bounds[i];
                for(int k=0; k<numberOfVerts; k++)
                {
                    bool isInside = bounds.IsInBounds(verts[k]);

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
    private List<Vector3[]> CalculateMeshChanges(Vector3[] jointChanges)
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
                Vector3 vert = meshChanges[singleAssignation.MeshNumber][singleAssignation.VertexNumber];

                Vector3 newPt = rotation.MultiplyPoint3x4(vert - currentSegmentStart) + currentSegmentStart;
                meshChanges[singleAssignation.MeshNumber][singleAssignation.VertexNumber] = newPt;
            }
            for (int j = i; j < transforms.Length; j++)
            {
                currentPositions[j] = rotation.MultiplyPoint3x4(currentPositions[j] - currentSegmentStart) + currentSegmentStart;
            }
        }

        return meshChanges;
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

    internal class Assignation
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
