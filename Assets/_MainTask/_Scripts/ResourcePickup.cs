using System;
using UnityEngine;
using UnityEngine.Serialization;


public class ResourcePickup : MonoBehaviour
{
	public ResourceType ResourceType;
	public bool IsReserved;
	[FormerlySerializedAs("IsReservedBy")]
	public string ReservedBy;
	public Vector3 Position;

	private void Awake()
	{
		IsReserved = false;
		ReservedBy = "";
		Position = transform.position;
	}

	public void Reserve(string agentName)
	{
		IsReserved = true;
		ReservedBy = agentName;
	}
}

