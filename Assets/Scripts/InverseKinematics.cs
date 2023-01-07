using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseKinematics : MonoBehaviour
{
    public bool IsEndGameObject = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        List<Transform> children = new List<Transform>();
        var childrenCount = this.transform.childCount;
        for (int i = 0; i < childrenCount; i++)
        {
            Transform child = this.transform.GetChild(i);
            children.Add(child);
            Debug.DrawLine(this.transform.position, child.position, Color.red, 1f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        List<Transform> children = new List<Transform>();
        var childrenCount = this.transform.childCount;
        for (int i = 0; i < childrenCount; i++)
        {
            Transform child = this.transform.GetChild(i);
            children.Add(child);
            //Debug.DrawLine(this.transform.position, child.position, Color.red, 1f);
            Gizmos.DrawLine(transform.position, child.position);
        }
        
        Gizmos.color = Color.gray;
    }
}
