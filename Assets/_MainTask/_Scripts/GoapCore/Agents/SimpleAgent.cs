using UnityEngine;

namespace GOAP.Agents
{
	public class SimpleAgent : MonoBehaviour
	{
		public bool IsAvailable => !TaskComplete;
		public bool TaskComplete {get; private set;} = false;
		
		public void CompleteTask()
		{
			TaskComplete = true;
		}
	}
}