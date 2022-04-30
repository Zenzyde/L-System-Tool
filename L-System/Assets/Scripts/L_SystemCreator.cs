using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Linq;
using System;

public class L_SystemCreator : MonoBehaviour
{
	[SerializeField] private GameObject Leaf;
	[SerializeField] private GameObject Flower;
	[SerializeField] private Material BranchMat;
	[SerializeField] private int Iterations = 1;
	[SerializeField] private int BranchResolution = 4;
	[SerializeField] private float TreeTrunkWidth = 3.5f;
	[SerializeField] private float LeafBranchWidth = 0.2f;
	[SerializeField] private bool UseDynamicBranchWidth;
	[SerializeField] private L_Rule Rule;
	[SerializeField] private string SystemName;
	[SerializeField] private string SavingDirectory;

	private Stack<SystemState> LStack = new Stack<SystemState>();

	private List<BranchState> TreeList = new List<BranchState>();

	private Transform TurtleDummy;
	private GameObject LSystemParent;

	private string InitialSentence;
	private string CurrentSentence;

	private bool IsFractal;

	private List<KeyValuePair<char, EL_Rule_LSystemAction>> LActionRules = new List<KeyValuePair<char, EL_Rule_LSystemAction>>();
	private List<KeyValuePair<EL_Rule_LSystemAction, float>> LActionMovementRules = new List<KeyValuePair<EL_Rule_LSystemAction, float>>();

	public void CreateLSystem()
	{
		InitializeVariables();
		BuildSentence();
		GenerateLSystem();
		BuildTreeMeshObjects();
		CombineFinalTreeMesh();
	}

	public void DestroySystem()
	{
		DestroyImmediate(LSystemParent);
		LSystemParent = null;
	}

	public void SaveSystem()
	{
		if (SavingDirectory == string.Empty)
		{
			Debug.LogWarning("No saving directory set! Saving in Assets root folder.");
			string path = $"Assets/L_System_{SystemName}.prefab";
			string meshPathSameFolder = $"Assets/L_System_{SystemName}_mesh.asset";
			Mesh mesh = LSystemParent.GetComponent<MeshFilter>().sharedMesh;
			bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (!exists)
			{
				AssetDatabase.CreateAsset(mesh, meshPathSameFolder);
				AssetDatabase.SaveAssets();
				PrefabUtility.SaveAsPrefabAsset(LSystemParent, path);
			}
			else
			{
				int i = 0;

				path = $"Assets/L-System_{SystemName}_{i}.prefab";
				exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				while (exists && i < 25)
				{
					i++;
					path = $"Assets/L-System_{SystemName}_{i}.prefab";
					exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				}

				meshPathSameFolder = $"Assets/L-System_{SystemName}_{i}_mesh.asset";

				AssetDatabase.CreateAsset(mesh, meshPathSameFolder);
				AssetDatabase.SaveAssets();
				PrefabUtility.SaveAsPrefabAsset(LSystemParent, path);
			}
		}
		else
		{
			string path = $"{SavingDirectory}/L-System_{SystemName}.prefab";
			string meshPathSameFolder = $"{SavingDirectory}/L_System_{SystemName}_mesh.asset";
			Mesh mesh = LSystemParent.GetComponent<MeshFilter>().sharedMesh;
			bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (!exists)
			{
				AssetDatabase.CreateAsset(mesh, meshPathSameFolder);
				AssetDatabase.SaveAssets();
				PrefabUtility.SaveAsPrefabAsset(LSystemParent, path);
			}
			else
			{
				int i = 0;

				path = $"{SavingDirectory}/L-System_{SystemName}_{i}.prefab";
				exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				while (exists && i < 25)
				{
					i++;
					path = $"{SavingDirectory}/L-System_{SystemName}_{i}.prefab";
					exists = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				}

				meshPathSameFolder = $"{SavingDirectory}/L-System_{SystemName}_{i}_mesh.asset";

				AssetDatabase.CreateAsset(mesh, meshPathSameFolder);
				AssetDatabase.SaveAssets();
				PrefabUtility.SaveAsPrefabAsset(LSystemParent, path);
			}
		}
	}

