using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable]
[NodeDescription(name: "ConnectArtifacts", story: "Creates [Combined] Magical Artifact", category: "Action", id: "ab28ac1d6f47973b8a7761564d1c5d71")]
public class ConnectArtifactsAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> CombinedBuild;
    private bool artifactsConnected = false;

    protected override Status OnStart()
    {
        Debug.Log("Starting the process to connect artifacts...");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // Preconditions: Check if both artifacts are built
        if (!GoapResourceManager.Instance.EnchantedStaff || !GoapResourceManager.Instance.RunedShield)
        {
            Debug.LogWarning("Cannot combine artifacts. Preconditions not met: Staff or Shield not built.");
            return Status.Failure;
        }

        if (!artifactsConnected)
        {
            // Simulate combining artifacts
            Debug.Log("Mage is connecting the Enchanted Staff and Runed Shield...");
            artifactsConnected = true;

            // Mark the Combined Magical Artifact as built
            GoapResourceManager.Instance.CombinedArtifact = true;

            Debug.Log("Combined Magical Artifact has been successfully created!");
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Debug.Log("ConnectArtifactsAction has ended.");
    }
}