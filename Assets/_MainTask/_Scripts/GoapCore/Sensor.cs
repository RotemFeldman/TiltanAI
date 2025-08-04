using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace GOAP
{
	public class Sensor : MonoBehaviour
	{
		[SerializeField] float detectionRadius = 10f;
		[SerializeField] private float timerInterval = 1f;
		[SerializeField] private List<string> relevantTags;
		[SerializeField] private LayerMask resourceLayer = 1 << 8; // Default to layer 8, change as needed
		
		GoapResourceManager resourceManager;
		
		public event Action OntargetChanged = delegate { };
		
		public Vector3 TargetPosition => target ? target.transform.position : Vector3.zero;
		public bool IsTargetInRange => TargetPosition != Vector3.zero;

		private GameObject target;
		Vector3 lastKnownPosition;
		private CountdownTimer timer;

		public void AddRelevantTag(string tag)
		{
			relevantTags.Add(tag);
		}
		
		private void Awake()
		{
			resourceManager = GoapResourceManager.Instance;
			if (resourceManager == null)
			{
				resourceManager = FindObjectOfType<GoapResourceManager>();
			}
		}

		private void Start()
		{
			timer = new CountdownTimer(timerInterval);
			timer.OnTimerStop += () =>
			{
				PerformOverlapCheck();
				timer.Start();
			};
			timer.Start();
		}

		private void Update()
		{
			timer.Tick(Time.deltaTime);
		}

		private void PerformOverlapCheck()
		{
			// Check for resources using overlap sphere
			Collider[] resourceColliders = Physics.OverlapSphere(transform.position, detectionRadius, resourceLayer);
			
			// Process found resources
			foreach (Collider collider in resourceColliders)
			{
				ResourcePickup pickup = collider.GetComponent<ResourcePickup>();
				if (pickup != null && !pickup.Discovered)
				{
					// Mark as discovered and add to manager
					pickup.Discovered = true;
					
					if (resourceManager != null)
					{
						resourceManager.AddResourcePickup(pickup);
						Debug.Log($"Sensor discovered new resource: {pickup.ResourceType} at {collider.transform.position}");
					}
				}
			}
			
			// Check for tagged objects (original functionality)
			Collider[] taggedColliders = Physics.OverlapSphere(transform.position, detectionRadius);
			GameObject closestTaggedObject = null;
			float closestDistance = float.MaxValue;
			
			foreach (Collider collider in taggedColliders)
			{
				if (relevantTags.Contains(collider.tag))
				{
					float distance = Vector3.Distance(transform.position, collider.transform.position);
					if (distance < closestDistance)
					{
						closestDistance = distance;
						closestTaggedObject = collider.gameObject;
					}
				}
			}
			
			UpdateTargetPosition(closestTaggedObject);
		}

		void UpdateTargetPosition(GameObject target = null)
		{
			this.target = target;
			if (IsTargetInRange && (lastKnownPosition != TargetPosition || lastKnownPosition != Vector3.zero))
			{
				lastKnownPosition = TargetPosition;
				OntargetChanged.Invoke();
			}
		}

		private bool IsInResourceLayer(GameObject obj)
		{
			return (resourceLayer.value & (1 << obj.layer)) != 0;
		}

		void OnDrawGizmosSelected()
		{
			Gizmos.color = IsTargetInRange ? Color.red : Color.green;
			Gizmos.DrawWireSphere(transform.position, detectionRadius);
			
			// Draw resource detection range in blue
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(transform.position, detectionRadius * 0.8f);
		}
	}
}