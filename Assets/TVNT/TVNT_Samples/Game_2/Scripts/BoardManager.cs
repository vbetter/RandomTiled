using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TVNT;

public class BoardManager : MonoBehaviour {

	//Holds a reference to the character prefabs
	public Transform characterPrefab;
	//The movement style to set on the character controller when the character prefab is initialized and assigned
	//to the active character variable
	public TVNTCharacterController.MovementStyle movementStyle = TVNTCharacterController.MovementStyle.DISCRETE_WALK;
	//Holds an initialized version of the character prefab
	[HideInInspector]
	public Transform activeCharacter = null;
	//The initial rotation of the player
	public Vector3 initialCharacterRotation;

	//The game object that will act as a parent to all the patterns spawned by the game.
	//this prevents the mess of all the patterns being in the main scene view heirarchy
	public Transform patternParentContainer;

	//The minimum board length. Increase this value if you want longer levels
	//Decrease it if you want shorter levels
	public int minBoardLength = 13;
	//The number of easy levels
	public int easyDifficultyIndex = 5;
	//The number of medium levels
	public int mediumDifficultyIndex = 10;
	//The level interval at which boss enemies are spawned
	//for instance if this value is set to 5, then level 5, 10, 15, 20 will all host bosses
	//and the bosses picked will be determined by the difficulty we are at
	//so if we are below the number of easy level and a boss is needed, we ask the pattern loader
	//for an easy boss.
	public int levelIntervalTillBoss = 5;

	//The list that holds a reference to all the spawned patterns
	private List<Transform> spawnedPatterns = new List<Transform>();
	//This list holds a reference to the top entrance values of the all the spawned patterns from
	//the spawnedPatterns list. This is used when requesting a new pattern, since the top pattern entrances
	//of the last pattern and the bottom pattern entrances of the pattern received from the pattern loader
	//need to ensure that stitching the two patterns together will not result in a dead end
	private List<int> spawnedPatternsEntrances = new List<int>();
	//This is a temporary array used when the level tiles within a pattern are initialized
	private LevelTiles[] levelTiles;

	//This variable keeps track of the position at which the last pattern was spaced.
	//this variable is relative to the patternParentContainer, and holds the local z value at
	//which the new pattern is to be spawned
	private float newPatternStartZ = 0;

	//Camera targeting variables
	//The variables that are used to make sure that while the camera moves with the player,
	//it does not exceed the level bounds
	private Transform cameraTarget;
	private float xMax = -float.Epsilon;
	private Vector3 xMaxPosition = Vector3.zero;
	private float yMax = -float.Epsilon;
	private Vector3 yMaxPosition = Vector3.zero;

	private float initialCharacterZPosition;
	private bool boardOverflow = false;

    GameObject _cameraMgr;

