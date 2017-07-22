using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TVNT;

public class Game1 : MonoBehaviour {

	//Holds a reference to the character prefab
	public Transform characterPrefab;
	//The movement style to set on the character controller when the character prefab is initialized and assigned
	//to the active character variable
	public TVNTCharacterController.MovementStyle movementStyle = TVNTCharacterController.MovementStyle.CONTINUOS_WALK;
	//Holds an initialized version of the character prefab
	private Transform activeCharacter = null;
	//The initial rotation of the player
	public Vector3 initialCharacterRotation;
	//This is the transform that the smooth camera follow script on the main camera will use as a target
	public Transform cameraTarget;
	//Since this is an endless runner, the camera moves forward at a certain speed to prevent the player from
	//being able to just stand in one place. This variable represents that speed.
	public float cameraTargetSpeed = 4f;

	//The game object that will act as a parent to all the patterns spawned by the game.
	//this prevents the mess of all the patterns being in the main scene view heirarchy
	public Transform patternParentContainer;

	//The list that holds a reference to all the spawned patterns still active
	private List<Transform> onScreenPatterns = new List<Transform> ();
	//The list that hold a reference to the top edge of pattern.
	//This is used to check if a pattern has gone off the screen and as a result when to remove the pattern
	private List<float> onScreenPatternEdges = new List<float> ();
	//This list holds a reference to the top entrance values of the all the active patterns from
	//the onScreenPatterns list. This is used when requesting a new pattern, since the top pattern entrances
	//of the last pattern and the bottom pattern entrances of the pattern received from the pattern loader
	//need to ensure that stitching the two patterns together will not result in a dead end
	private List<int> onScreenPatternEntrances = new List<int> ();
	//This is a temporary array used when the level tiles within a pattern are initialized
	private LevelTiles[] levelTiles;

	//The variable that keeps track of whether a start pattern needs to be spawned
	private bool spawnStartPattern = true;
	//used to keep track of whether the active character has been initialized
	private bool spawnCharacter = true;
	//This variable keeps track of the position at which the last pattern was spaced.
	//this variable is relative to the patternParentContainer, and holds the local z value at
	//which the new pattern is to be spawned
	private float newPatternStartZ = 0;
	//The pattern index variable is used to implement connector patterns to give players a breather
	private int patternIndex = 0;
	//The distance from the active characters position to spawn patterns out till when the spawn
	//pattern method is called
	private float patternSpawnDistance = 80f;
	//When checking if a set of new patterns need to be spawned, this value represents the minimum distance
	//between the player and the last spawned pattern below which the spawn pattern method will be called
	private float newSpawnDistance = 30f;

	//A reference to the initial position of the character. This variable is used for score purposes.
	//To check how much distance in the z axis the player has travelled.
	private float characterInitialPosition;

	//A reference to the Text object in the UI canvas that represents the score
	public Text scoreText;
	//The score int
	private int score = 0;
	//A reference to the Text object in the UI canvas that represents the amount of gold the player has collected
	public Text goldText;
	//The gold int
	private int gold = 0;

	//Variable that is used to mark when the tap to start button has been clicked
	private bool gameStarted = false;
	private bool gameOver = false;
	//The value to add to the gold int when a player smashes into a pot
	private int potScore = 20;
	//The value to add to the gold int when a player picks up a coin
	private int coinScore = 5;

	//UI Stuff
	public Canvas uiCanvas;
	public RectTransform gameOverPanel;
	public Transform floatingTextPrefab;

	//Game difficulty
	//Variable used to keep track of the number of patterns spawned
	private int numberOfPatternsAdded = 0;
	//If the number of patterns added is less than the number of easy patterns then keep requesting easy patterns from the pattern loader
	public int numberOfEasyPatterns = 60;
	//If the number of patterns added is greater than the number of easy patterns by less than the number of easyPatterns+the number of medium patterns
	//keep requesting medium patterns from the pattern loader
	//else request hard patterns
	public int numberOfMediumPatterns = 100;

	//Sounds
	//The sound clip to play when the player picks up a coin
	public AudioClip pickup1;
	//The sound clip to play when the player smashes a pot
	public AudioClip pickup2;
	//The sound clip to play when the player clicks on the click to start button
	public AudioClip buttonClip;
	public AudioSource myAudio = null;

	//Game over menu delay
	public float gameOverMenuDelay = 1f; //in seconds

	void Start () {
		PatternLoader.instance.LoadPatterns ();
		LoadGame ();
	}

	private void LoadGame() {
		StartCoroutine (SpawnPattern ());
	}

	public void StartGame() {
		//Debug.Log ("start game");
		activeCharacter.GetComponent<TVNTPlayerController> ().activate = true;
		gameStarted = true;
		if (myAudio) {
			myAudio.clip = buttonClip;
			myAudio.Play ();
		}
	}

