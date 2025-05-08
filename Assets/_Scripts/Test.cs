using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

public class Test : MonoBehaviour
{
    public List<NavMeshAgent> agents;
    public Vector3 target00;
    public int rows;
    public float spacing;

    [ContextMenu("To Formation")]
    public void ToFormation()
    {
        //target00 = agents[0].transform.position;   
        
        var col = agents.Count / rows;
        if (agents.Count % rows != 0)
        {
            col++;
        }
        int currentAgent = 0;
        
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < col; j++)
            {
                if (currentAgent >= agents.Count)
                    return;
        
                Vector3 targetPosition = target00 + new Vector3(j * spacing, 0, -i * spacing);
                
                agents[currentAgent].SetDestination(targetPosition);
                currentAgent++;
            }
        }
        
    }
}
