using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Run From Agent", story: "[Self] Run From [Agent]", category: "Action", id: "63e8aba71054e9228436a4f029bd5da9")]
public partial class RunFromAgentAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> Speed = new BlackboardVariable<float>(1.0f);
    [SerializeReference] public BlackboardVariable<float> FleeDistance = new BlackboardVariable<float>(100.0f);
    [SerializeReference] public BlackboardVariable<float> DistanceThreshold = new BlackboardVariable<float>(10.0f);

    private Vector3 startPosition;
    private Vector3 fleeDirection;
    private float distanceTraveled = 0f;

    protected override Status OnStart()
    {
        if (Self.Value == null || Agent.Value == null)
        {
            Debug.LogError("RunFromAgentAction: Self or Agent is null");
            return Status.Failure;
        }

        // Store starting position
        startPosition = Self.Value.transform.position;
        distanceTraveled = 0f;
        
        // Calculate flee direction
        fleeDirection = CalculateFleeDirection();
        
        Debug.Log("RunFromAgentAction: Started with flee direction: " + fleeDirection);
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null || Agent.Value == null)
        {
            return Status.Failure;
        }

        // Calculate straight-line distance from start
        distanceTraveled = Vector3.Distance(startPosition, Self.Value.transform.position);
        
        // Check if we've fled far enough
        if (distanceTraveled >= FleeDistance.Value - DistanceThreshold.Value)
        {
            Debug.Log("RunFromAgentAction: Success - reached required distance: " + distanceTraveled);
            return Status.Success;
        }
        
        // Move the agent directly
        MoveAgent();
        
        // Update flee direction periodically to avoid obstacles
        if (Time.frameCount % 30 == 0)
        {
            fleeDirection = CalculateFleeDirection();
        }
        
        return Status.Running;
    }

    protected override void OnEnd()
    {
        // Nothing to clean up
    }
    
    private Vector3 CalculateFleeDirection()
    {
        // Get direction away from the agent
        Vector3 directionFromAgent = Self.Value.transform.position - Agent.Value.transform.position;
        
        // If agents are too close, use random direction
        if (directionFromAgent.magnitude < 0.01f)
        {
            directionFromAgent = new Vector3(
                UnityEngine.Random.Range(-1f, 1f), 
                0, 
                UnityEngine.Random.Range(-1f, 1f)
            );
        }
        
        // Normalize and ensure Y is 0 for proper ground movement
        directionFromAgent.y = 0;
        return directionFromAgent.normalized;
    }
    
    private void MoveAgent()
    {
        // Cast a ray to check for obstacles
        if (Physics.Raycast(Self.Value.transform.position, fleeDirection, out RaycastHit hit, 2f))
        {
            // If there's an obstacle, adjust direction
            fleeDirection = Vector3.Reflect(fleeDirection, hit.normal).normalized;
            fleeDirection.y = 0; // Keep it on the ground plane
        }
        
        // Calculate the movement this frame
        Vector3 movement = fleeDirection * Speed.Value * Time.deltaTime;
        
        // Move the transform directly
        Self.Value.transform.position += movement;
        
        // Rotate the agent to face the movement direction
        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(fleeDirection);
            Self.Value.transform.rotation = Quaternion.Slerp(
                Self.Value.transform.rotation, 
                targetRotation, 
                Time.deltaTime * 8f
            );
        }
        
        Debug.Log($"Moving agent: Direction={fleeDirection}, Speed={Speed.Value}, Distance traveled={distanceTraveled}/{FleeDistance.Value}");
    }
}