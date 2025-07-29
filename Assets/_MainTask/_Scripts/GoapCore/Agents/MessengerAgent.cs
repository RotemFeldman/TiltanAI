using GOAP.Interfaces;
using UnityEngine;

namespace GOAP.Agents
{
	public class MessengerAgent : SimpleAgent , IResourceCarrier
	{
		public ResourceType CurrentResource { get; private set; }
		
		public void PickupResource(ResourcePickup resource)
		{
			CurrentResource = resource.ResourceType;
		}

		public void DropResource()
		{
			CurrentResource = ResourceType.None;
		}
		
		public void StartDelivaryTask()
		{
			Debug.Log("delivery task started");
		}
	}
}