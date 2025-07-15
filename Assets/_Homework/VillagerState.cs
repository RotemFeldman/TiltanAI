using System;
using Unity.Behavior;

[BlackboardEnum]
public enum VillagerState
{
    SearchForResource,
	ChopTree,
	RefineCrystals,
	CollectIronIngot,
	MarkResourceForPickup,
	CarryResourceToBuild,
	Idle
}
