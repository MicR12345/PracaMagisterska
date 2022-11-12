using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    public static List<PhysicsPart> activePhysicsParts = new List<PhysicsPart>();
    // Start is called before the first frame update
    void Start()
    {
        Point P1 = new Point(new Vector3(0, 0, 0));
        P1.staticForces.Add(new Gravity());
        Point P2 = new Point(new Vector3(0, 5, 0));
        Point P3 = new Point(new Vector3(5, 5, 0));
        P3.staticForces.Add(new Gravity());
        P2.debugDisableMovement = true;
        Connector C1 = new Connector(P1, P2, new List<Force>());
        Connector C2 = new Connector(P1, P3, new List<Force>());
        Connector C3 = new Connector(P2, P3, new List<Force>());
        C1.forces.Add(new Elasticity(P1,P2,2.5f,3f));
        C2.forces.Add(new Elasticity(P1, P3, 0.5f, 7f));
        C3.forces.Add(new Elasticity(P2, P3, 2.5f, 3f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void FixedUpdate()
    {
        foreach (PhysicsPart part in activePhysicsParts)
        {
            part.ApplyStaticEffectors();
            //part.CalculatePhysics();
            part.CalculateMovement();
            part.ChangePosition();
        }
    }
}
