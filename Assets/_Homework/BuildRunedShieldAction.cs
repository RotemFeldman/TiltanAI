using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable]
[NodeDescription(name: "BuildRunedShield", story: "Builds a Runed Shield", category: "Action", id: "2fab089c88910a740a21051c7764c73f")]
public class BuildRunedShieldAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> ShieldBuildLocation;
    [SerializeReference] public BlackboardVariable<GameObject> Shield;

    // Requirements for the Shield
    private const int CrystalShardsRequired = 4;
    private const int IronIngotsRequired = 2;

    private bool resourcesDelivered = false;
    private bool shieldCrafted = false;

    protected override Status OnStart()
    {
        Debug.Log("Starting to build the Runed Shield...");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!resourcesDelivered)
        {
            Debug.Log("Checking resources for Runed Shield...");
            if (!HasRequiredResources())
            {
                Debug.LogWarning("Not enough resources for Runed Shield. Waiting...");
                return Status.Running;
            }

            Debug.Log("Resources are prepared. Delivering resources...");
            DeliverResourcesToBuildLocation();

            resourcesDelivered = true;
        }

        if (resourcesDelivered && !shieldCrafted)
        {
            Debug.Log("Crafting the Runed Shield at the build location...");
            CraftShield();

            Debug.Log("Runed Shield has been crafted.");
            shieldCrafted = true;
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Debug.Log("BuildRunedShieldAction has ended.");
    }

    private bool HasRequiredResources()
    {
        return (GoapResourceManager.Instance.CrystalsEffectiveCount >= CrystalShardsRequired &&
                GoapResourceManager.Instance.IronIngotsEffectiveCount >= IronIngotsRequired);
    }

    private void DeliverResourcesToBuildLocation()
    {
        GoapResourceManager.Instance.UseResource(ResourceType.CrystalShard, CrystalShardsRequired);
        GoapResourceManager.Instance.UseResource(ResourceType.IronIngot, IronIngotsRequired);
        Debug.Log("All resources delivered for Runed Shield.");
    }

    private void CraftShield()
    {
        GoapResourceManager.Instance.RunedShield = true;

        if (Shield != null)
        {
            Shield.Value = new GameObject("RunedShield");
        }
    }
}