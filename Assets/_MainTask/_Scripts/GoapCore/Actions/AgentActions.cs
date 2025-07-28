using Unity.Behavior;

[BlackboardEnum]
public enum VillagerActions
{
	Idle,
	SearchForResources,
	ChopTree,
	RefineCrystals,
	CollectIronIngot,
	MarkResourceForPickup,
	CarryResourceToBuild,
	TaskCompleted
}

[BlackboardEnum]
public enum MessengerActions
{
	Idle,
	CarryResourceToBuild,
	TaskCompleted
}

[BlackboardEnum]
public enum MageActions
{
	Idle,
	SearchForResources,
	BuildEnchantedStaff,
	BuildRunedShield,
	ConnectArtifacts
}

[BlackboardEnum]
public enum AgentType
{
	Villager,
	Messenger,
	Mage
}

[BlackboardEnum]
public enum ItemType
{
	IronIngot,
	CrystalShard,
	TreeLog
}