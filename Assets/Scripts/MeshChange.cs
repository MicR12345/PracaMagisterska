using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshChange : MonoBehaviour
{
    [SerializeField]
    List<InverseKinematics> Iks;
    [SerializeField]
    List<FabricSimulatorGPU> FabricSims;
    List<Mesh> meshes;
    List<SkinnedMeshRenderer> skinnedMeshRenderers;
    void FixedUpdate()
    {
        
    }
}
