using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;

public class MakeBuilds : Editor {
	struct NamedTarget
	{
		public BuildTarget build;
		public string name;
	};
	public static string Name = "Coven";

	[MenuItem("Coven/Build All Platforms")]
	static public void BuildAllPlatforms ()
	{
		string[] levels = { "Assets/_Sacrifice.unity" };
	
		NamedTarget[] targets = new NamedTarget[] {
			new NamedTarget { build=BuildTarget.WebPlayer,			name="WebPlayer" } ,
			new NamedTarget { build=BuildTarget.WebGL,				name="HTML5" } ,
			new NamedTarget { build=BuildTarget.StandaloneOSXIntel,	name="OSX" } ,
			new NamedTarget { build=BuildTarget.StandaloneWindows,	name="PC" } };

		foreach (var t in targets)
		{
			//if (!EditorUserBuildSettings.SwitchActiveBuildTarget (t))
			//	continue;

			BuildPipeline.BuildPlayer (levels, Path.Combine("Builds", Name + "_" + t.name), t.build, BuildOptions.None);
		}
	}
}
