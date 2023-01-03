using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    public static List<PhysicsPart> activePhysicsParts = new List<PhysicsPart>();
    public static float movementToleranceDampening = 0.01f;
    // Start is called before the first frame update
    /*void Start()
    {
        Point[,] points = new Point[15, 15];
        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                if (i!=0)
                {
                    points[i,j] = new Point(new Vector3(j * 5, i * -5, 0));
                }
                else
                {
                    points[i, j] = new Point(new Vector3(j * 5, i * -5, 0));
                    points[i,j].debugDisableMovement = true;
                }
                points[i, j].staticForces.Add(new Gravity());
            }
        }
        List<Connector> connectors = new List<Connector>();
        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                if (i<14)
                {
                    Connector connector = new Connector(points[i, j], points[i + 1, j], new List<Force>());
                    connector.forces.Add(new Elasticity(points[i, j], points[i + 1, j], 5.5f, 5f));
                }
                if (j < 14)
                {
                    Connector connector = new Connector(points[i, j], points[i, j+1], new List<Force>());
                    connector.forces.Add(new Elasticity(points[i, j], points[i, j+1], 5.5f, 5f));
                }
                if (i<14 && j<14)
                {
                    Connector connector = new Connector(points[i, j], points[i+1, j + 1], new List<Force>());
                    connector.forces.Add(new Elasticity(points[i, j], points[i+1, j + 1], 5.5f, 7f));
                }
                if (i < 14 && j >0)
                {
                    Connector connector = new Connector(points[i, j], points[i + 1, j - 1], new List<Force>());
                    connector.forces.Add(new Elasticity(points[i, j], points[i + 1, j - 1], 5.5f, 7f));
                }
            }
        }
    }
    */
    public static float WaitTime = 0.1f;
    public float TotalTime = WaitTime;
    private void FixedUpdate()
    {
        float time = Time.deltaTime;
        TotalTime = TotalTime - time;
        if (TotalTime<=0)
        {
            TotalTime = WaitTime;
            foreach (PhysicsPart part in activePhysicsParts)
            {
                part.ApplyStaticEffectors();
                //part.CalculatePhysics();
                part.ChangePosition();
            }
        }
    }
    private void OnDrawGizmos()
    {
        foreach (PhysicsPart item in activePhysicsParts)
        {
            Gizmos.color = Color.yellow;
            //Gizmos.DrawCube(item.gameObject.transform.position + item.Position()[0], new Vector3(0.3f,0.3f,0.3f));
            Gizmos.color = Color.gray;
        }
    }
}