	void InitializeVariables()
	{
		if (Iterations < 1)
			Iterations = 1;

		TurtleDummy = new GameObject("Turtle_Dummy").transform;
		TurtleDummy.position = transform.position;

		StringBuilder sb = new StringBuilder();
		string lSystemName = $"L-System_{SystemName}";
		LSystemParent = new GameObject(lSystemName);
		LSystemParent.transform.position = TurtleDummy.position;

		InitialSentence = Rule.StartingSentence;

		for (int i = 0; i < Rule.LetterRules.Length; i++)
		{
			L_Rule_Declaration ruleDeclaration = Rule.LetterRules[i];
			LActionRules.Add(new KeyValuePair<char, EL_Rule_LSystemAction>(ruleDeclaration.Identifier, ruleDeclaration.LSystemtAction));
			LActionMovementRules.Add(new KeyValuePair<EL_Rule_LSystemAction, float>(ruleDeclaration.LSystemtAction, ruleDeclaration.MovementAmount.RandomValue));
		}

		IsFractal = Rule.IsFractal;
	}

	void BuildSentence()
	{
		CurrentSentence = InitialSentence;

		StringBuilder sb = new StringBuilder();

		for (int i = 0; i < Iterations; i++)
		{
			foreach (char letter in CurrentSentence)
			{
				List<string> replacements = new List<string>();
				foreach (L_Rule_Letter_Replacement ruleReplacement in Rule.RuleLetterReplacements)
				{
					if (ruleReplacement.Identifier == letter)
					{
						replacements.Add(ruleReplacement.Replacement);
					}
				}
				sb.Append(replacements.Count == 0 ? letter.ToString() : replacements[0]);
			}

			CurrentSentence = sb.ToString();
			sb = new StringBuilder();
		}
	}

	EL_Rule_LSystemAction CharToAction(char letter)
	{
		for (int i = 0; i < LActionRules.Count; i++)
		{
			KeyValuePair<char, EL_Rule_LSystemAction> pair = LActionRules[i];
			if (pair.Key == letter)
				return pair.Value;
		}
		return EL_Rule_LSystemAction.none;
	}

	float ActionToFloat(EL_Rule_LSystemAction action)
	{
		for (int i = 0; i < LActionMovementRules.Count; i++)
		{
			KeyValuePair<EL_Rule_LSystemAction, float> pair = LActionMovementRules[i];
			if (pair.Key == action)
				return pair.Value;
		}
		return 0;
	}

