using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Anchor : MonoBehaviour
{
    FabricSimulatorGPU fabricSimulator;
    public List<int> managedPoints = new List<int>();
    public List<Vector3> pointVector = new List<Vector3>();
    [SerializeField]
    public float range = 1f;
    private void OnEnable()
    {
        fabricSimulator = GetComponent<FabricSimulatorGPU>();
    }
}