	private IEnumerator SpawnPattern() {
		while (spawnStartPattern || activeCharacter.position.z - (newPatternStartZ * PatternSettings.tiledSize) < patternSpawnDistance) {
			patternIndex++;
			numberOfPatternsAdded++;
			Transform selectedPatternPrefab = null;

			if (!spawnStartPattern) {
				if (patternIndex % 5 != 0) {
					if (numberOfPatternsAdded <= numberOfEasyPatterns) {
						selectedPatternPrefab = PatternLoader.instance.GetEasyPattern (onScreenPatternEntrances [onScreenPatternEntrances.Count - 1]);
					} else if (numberOfPatternsAdded <= numberOfEasyPatterns + numberOfMediumPatterns) {
						selectedPatternPrefab = PatternLoader.instance.GetMediumPattern (onScreenPatternEntrances [onScreenPatternEntrances.Count - 1]);
					} else {
						selectedPatternPrefab = PatternLoader.instance.GetHardPattern (onScreenPatternEntrances [onScreenPatternEntrances.Count - 1]);
					}
				} else {
					selectedPatternPrefab = PatternLoader.instance.GetConnectorPattern ();
				}
			} else {
				selectedPatternPrefab = PatternLoader.instance.GetStartPattern ();
				spawnStartPattern = false;
			}

			Transform selectedPattern = (Transform)Instantiate (selectedPatternPrefab);
			selectedPattern.name = selectedPatternPrefab.name; //to prevent the clone thing that just irritates me personally :)
			Pattern patternScript = selectedPattern.GetComponent<Pattern>();

			bool evenGridZ = patternScript.gridZ % 2 == 0 ? true : false;
			float halfGridZ = patternScript.gridZ * 0.5f;
			if (evenGridZ) {
				newPatternStartZ += 0.5f;
			}
			newPatternStartZ -= halfGridZ;

			selectedPattern.parent = patternParentContainer;
			selectedPattern.localPosition = new Vector3 (0, 0, newPatternStartZ * PatternSettings.tiledSize);

			levelTiles = selectedPattern.GetComponentsInChildren<LevelTiles> ();
			for (int i = 0; i < levelTiles.Length; i++) {
				levelTiles [i].Initialize ();
			}

			newPatternStartZ -= halfGridZ;
			if (evenGridZ) {
				newPatternStartZ -= 0.5f;
			}

			onScreenPatterns.Add (selectedPattern);
			onScreenPatternEdges.Add (-(halfGridZ + 1) * PatternSettings.tiledSize);
			onScreenPatternEntrances.Add (patternScript.topEntrances);

			if (spawnCharacter) {
				activeCharacter = (Transform)Instantiate (characterPrefab, Vector3.zero, Quaternion.Euler(initialCharacterRotation));
				activeCharacter.parent = patternParentContainer;
				activeCharacter.GetComponent<TVNTCharacterController> ().movementStyle = movementStyle;
				activeCharacter.GetComponent<TVNTPlayerController> ().tapToMove = false;
				//Find the start tile and the place the character over it
				//there should be a start tile in the start pattern
				Vector3 startPosition = GameObject.Find("Start_Symbol").transform.position + new Vector3(0,PatternSettings.playerYOffset,0);
				activeCharacter.position = startPosition;
				characterInitialPosition = startPosition.z;
				cameraTarget.position = activeCharacter.position;
				Camera.main.GetComponent<SmoothFollowCamera2D> ().target = cameraTarget;
				spawnCharacter = false;
			}
			yield return null;
		}
	}

	void LateUpdate () {
		if (gameStarted && !gameOver) {
			if (!activeCharacter) {
				gameOver = true;
				Invoke ("GameOver", gameOverMenuDelay);
				return;
			}
			Vector2 playerCameraPosition = Camera.main.WorldToViewportPoint (activeCharacter.position + new Vector3 (0, 0, -0.5f * PatternSettings.tiledSize));
			if (playerCameraPosition.x > 1 || playerCameraPosition.y > 1) {
				activeCharacter.GetComponent<TVNTPlayerController> ().activate = false;
				GameOver ();
			}

			cameraTarget.Translate (0, 0, -cameraTargetSpeed * Time.deltaTime);
			if (activeCharacter.position.z < cameraTarget.position.z) {
				cameraTarget.Translate (0, 0, -(cameraTarget.position.z - activeCharacter.position.z) * Time.deltaTime);
			}

			int newScore = Mathf.FloorToInt ((characterInitialPosition - activeCharacter.position.z) / PatternSettings.tiledSize);
			if (newScore > score) {
				score = newScore;
				scoreText.text = score.ToString ();
			}
		}
	}

