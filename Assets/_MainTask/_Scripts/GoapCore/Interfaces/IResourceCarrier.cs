namespace GOAP.Interfaces
{
	public interface IResourceCarrier
	{
		ResourceType CurrentResource { get; }
		void PickupResource(ResourcePickup resource);
		void DropResource();
		void StartDelivaryTask();
	}
}