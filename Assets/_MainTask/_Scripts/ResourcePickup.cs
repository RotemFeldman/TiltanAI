using System;
using UnityEngine;
using UnityEngine.Serialization;


public class ResourcePickup : MonoBehaviour
{
	public ResourceType ResourceType;
	public bool IsReserved;
	public bool Discovered;
	public string ReservedBy;
	public Vector3 Position;

	private void Awake()
	{
		IsReserved = false;
		ReservedBy = "";
		Position = transform.position;
		Discovered = false;
	}

	public void Reserve(string agentName)
	{
		IsReserved = true;
		ReservedBy = agentName;
	}
}

