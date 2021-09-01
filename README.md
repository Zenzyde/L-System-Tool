# L-System-Tool
 The result of a specialization course with free choice to implement something in a specific topic. This is a very basic tool for generating a tree-like prefab object based on an L-System implementation, made for use with the Unity engine

This very basic tool is more for showing off my work during aforementioned course, moreso than actually being a fully fledged tool. During the development of this tool i learned of various ways to implement L-Systems as well as constructing procedural meshes in Unity and a way to combine several meshes into a single mesh. Lastly i learned how to save a procedurally created mesh object as a prefab to be able to use it later on.

The tool is controlled via a single editor script that with this implementation needs to be placed on it's own transform-object.\
![Generator](/images/l_system_generator.png)

The tool has support for creating an L-System object based on user defined scriptable objects called "Custom Rules".\
![Custom Rule](/images/l_system_custom_rule.png)

The following are a few examples of L-Systems generated with the tool, as well as an example of a saved L-System prefab and the generated mesh belonging to the prefab.\
![Algae](/images/algae_example.png)

![Randomized fern](/images/fern_rand_example.png)

![Tree](/images/tree_example.png)

![Non-rule randomized flower](/images/flower_non_rule_rand_example.png)

![Prefab](/images/l_system_prefab.png)

![Mesh](/images/l_system_prefab_mesh.png)
