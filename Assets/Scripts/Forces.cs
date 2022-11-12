using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity: Force
{
    public Effector generateEffector()
    {
        return new Effector(Vector3.down, 9.81f);
    }
    public override (Effector, Effector) GenerateEffectors()
    {
        Effector gravity = generateEffector();
        return (gravity, gravity);
    }
}
public class Elasticity : Force
{
    PhysicsPart part1Handle;
    PhysicsPart part2Handle;
    int part1HandleNumber;
    int part2HandleNumber;
    float k;
    float baseLenght;
    public Elasticity(PhysicsPart part1, PhysicsPart part2,float k,float baseLenght,int part1ConnectionNumber = 0,
        int part2ConnectionNumber = 0)
    {
        part1Handle = part1;
        part2Handle = part2;
        this.k = k;
        this.baseLenght = baseLenght;
        this.part1HandleNumber = part1ConnectionNumber;
        this.part2HandleNumber = part2ConnectionNumber;
    }
    public (Effector, Effector) generateEffectors()
    {
        Vector3 direction =
            part1Handle.Position()[part1HandleNumber] - part2Handle.Position()[part2HandleNumber];
        float distance = 
            Vector3.Distance(part1Handle.Position()[part1HandleNumber], part2Handle.Position()[part2HandleNumber]);
        Effector elasticity = new Effector(direction, (baseLenght - distance) * k);
        Effector elasticityReversed = new Effector(-direction, (baseLenght - distance) * k);
        return (elasticity, elasticityReversed);
    }
    public override (Effector, Effector) GenerateEffectors()
    {
        return generateEffectors();
    }
}