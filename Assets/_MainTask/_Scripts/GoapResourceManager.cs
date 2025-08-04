using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class GoapResourceManager : MonoBehaviour
{
	public static GoapResourceManager Instance;
	
	[Header("Found Resources")]
	[SerializeField] private int OakLogsFound;
	[SerializeField] int IronIngotsFound;
	[SerializeField] int CrystalsFound;
	
	public int OakLogsFoundCount => OakLogsFound;
	public int IronIngotsFoundCount => IronIngotsFound;
	public int CrystalsFoundCount => CrystalsFound;

	[Header( "Gathered Resources" )]
	[SerializeField] private int OakLogsGathered;
	[SerializeField] int IronIngotsGathered;
	[SerializeField] int CrystalsGathered;
	
	public int OakLogsGatheredCount => OakLogsGathered;
	public int IronIngotsGatheredCount => IronIngotsGathered;
	public int CrystalShardsGatheredCount => CrystalsGathered;
	
	[Header("Resources at build site")]
	[SerializeField] int OakLogsEffective;
	[SerializeField] int IronIngotsEffective;
	[SerializeField] int CrystalsEffective;
	
	public int OakLogsEffectiveCount => OakLogsEffective;
	public int IronIngotsEffectiveCount => IronIngotsEffective;
	public int CrystalsEffectiveCount => CrystalsEffective;
	
	[Header("Known ResourcePickups")]
	[SerializeField] private List<ResourcePickup> ResourcePickups = new();

	[Header("Artifacts")]
	public bool EnchantedStaff = false;
	public bool RunedShield = false;
	public bool CombinedArtifact = false;

	public bool TryFindUnreservedResource(ResourceType resourceType, out ResourcePickup resourcePickup)
	{
		resourcePickup = ResourcePickups.FirstOrDefault(pickup => 
			!pickup.IsReserved && 
			pickup.ResourceType == resourceType);
		return resourcePickup != null;
	}
	
	public bool TryFindUnreservedResource(out ResourcePickup resourcePickup)
	{
		ResourceType neededResourceType = GetNextNeededResourceType();
		
		if (neededResourceType == ResourceType.None)
		{
			resourcePickup = null;
			return false;
		}
		
		resourcePickup = ResourcePickups.FirstOrDefault(pickup => 
			!pickup.IsReserved && 
			pickup.ResourceType == neededResourceType);

		if (resourcePickup != null)
		{
			ResourcePickups.Remove(resourcePickup);
			resourcePickup.Reserve();
		}
		
		return resourcePickup != null;
	}
	
	public bool HasUnreservedResource(ResourceType resourceType)
	{
		var resourcePickup = ResourcePickups.FirstOrDefault(pickup => 
			!pickup.IsReserved && 
			pickup.ResourceType == resourceType);
		return resourcePickup != null;
	}
	
	public bool HasUnreservedResource()
	{
		ResourceType neededResourceType = GetNextNeededResourceType();
		if (neededResourceType == ResourceType.None)
			return false;
			
		return ResourcePickups.Any(pickup => 
			!pickup.IsReserved && 
			pickup.ResourceType == neededResourceType);
	}

	public ResourceType GetNextNeededResourceType()
	{
		// Priority order: Oak Logs first, then Crystals, then Iron
		if (OakLogsGatheredCount < 5)
		{
			return ResourceType.OakLog;
		}
		
		if (CrystalShardsGatheredCount < 4)
		{
			return ResourceType.CrystalShard;
		}
		
		if (IronIngotsGatheredCount < 5)
		{
			return ResourceType.IronIngot;
		}
		
		// All resources collected
		return ResourceType.None;
	}

	public void AddFoundResource(ResourceType resourceType)
	{
		switch (resourceType)
		{
			case ResourceType.OakLog:
				OakLogsFound++;
				break;
			case ResourceType.IronIngot:
				IronIngotsFound++;
				break;
			case ResourceType.CrystalShard:
				CrystalsFound++;
				break;
		}
	}

	public void AddGatheredResource(ResourceType resourceType)
	{
		switch (resourceType)
		{
			case ResourceType.OakLog:
				OakLogsGathered++;
				OakLogsEffective++;
				break;
			case ResourceType.IronIngot:
				IronIngotsGathered++;
				IronIngotsEffective++;
				break;
			case ResourceType.CrystalShard:
				CrystalsGathered++;
				CrystalsEffective++;
				break;
		}
	}

	public void AddResourcePickup(ResourcePickup pickup)
	{
		if (!ResourcePickups.Contains(pickup) && pickup.Discovered)
		{
			ResourcePickups.Add(pickup);
			AddFoundResource(pickup.ResourceType);
		}
	}

	public void UseResource(ResourceType resourceType, int amount)
	{
		switch (resourceType)
		{
			case ResourceType.OakLog:
				OakLogsEffective -= amount;
				if (OakLogsEffective < 0)
					Debug.LogWarning($"OakLogsEffective < 0, {OakLogsEffective}");
				break;
			case ResourceType.IronIngot:
				IronIngotsEffective -= amount;
				if (IronIngotsEffective < 0)
					Debug.LogWarning($"IronIngotsEffective < 0, {IronIngotsEffective}");
				break;
			case ResourceType.CrystalShard:
				CrystalsEffective -= amount;
				if (CrystalsEffective < 0)
					Debug.LogWarning($"CrystalsEffective < 0, {CrystalsEffective}");
				break;
			default:
				break;
		}
	}
	
	private void Awake()
	{
		if(Instance == null)
			Instance = this;
	}
}