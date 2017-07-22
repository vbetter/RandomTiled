using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TVNT {
	[CustomEditor(typeof(LevelTiles),true)]
	public class LevelTilesInspector : Editor {

		LevelTiles levelTile;

		public void OnEnable() {
			levelTile = (LevelTiles)target;
		}

		public override void OnInspectorGUI() {
			DrawDefaultInspector ();
			if (Application.isPlaying == false) {
				levelTile.InspectorUpdate ();
			}
		}
	}
}
