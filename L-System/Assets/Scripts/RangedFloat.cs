using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class RangedFloat : RangedValue<float>
{
	public float RandomValue
	{
		get
		{
			if (RandValue == 0)
				return RandValue = Random.Range(minValue, maxValue);
			return RandValue;
		}
	}

	public RangedFloat(float min, float max)
	{
		minValue = min;
		maxValue = max;
		RandValue = Random.Range(minValue, maxValue);
	}

	public float GetMinMaxValue() => Random.Range(minValue, maxValue);
	public float GetMinValue() => minValue;
	public float GetMaxValue() => maxValue;
}
