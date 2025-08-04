namespace GOAP.Interfaces
{
	public interface IResourceCarrier
	{
		ResourcePickup TargetResourcePickup { get; set; }
		ResourcePickup CurrentResource { get; }
		void PickupResource(ResourcePickup resource);
		void DropResource();
	}
}