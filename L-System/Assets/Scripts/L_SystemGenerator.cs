using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System;

public class L_SystemGenerator : MonoBehaviour
{
	[SerializeField] private GameObject leaf, flower;
	[SerializeField] private Material branchMat, leafMat, flowerMat;
	[SerializeField] private int iterations, branchFaces = 4;
	[SerializeField] private float treeTrunkRadius = 3.5f, leafBranchRadius = 0.2f, leafRadius = 0.2f;
	[SerializeField] private bool useDynamicBranchWidth;
	[SerializeField] private CustomRule customRule;
	[SerializeField] private SystemType systemType;
	[SerializeField] [Range(0f, 1f)] private float nonRuleRandomness;

	private Stack<SystemState> lStack = new Stack<SystemState>();

	private List<BranchState> treeList = new List<BranchState>();

	private string currentSentence, sentence;
	private readonly string[] threeDAlphabet = new string[] { "X", "Y", "F", "f", "S", "s", "+", "-", "*", "/", "<", ">" };
	private readonly string[] twoDAlphabet = new string[] { "X", "Y", "F", "f", "S", "s", "+", "-" };

	private Rule currentRule;
	private Rule[] rules;
	private bool use3DAlphabet;

	private Transform turtleDummy;
	private GameObject l_SystemParent;

	public void CreateLSystem()
	{
		if (iterations < 1)
			iterations = 1;

		turtleDummy = new GameObject("Turtle_Dummy").transform;
		turtleDummy.position = transform.position;

		InitiateRules();

		currentRule = customRule == null ? rules.Where(x => x.systemType == systemType).FirstOrDefault() :
			customRule.rule;
		if (customRule == null)
			currentRule.rndChance = nonRuleRandomness;

		l_SystemParent = new GameObject("L-System_" + (customRule == null ? systemType.ToString() : customRule.name) +
			(currentRule.rndChance > 0.0f ? "_rnd" : string.Empty));
		l_SystemParent.transform.position = turtleDummy.position;

		sentence = currentRule.sentence;

		use3DAlphabet = Use3DAlphabet();

		BuildSentence();
		GenerateLSystem();
		StartTreeMesh();
		MakeTotalMesh();
	}

	public void DestroySystem()
	{
		GameObject.DestroyImmediate(l_SystemParent);
		l_SystemParent = null;
	}

	public void SaveSystem()
	{
		string path = $"Assets/Prefabs/L-System_{(customRule == null ? systemType.ToString() : customRule.name)}.prefab";
		string meshPath = $"Assets/Prefabs/L_System_{(customRule == null ? systemType.ToString() : customRule.name)}/L-System_{(customRule == null ? systemType.ToString() : customRule.name)}_mesh.asset";
		string meshPathSameFolder = $"Assets/Prefabs/L_System_{(customRule == null ? systemType.ToString() : customRule.name)}_mesh.asset";
		Mesh mesh = l_SystemParent.GetComponent<MeshFilter>().sharedMesh;
		bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if (!exists)
		{
			AssetDatabase.CreateAsset(mesh, meshPathSameFolder);
			AssetDatabase.SaveAssets();
			PrefabUtility.SaveAsPrefabAsset(l_SystemParent, path);
		}
		else
		{
			for (int i = 0; i < 25; i++)
			{
				path = $"Assets/Prefabs/L-System_{(customRule == null ? systemType.ToString() : customRule.name) + i}.prefab";
				meshPath = $"Assets/Prefabs/L_System_{(customRule == null ? systemType.ToString() : customRule.name) + i}/L-System_{(customRule == null ? systemType.ToString() : customRule.name) + i}_mesh.asset";
				meshPathSameFolder = $"Assets/Prefabs/L-System_{(customRule == null ? systemType.ToString() : customRule.name) + i}_mesh.asset";
				exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (!exists)
				{
					AssetDatabase.CreateAsset(mesh, meshPathSameFolder);
					AssetDatabase.SaveAssets();
					PrefabUtility.SaveAsPrefabAsset(l_SystemParent, path);
					break;
				}
				else
					continue;
			}
		}
	}

	bool Use3DAlphabet()
	{
		foreach (Rule.RulePair rulePair in currentRule.rulePairs)
		{
			foreach (Char c in rulePair.replacement)
			{
				if (c == '<' || c == '>' || c == '*' || c == '/')
					return true;
			}
		}
		return false;
	}

