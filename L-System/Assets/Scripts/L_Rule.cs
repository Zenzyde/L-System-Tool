using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu()]
public class L_Rule : ScriptableObject
{
	public string StartingSentence;
	public L_Rule_Letter_Replacement[] RuleLetterReplacements;
	public L_Rule_Declaration[] LetterRules;
	[Header("Tick if creating a fractal L-System")] public bool IsFractal;
}

[System.Serializable]
public class L_Rule_Letter_Replacement
{
	public char Identifier;
	public string Replacement;
}

[System.Serializable]
public class L_Rule_Declaration
{
	public char Identifier;
	public EL_Rule_LSystemAction LSystemtAction;
	public RangedFloat MovementAmount;
}

public enum EL_Rule_LSystemAction
{
	makeBranchAndMove, makeFlowerAndMove, makeLeafAndMove, rotateXPositive, rotateXNegative, rotateYPositive, rotateYNegative, rotateZPositive, rotateZNegative, makeTreeState, restoreTreeState, none
}

public enum EL_Rule_BranchType
{
	flower, leaf, branch
}