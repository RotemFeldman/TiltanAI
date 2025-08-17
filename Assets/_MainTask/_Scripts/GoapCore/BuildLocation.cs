using UnityEngine;

namespace GOAP
{
	[DefaultExecutionOrder(0)]
	public class BuildLocation : MonoBehaviour
	{
		private static BuildLocation instance;

		public static BuildLocation Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindFirstObjectByType<BuildLocation>();
				}
				return instance;
			}
		}

		private void Awake()
		{
			if (instance != null && instance != this)
			{
				Destroy(gameObject);
				return;
			}

			instance = this;
			DontDestroyOnLoad(gameObject);
		}
	}
}