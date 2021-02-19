using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SystemState
{
	public Vector3 position;
	public Quaternion rotation;

	public SystemState(Vector3 position, Quaternion rotation)
	{
		this.position = position;
		this.rotation = rotation;
	}
}
