using UnityEngine;

namespace _MainTask._Scripts
{
	[CreateAssetMenu(fileName = "Resource Pickup", menuName = "Resources", order = 0)]
	public class ResourcePickupData : ScriptableObject
	{
		public ResourcePickup prefab;
		public ResourceType resourceType;
		
		public void Spawn(Vector3 position)
		{
			var resourcePickup = Instantiate(prefab, position, Quaternion.identity);
			resourcePickup.ResourceType = resourceType;
		}
		
	}
}