	void FixedUpdate() {
		if (gameStarted && !gameOver && activeCharacter) {
			for (int i = 0; i < onScreenPatterns.Count; i++) {
				//Check if the patterns that are being display are on the screen or off it.
				//If the pattern is off the screen then delete it
				if (Camera.main.WorldToViewportPoint (onScreenPatterns [i].transform.position + new Vector3 (-((PatternSettings.gridX * 0.5f) + 1) * PatternSettings.tiledSize,
					   0, onScreenPatternEdges [i])).x > 1) {
					Transform onScreenPatternToDestroy = onScreenPatterns [i];
					onScreenPatterns.RemoveAt (i);
					onScreenPatternEdges.RemoveAt (i);
					onScreenPatternEntrances.RemoveAt (i);

					Bullet[] bullets = onScreenPatternToDestroy.GetComponentsInChildren<Bullet> ();
					for (int u = 0; u < bullets.Length; u++) {
						TVNTObjectPool.instance.ReleaseObject (bullets [u].transform);
					}
					levelTiles = onScreenPatternToDestroy.GetComponentsInChildren<LevelTiles> ();
					for (int j = 0; j < levelTiles.Length; j++) {
						//levelTiles [j].Initialize ();
						if (levelTiles [j].myPrefab) {
							TVNTObjectPool.instance.ReleaseObject (levelTiles [j].myPrefab);
						}
						levelTiles [j].myPrefab = null;
					}

					Destroy (onScreenPatternToDestroy.gameObject);
					break;
				}
			}

			if (activeCharacter.position.z < -2000) {
				float amountToReset = -activeCharacter.position.z;
				Camera.main.transform.position += new Vector3 (0, 0, amountToReset);
				for (int i = 0; i < onScreenPatterns.Count; i++) {
					onScreenPatterns[i].position += new Vector3 (0, 0, amountToReset);
				}
				cameraTarget.position += new Vector3 (0, 0, amountToReset);
				activeCharacter.position += new Vector3 (0, 0, amountToReset);
				newPatternStartZ += (amountToReset / PatternSettings.tiledSize);
				characterInitialPosition += amountToReset;
				return;
			}

			if (activeCharacter.position.z - newPatternStartZ < newSpawnDistance) {
				StartCoroutine (SpawnPattern ());
			}
		}
	}

	private void GameOver() {
		gameOverPanel.gameObject.SetActive (true);
		gameOver = true;
	}

	public void SmashedPot(Vector3 position) {
		gold += potScore;
		goldText.text = gold.ToString ();
		CreateFloatingText (position, potScore.ToString());
		if (myAudio) {
			myAudio.clip = pickup2;
			myAudio.Play ();
		}
	}

	public void PickedUpCoin(Vector3 position) {
		gold += coinScore;
		goldText.text = gold.ToString ();
		CreateFloatingText (position, coinScore.ToString());
		if (myAudio) {
			myAudio.clip = pickup1;
			myAudio.Play ();
		}
	}

	public void CreateFloatingText(Vector3 position, string text) {
		Transform floatingText = (Transform)Instantiate (floatingTextPrefab);
		floatingText.GetChild(0).GetComponent<Text> ().text = "+"+text;
		floatingText.SetParent (uiCanvas.transform, false);
		Vector2 screenPosition = Camera.main.WorldToScreenPoint (position);
		floatingText.transform.position = screenPosition;
	}

	public void RestartGame() {
		if (myAudio) {
			myAudio.clip = buttonClip;
			myAudio.Play ();
		}
		if (activeCharacter) {
			Destroy (activeCharacter.gameObject);
		}
		for (int i = onScreenPatterns.Count-1; i > -1; i--) {
			Transform onScreenPatternToDestroy = onScreenPatterns [i];
			onScreenPatterns.RemoveAt (i);
			onScreenPatternEdges.RemoveAt (i);
			onScreenPatternEntrances.RemoveAt (i);

			Bullet[] bullets = onScreenPatternToDestroy.GetComponentsInChildren<Bullet> ();
			for (int j = 0; j < bullets.Length; j++) {
				TVNTObjectPool.instance.ReleaseObject (bullets [j].transform);
			}
			GroundCollider[] groundColliders = onScreenPatternToDestroy.GetComponentsInChildren<GroundCollider> ();
			for (int j = 0; j < groundColliders.Length; j++) {
				groundColliders [j].occupied = false;
			}
			levelTiles = onScreenPatternToDestroy.GetComponentsInChildren<LevelTiles> ();
			for (int j = 0; j < levelTiles.Length; j++) {
				if (levelTiles [j].myPrefab) {
					TVNTObjectPool.instance.ReleaseObject (levelTiles [j].myPrefab);
				}
				levelTiles [j].myPrefab = null;
			}

			Destroy (onScreenPatternToDestroy.gameObject);
		}
		gameOverPanel.gameObject.SetActive (false);
		Application.LoadLevel ("Game1");
	}

	public void Pause() {
		if (Mathf.Approximately (Time.timeScale, 0)) {
			Time.timeScale = 1;
		} else {
			Time.timeScale = 0;
		}
	}
}

