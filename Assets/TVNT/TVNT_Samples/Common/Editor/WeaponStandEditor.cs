using UnityEngine;
using UnityEditor;
using System.Collections;

namespace TVNT {
	[CustomEditor(typeof(WeaponStand),true)]
	public class WeaponStandEditor : Editor {

		WeaponStand weaponStand;

		public void OnEnable() {
			weaponStand = (WeaponStand)target;
		}

		public override void OnInspectorGUI() {
			serializedObject.Update ();
			DrawDefaultInspector ();
			serializedObject.ApplyModifiedProperties ();
		}

		private void OnSceneGUI() {
			if (weaponStand.stand) {
				Handles.color = new Color (1, 0, 0, 0.25f);
				float radius = 1.5f;
				Handles.DrawSolidDisc (weaponStand.stand.position + new Vector3 (0, 0.45f, 0), Vector3.up, radius);
				int standCycleIndex = 0;

				if (weaponStand.rotateInSingleDirection == false) {
					Handles.DrawSolidDisc (weaponStand.stand.position + new Vector3 (0, 0.45f, 0), Vector3.up, 0.25f);
				}

				for (int i = 0; i < weaponStand.standCycle.Length; i++) {

					int standDirectionInt = (int)weaponStand.standCycle [standCycleIndex];

					Handles.color = new Color (1 * (i + 1) / (weaponStand.standCycle.Length * 1f), 1 * (i + 1) / (weaponStand.standCycle.Length * 1f), 1 * (i + 1) / (weaponStand.standCycle.Length * 1f), 1);

					Vector3 discCenter = Vector3.zero;
					switch (standDirectionInt) {
					case 0:
						discCenter = weaponStand.stand.position + new Vector3 (radius, 0.5f, 0);
						break;
					case 1:
						discCenter = weaponStand.stand.position + new Vector3 (0, 0.5f, -radius);
						break;
					case 2:
						discCenter = weaponStand.stand.position + new Vector3 (-radius, 0.5f, 0);
						break;
					case 3:
						discCenter = weaponStand.stand.position + new Vector3 (0, 0.5f, radius);
						break;
					}
					Handles.DrawSolidDisc (discCenter, Vector3.up, 0.25f);
					Handles.Label (discCenter, (i + 1).ToString ());
					if (i == 0) {
						Vector3 rotationEnd = Vector3.zero;
						if (weaponStand.rotateClockwise) {
							
							switch (standDirectionInt) {
							case 0:
								rotationEnd = new Vector3 (0, 0, radius);
								break;
							case 1:
								rotationEnd = new Vector3 (radius, 0, 0);
								break;
							case 2:
								rotationEnd = new Vector3 (0, 0, -radius);
								break;
							case 3:
								rotationEnd = new Vector3 (-radius, 0, 0);
								break;
							}
							Handles.DrawLine (discCenter, discCenter + rotationEnd);
						} else {
							
							switch (standDirectionInt) {
							case 0:
								rotationEnd = new Vector3 (0, 0, -radius);
								break;
							case 1:
								rotationEnd = new Vector3 (-radius, 0, 0);
								break;
							case 2:
								rotationEnd = new Vector3 (0, 0, radius);
								break;
							case 3:
								rotationEnd = new Vector3 (radius, 0, 0);
								break;
							}
						}
						Handles.DrawLine (discCenter, discCenter + rotationEnd);
					}

					if (weaponStand.rotateClockwise) {
						standCycleIndex++;
						if (standCycleIndex >= weaponStand.standCycle.Length) {
							standCycleIndex = 0;
						}
					} else {
						standCycleIndex--;
						if (standCycleIndex < 0) {
							standCycleIndex = weaponStand.standCycle.Length - 1;
						}
					}
				}
				Handles.color = Color.red;

				Handles.color = Color.white;
			}
		}
	}
}