	void InitiateRules()
	{
		rules = new Rule[]
		{
			new Rule()
			{
				sentence = "X",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'X',
						replacement = "*[FX]*[-FX]*[+FX]"
					},
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "FF"
					}
				},
				zAxisRotationAmount = new RangedFloat(35f, 35f),
				yAxisRotationAmount = new RangedFloat(12f, 12f),
				xAxisRotationAmount = new RangedFloat(12f, 12f),
				systemType = SystemType.flower3D
			},
			new Rule()
			{
				sentence = "X",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'X',
						replacement = "[FX][-FX][+FX]"
					},
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "FF"
					}
				},
				zAxisRotationAmount = new RangedFloat(35f, 35f),
				yAxisRotationAmount = new RangedFloat(0, 0),
				xAxisRotationAmount = new RangedFloat(0, 0),
				systemType = SystemType.flower
			},
			new Rule()
			{
				sentence = "X",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'X',
						replacement = "F-[[X]+X]+F[+FX]-X"
					},
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "FF"
					}
				},
				zAxisRotationAmount = new RangedFloat(22.5f, 22.5f),
				yAxisRotationAmount = new RangedFloat(12f, 12f),
				xAxisRotationAmount = new RangedFloat(12f, 12f),
				systemType = SystemType.fern
			},
			new Rule()
			{
				sentence = "F",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "FF+[+F-F-FX]-[-F+F+FX]"
					}
				},
				zAxisRotationAmount = new RangedFloat(22.5f, 22.5f),
				yAxisRotationAmount = new RangedFloat(12f, 12f),
				xAxisRotationAmount = new RangedFloat(12f, 12f),
				systemType = SystemType.tree
			},
			new Rule()
			{
				sentence = "F",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "f-F-f"
					},
					new Rule.RulePair()
					{
						identifier = 'f',
						replacement = "F+f+F"
					}
				},
				zAxisRotationAmount = new RangedFloat(60f, 60f),
				yAxisRotationAmount = new RangedFloat(12f, 12f),
				xAxisRotationAmount = new RangedFloat(12f, 12f),
				systemType = SystemType.sierpinskiArrowhead,
				isFractal = true
			},
			new Rule()
			{
				sentence = "Y",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'Y',
						replacement = "[*++X]FY"
					},
					new Rule.RulePair()
					{
						identifier = 'Y',
						replacement = "[/++X]FY"
					},
					new Rule.RulePair()
					{
						identifier = 'Y',
						replacement = "[*--X]FY"
					},
					new Rule.RulePair()
					{
						identifier = 'Y',
						replacement = "[/--X]FY"
					}
				},
				zAxisRotationAmount = new RangedFloat(7f, 20f),
				yAxisRotationAmount = new RangedFloat(30f, 160f),
				xAxisRotationAmount = new RangedFloat(12f, 12f),
				systemType = SystemType.rose
			},
			new Rule()
			{
				sentence = "X",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "FF"
					},
					new Rule.RulePair()
					{
						identifier = 'X',
						replacement = "F[-X]+X"
					}
				},
				zAxisRotationAmount = new RangedFloat(45f, 45f),
				yAxisRotationAmount = new RangedFloat(0, 0),
				xAxisRotationAmount = new RangedFloat(0, 0),
				systemType = SystemType.fractalTree
			},
			new Rule()
			{
				sentence = "S",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'S',
						replacement = "+sF-SFS-Fs+"
					},
					new Rule.RulePair()
					{
						identifier = 's',
						replacement = "-SF+sFs+FS-"
					}
				},
				zAxisRotationAmount = new RangedFloat(90f, 90f),
				yAxisRotationAmount = new RangedFloat(0, 0),
				xAxisRotationAmount = new RangedFloat(0, 0),
				systemType = SystemType.hilbert2D,
				isFractal = true
			},
			new Rule()
			{
				sentence = "X",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'X',
						replacement = "F[+X]F[-X]+X"
					},
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "FF"
					}
				},
				zAxisRotationAmount = new RangedFloat(20f, 20f),
				yAxisRotationAmount = new RangedFloat(0, 0),
				xAxisRotationAmount = new RangedFloat(0, 0),
				systemType = SystemType.fern1
			},
			new Rule()
			{
				sentence = "X",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'X',
						replacement = "F[+X][-X]FX"
					},
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "FF"
					}
				},
				zAxisRotationAmount = new RangedFloat(25.7f, 25.7f),
				yAxisRotationAmount = new RangedFloat(0, 0),
				xAxisRotationAmount = new RangedFloat(0, 0),
				systemType = SystemType.mirrorFern
			},
			new Rule()
			{
				sentence = "F",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "F[+F]F[-F]F"
					}
				},
				zAxisRotationAmount = new RangedFloat(25.7f, 25.7f),
				yAxisRotationAmount = new RangedFloat(0, 0),
				xAxisRotationAmount = new RangedFloat(0, 0),
				systemType = SystemType.simpleAlgae
			},
			new Rule()
			{
				sentence = "F",
				rulePairs = new Rule.RulePair[]
				{
					new Rule.RulePair()
					{
						identifier = 'F',
						replacement = "F[+F]F[-F][F]"
					}
				},
				zAxisRotationAmount = new RangedFloat(20f, 20f),
				yAxisRotationAmount = new RangedFloat(0, 0),
				xAxisRotationAmount = new RangedFloat(0, 0),
				systemType = SystemType.clusterAlgae
			}
		};
	}

	void BuildSentence()
	{
		currentSentence = sentence;

		StringBuilder sb = new StringBuilder();

		System.Random rnd = new System.Random();

		for (int i = 0; i < iterations; i++)
		{
			foreach (char c in currentSentence)
			{
				if (c != '[' && c != ']' && (float)rnd.NextDouble() > 1f - currentRule.rndChance)
				{
					// Check: c != '[' && c != ']' => for avoiding messing up the stack later on when generating system
					if (use3DAlphabet)
					{
						string toAppend = threeDAlphabet[rnd.Next(0, threeDAlphabet.Length)];
						while (toAppend == "X" || toAppend == "Y" || toAppend == "F" || toAppend == "f")
						{
							toAppend = threeDAlphabet[rnd.Next(0, threeDAlphabet.Length)];
						}
						sb.Append(toAppend);
					}
					else
					{
						string toAppend = twoDAlphabet[rnd.Next(0, twoDAlphabet.Length)];
						while (toAppend == "X" || toAppend == "Y" || toAppend == "F" || toAppend == "f")
						{
							toAppend = twoDAlphabet[rnd.Next(0, twoDAlphabet.Length)];
						}
						sb.Append(toAppend);
					}
					//continue;
				}
				List<string> replacements = new List<string>();
				foreach (Rule.RulePair rulePair in currentRule.rulePairs)
				{
					if (rulePair.identifier == c)
						replacements.Add(rulePair.replacement);
				}
				sb.Append(replacements.Count == 0 ? c.ToString() :
					replacements.Count == 1 ? replacements[0] :
					replacements[rnd.Next(0, replacements.Count)]);
			}

			currentSentence = sb.ToString();
			sb = new StringBuilder();
		}
	}

	#region MeshObjectGeneration
	void GenerateLSystem()
	{
		treeList.Clear();
		bool createNewBranch = false;
		int branchDepth = 0, trunkDepth = -1;
		bool madeTrunk = false;
		Vector3 initialPosition;
		BranchState parent;
		int counter = 0;
		foreach (char c in currentSentence)
		{
			switch (c)
			{
				case 'F':
				case 'f':
					initialPosition = turtleDummy.position;
					turtleDummy.Translate(Vector3.up * currentRule.segmentLength);
					Vector3 branchDir = (turtleDummy.position - initialPosition).normalized;

					if (CheckOverlapForNewBranch(initialPosition, branchDir))
						break;

					GameObject treeBranch = new GameObject("TreeBranch");
					MeshFilter filter = treeBranch.AddComponent<MeshFilter>();
					MeshRenderer renderer = treeBranch.AddComponent<MeshRenderer>();
					renderer.material = branchMat;
					treeBranch.transform.position = initialPosition;
					treeBranch.transform.rotation = turtleDummy.rotation;
					treeBranch.transform.SetParent(l_SystemParent.transform);

					//First trunk
					if (!madeTrunk)
					{
						madeTrunk = true;
						trunkDepth = branchDepth;
						BranchState branchState = treeBranch.AddComponent<BranchState>();
						if (iterations == 1)
							branchState.SetBranchState(initialPosition, branchDir, 0, treeBranch, BranchType.branch, null,
							renderer, filter);
						else
							branchState.SetBranchState(initialPosition, branchDir, 0, treeBranch, BranchType.trunk, null,
							renderer, filter);
						treeList.Add(branchState);
					}
					//Branch-segments
					else
					{
						if (createNewBranch)
						{
							BranchState branchState;
							if (branchDepth == trunkDepth)
							{
								branchState = treeBranch.AddComponent<BranchState>();
								branchState.SetBranchState(initialPosition, branchDir, 0, treeBranch, BranchType.newBranch, null,
								renderer, filter);
								treeList.Add(branchState);
							}
							else
							{
								parent = treeList.Where(x =>
									x.branchPosition + x.branchDirection * currentRule.segmentLength == initialPosition &&
									x.branchType != BranchType.flower && x.branchType != BranchType.leaf).FirstOrDefault();
								if (parent == null)
								{
									parent = treeList.Where(x =>
									x.branchPosition == initialPosition && x.branchParent.branchDirection == x.branchDirection &&
									x.branchType != BranchType.flower && x.branchType != BranchType.leaf).FirstOrDefault();
								}
								if (parent == null)
								{
									parent = treeList.Where(x => x.branchPosition == initialPosition &&
									x.branchType != BranchType.flower && x.branchType != BranchType.leaf).FirstOrDefault();
								}
								branchState = treeBranch.AddComponent<BranchState>();
								branchState.SetBranchState(initialPosition, branchDir, parent.branchIndex + 1, treeBranch,
								parent.branchDirection == branchDir ? BranchType.branch : BranchType.newBranch, parent,
								renderer, filter);
								treeList.Add(branchState);
								parent.IncChildCound();
							}
						}
						else
						{
							parent = treeList.Where(x =>
								x.branchPosition + x.branchDirection * currentRule.segmentLength == initialPosition &&
									x.branchType != BranchType.flower && x.branchType != BranchType.leaf).FirstOrDefault();
							if (parent == null)
							{
								parent = treeList.Where(x =>
								x.branchPosition == initialPosition && x.branchParent.branchDirection == x.branchDirection &&
									x.branchType != BranchType.flower && x.branchType != BranchType.leaf).FirstOrDefault();
							}
							if (parent == null)
							{
								parent = treeList.Where(x => x.branchPosition == initialPosition &&
								x.branchType != BranchType.flower && x.branchType != BranchType.leaf).FirstOrDefault();
							}
							BranchState branchState = treeBranch.AddComponent<BranchState>();
							branchState.SetBranchState(initialPosition, branchDir, parent.branchIndex + 1, treeBranch, BranchType.branch, parent,
								renderer, filter);
							treeList.Add(branchState);
							parent.IncChildCound();
						}
					}

					if (createNewBranch)
						createNewBranch = false;
					break;
				case 'X':
					initialPosition = turtleDummy.position;
					turtleDummy.Translate(Vector3.up * currentRule.segmentLength);
					Vector3 leafDir = (turtleDummy.position - initialPosition).normalized;

					if (CheckOverlapForNewLeaf(initialPosition, leafDir))
					{
						turtleDummy.position = initialPosition;
						break;
					}

					if (leaf != null)
					{
						GameObject leafObj = Instantiate(leaf, initialPosition, turtleDummy.rotation, l_SystemParent.transform);
						BranchState branchState = leafObj.AddComponent<BranchState>();
						branchState.SetBranchState(initialPosition, leafDir, 0, leafObj, BranchType.leaf, null,
						leafObj.GetComponent<MeshRenderer>(), leafObj.GetComponent<MeshFilter>());
						branchState.IncChildCound();
						treeList.Add(branchState);
					}
					else
					{
						GameObject treeleaf = new GameObject("TreeLeaf");
						filter = treeleaf.AddComponent<MeshFilter>();
						renderer = treeleaf.AddComponent<MeshRenderer>();
						renderer.material = leafMat;
						treeleaf.transform.position = initialPosition;
						treeleaf.transform.rotation = turtleDummy.rotation;
						treeleaf.transform.SetParent(l_SystemParent.transform);

						BranchState branchState = treeleaf.AddComponent<BranchState>();
						branchState.SetBranchState(initialPosition, leafDir, 0, treeleaf, BranchType.leaf, null,
							renderer, filter);
						branchState.IncChildCound();
						treeList.Add(branchState);

						UpdateMeshWithLeaf(treeleaf, leafDir, leafRadius);
					}
					break;
				case 'Y':
					initialPosition = turtleDummy.position;
					turtleDummy.Translate(Vector3.up * currentRule.segmentLength);
					Vector3 flowerDir = (turtleDummy.position - initialPosition).normalized;

					if (CheckOverlapForNewFlower(initialPosition, flowerDir))
					{
						turtleDummy.position = initialPosition;
						break;
					}

					if (flower != null)
					{
						GameObject flowerObj = Instantiate(flower, initialPosition, turtleDummy.rotation, l_SystemParent.transform);

						BranchState branchState = flowerObj.AddComponent<BranchState>();
						branchState.SetBranchState(initialPosition, flowerDir, 0, flowerObj, BranchType.flower, null,
							flowerObj.GetComponent<MeshRenderer>(), flowerObj.GetComponent<MeshFilter>());
						branchState.IncChildCound();
						treeList.Add(branchState);
					}
					else
					{
						GameObject treeflower = new GameObject("TreeFlower");
						filter = treeflower.AddComponent<MeshFilter>();
						renderer = treeflower.AddComponent<MeshRenderer>();
						renderer.material = flowerMat;
						treeflower.transform.position = initialPosition;
						treeflower.transform.rotation = turtleDummy.rotation;
						treeflower.transform.SetParent(l_SystemParent.transform);

						BranchState branchState = treeflower.AddComponent<BranchState>();
						branchState.SetBranchState(initialPosition, flowerDir, 0, treeflower, BranchType.flower, null,
							renderer, filter);
						branchState.IncChildCound();
						treeList.Add(branchState);

						UpdateMeshWithFlower(treeflower, flowerDir, leafBranchRadius);
					}
					break;
				case '+':
					turtleDummy.Rotate(Vector3.back * currentRule.zAxisRotationAmount.GetMinMaxValue());
					break;
				case '-':
					turtleDummy.Rotate(Vector3.forward * currentRule.zAxisRotationAmount.GetMinMaxValue());
					break;
				case '*':
					turtleDummy.Rotate(Vector3.up * currentRule.yAxisRotationAmount.GetMinMaxValue());
					break;
				case '/':
					turtleDummy.Rotate(Vector3.down * currentRule.yAxisRotationAmount.GetMinMaxValue());
					break;
				case '<':
					turtleDummy.Rotate(Vector3.left * currentRule.xAxisRotationAmount.GetMinMaxValue());
					break;
				case '>':
					turtleDummy.Rotate(Vector3.right * currentRule.xAxisRotationAmount.GetMinMaxValue());
					break;
				case '[':
					lStack.Push(
						new SystemState(
							turtleDummy.position,
							turtleDummy.rotation
						)
					);
					createNewBranch = true;
					branchDepth++;
					break;
				case ']':
					SystemState state = lStack.Pop();
					turtleDummy.position = state.position;
					turtleDummy.rotation = state.rotation;
					if (createNewBranch)
						createNewBranch = false;
					branchDepth--;
					break;
				case 'S':
				case 's':
					break;
			}
			counter++;
		}
		DestroyImmediate(turtleDummy.gameObject);
	}
	#endregion

	#region MeshCreation
	void StartTreeMesh()
	{
		float endWidth = treeTrunkRadius;
		float widthModifier;

		BranchState[] branchTips = treeList.Where(x => x.children == 0).ToArray();
		IEnumerable<BranchState> branches = branchTips.OrderBy(x => x.branchIndex);
		int branchTipIndex = 0;
		BranchState tip = branches.ToArray()[branchTipIndex];
		if (useDynamicBranchWidth && currentRule.isFractal)
			Debug.LogWarning("Cannot use dynamic branch width since L-System structure is a fractal!");
		bool dynamicWidth = useDynamicBranchWidth && !currentRule.isFractal;
		while (!tip.hasBeenVisited)
		{
			widthModifier = (endWidth - leafBranchRadius) / (tip.branchIndex + 1);
			float currentWidth = leafBranchRadius;
			if (dynamicWidth)
			{
				tip.ChangeBranchState(BranchType.branchTip);
				tip.SetStartWidth(currentWidth + widthModifier);
				tip.SetEndWidth(currentWidth);
				Vector3[] lastBottomVerts = new Vector3[] { };
				UpdateMeshWithTipOut(tip.branchObj, tip.branchDirection, currentWidth + widthModifier, currentWidth, out lastBottomVerts);
				currentWidth += widthModifier;

				BranchState currentBranch = tip.branchParent;
				do
				{
					if (currentBranch == null) break;

					if (currentWidth <= currentBranch.endWidth) break;

					currentBranch.SetStartWidth(currentWidth + widthModifier);
					currentBranch.SetEndWidth(currentWidth);
					switch (currentBranch.branchType)
					{
						case BranchType.branch:
						case BranchType.newBranch:
							UpdateMeshWithBranchOut(currentBranch.branchObj, currentBranch.branchDirection, currentWidth + widthModifier, currentWidth,
								out lastBottomVerts, lastBottomVerts.Length == 0 ? null : lastBottomVerts);
							break;
						case BranchType.trunk:
							StartMesh(currentBranch.branchObj, currentBranch.branchDirection, currentWidth + widthModifier, currentWidth);
							break;
					}
					currentWidth += widthModifier;
					currentBranch = currentBranch.branchParent;
				} while (currentBranch != null);
			}
			else
			{
				tip.SetStartWidth(currentWidth);
				tip.SetEndWidth(currentWidth);
				Vector3[] lastBottomVerts = new Vector3[] { };
				UpdateMeshWithTipOut(tip.branchObj, tip.branchDirection, currentWidth, currentWidth, out lastBottomVerts);

				BranchState currentBranch = tip.branchParent;
				do
				{
					if (currentBranch == null) break;

					if (currentWidth <= currentBranch.endWidth) break;

					currentBranch.SetStartWidth(currentWidth);
					currentBranch.SetEndWidth(currentWidth);
					switch (currentBranch.branchType)
					{
						case BranchType.branch:
						case BranchType.newBranch:
							UpdateMeshWithBranchOut(currentBranch.branchObj, currentBranch.branchDirection, currentWidth, currentWidth,
								out lastBottomVerts, lastBottomVerts.Length == 0 ? null : lastBottomVerts);
							break;
						case BranchType.trunk:
							StartMesh(currentBranch.branchObj, currentBranch.branchDirection, currentWidth, currentWidth);
							break;
					}
					currentBranch = currentBranch.branchParent;
				} while (currentBranch != null);
			}

			tip.SetVisited();
			branchTipIndex = (branchTipIndex + 1) % branchTips.Length;
			tip = branchTips[branchTipIndex];
		}
	}
	#endregion

	#region Mesh Combination
	void MakeTotalMesh()
	{
		List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
		treeList.ForEach(x => meshRenderers.Add(x.renderer));
		List<Material> subMaterials = new List<Material>();

		//Find all materials in the various meshes & add them if they haven't already been added.
		foreach (MeshRenderer meshr in meshRenderers)
		{
			Material[] localMats = meshr.sharedMaterials;
			foreach (Material localMat in localMats)
				if (!subMaterials.Contains(localMat))
					subMaterials.Add(localMat);
		}

		List<Mesh> subMeshes = new List<Mesh>();
		List<MeshFilter> meshfilters = new List<MeshFilter>();
		treeList.ForEach(x => meshfilters.Add(x.filter));

		//Check with every material gathered & look for any mesh that uses it --
		//Create a new mesh & combine the various meshes that uses the material in the new mesh --
		//Save the new mesh as a submesh
		int vertexAmount = 0;
		foreach (Material material in subMaterials)
		{
			List<CombineInstance> combineInstances = new List<CombineInstance>();
			foreach (MeshFilter meshFilter in meshfilters)
			{
				MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();
				if (!renderer)
					continue;

				Material[] localMats = renderer.sharedMaterials;
				for (int i = 0; i < localMats.Length; i++)
				{
					if (localMats[i] != material)
						continue;
					CombineInstance ci = new CombineInstance();
					ci.mesh = meshFilter.sharedMesh;
					ci.subMeshIndex = i;
					ci.transform = meshFilter.transform.localToWorldMatrix;
					combineInstances.Add(ci);
				}
			}
			Mesh mesh = new Mesh();
			mesh.CombineMeshes(combineInstances.ToArray(), true);
			vertexAmount += mesh.vertices.Count();
			subMeshes.Add(mesh);
		}

		List<Transform> meshTransforms = new List<Transform>();
		treeList.ForEach(x => meshTransforms.Add(x.branchObj.transform));

		//Go through all of the created submeshes & combine them into the final mesh
		List<CombineInstance> finalCombiners = new List<CombineInstance>();
		foreach (Mesh sharedSubMesh in subMeshes)
		{
			CombineInstance ci = new CombineInstance();
			ci.mesh = sharedSubMesh;
			ci.subMeshIndex = 0;
			ci.transform = l_SystemParent.transform.localToWorldMatrix;
			finalCombiners.Add(ci);
		}
		Mesh final = new Mesh();
		final.name = "ProceduralTreeMesh";
		if (vertexAmount > 65535)
		{
			print("Too many vertices, attempting to fix by changing indexFormat of final mesh");
			final.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		}
		final.CombineMeshes(finalCombiners.ToArray(), false);
		//final.RecalculateNormals();
		final.RecalculateTangents();
		final.Optimize();
		final.OptimizeReorderVertexBuffer();
		final.OptimizeIndexBuffers();

		for (int i = 0; i < treeList.Count; i++)
		{
			DestroyImmediate(treeList[i].branchObj);
		}
		treeList.Clear();

		//Get & assign the various parts for the final mesh
		MeshFilter filter = l_SystemParent.AddComponent<MeshFilter>();
		MeshRenderer mr = l_SystemParent.AddComponent<MeshRenderer>();
		filter.sharedMesh = final;
		mr.sharedMaterials = subMaterials.ToArray();
	}
	#endregion

	#region Dupe-Detection
	bool CheckOverlapForNewBranch(Vector3 initialPosition, Vector3 branchDir)
	{
		BranchState[] branchArray = treeList.Where(x => x.branchType == BranchType.branch || x.branchType == BranchType.trunk ||
		x.branchType == BranchType.newBranch).ToArray();
		BranchState[] leafArray = treeList.Where(x => x.branchType == BranchType.leaf).ToArray();
		BranchState[] flowerArray = treeList.Where(x => x.branchType == BranchType.flower).ToArray();
		for (int j = leafArray.Length - 1; j > 0; j--)
		{
			if (leafArray[j].branchObj == null)
				continue;
			if (leafArray[j].branchObj.transform.position == initialPosition &&
				leafArray[j].branchDirection == branchDir)
			{
				DestroyImmediate(leafArray[j].branchObj);
				int index = Array.IndexOf(treeList.ToArray(), leafArray[j]);
				treeList.RemoveAt(index);
			}
		}
		for (int j = flowerArray.Length - 1; j > 0; j--)
		{
			if (flowerArray[j].branchObj == null)
				continue;
			if (flowerArray[j].branchObj.transform.position == initialPosition &&
				flowerArray[j].branchDirection == branchDir)
			{
				DestroyImmediate(flowerArray[j].branchObj);
				int index = Array.IndexOf(treeList.ToArray(), flowerArray[j]);
				treeList.RemoveAt(index);
			}
		}
		for (int k = branchArray.Length - 1; k > 0; k--)
		{
			if (branchArray[k].branchObj == null)
				continue;
			if (branchArray[k].branchObj.transform.position == initialPosition &&
				branchArray[k].branchDirection == branchDir)
			{
				return true;
			}
		}
		return false;
	}

	bool CheckOverlapForNewLeaf(Vector3 initialPosition, Vector3 branchDir)
	{
		BranchState[] branchArray = treeList.Where(x => x.branchType == BranchType.branch || x.branchType == BranchType.trunk ||
		x.branchType == BranchType.newBranch).ToArray();
		BranchState[] leafArray = treeList.Where(x => x.branchType == BranchType.leaf).ToArray();
		BranchState[] flowerArray = treeList.Where(x => x.branchType == BranchType.flower).ToArray();
		for (int j = leafArray.Length - 1; j > 0; j--)
		{
			if (leafArray[j].branchObj == null)
				continue;
			if (leafArray[j].branchObj.transform.position == initialPosition &&
				leafArray[j].branchDirection == branchDir)
			{
				return true;
			}
		}
		for (int j = flowerArray.Length - 1; j > 0; j--)
		{
			if (flowerArray[j].branchObj == null)
				continue;
			if (flowerArray[j].branchObj.transform.position == initialPosition &&
				flowerArray[j].branchDirection == branchDir)
			{
				return true;
			}
		}
		for (int k = branchArray.Length - 1; k > 0; k--)
		{
			if (branchArray[k].branchObj == null)
				continue;
			if (branchArray[k].branchObj.transform.position == initialPosition &&
				branchArray[k].branchDirection == branchDir)
			{
				return true;
			}
		}
		return false;
	}

	bool CheckOverlapForNewFlower(Vector3 initialPosition, Vector3 branchDir)
	{
		BranchState[] branchArray = treeList.Where(x => x.branchType == BranchType.branch || x.branchType == BranchType.trunk ||
		x.branchType == BranchType.newBranch).ToArray();
		BranchState[] leafArray = treeList.Where(x => x.branchType == BranchType.leaf).ToArray();
		BranchState[] flowerArray = treeList.Where(x => x.branchType == BranchType.flower).ToArray();
		for (int j = leafArray.Length - 1; j > 0; j--)
		{
			if (leafArray[j].branchObj == null)
				continue;
			if (leafArray[j].branchObj.transform.position == initialPosition &&
				leafArray[j].branchDirection == branchDir)
			{
				DestroyImmediate(leafArray[j].branchObj);
				int index = Array.IndexOf(treeList.ToArray(), leafArray[j]);
				treeList.RemoveAt(index);
			}
		}
		for (int j = flowerArray.Length - 1; j > 0; j--)
		{
			if (flowerArray[j].branchObj == null)
				continue;
			if (flowerArray[j].branchObj.transform.position == initialPosition &&
				flowerArray[j].branchDirection == branchDir)
			{
				return true;
			}
		}
		for (int k = branchArray.Length - 1; k > 0; k--)
		{
			if (branchArray[k].branchObj == null)
				continue;
			if (branchArray[k].branchObj.transform.position == initialPosition &&
				branchArray[k].branchDirection == branchDir)
			{
				return true;
			}
		}
		return false;
	}
	#endregion

	#region Trunk/Stem
	void StartMesh(GameObject obj, Vector3 direction, float startWidth, float endWidth)
	{
		float height = currentRule.segmentLength;
		float bottomRadius = startWidth;
		float topRadius = endWidth;
		int numSides = branchFaces;
		int numHeightSegments = 1;

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Vector3> normals = new List<Vector3>();

		#region Vertices (bottom & sides)
		int vertex = 0;
		float Two_Pi = Mathf.PI * 2f;

		//Bottom
		verts.Add(new Vector3(0, -0.2f, 0));
		for (int i = vertex; i < numSides; i++)
		{
			float rad = (float)vertex / numSides * Two_Pi;
			verts.Add(new Vector3(Mathf.Cos(rad) * bottomRadius, 0f, Mathf.Sin(rad) * bottomRadius));
			//[vertex] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0f, Mathf.Sin(rad) * bottomRadius);
			vertex++;
		}
		vertex = 0;
		int v = 0;
		//Sides
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			verts.Add(new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius));
			//vertices.Add(new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius));
			v++;
			//vertex++;
		}
		#endregion

		#region Triangles
		int numTriangles = numSides * 2;
		int tri = 0;
		//Bottom
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(0);//[i] = 0;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(1);//[i + 2] = tri + 2;
				tri++;
				//i += 3;
			}
			else
			{
				tris.Add(0);//[i] = 0;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(tri + 2);//[i + 2] = tri + 2;
				tri++;
				//i += 3;
			}
		}

		tri = 0;
		//Sides
		for (int i = tri; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(numSides + 1);//[i] = tri + 2;
				tris.Add(numSides);//[i + 1] = tri + 1;
				tris.Add(numSides * 2);//[i + 2] = tri + 0;
									   //tri++;
									   //i += 3;

				tris.Add(numSides + 1);// triangles.Add(0);//[i] = tri + 1;
				tris.Add(numSides - tri);// triangles.Add(1);//[i + 1] = tri + 2;
				tris.Add(numSides);// triangles.Add(tri);//[i + 2] = tri + 0;
								   // tri++;
								   //i += 3;
			}
			else
			{
				tris.Add(numSides + tri + 2);// triangles.Add(tri + 2);//[i] = tri + 2;
				tris.Add(tri + 1);// triangles.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(numSides + tri + 1);// triangles.Add(tri);//[i + 2] = tri + 0;
											 //tri++;
											 //i += 3;

				tris.Add(numSides + tri + 2);//triangles.Add(tri + 1);//[i] = tri + 1;
				tris.Add(tri + 2);//triangles.Add(tri + 2);//[i + 1] = tri + 2;
				tris.Add(tri + 1); //triangles.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;
			}
		}
		#endregion

		#region Normals
		//Bottom
		normals.Add(Vector3.down);
		vertex++;
		v = 0;
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);
			normals.Add(new Vector3(cos, 0f, sin));
			vertex++;
			v++;
		}
		//Sides
		v = 0;
		for (int i = vertex; i < verts.Count; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			normals.Add(new Vector3(cos, 0f, sin));
			//normals.Add(new Vector3(cos, 0f, sin));

			//vertex += 2;
			v++;
		}
		#endregion

		#region UVs
		// int u = 0;
		// int u_sides = 0;
		// for (int i = u; i < uvs.Count; i++)
		// {
		// 	float t = (float)u_sides / numSides;
		// 	uvs.Add(new Vector2(t, 1f));//[u] = new Vector3(t, 1f);
		// 	uvs.Add(new Vector2(t, 0f));//[u + 1] = new Vector3(t, 0f);
		// 								//u += 2;
		// 	u_sides++;
		// }
		#endregion

		MeshFilter mf = obj.GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mesh.name = "TrunkMesh";
		mesh.vertices = verts.ToArray();
		mesh.normals = normals.ToArray();
		//mesh.uv = uvs.ToArray();
		mesh.triangles = tris.ToArray();

		//mesh.RecalculateBounds();
		//mesh.RecalculateNormals();
		//mesh.OptimizeReorderVertexBuffer();
		//mesh.OptimizeIndexBuffers();
		//mesh.Optimize();
		mf.mesh = mesh;
	}
	#endregion

	#region Branch
	void UpdateMeshWithBranchOut(GameObject obj, Vector3 direction, float startWidth, float endWidth, out Vector3[] baseVerts, Vector3[] topVerts = null)
	{
		float height = currentRule.segmentLength;
		float bottomRadius = startWidth;
		float topRadius = endWidth;
		int numSides = branchFaces;
		int numHeightSegments = 1;

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Vector3> normals = new List<Vector3>();

		List<Vector3> vertsToSend = new List<Vector3>();

		#region Vertices sides
		float Two_Pi = Mathf.PI * 2f;
		int v = 0;
		//Sides
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			//Quaternion rotation = Quaternion.LookRotation(obj.transform.forward, direction);
			//Vector3 rotatedPoint = rotation * point;
			if (topVerts == null)
			{
				Vector3 point = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
				verts.Add(point);//rotatedPoint);
			}
			else
				verts.Add(obj.transform.InverseTransformPoint(topVerts[i]));
			//vertices.Add(point);
			Vector3 nextPoint = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
			vertsToSend.Add(obj.transform.TransformPoint(nextPoint));
			verts.Add(nextPoint);
			//vertices.Add(nextPoint);
			v++;
		}
		#endregion

		baseVerts = vertsToSend.ToArray();

		#region Triangles
		int numTriangles = numSides * 2;
		int tri = 0;
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(0);//[i] = tri + 2;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;

				tris.Add(0);//[i] = tri + 1;
				tris.Add(1);//[i + 1] = tri + 2;
				tris.Add(tri);//[i + 2] = tri + 0;
							  // tri++;
							  //i += 3;
			}
			else
			{
				tris.Add(tri + 2);//[i] = tri + 2;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;

				tris.Add(tri + 1);//[i] = tri + 1;
				tris.Add(tri + 2);//[i + 1] = tri + 2;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;
			}
		}
		#endregion

		#region Normals
		//Sides
		v = 0;
		for (int i = 0; i < verts.Count; i += 2)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			normals.Add(new Vector3(cos, 0f, sin));//[vertex] = new Vector3(cos, 0f, sin);
			normals.Add(new Vector3(cos, 0f, sin));//[vertex + 1] = normals[vertex];

			//vertex += 2;
			v++;
		}
		#endregion

		#region UVs
		// int u = 0;
		// int u_sides = 0;
		// for (int i = u; i < uvs.Count; i++)
		// {
		// 	float t = (float)u_sides / numSides;
		// 	uvs.Add(new Vector2(t, 1f));//[u] = new Vector3(t, 1f);
		// 	uvs.Add(new Vector2(t, 0f));//[u + 1] = new Vector3(t, 0f);
		// 								//u += 2;
		// 	u_sides++;
		// }
		#endregion

		MeshFilter mf = obj.GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mesh.name = "TreeBranchMesh";
		mesh.vertices = verts.ToArray();
		mesh.normals = normals.ToArray();
		//mesh.uv = uvs.ToArray();
		mesh.triangles = tris.ToArray();

		//mesh.RecalculateBounds();
		//mesh.RecalculateNormals();
		//mesh.OptimizeReorderVertexBuffer();
		//mesh.OptimizeIndexBuffers();
		//mesh.Optimize();
		mf.mesh = mesh;
	}

	void UpdateMeshWithBranch(GameObject obj, Vector3 direction, float startWidth, float endWidth)
	{
		float height = currentRule.segmentLength;
		float bottomRadius = startWidth;
		float topRadius = endWidth;
		int numSides = branchFaces;
		int numHeightSegments = 1;

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Vector3> normals = new List<Vector3>();

		#region Vertices sides
		float Two_Pi = Mathf.PI * 2f;
		int v = 0;
		//Sides
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			Vector3 point = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
			//Quaternion rotation = Quaternion.LookRotation(direction);
			//Vector3 rotatedPoint = rotation * point;
			verts.Add(point);//rotatedPoint);
							 //vertices.Add(point);
			Vector3 nextPoint = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
			//Vector3 nextRotatedPoint = rotation * nextPoint;
			verts.Add(nextPoint);//nextRotatedPoint);
								 //vertices.Add(nextPoint);
			v++;
		}
		#endregion

		#region Triangles
		int numTriangles = numSides * 2;
		int tri = 0;
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(0);//[i] = tri + 2;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;

				tris.Add(0);//[i] = tri + 1;
				tris.Add(1);//[i + 1] = tri + 2;
				tris.Add(tri);//[i + 2] = tri + 0;
							  // tri++;
							  //i += 3;
			}
			else
			{
				tris.Add(tri + 2);//[i] = tri + 2;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;

				tris.Add(tri + 1);//[i] = tri + 1;
				tris.Add(tri + 2);//[i + 1] = tri + 2;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;
			}
		}
		#endregion

		#region Normals
		//Sides
		v = 0;
		for (int i = 0; i < verts.Count; i += 2)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			normals.Add(new Vector3(cos, 0f, sin));//[vertex] = new Vector3(cos, 0f, sin);
			normals.Add(new Vector3(cos, 0f, sin));//[vertex + 1] = normals[vertex];

			//vertex += 2;
			v++;
		}
		#endregion

		#region UVs
		// int u = 0;
		// int u_sides = 0;
		// for (int i = u; i < uvs.Count; i++)
		// {
		// 	float t = (float)u_sides / numSides;
		// 	uvs.Add(new Vector2(t, 1f));//[u] = new Vector3(t, 1f);
		// 	uvs.Add(new Vector2(t, 0f));//[u + 1] = new Vector3(t, 0f);
		// 								//u += 2;
		// 	u_sides++;
		// }
		#endregion

		MeshFilter mf = obj.GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mesh.name = "TreeBranchMesh";
		mesh.vertices = verts.ToArray();
		mesh.normals = normals.ToArray();
		//mesh.uv = uvs.ToArray();
		mesh.triangles = tris.ToArray();

		//mesh.RecalculateBounds();
		//mesh.RecalculateNormals();
		//mesh.OptimizeReorderVertexBuffer();
		//mesh.OptimizeIndexBuffers();
		//mesh.Optimize();
		mf.mesh = mesh;
	}
	#endregion

	#region BranchTip
	void UpdateMeshWithTipOut(GameObject obj, Vector3 direction, float startWidth, float endWidth, out Vector3[] baseVerts)
	{
		float height = currentRule.segmentLength;
		float bottomRadius = startWidth;
		float topRadius = endWidth;
		int numSides = branchFaces;
		int numHeightSegments = 1;

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Vector3> normals = new List<Vector3>();

		List<Vector3> vertsToSend = new List<Vector3>();

		#region Vertices sides & Top
		float Two_Pi = Mathf.PI * 2f;
		int v = 0;
		//Top
		verts.Add(new Vector3(0f, height + 0.2f, 0f));
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			verts.Add(new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius));
			v++;
		}
		//Sides
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			Vector3 nextPoint = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
			//Vector3 nextRotatedPoint = rotation * nextPoint;
			verts.Add(nextPoint);//nextRotatedPoint);
			vertsToSend.Add(obj.transform.TransformPoint(nextPoint));
			//Vector3 point = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
			//Quaternion rotation = Quaternion.Euler(direction);
			//Vector3 rotatedPoint = rotation * point;
			//verts.Add(point);//rotatedPoint);
			v++;
		}
		#endregion

		baseVerts = vertsToSend.ToArray();

		#region Triangles
		int numTriangles = numSides * 2;
		int tri = 0;

		//Top
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(1);//[i] = 0;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(0);//[i + 2] = tri + 2;
				tri++;
				//i += 3;
			}
			else
			{
				tris.Add(tri + 2);//[i] = 0;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(0);//[i + 2] = tri + 2;
				tri++;
				//i += 3;
			}
		}

		tri = 0;
		//Sides
		for (int i = tri; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(numSides * 2);//[i] = tri + 2;
				tris.Add(numSides);//[i + 1] = tri + 1;
				tris.Add(numSides + 1);//[i + 2] = tri + 0;
									   //tri++;
									   //i += 3;

				tris.Add(numSides);// triangles.Add(0);//[i] = tri + 1;
				tris.Add(numSides - tri);// triangles.Add(1);//[i + 1] = tri + 2;
				tris.Add(numSides + 1);// triangles.Add(tri);//[i + 2] = tri + 0;
									   // tri++;
									   //i += 3;
			}
			else
			{
				tris.Add(numSides + tri + 1);// triangles.Add(tri + 2);//[i] = tri + 2;
				tris.Add(tri + 1);// triangles.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(numSides + tri + 2);// triangles.Add(tri);//[i + 2] = tri + 0;
											 //tri++;
											 //i += 3;

				tris.Add(tri + 1);//triangles.Add(tri + 1);//[i] = tri + 1;
				tris.Add(tri + 2);//triangles.Add(tri + 2);//[i + 1] = tri + 2;
				tris.Add(numSides + tri + 2); //triangles.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;
			}
		}

		// for (int i = 0; i < numSides; i++)
		// {
		// 	if (i == numSides - 1)
		// 	{
		// 		tris.Add(0);//[i] = tri + 2;
		// 		tris.Add(tri + 1);//[i + 1] = tri + 1;
		// 		tris.Add(tri);//[i + 2] = tri + 0;
		// 		tri++;
		// 		//i += 3;

		// 		tris.Add(0);//[i] = tri + 1;
		// 		tris.Add(1);//[i + 1] = tri + 2;
		// 		tris.Add(tri);//[i + 2] = tri + 0;
		// 					  // tri++;
		// 					  //i += 3;

		// 	}
		// 	else
		// 	{
		// 		tris.Add(tri + 2);//[i] = tri + 2;
		// 		tris.Add(tri + 1);//[i + 1] = tri + 1;
		// 		tris.Add(tri);//[i + 2] = tri + 0;
		// 		tri++;
		// 		//i += 3;

		// 		tris.Add(tri + 1);//[i] = tri + 1;
		// 		tris.Add(tri + 2);//[i + 1] = tri + 2;
		// 		tris.Add(tri);//[i + 2] = tri + 0;
		// 		tri++;
		// 		//i += 3;
		// 	}
		// }

		// //Top
		// for (int i = 0; i < numSides; i++)
		// {
		// 	if (i == numSides - 1)
		// 	{
		// 		tris.Add(verts.Count - 1);
		// 		tris.Add(verts.Count - numSides - 1);
		// 		tris.Add(tri + 1);
		// 	}
		// 	else
		// 	{
		// 		tris.Add(tri + 2);
		// 		tris.Add(tri + 1);
		// 		tris.Add(verts.Count - 1);
		// 		tri++;
		// 	}
		// }
		#endregion

		#region Normals
		//Sides
		//print(verts.Count);
		normals.Add(Vector3.up);
		v = 0;
		int vertex = 0;
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			normals.Add(new Vector3(cos, 0f, sin));
			//normals.Add(new Vector3(cos, 0f, sin));

			vertex++;
			v++;
		}
		//print(normals.Count);

		//Top
		v = 0;
		for (int i = vertex; i < verts.Count - 1; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			normals.Add(new Vector3(cos, 0f, sin));
			v++;
		}
		//print(normals.Count);
		#endregion

		#region UVs
		// int u = 0;
		// int u_sides = 0;
		// for (int i = u; i < uvs.Count; i++)
		// {
		// 	float t = (float)u_sides / numSides;
		// 	uvs.Add(new Vector2(t, 1f));//[u] = new Vector3(t, 1f);
		// 	uvs.Add(new Vector2(t, 0f));//[u + 1] = new Vector3(t, 0f);
		// 								//u += 2;
		// 	u_sides++;
		// }
		#endregion

		MeshFilter mf = obj.GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mesh.name = "TreeBranchTipMesh";
		mesh.vertices = verts.ToArray();
		mesh.normals = normals.ToArray();
		//mesh.uv = uvs.ToArray();
		mesh.triangles = tris.ToArray();

		//mesh.RecalculateBounds();
		//mesh.RecalculateNormals();
		//mesh.OptimizeReorderVertexBuffer();
		//mesh.OptimizeIndexBuffers();
		//mesh.Optimize();
		mf.mesh = mesh;
	}

	void UpdateMeshWithTip(GameObject obj, Vector3 direction, float startWidth, float endWidth)
	{
		float height = currentRule.segmentLength;
		float bottomRadius = startWidth;
		float topRadius = endWidth;
		int numSides = branchFaces;
		int numHeightSegments = 1;

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Vector3> normals = new List<Vector3>();

		#region Vertices sides & Top
		float Two_Pi = Mathf.PI * 2f;
		int v = 0;
		//Top
		verts.Add(new Vector3(0f, height + 0.2f, 0f));
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			verts.Add(new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius));
			v++;
		}
		//Sides
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			Vector3 nextPoint = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
			//Vector3 nextRotatedPoint = rotation * nextPoint;
			verts.Add(nextPoint);//nextRotatedPoint);
								 //Vector3 point = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
								 //Quaternion rotation = Quaternion.Euler(direction);
								 //Vector3 rotatedPoint = rotation * point;
								 //verts.Add(point);//rotatedPoint);
			v++;
		}
		#endregion

		#region Triangles
		int numTriangles = numSides * 2;
		int tri = 0;

		//Top
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(1);//[i] = 0;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(0);//[i + 2] = tri + 2;
				tri++;
				//i += 3;
			}
			else
			{
				tris.Add(tri + 2);//[i] = 0;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(0);//[i + 2] = tri + 2;
				tri++;
				//i += 3;
			}
		}

		tri = 0;
		//Sides
		for (int i = tri; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(numSides * 2);//[i] = tri + 2;
				tris.Add(numSides);//[i + 1] = tri + 1;
				tris.Add(numSides + 1);//[i + 2] = tri + 0;
									   //tri++;
									   //i += 3;

				tris.Add(numSides);// triangles.Add(0);//[i] = tri + 1;
				tris.Add(numSides - tri);// triangles.Add(1);//[i + 1] = tri + 2;
				tris.Add(numSides + 1);// triangles.Add(tri);//[i + 2] = tri + 0;
									   // tri++;
									   //i += 3;
			}
			else
			{
				tris.Add(numSides + tri + 1);// triangles.Add(tri + 2);//[i] = tri + 2;
				tris.Add(tri + 1);// triangles.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(numSides + tri + 2);// triangles.Add(tri);//[i + 2] = tri + 0;
											 //tri++;
											 //i += 3;

				tris.Add(tri + 1);//triangles.Add(tri + 1);//[i] = tri + 1;
				tris.Add(tri + 2);//triangles.Add(tri + 2);//[i + 1] = tri + 2;
				tris.Add(numSides + tri + 2); //triangles.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;
			}
		}

		// for (int i = 0; i < numSides; i++)
		// {
		// 	if (i == numSides - 1)
		// 	{
		// 		tris.Add(0);//[i] = tri + 2;
		// 		tris.Add(tri + 1);//[i + 1] = tri + 1;
		// 		tris.Add(tri);//[i + 2] = tri + 0;
		// 		tri++;
		// 		//i += 3;

		// 		tris.Add(0);//[i] = tri + 1;
		// 		tris.Add(1);//[i + 1] = tri + 2;
		// 		tris.Add(tri);//[i + 2] = tri + 0;
		// 					  // tri++;
		// 					  //i += 3;

		// 	}
		// 	else
		// 	{
		// 		tris.Add(tri + 2);//[i] = tri + 2;
		// 		tris.Add(tri + 1);//[i + 1] = tri + 1;
		// 		tris.Add(tri);//[i + 2] = tri + 0;
		// 		tri++;
		// 		//i += 3;

		// 		tris.Add(tri + 1);//[i] = tri + 1;
		// 		tris.Add(tri + 2);//[i + 1] = tri + 2;
		// 		tris.Add(tri);//[i + 2] = tri + 0;
		// 		tri++;
		// 		//i += 3;
		// 	}
		// }

		// //Top
		// for (int i = 0; i < numSides; i++)
		// {
		// 	if (i == numSides - 1)
		// 	{
		// 		tris.Add(verts.Count - 1);
		// 		tris.Add(verts.Count - numSides - 1);
		// 		tris.Add(tri + 1);
		// 	}
		// 	else
		// 	{
		// 		tris.Add(tri + 2);
		// 		tris.Add(tri + 1);
		// 		tris.Add(verts.Count - 1);
		// 		tri++;
		// 	}
		// }
		#endregion

		#region Normals
		//Sides
		//print(verts.Count);
		normals.Add(Vector3.up);
		v = 0;
		int vertex = 0;
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			normals.Add(new Vector3(cos, 0f, sin));
			//normals.Add(new Vector3(cos, 0f, sin));

			vertex++;
			v++;
		}
		//print(normals.Count);

		//Top
		v = 0;
		for (int i = vertex; i < verts.Count - 1; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			normals.Add(new Vector3(cos, 0f, sin));
			v++;
		}
		//print(normals.Count);
		#endregion

		#region UVs
		// int u = 0;
		// int u_sides = 0;
		// for (int i = u; i < uvs.Count; i++)
		// {
		// 	float t = (float)u_sides / numSides;
		// 	uvs.Add(new Vector2(t, 1f));//[u] = new Vector3(t, 1f);
		// 	uvs.Add(new Vector2(t, 0f));//[u + 1] = new Vector3(t, 0f);
		// 								//u += 2;
		// 	u_sides++;
		// }
		#endregion

		MeshFilter mf = obj.GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mesh.name = "TreeBranchTipMesh";
		mesh.vertices = verts.ToArray();
		mesh.normals = normals.ToArray();
		//mesh.uv = uvs.ToArray();
		mesh.triangles = tris.ToArray();

		//mesh.RecalculateBounds();
		//mesh.RecalculateNormals();
		//mesh.OptimizeReorderVertexBuffer();
		//mesh.OptimizeIndexBuffers();
		//mesh.Optimize();
		mf.mesh = mesh;
	}
	#endregion

	#region Leaf
	void UpdateMeshWithLeaf(GameObject obj, Vector3 direction, float startWidth)
	{
		float height = currentRule.segmentLength;
		float middleRadius = startWidth;
		int numSides = branchFaces;
		int numHeightSegments = 1;

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Vector3> normals = new List<Vector3>();

		float start = UnityEngine.Random.Range(0, 360f);
		float end = start + UnityEngine.Random.Range(45f, 315f);

		float randX1 = Mathf.Sin(Mathf.Deg2Rad * start) * middleRadius;
		float randZ1 = Mathf.Cos(Mathf.Deg2Rad * start) * middleRadius;

		float randX2 = Mathf.Sin(Mathf.Deg2Rad * end) * middleRadius;
		float randZ2 = Mathf.Cos(Mathf.Deg2Rad * end) * middleRadius;

		verts.Add(new Vector3(0, 0, 0));
		verts.Add(new Vector3(randX1, (height / 2f) - 0.2f, randZ1));
		verts.Add(new Vector3(0, height, 0));
		verts.Add(new Vector3(randX2, (height / 2f) - 0.2f, randZ2));

		verts.Add(new Vector3(0, 0, 0));
		verts.Add(new Vector3(randX1, (height / 2f) - 0.2f, randZ1));
		verts.Add(new Vector3(0, height, 0));
		verts.Add(new Vector3(randX2, (height / 2f) - 0.2f, randZ2));

		tris.Add(0);
		tris.Add(1);
		tris.Add(2);

		tris.Add(0);
		tris.Add(2);
		tris.Add(3);

		tris.Add(4);
		tris.Add(6);
		tris.Add(5);

		tris.Add(4);
		tris.Add(7);
		tris.Add(6);

		#region Normals
		//Sides
		float Two_Pi = Mathf.PI * 2f;
		int v = 0;
		float X, Z;
		for (int i = 0; i < 2; i++)
		{
			float rad = (float)(v / 4) * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			if (i == 0)
			{
				X = Mathf.Cos(Mathf.Deg2Rad * start);
				Z = Mathf.Sin(Mathf.Deg2Rad * end);
			}
			else
			{
				X = 1f - Mathf.Cos(Mathf.Deg2Rad * start);
				Z = 1f - Mathf.Sin(Mathf.Deg2Rad * end);
			}

			for (int j = 0; j < 4; j++)
			{
				if (j == 0)
					normals.Add(new Vector3(X, -0.5f, Z));//Vector3.down);
				else if (j == 2)
					normals.Add(new Vector3(X, 0.5f, Z));//Vector3.up);
				else
				{
					normals.Add(new Vector3(X, 0f, Z));//[vertex] = new Vector3(cos, 0f, sin);
					v++;
				}
			}

			//vertex += 2;
			//v++;
		}
		#endregion

		#region UVs
		// int u = 0;
		// int u_sides = 0;
		// for (int i = u; i < uvs.Count; i++)
		// {
		// 	float t = (float)u_sides / numSides;
		// 	uvs.Add(new Vector2(t, 1f));//[u] = new Vector3(t, 1f);
		// 	uvs.Add(new Vector2(t, 0f));//[u + 1] = new Vector3(t, 0f);
		// 								//u += 2;
		// 	u_sides++;
		// }
		#endregion

		MeshFilter mf = obj.GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mesh.name = "TreeLeafMesh";
		mesh.vertices = verts.ToArray();
		mesh.normals = normals.ToArray();
		//mesh.uv = uvs.ToArray();
		mesh.triangles = tris.ToArray();

		//mesh.RecalculateBounds();
		//mesh.Optimize();
		mf.mesh = mesh;
	}
	#endregion

	#region Flower
	void UpdateMeshWithFlower(GameObject obj, Vector3 direction, float startWidth)
	{
		float height = currentRule.segmentLength;
		float bottomRadius = startWidth;
		float topRadius = bottomRadius * 4.5f;
		int numSides = branchFaces;
		int numHeightSegments = 1;

		List<Vector3> verts = new List<Vector3>();
		List<int> tris = new List<int>();

		#region Vertices sides & Top
		float Two_Pi = Mathf.PI * 2f;
		int v = 0;
		//Sides
		bool varyingSidesForHeight = numSides >= 6 && numSides % 2 == 0;
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			Vector3 point = new Vector3(Mathf.Cos(rad) * topRadius, height * 0.35f, Mathf.Sin(rad) * topRadius);
			if (varyingSidesForHeight && i % 2 == 0)
				point = new Vector3(Mathf.Cos(rad) * (topRadius * 0.3f), height * 0.25f, Mathf.Sin(rad) * (topRadius * 0.3f));
			//Quaternion rotation = Quaternion.Euler(direction);
			//Vector3 rotatedPoint = rotation * point;
			verts.Add(point);//rotatedPoint);
			Vector3 nextPoint = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
			//Vector3 nextRotatedPoint = rotation * nextPoint;
			verts.Add(nextPoint);//nextRotatedPoint);
			v++;
		}

		//Top
		v = 0;
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			if (varyingSidesForHeight && i % 2 == 0)
				verts.Add(new Vector3(Mathf.Cos(rad) * (topRadius * 0.3f), height * 0.25f, Mathf.Sin(rad) * (topRadius * 0.3f)));
			else
				verts.Add(new Vector3(Mathf.Cos(rad) * topRadius, height * 0.35f, Mathf.Sin(rad) * topRadius));
			v++;
		}
		verts.Add(new Vector3(0f, height * 0.2f, 0f));
		#endregion

		#region Triangles
		int numTriangles = numSides * 2;
		int tri = 0;
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(0);//[i] = tri + 2;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;

				tris.Add(0);//[i] = tri + 1;
				tris.Add(1);//[i + 1] = tri + 2;
				tris.Add(tri);//[i + 2] = tri + 0;
							  // tri++;
							  //i += 3;

			}
			else
			{
				tris.Add(tri + 2);//[i] = tri + 2;
				tris.Add(tri + 1);//[i + 1] = tri + 1;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;

				tris.Add(tri + 1);//[i] = tri + 1;
				tris.Add(tri + 2);//[i + 1] = tri + 2;
				tris.Add(tri);//[i + 2] = tri + 0;
				tri++;
				//i += 3;
			}
		}

		//Top
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(verts.Count - 1);
				tris.Add(verts.Count - numSides - 1);
				tris.Add(tri + 1);
			}
			else
			{
				tris.Add(tri + 2);
				tris.Add(tri + 1);
				tris.Add(verts.Count - 1);
				tri++;
			}
		}
		#endregion

		#region Normals
		// vertex = 0;
		// //Sides
		// v = 0;
		// for (int i = vertex; i < vertices.Count; i += 2)
		// {
		// 	float rad = (float)v / numSides * Two_Pi;
		// 	float cos = Mathf.Cos(rad);
		// 	float sin = Mathf.Sin(rad);

		// 	normals.Add(new Vector3(cos, 0f, sin));//[vertex] = new Vector3(cos, 0f, sin);
		// 	normals.Add(new Vector3(cos, 0f, sin));//[vertex + 1] = normals[vertex];

		// 	//vertex += 2;
		// 	v++;
		// }
		#endregion

		#region UVs
		// int u = 0;
		// int u_sides = 0;
		// for (int i = u; i < uvs.Count; i++)
		// {
		// 	float t = (float)u_sides / numSides;
		// 	uvs.Add(new Vector2(t, 1f));//[u] = new Vector3(t, 1f);
		// 	uvs.Add(new Vector2(t, 0f));//[u + 1] = new Vector3(t, 0f);
		// 								//u += 2;
		// 	u_sides++;
		// }
		#endregion

		MeshFilter mf = obj.GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mesh.name = "TreeLeafMesh";
		mesh.vertices = verts.ToArray();
		//mesh.normals = normals.ToArray();
		//mesh.uv = uvs.ToArray();
		mesh.triangles = tris.ToArray();

		//mesh.RecalculateBounds();
		//mesh.RecalculateNormals();
		//mesh.OptimizeReorderVertexBuffer();
		//mesh.OptimizeIndexBuffers();
		//mesh.Optimize();
		mf.mesh = mesh;
	}
	#endregion
}
