using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshAssignation : MonoBehaviour
{
    public Transform[] centroids;
    public Transform[] vertices;
    public float fuzziness= (float)0.3;
    // Start is called before the first frame update
    void Start()
    {
        AssignVerticesToCentroids();
    }

    void AssignVerticesToCentroids()
    {
        foreach (Transform vertex in vertices)
        {
            float[] proportions = new float[centroids.Length];
            float[] distances = new float[centroids.Length];

            for (int i = 0; i < centroids.Length; i++)
            {
                distances[i] = Vector3.Distance(vertex.position, centroids[i].position);
            }

            for (int i = 0; i < centroids.Length; i++)
            {
                float numerator = 0f;
                for (int j = 0; j < centroids.Length; j++)
                {
                    numerator += Mathf.Pow((1f / distances[i]) / (1f / distances[j]), 2f / (fuzziness - 1f));
                }
                proportions[i] = 1f / numerator;
            }

        }
    }
}
