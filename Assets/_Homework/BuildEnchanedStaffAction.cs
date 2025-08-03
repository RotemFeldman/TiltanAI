using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable]
[NodeDescription(name: "BuildEnchantedStaff", story: "Builds an Enchanted Staff", category: "Action", id: "7438b619b4debfac5938e4c114352430")]
public class BuildEnchantedStaffAction : Action
{
    [SerializeReference] public BlackboardVariable<Vector3> StaffBuildLocation;
    [SerializeReference] public BlackboardVariable<GameObject> Staff;

    // Requirements for the Staff
    private const int OakLogsRequired = 5;
    private const int IronIngotsRequired = 3;

    private bool resourcesDelivered = false;
    private bool staffCrafted = false;

    protected override Status OnStart()
    {
        Debug.Log("Starting to build the Enchanted Staff...");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!resourcesDelivered)
        {
            Debug.Log("Checking resources for Enchanted Staff...");
            if (!HasRequiredResources())
            {
                Debug.LogWarning("Not enough resources for Enchanted Staff. Waiting...");
                return Status.Running;
            }

            Debug.Log("Resources are prepared. Delivering resources...");
            DeliverResourcesToBuildLocation();

            resourcesDelivered = true;
        }

        if (resourcesDelivered && !staffCrafted)
        {
            Debug.Log("Crafting the Enchanted Staff at the build location...");
            CraftStaff();

            Debug.Log("Enchanted Staff has been crafted.");
            staffCrafted = true;
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        Debug.Log("BuildEnchantedStaffAction has ended.");
    }

    private bool HasRequiredResources()
    {
        return (GoapResourceManager.Instance.OakLogsEffectiveCount >= OakLogsRequired &&
                GoapResourceManager.Instance.IronIngotsEffectiveCount >= IronIngotsRequired);
    }

    private void DeliverResourcesToBuildLocation()
    {
        GoapResourceManager.Instance.UseResource(ResourceType.OakLog, OakLogsRequired);
        GoapResourceManager.Instance.UseResource(ResourceType.IronIngot, IronIngotsRequired);
        Debug.Log("All resources delivered for Enchanted Staff.");
    }

    private void CraftStaff()
    {
        GoapResourceManager.Instance.EnchantedStaff = true;

        if (Staff != null)
        {
            Staff.Value = new GameObject("EnchantedStaff");
        }
    }
}