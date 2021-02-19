using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class RangedFloat : RangedValue<float>
{
	public RangedFloat(float min, float max)
	{
		minValue = min;
		maxValue = max;
	}

	public float GetMinMaxValue() => Random.Range(minValue, maxValue);
	public float GetMinValue() => minValue;
	public float GetMaxValue() => maxValue;
}
