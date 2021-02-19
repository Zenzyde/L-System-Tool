using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class RangedValue<T>
{
	public T minValue;
	public T maxValue;
}