	public void SetupScene(int level) {
		int enemyCount = (int)Mathf.Log (level, 2f);
		int currentBoardLength = 0;
		bool spawnStartPattern = true;
		bool spawnEndPattern = true;
		bool spawnCharacter = true;
		while (currentBoardLength < minBoardLength || spawnEndPattern) {
			Transform selectedPatternPrefab = null;
			if (currentBoardLength >= minBoardLength) {
				selectedPatternPrefab = PatternLoader.instance.GetEndPattern ();
				spawnEndPattern = false;
			} else if (spawnStartPattern) {
				selectedPatternPrefab = PatternLoader.instance.GetStartPattern ();
				spawnStartPattern = false;
			} else {
				bool bossSpawned = false;
				if (enemyCount > 0) {
					bool spawnEnemy = Random.Range (0f, 1f*enemyCount) > 0.5f ? true : false;
					if (spawnEnemy) {
						if (level <= easyDifficultyIndex) {
							if (level % levelIntervalTillBoss != 0 || bossSpawned) {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyEasyPattern(spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
							} else {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyEasyBossPattern(spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
								bossSpawned = true;
							}
						} else if (level <= easyDifficultyIndex + mediumDifficultyIndex) {
							if (level % levelIntervalTillBoss != 0 || bossSpawned) {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyMediumPattern(spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
							} else {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyMediumBossPattern(spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
								bossSpawned = true;
							}
						} else {
							if (level % levelIntervalTillBoss != 0 || bossSpawned) {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyHardPattern(spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
							} else {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyHardBossPattern(spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
								bossSpawned = true;
							}
						}
						enemyCount -= selectedPatternPrefab.GetComponent<Pattern> ().enemyCount;
					}
				}
				if (selectedPatternPrefab == null) {
					if (level < easyDifficultyIndex) {
						selectedPatternPrefab = PatternLoader.instance.GetEasyPattern (spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
					} else if (level < easyDifficultyIndex + mediumDifficultyIndex) {
						selectedPatternPrefab = PatternLoader.instance.GetMediumPattern (spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
					} else {
						selectedPatternPrefab = PatternLoader.instance.GetHardPattern (spawnedPatternsEntrances[spawnedPatternsEntrances.Count-1]);
					}
				}
			}

			Transform selectedPattern = (Transform)Instantiate (selectedPatternPrefab);
			selectedPattern.name = selectedPatternPrefab.name; //to prevent the clone thing that just irritates me personally :)
			Pattern patternScript = selectedPattern.GetComponent<Pattern>();

			bool evenGridZ = patternScript.gridZ % 2 == 0 ? true : false;
			float halfGridZ = patternScript.gridZ * 0.5f;
			if (evenGridZ) {
				//Used to compensate for the padding in the pattern if the gridz is even
                newPatternStartZ -= 0.5f;
            }
            newPatternStartZ += halfGridZ;
            selectedPattern.parent = patternParentContainer;
			selectedPattern.localPosition = new Vector3 (0, 0, newPatternStartZ * PatternSettings.tiledSize);

			levelTiles = selectedPattern.GetComponentsInChildren<LevelTiles> ();
			for (int i = 0; i < levelTiles.Length; i++) {
				levelTiles [i].Initialize ();
				Vector3 viewPointPosition = Camera.main.WorldToViewportPoint (levelTiles [i].transform.position);
				if (viewPointPosition.x > xMax) {
					xMax = viewPointPosition.x;
					xMaxPosition = levelTiles [i].transform.position;
				}
				if (viewPointPosition.y > yMax) {
					yMax = viewPointPosition.y;
					yMaxPosition = levelTiles [i].transform.position;
				}
			}

            newPatternStartZ += halfGridZ;
            if (evenGridZ) {
                newPatternStartZ -= 0.5f;
            }

			spawnedPatterns.Add (selectedPattern);
			spawnedPatternsEntrances.Add (patternScript.topEntrances);

			currentBoardLength += patternScript.gridZ;

			if (spawnCharacter) {
				spawnCharacter = false;
				activeCharacter = (Transform)Instantiate (characterPrefab, Vector3.zero, Quaternion.Euler(initialCharacterRotation));
				activeCharacter.parent = patternParentContainer;
				activeCharacter.GetComponent<TVNTCharacterController> ().movementStyle = movementStyle;
				Vector3 startPosition = GameObject.Find("Start_Symbol").transform.position + new Vector3(0,PatternSettings.playerYOffset,0);
				activeCharacter.position = startPosition;
				initialCharacterZPosition = startPosition.z;
			}
		}
    }

	public void ClearBoard() {
		for (int i = spawnedPatterns.Count - 1; i > -1; i--) {
			//Destroy enemies
			TVNTAIController[] activeEnemies = spawnedPatterns[i].GetComponentsInChildren<TVNTAIController>();
			for (int j = 0; j < activeEnemies.Length; j++) {
				Destroy (activeEnemies [j].gameObject);
			}
			//Bullet[] bullets = spawnedPatterns[i].GetComponentsInChildren<Bullet> ();
			Bullet[] bullets = GameObject.FindObjectsOfType<Bullet> ();
			for (int j = 0; j < bullets.Length; j++) {
				TVNTObjectPool.instance.ReleaseObject (bullets [j].transform);
			}
			GroundCollider[] groundColliders = spawnedPatterns[i].GetComponentsInChildren<GroundCollider> ();
			for (int j = 0; j < groundColliders.Length; j++) {
				groundColliders [j].occupied = false;
			}
			levelTiles = spawnedPatterns[i].GetComponentsInChildren<LevelTiles> ();
			for (int j = 0; j < levelTiles.Length; j++) {
				if (levelTiles [j].myPrefab) {
					TVNTObjectPool.instance.ReleaseObject (levelTiles [j].myPrefab);
				}
				levelTiles [j].myPrefab = null;
			}
		}
		spawnedPatterns.Clear ();
		spawnedPatternsEntrances.Clear ();
		if (activeCharacter) {
			Destroy (activeCharacter.gameObject);
		}
		for (int i = 0; i < patternParentContainer.childCount; i++) {
			Destroy (patternParentContainer.GetChild (i).gameObject);
		}
	}

    private void LateUpdate()
    {
        if (activeCharacter && _cameraMgr!=null && _cameraMgr.transform.position!= activeCharacter.position)
        {
            Vector3 pos = _cameraMgr.transform.position;

            _cameraMgr.transform.position = Vector3.Lerp(pos, activeCharacter.position, Time.deltaTime*2f);
        }
    }
}
