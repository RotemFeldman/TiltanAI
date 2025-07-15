using System;
using Unity.Behavior;

[BlackboardEnum]
public enum VillagerStates
{
    SearchForResource,
	ChopTree,
	RefineCrystals,
	CollectIronIngot,
	MarkResourceForPickup,
	CarryResourceToBuild,
	Idle
}
