using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKManager : MonoBehaviour
{
    public Transform targer;
    public Transform headBone;
    // Start is called before the first frame update
    void Start()
    {
        var children = this.gameObject.transform.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
