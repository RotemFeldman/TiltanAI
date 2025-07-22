using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemLocation
{
    public ItemType itemType;
    public Vector3 position;
    public float timeDiscovered;
    public bool isReserved;
    public string isReservedBy;

    public ItemLocation(ItemType itemType, Vector3 position)
    {
        this.itemType = itemType;
        this.position = position;
        this.timeDiscovered = Time.time;
        this.isReserved = false;
        this.isReservedBy = "";
    }
}

public class GoapWorldStateManager 
{
    private IGoapState worldState;

    public GoapWorldStateManager()
    {
        worldState = new GoapState();
        InitializeState();
    }

    public IGoapState GetWorldState()
    {
        return worldState;
    }

    private void InitializeState()
    {
        // Basic Resources
        worldState.SetState("OakLogs", 0);
        worldState.SetState("IronIngots", 0);
        worldState.SetState("CrystalShards", 0);
        
        // Items location tracking
        worldState.SetState("OakLogsLocation", new List<ItemLocation>());
        worldState.SetState("IronIngotsLocation", new List<ItemLocation>());
        worldState.SetState("CrystalShardsLocation", new List<ItemLocation>());
        
        // Artifacts
        worldState.SetState("BuiltEnchantedStaff",false);
        worldState.SetState("BuiltRunedShield",false);
        worldState.SetState("BuiltCombinedArtifact",false);
    }

    public void UpdateAgentAvailability(int availableVillagers, int availableMessengers, int availableMages)
    {
        worldState.SetState("AvailableVillagers", availableVillagers);
        worldState.SetState("AvailableMessengers", availableMessengers);
        worldState.SetState("AvailableMages", availableMages);
    }

    public void RegisterItemLocation(ItemType itemType, Vector3 position)
    {
        string stateKey = GetLocationStateKeyForItemType(itemType);

        if (worldState.HasState(stateKey))
        {
            var locations = worldState.GetState(stateKey) as List<ItemLocation>;
            if (locations == null)
            {
                locations = new List<ItemLocation>();
                worldState.SetState(stateKey, locations);
            }
            
            bool alreadyExists = locations.Exists(loc => Vector3.Distance(loc.position, position) < 0.1f);
            
            if (!alreadyExists)
            {
                var newLocation = new ItemLocation(itemType, position);
                locations.Add(newLocation);

                Debug.Log($"[WorldState] Registered {itemType} at {position}");
            }
        }
    }

    public void ReserveItemLocation(ItemType itemType, Vector3 position, string agentName)
    {
        string stateKey = GetLocationStateKeyForItemType(itemType);

        if (worldState.HasState(stateKey))
        {
            var locations = worldState.GetState(stateKey) as List<ItemLocation>;
            if (locations != null)
            {
                var location = locations.Find(loc => Vector3.Distance(loc.position, position) < 0.1f);
                if (location != null && !location.isReserved)
                {
                    location.isReserved = true;
                    location.isReservedBy = agentName;
                    Debug.Log($"[WorldState] Reserved {itemType} at {position} by {agentName}");
                }
            }
        }
    }

    public void RemoveItemLocation(ItemType itemType, Vector3 position)
    {
        string stateKey = GetLocationStateKeyForItemType(itemType);
        
        if (worldState.HasState(stateKey))
        {
            var locations = worldState.GetState(stateKey) as List<ItemLocation>;
            if (locations != null)
            {
                locations.RemoveAll(loc => Vector3.Distance(loc.position, position) < 0.1f);
                Debug.Log($"[WorldState] Removed {itemType} from {position}");
            }
        }
    }

    public List<ItemLocation> GetAvailableItemLocations(ItemType itemType)
    {
        string stateKey = GetLocationStateKeyForItemType(itemType);
        
        if (worldState.HasState(stateKey))
        {
            var allLocations = worldState.GetState(stateKey) as List<ItemLocation>;
            if (allLocations != null)
            {
                return allLocations.FindAll(loc => !loc.isReserved);
            }
        }
    
        return new List<ItemLocation>();
    }

    public void CollectItems(ItemType itemType, int amount = 1)
    {
        var stateKey = GetStateKeyForItemType(itemType);

        if (worldState.HasState(stateKey))
        {
            var currentCount = (int)worldState.GetState(stateKey);
            if (currentCount != null)
            {
                worldState.SetState(stateKey, currentCount + amount);
                Debug.Log($"[WorldState] Collected {amount} {itemType} - Total: {currentCount + amount}");
            }
        }
    }
    
    public void RemoveItems(ItemType itemType, int amount = 1)
    {
        var stateKey = GetStateKeyForItemType(itemType);
        
        if (worldState.HasState(stateKey))
        {
            var currentCount = (int)worldState.GetState(stateKey);
            if (currentCount != null)
            {
                worldState.SetState(stateKey, currentCount - amount);
                Debug.Log($"[WorldState] Removed {amount} {itemType} - Total Left: {currentCount - amount}");
            }
        }
    }

    public void SetState(string key, object value)
    {
        worldState.SetState(key, value);
    }

    public object GetState(string key)
    {
        return worldState.GetState(key);
    }

    public bool HasState(string key)
    {
        return worldState.HasState(key);
    }

    private string GetLocationStateKeyForItemType(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.TreeLog => "OakLogsLocation",
            ItemType.IronIngot => "IronIngotsLocation",
            ItemType.CrystalShard => "CrystalShardsLocation",
            _ => String.Empty
        };
    }
    
    private string GetStateKeyForItemType(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.TreeLog => "OakLogs",
            ItemType.IronIngot => "IronIngots",
            ItemType.CrystalShard => "CrystalShards",
            _ => String.Empty
        };
    }
}
