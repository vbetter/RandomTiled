using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TVNT {
	[CustomEditor(typeof(PatternLoader))]
	public class PatternLoaderInspector : Editor {

		PatternLoader patternLoader;

		public void OnEnable() {
			patternLoader = (PatternLoader)target;
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();
			DrawDefaultInspector ();
			EditorGUILayout.TextField ("Pattern Location",patternLoader.patternLocation);
			if (GUILayout.Button ("Select Pattern Location")) {
				string strTempPatternLocation = EditorUtility.OpenFolderPanel ("Choose Pattern Location", "", "");
				if (strTempPatternLocation.StartsWith (Application.dataPath) && strTempPatternLocation.Contains("/Resources/")) {
					strTempPatternLocation = strTempPatternLocation.Substring (Application.dataPath.Length + 1, strTempPatternLocation.Length - (Application.dataPath.Length + 1));
					patternLoader.patternLocation = strTempPatternLocation.Substring(strTempPatternLocation.LastIndexOf("/Resources/")+11);
				} else {
					EditorUtility.DisplayDialog ("Wrong Location!", "The pattern location must be within the scope of the asset directory and inside a Resources folder!", "Ok");
				}
			}
			serializedObject.ApplyModifiedProperties ();
		}
	}
}
