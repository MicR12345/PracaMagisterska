using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputationManager : MonoBehaviour
{
    public List<FabricSimulatorGPU> fabricSimulatorGPUs = new List<FabricSimulatorGPU>();
    void FixedUpdate()
    {
        foreach (FabricSimulatorGPU item in fabricSimulatorGPUs)
        {
            item.Execute();
        }
    }
}
