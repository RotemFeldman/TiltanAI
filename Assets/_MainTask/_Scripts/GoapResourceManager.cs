using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class GoapResourceManager : MonoBehaviour
{
	public static GoapResourceManager Instance;

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
	
	public bool TryFindUnreservedResource(ResourceType resourceType)
	{
		var resourcePickup = ResourcePickups.FirstOrDefault(pickup => 
			!pickup.IsReserved && 
			pickup.ResourceType == resourceType);
		return resourcePickup != null;
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
