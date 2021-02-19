using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BranchState : MonoBehaviour
{
	public float startWidth = 0, endWidth = 0;
	public int branchIndex, children = 0;
	public Vector3 branchPosition, branchDirection;
	public BranchType branchType;
	public BranchState branchParent;
	public GameObject branchObj;
	public bool hasBeenVisited = false;
	public MeshRenderer renderer;
	public MeshFilter filter;

	public void SetBranchState(Vector3 pos, Vector3 dir, int index, GameObject branch, BranchType type, BranchState parent,
		MeshRenderer renderer, MeshFilter filter)
	{
		this.branchIndex = index;
		this.branchPosition = pos;
		this.branchDirection = dir;
		this.branchType = type;
		this.branchObj = branch;
		this.hasBeenVisited = false;
		this.branchParent = parent;
		this.renderer = renderer;
		this.filter = filter;
	}

	public void IncChildCound() => children++;

	public void SetVisited() => hasBeenVisited = true;

	public void SetStartWidth(float value) => startWidth = value;

	public void SetEndWidth(float value) => endWidth = value;

	public void ChangeBranchState(BranchType type) => branchType = type;
}

public enum BranchType
{
	trunk, newBranch, branch, branchTip, leaf, flower
}