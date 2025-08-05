using System.Linq;
using _MainTask._Scripts;
using GOAP.Agents;
using GOAP.Interfaces;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP
{
	public interface IActionStrategy
	{
		bool CanPerform { get; }
		bool Complete { get; }

		void Start()
		{
			// noop
		}

		void Update(float deltaTime)
		{
			// noop
		}

		void Stop()
		{
			// noop
		}
	}

	public class IdleStrategy : IActionStrategy
	{
		public bool CanPerform => true;
		public bool Complete { get; private set; }

		private readonly CountdownTimer timer;

		public IdleStrategy(float duration)
		{
			timer = new CountdownTimer(duration);
			timer.OnTimerStart += (() => Complete = false);
			timer.OnTimerStop += (() => Complete = true);
		}
		
		public void Start()
		{
			timer.Start();
		}

		public void Update(float deltaTime)
		{
			timer.Tick(deltaTime);
		}
	}
	
	public class WanderStrategy : IActionStrategy
	{
		readonly NavMeshAgent agent;
		readonly float wanderRadius;
		
		 public bool CanPerform => !Complete;
		 public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;
		
		public WanderStrategy(NavMeshAgent agent, float wanderRadius)
		{
			this.agent = agent;
			this.wanderRadius = wanderRadius;
		}
		
		public void Start()
		{
			for (int i = 0; i < 10; i++)
			{
				Vector3 randomDirection = (Random.insideUnitCircle * wanderRadius);
				randomDirection.y = 0;
				NavMeshHit hit;
		
				if (NavMesh.SamplePosition(agent.transform.position + randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
				{
					agent.SetDestination(hit.position);
					return;
				}
			}
		}
	}
	
	public class MoveToStrategy : IActionStrategy
	{
		readonly NavMeshAgent agent;
		readonly Vector3 destination;
		
		public bool CanPerform => !Complete;
		public bool Complete => agent.remainingDistance <= 2f && !agent.pathPending;
		
		public MoveToStrategy(NavMeshAgent agent, Vector3 destination)
		{
			this.agent = agent;
			this.destination = destination;
		}

		public void Start()
		{
			agent.SetDestination(destination);
		}

		public void Stop()
		{
			agent.ResetPath();
		}
	}

	
	
	public class CraftCombinedArtifactStrategy : IActionStrategy
	{
		readonly GoapResourceManager resourceManager;
		private readonly CountdownTimer timer;

		public bool CanPerform => !Complete;
		public bool Complete { get; private set; }

		public CraftCombinedArtifactStrategy(GoapResourceManager resourceManager, float duration)
		{
			this.resourceManager = resourceManager;
			timer = new CountdownTimer(duration);
			timer.OnTimerStart += (() => Complete = false);
			timer.OnTimerStop += (() => Complete = true);
		}

		public void Start()
		{
			timer.Start();

		}

		public void Update(float deltaTime)
		{
			timer.Tick(deltaTime);
		}

		public void Stop()
		{
			if (Complete)
			{
				resourceManager.CombinedArtifact = true;
				resourceManager.EnchantedStaff = false;
				resourceManager.RunedShield = false;
				Debug.Log("Successfully crafted Combined Artifact!");
			}
			
		}
	}
	
	public class CraftEnchantedStaffStrategy : IActionStrategy
	{
		readonly GoapResourceManager resourceManager;
		private readonly CountdownTimer timer;

		public bool CanPerform => !Complete;
		public bool Complete { get; private set; }
		
		public CraftEnchantedStaffStrategy(GoapResourceManager resourceManager, float duration)
		{
			this.resourceManager = resourceManager;
			timer = new CountdownTimer(duration);
			timer.OnTimerStart += (() => Complete = false);
			timer.OnTimerStop += (() => Complete = true);
		}

		public void Start()
		{
			timer.Start();
		}

		public void Update(float deltaTime)
		{
			timer.Tick(deltaTime);
		}
		
		public void Stop()
		{
			if (Complete)
			{
				resourceManager.EnchantedStaff = true;
				resourceManager.UseResource(ResourceType.OakLog,5);
				resourceManager.UseResource(ResourceType.IronIngot,3);
				Debug.Log("Successfully crafted Enchanted Staff!");
			}
			else
			{
				Debug.Log("Enchanted Staff crafting was interrupted - no resources consumed");
			}
		}
	}
	
	public class CraftRunedShieldStrategy : IActionStrategy
	{
		readonly GoapResourceManager resourceManager;
		private readonly CountdownTimer timer;
		
		public bool CanPerform => !Complete;
		public bool Complete { get; private set; }
		
		public CraftRunedShieldStrategy(GoapResourceManager resourceManager, float duration)
		{
			this.resourceManager = resourceManager;
			timer = new CountdownTimer(duration);
			timer.OnTimerStart += (() => Complete = false);
			timer.OnTimerStop += (() => Complete = true);
		}

		public void Start()
		{
			timer.Start();
			
		}

		public void Update(float deltaTime)
		{
			timer.Tick(deltaTime);
		}
		
		public void Stop()
		{
			if (Complete)
			{
				resourceManager.RunedShield = true;
				resourceManager.UseResource(ResourceType.CrystalShard,4); 
				resourceManager.UseResource(ResourceType.IronIngot,2);
				Debug.Log("Successfully crafted Runed Shield!");
			}
			else
			{
				Debug.Log("Runed Shield crafting was interrupted - no resources consumed");
			}
		}
	}
	
	public class DeliverResourceStrategy<T> : IActionStrategy where T : IResourceCarrier 
	{
		readonly T agent;

		public bool CanPerform => !Complete;
		public bool Complete => true;// { get; private set; }//agent.CurrentResource == null;
		
		public DeliverResourceStrategy( T agent)
		{
			this.agent = agent;
		}

		public void Update(float deltaTime)
		{
			//agent.DropResource();
			//Complete = true;
		}
	}

	public class PickupResourceStrategy<T> : IActionStrategy where T : IResourceCarrier
	{
		readonly T agent;

		public bool CanPerform => !Complete;
		public bool Complete => true;// { get; private set; } = false;
		
		public PickupResourceStrategy(T agent)
		{
			this.agent = agent;
			Debug.Log($"PickupResourceStrategy created for {agent}");
		}
    
		public void Start()
		{
			Debug.Log($"PickupResourceStrategy STARTED for {agent}");
			Debug.Log($"TargetResourcePickup: {agent.TargetResourcePickup?.name}");
		}
    
		public void Update(float deltaTime)
		{
			Debug.Log($"PickupResourceStrategy UPDATE called for {agent}");
			Debug.Log($"TargetResourcePickup: {agent.TargetResourcePickup?.name}");
        
			if (agent.TargetResourcePickup == null)
			{
				Debug.LogError($"TargetResourcePickup is null! Cannot pickup resource.");
				//Complete = true;
				return;
			}
        
			//agent.PickupResource(agent.TargetResourcePickup);
			Debug.Log($"PickupResource called, setting Complete = true");
			//Complete = true;
		}
    
		public void Stop()
		{
			Debug.Log($"PickupResourceStrategy STOPPED for {agent}");
		}

		
	}

	public class RequestPickupStrategy<T> : IActionStrategy where T : class, IResourceCarrier
	{
		// private readonly CountdownTimer timer;
		readonly T agent;
		public bool CanPerform => !Complete;
		public bool Complete { get; private set; }

		public RequestPickupStrategy(T agent)
		{
			this.agent = agent;
		// 	timer = new CountdownTimer(2f);
		// 	timer.OnTimerStart += (() => Complete = false);
		// 	timer.OnTimerStop += (() => Complete = true);
		 }

		public void Start()
		{
			//timer.Start();
			var pickup = GoapResourceManager.Instance.TryFindUnreservedResource(out var resource);
			if (pickup)
			{
				agent.TargetResourcePickup = resource;
				resource.Reserve();
			}
			Complete = true;
		}

		public void Update(float deltaTime)
		{
			//timer.Tick(deltaTime);
		}
	}
	
	public class MoveToPickupStrategy<T> : IActionStrategy where T : class, IResourceCarrier
	{
		readonly NavMeshAgent agent;
		readonly T agentTarget;
		private bool complete = false;
		
		public bool CanPerform => !Complete;
		public bool Complete => (agent.remainingDistance <= 2f && !agent.pathPending) || complete;
		
		public MoveToPickupStrategy(NavMeshAgent agent, T agentTarget)
		{
			this.agent = agent;
			this.agentTarget = agentTarget;
		}

		public void Start()
		{
			complete = false;
			
			if (agentTarget.TargetResourcePickup == null)
			{
				complete = true;
				return;
			}
			
			agent.SetDestination(agentTarget.TargetResourcePickup.transform.position);
		}

		public void Stop()
		{
			agent.ResetPath();
		}
	}

	// public class SearchForResourceStrategy<T> : IActionStrategy where T : GoapAgent
	// {
	// 	readonly ResourceType resource;
	// 	private readonly T agent;
	//
	// 	public bool CanPerform => !Complete;
	// 	public bool Complete {get; private set;}
	//
	// 	public SearchForResourceStrategy(ResourceType resource, MetaAgent metaAgent)
	// 	{
	// 		this.resource = resource;
	// 		this.metaAgent = metaAgent;
	// 	}
	// 	
	// 	public void Start()
	// 	{
	// 		agent = GetAvailableAgent();
	// 		if (agent != null)
	// 		{
	// 			//agent.StartDelivaryTask();
	// 		}
	// 	}
	//
	// 	private T GetAvailableAgent()
	// 	{
	// 		if (typeof(T) == typeof(VillagerAgent))
	// 			return metaAgent.GetAvailableVillagers().FirstOrDefault() as T;
	// 		else if (typeof(T) == typeof(MessengerAgent))
	// 			return metaAgent.GetAvailableMessengers().FirstOrDefault() as T;
	// 		else if (typeof(T) == typeof(MageAgent))
	// 			return metaAgent.GetAvailableMage() as T;
	// 			
	// 		return null;
	// 	}
	// }
}