	#region MeshObjectGeneration
	void GenerateLSystem()
	{
		TreeList.Clear();
		bool createNewBranch = false;
		int branchDepth = 0, trunkDepth = -1;
		bool madeTrunk = false;
		Vector3 initialPosition;
		BranchState parent = null;
		foreach (char rule in CurrentSentence)
		{
			EL_Rule_LSystemAction currentAction = CharToAction(rule);
			float MovementAmount = ActionToFloat(currentAction);
			switch (currentAction)
			{
				case EL_Rule_LSystemAction.makeBranchAndMove:
					initialPosition = TurtleDummy.position;
					TurtleDummy.Translate(Vector3.up * MovementAmount);

					Vector3 branchDirection = (TurtleDummy.position - initialPosition).normalized;

					// if (CheckOverlap(initialPosition, branchDirection, BranchType.branch))
					// {
					// 	break;
					// }

					GameObject treeBranch = new GameObject("TreeBranch");
					MeshFilter filter = treeBranch.AddComponent<MeshFilter>();
					MeshRenderer renderer = treeBranch.AddComponent<MeshRenderer>();
					renderer.material = BranchMat;
					treeBranch.transform.position = initialPosition;
					treeBranch.transform.rotation = TurtleDummy.rotation;
					treeBranch.transform.SetParent(LSystemParent.transform);

					//First trunk
					if (!madeTrunk)
					{
						madeTrunk = true;
						trunkDepth = branchDepth;
						BranchState branchState = treeBranch.AddComponent<BranchState>();
						if (Iterations == 1)
							branchState.SetBranchState(initialPosition, branchDirection, initialPosition + (branchDirection * MovementAmount), 0,
							treeBranch, BranchType.branch, null, renderer, filter);
						else
							branchState.SetBranchState(initialPosition, branchDirection, initialPosition + (branchDirection * MovementAmount), 0,
							treeBranch, BranchType.trunk, null, renderer, filter);
						TreeList.Add(branchState);
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
								branchState.SetBranchState(initialPosition, branchDirection, initialPosition + (branchDirection * MovementAmount), 0,
								treeBranch, BranchType.newBranch, null, renderer, filter);
								TreeList.Add(branchState);
							}
							else
							{
								parent = TreeList.Where(
									x => x.branchEndPosition == initialPosition &&
									(x.branchType == BranchType.branch || x.branchType == BranchType.newBranch || x.branchType == BranchType.trunk)
								).FirstOrDefault();

								branchState = treeBranch.AddComponent<BranchState>();
								branchState.SetBranchState(initialPosition, branchDirection, initialPosition + branchDirection * MovementAmount, parent.branchIndex + 1, treeBranch,
								parent.branchDirection == branchDirection ? BranchType.branch : BranchType.newBranch, parent,
								renderer, filter);
								TreeList.Add(branchState);
								parent.IncreaseChildCount();
							}
						}
						else
						{
							parent = TreeList.Where(
								x => x.branchEndPosition == initialPosition &&
								(x.branchType == BranchType.branch || x.branchType == BranchType.newBranch || x.branchType == BranchType.trunk)
							).FirstOrDefault();


							BranchState branchState = treeBranch.AddComponent<BranchState>();
							branchState.SetBranchState(initialPosition, branchDirection, initialPosition + branchDirection * MovementAmount,
								parent.branchIndex + 1, treeBranch, BranchType.branch, parent, renderer, filter);
							TreeList.Add(branchState);
							parent.IncreaseChildCount();
						}
					}

					if (createNewBranch)
						createNewBranch = false;
					break;
				case EL_Rule_LSystemAction.makeFlowerAndMove:
					initialPosition = TurtleDummy.position;
					TurtleDummy.Translate(Vector3.up * MovementAmount);
					Vector3 flowerDir = (TurtleDummy.position - initialPosition).normalized;

					if (Flower == null)//CheckOverlap(initialPosition, flowerDir, BranchType.flower) || Flower == null)
					{
						TurtleDummy.position = initialPosition;
						break;
					}

					if (Flower != null)
					{
						GameObject flowerObj = Instantiate(Flower, initialPosition, TurtleDummy.rotation, LSystemParent.transform);

						BranchState branchState = flowerObj.AddComponent<BranchState>();
						branchState.SetBranchState(initialPosition, flowerDir, initialPosition + flowerDir * MovementAmount, 0, flowerObj,
							BranchType.flower, null, flowerObj.GetComponent<MeshRenderer>(), flowerObj.GetComponent<MeshFilter>());
						branchState.IncreaseChildCount();
						TreeList.Add(branchState);
					}
					break;
				case EL_Rule_LSystemAction.makeLeafAndMove:
					initialPosition = TurtleDummy.position;
					TurtleDummy.Translate(Vector3.up * MovementAmount);
					Vector3 leafDir = (TurtleDummy.position - initialPosition).normalized;

					if (Leaf == null)//CheckOverlap(initialPosition, leafDir, BranchType.leaf) || Leaf == null)
					{
						TurtleDummy.position = initialPosition;
						break;
					}

					if (Leaf != null)
					{
						GameObject leafObj = Instantiate(Leaf, initialPosition, TurtleDummy.rotation, LSystemParent.transform);
						BranchState branchState = leafObj.AddComponent<BranchState>();
						branchState.SetBranchState(initialPosition, leafDir, initialPosition + leafDir * MovementAmount, 0, leafObj,
							BranchType.leaf, null, leafObj.GetComponent<MeshRenderer>(), leafObj.GetComponent<MeshFilter>());
						branchState.IncreaseChildCount();
						TreeList.Add(branchState);
					}
					break;
				case EL_Rule_LSystemAction.rotateXNegative:
					TurtleDummy.Rotate(Vector3.down * MovementAmount);
					break;
				case EL_Rule_LSystemAction.rotateXPositive:
					TurtleDummy.Rotate(Vector3.up * MovementAmount);
					break;
				case EL_Rule_LSystemAction.rotateYNegative:
					TurtleDummy.Rotate(Vector3.back * MovementAmount);
					break;
				case EL_Rule_LSystemAction.rotateYPositive:
					TurtleDummy.Rotate(Vector3.forward * MovementAmount);
					break;
				case EL_Rule_LSystemAction.rotateZNegative:
					TurtleDummy.Rotate(Vector3.left * MovementAmount);
					break;
				case EL_Rule_LSystemAction.rotateZPositive:
					TurtleDummy.Rotate(Vector3.right * MovementAmount);
					break;
				case EL_Rule_LSystemAction.makeTreeState:
					LStack.Push(new SystemState(TurtleDummy.position, TurtleDummy.rotation));
					createNewBranch = true;
					branchDepth++;
					break;
				case EL_Rule_LSystemAction.restoreTreeState:
					SystemState state = LStack.Pop();
					TurtleDummy.position = state.position;
					TurtleDummy.rotation = state.rotation;
					if (createNewBranch)
						createNewBranch = false;
					branchDepth--;
					break;
				default:
					break;
			}
		}
		DestroyImmediate(TurtleDummy.gameObject);
	}
	#endregion

	#region MeshCreation
	void BuildTreeMeshObjects()
	{
		float widthModifier;

		BranchState[] branchTips = TreeList.Where(x => x.children == 0).ToArray();
		IEnumerable<BranchState> branches = branchTips.OrderBy(x => x.branchIndex);
		int branchTipIndex = 0;
		BranchState tip = branches.ToArray()[branchTipIndex];
		if (UseDynamicBranchWidth && IsFractal)
			Debug.LogWarning("Skipping dynamic branch width since L-System is marked as a fractal!");
		bool dynamicWidth = UseDynamicBranchWidth && !IsFractal;
		while (!tip.hasBeenVisited)
		{
			widthModifier = (TreeTrunkWidth - LeafBranchWidth) / (tip.branchIndex + 1);
			float currentWidth = LeafBranchWidth;
			if (dynamicWidth)
			{
				tip.ChangeBranchState(BranchType.branchTip);
				tip.SetStartWidth(currentWidth + widthModifier);
				tip.SetEndWidth(currentWidth);
				Vector3[] lastBottomVerts = new Vector3[] { };
				CreateBranchTipMesh(tip.branchObj, currentWidth + widthModifier, currentWidth, out lastBottomVerts);
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
							CreateBranchMesh(currentBranch.branchObj, currentWidth + widthModifier, currentWidth,
								out lastBottomVerts, lastBottomVerts.Length == 0 ? null : lastBottomVerts);
							break;
						case BranchType.trunk:
							CreateTreeTrunkMesh(currentBranch.branchObj, currentWidth + widthModifier, currentWidth);
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
				CreateBranchTipMesh(tip.branchObj, currentWidth, currentWidth, out lastBottomVerts);

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
							CreateBranchMesh(currentBranch.branchObj, currentWidth, currentWidth,
								out lastBottomVerts, lastBottomVerts.Length == 0 ? null : lastBottomVerts);
							break;
						case BranchType.trunk:
							CreateTreeTrunkMesh(currentBranch.branchObj, currentWidth, currentWidth);
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
	void CombineFinalTreeMesh()
	{
		List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
		TreeList.ForEach(x => meshRenderers.Add(x.renderer));
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
		TreeList.ForEach(x => meshfilters.Add(x.filter));

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
		TreeList.ForEach(x => meshTransforms.Add(x.branchObj.transform));

		//Go through all of the created submeshes & combine them into the final mesh
		List<CombineInstance> finalCombiners = new List<CombineInstance>();
		foreach (Mesh sharedSubMesh in subMeshes)
		{
			CombineInstance ci = new CombineInstance();
			ci.mesh = sharedSubMesh;
			ci.subMeshIndex = 0;
			ci.transform = LSystemParent.transform.worldToLocalMatrix;
			finalCombiners.Add(ci);
		}
		Mesh final = new Mesh();
		final.name = $"{SystemName} mesh";
		if (vertexAmount > 65535)
		{
			Debug.LogWarning("Too many vertices, attempting fix by changing indexFormat of final mesh");
			final.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		}
		final.CombineMeshes(finalCombiners.ToArray(), false);
		//final.RecalculateNormals();
		final.RecalculateTangents();
		final.Optimize();
		final.OptimizeReorderVertexBuffer();
		final.OptimizeIndexBuffers();

		for (int i = 0; i < TreeList.Count; i++)
		{
			DestroyImmediate(TreeList[i].branchObj);
		}
		TreeList.Clear();

		//Get & assign the various parts for the final mesh
		MeshFilter filter = LSystemParent.AddComponent<MeshFilter>();
		MeshRenderer mr = LSystemParent.AddComponent<MeshRenderer>();
		filter.sharedMesh = final;
		mr.sharedMaterials = subMaterials.ToArray();
	}
	#endregion

	#region Trunk/Stem
	void CreateTreeTrunkMesh(GameObject obj, float startWidth, float endWidth)
	{
		float height = ActionToFloat(EL_Rule_LSystemAction.makeBranchAndMove);
		float bottomRadius = startWidth / 2.0f;
		float topRadius = endWidth / 2.0f;
		int numSides = BranchResolution;

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
			vertex++;
		}
		vertex = 0;
		int v = 0;
		//Sides
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			verts.Add(new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius));
			v++;
		}
		#endregion

		#region Triangles
		int tri = 0;
		//Bottom
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(0);
				tris.Add(tri + 1);
				tris.Add(1);
				tri++;
			}
			else
			{
				tris.Add(0);
				tris.Add(tri + 1);
				tris.Add(tri + 2);
				tri++;
			}
		}

		tri = 0;
		//Sides
		for (int i = tri; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(numSides + 1);
				tris.Add(numSides);
				tris.Add(numSides * 2);

				tris.Add(numSides + 1);
				tris.Add(numSides - tri);
				tris.Add(numSides);
			}
			else
			{
				tris.Add(numSides + tri + 2);
				tris.Add(tri + 1);
				tris.Add(numSides + tri + 1);

				tris.Add(numSides + tri + 2);
				tris.Add(tri + 2);
				tris.Add(tri + 1);
				tri++;
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
		mf.mesh = mesh;
	}
	#endregion

	#region Branch
	void CreateBranchMesh(GameObject obj, float startWidth, float endWidth, out Vector3[] baseVerts, Vector3[] topVerts = null)
	{
		float height = ActionToFloat(EL_Rule_LSystemAction.makeBranchAndMove);
		float bottomRadius = startWidth / 2.0f;
		float topRadius = endWidth / 2.0f;
		int numSides = BranchResolution;

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
			if (topVerts == null)
			{
				Vector3 point = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
				verts.Add(point);
			}
			else
				verts.Add(obj.transform.InverseTransformPoint(topVerts[i]));
			Vector3 nextPoint = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
			vertsToSend.Add(obj.transform.TransformPoint(nextPoint));
			verts.Add(nextPoint);
			v++;
		}
		#endregion

		baseVerts = vertsToSend.ToArray();

		#region Triangles
		int tri = 0;
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(0);
				tris.Add(tri + 1);
				tris.Add(tri);
				tri++;

				tris.Add(0);
				tris.Add(1);
				tris.Add(tri);
			}
			else
			{
				tris.Add(tri + 2);
				tris.Add(tri + 1);
				tris.Add(tri);
				tri++;

				tris.Add(tri + 1);
				tris.Add(tri + 2);
				tris.Add(tri);
				tri++;
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

			normals.Add(new Vector3(cos, 0f, sin));
			normals.Add(new Vector3(cos, 0f, sin));

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
		mf.mesh = mesh;
	}
	#endregion

	#region BranchTip
	void CreateBranchTipMesh(GameObject obj, float startWidth, float endWidth, out Vector3[] baseVerts)
	{
		float height = ActionToFloat(EL_Rule_LSystemAction.makeBranchAndMove);
		float bottomRadius = startWidth / 2.0f;
		float topRadius = endWidth / 2.0f;
		int numSides = BranchResolution;

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
			verts.Add(nextPoint);
			vertsToSend.Add(obj.transform.TransformPoint(nextPoint));
			v++;
		}
		#endregion

		baseVerts = vertsToSend.ToArray();

		#region Triangles
		int tri = 0;

		//Top
		for (int i = 0; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(1);
				tris.Add(tri + 1);
				tris.Add(0);
				tri++;
			}
			else
			{
				tris.Add(tri + 2);
				tris.Add(tri + 1);
				tris.Add(0);
				tri++;
			}
		}

		tri = 0;
		//Sides
		for (int i = tri; i < numSides; i++)
		{
			if (i == numSides - 1)
			{
				tris.Add(numSides * 2);
				tris.Add(numSides);
				tris.Add(numSides + 1);

				tris.Add(numSides);
				tris.Add(numSides - tri);
				tris.Add(numSides + 1);
			}
			else
			{
				tris.Add(numSides + tri + 1);
				tris.Add(tri + 1);
				tris.Add(numSides + tri + 2);

				tris.Add(tri + 1);
				tris.Add(tri + 2);
				tris.Add(numSides + tri + 2);
				tri++;
			}
		}
		#endregion

		#region Normals
		//Sides
		normals.Add(Vector3.up);
		v = 0;
		int vertex = 0;
		for (int i = 0; i < numSides; i++)
		{
			float rad = (float)v / numSides * Two_Pi;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);

			normals.Add(new Vector3(cos, 0f, sin));

			vertex++;
			v++;
		}

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
		mf.mesh = mesh;
	}
	#endregion
}
