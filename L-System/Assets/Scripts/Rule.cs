using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Rule
{
	public string sentence;
	public float segmentLength = 1f;
	[Range(0f, 1f)] public float rndChance;
	public RangedFloat zAxisRotationAmount, yAxisRotationAmount, xAxisRotationAmount;
	public RulePair[] rulePairs;
	public bool isFractal;
	[HideInInspector] public SystemType systemType;

	[System.Serializable]
	public class RulePair
	{
		public char identifier;
		public string replacement;
	}
}

public enum SystemType
{
	fern, flower, tree, sierpinskiArrowhead, rose, fractalTree, hilbert2D, fern1, mirrorFern, simpleAlgae, clusterAlgae, flower3D
}
