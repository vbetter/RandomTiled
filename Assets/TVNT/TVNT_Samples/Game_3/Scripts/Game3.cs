using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TVNT;
using Uween;

public class Game3 : MonoBehaviour {

	//Screen fader used to transition between levels
	public ScreenFader screenFader;

	//UI Variables
	public GameObject gameTitle;
	public Image mainMenuOverlay;
	public GameObject settingsButton;
	public Text textBestFloorMainMenu;
	public GameObject textFloor;
	public GameObject hearts;
	public Image[] individualHearts;
	public GameObject tapToStartButton;
	public GameObject gameOverMenu;
	public GameObject gameSettingsMenu;
	public Text textCurrentFloor;
	public Text textBestFloor;

	public Transform[] patternContainers;
	private int currentPatternContainer = 0;
	public Transform patternContainer1;
	public Transform patternContainer2;

	public Transform characterPrefab;
	public TVNTCharacterController.MovementStyle movementStyle;
	private int playerLives = 3; //Don't change this value unless you have accounted for it by adding more hearts to the UI and referencing them right!
	public bool flipVerticalInput = true;
	public bool flipHorizontalInput = true;
	[HideInInspector]
	public Transform activeCharacter = null;
	public Vector3 initialCharacterRotation;
	private float initialCharacterZPosition;
	private float currBoardProgressRate = 0;
	private float prevBoardProgressRate = 0;

	private bool patternsLoaded = false;
	private float minGameTitleDisplayTime = 2f;
	private float newPatternStartZ = 0;
	private LevelTiles[] levelTiles; //Used to temporarily hold the level tiles within a pattern
	private List<Transform> spawnedPatterns = new List<Transform>();
	private List<int> spawnedPatternEntrances = new List<int>();

	//Variables used to setup the camera tracking
	private float xMax = -float.MaxValue;
	private float xMin = float.MaxValue;
	private float yMax = -float.MaxValue;
	private float yMin = float.MaxValue;
	private Vector3 xMaxPosition;
	private Vector3 xMinPosition;
	private Vector3 yMaxPosition;
	private Vector3 yMinPosition;
	private float camPadXMax = 0.125f; //in pixels
	private float camPadXMin = 0.1f; //in pixels
	private float camPadYMax = 0.225f; //in pixels
	private float camPadYMin = 0.125f;// in pixels
	private float widthToHeightRatio = 1.33f;
	private Vector3 minCameraPosition;
	private Vector3 maxCameraPosition;
	private bool floorOverflowsCameraBounds = false;
	private bool interFloorTransition = false;
	private Vector3 initialCameraPosition;
	private float interFloorTransitionDuration = 0.875f; //In seconds
	private float patternTransitionAmount = 30; //in level tile sizes; i.e 1 would be 1 level tile distance
	public Text floorText;

	//Board variables
	private int floor = 1;
	public int minBoardLength = 13;
	public int easyDifficultyIndex = 5;
	public int mediumDifficultyIndex = 10;
	public int floorIntervalTillBoss = 5;

	//Variables used to fade sound in and out
	public Slider volumeSlider;
	public static float initialAudioListenerSound = -1f;

	//Variables dealing with the coins and pots in the game
	private int potScore = 20;
	private int coinScore = 5;
	public Text goldText;
	private int gold = 0;
	public Canvas uiCanvas;
	public Transform floatingTextPrefab;
	public AudioClip coinPickupAudioClip;
	public AudioClip potSmashAudioClip;
	public AudioSource myAudio = null;

	public Text debugText;

	void Start () {
		System.GC.Collect ();

		if (initialAudioListenerSound < 0) {
			initialAudioListenerSound = AudioListener.volume;
		}
		AudioListener.volume = 0;

		TweenY.Add (gameTitle, 2f, 0f).EaseOutElasticWith (20, 0.625f);
		PatternLoader.instance.LoadPatterns ();
		initialCameraPosition = Camera.main.transform.position;

		float xPaddingMulti = (widthToHeightRatio/((Screen.width * 1f) / (Screen.height * 1f)));
		camPadXMin =  xPaddingMulti * camPadXMin;
		camPadXMax = xPaddingMulti * camPadXMax;
		floorText.text = "FLOOR " + floor;
		textBestFloorMainMenu.text = "BEST " + GetHighScore (0).ToString ();

		goldText.text = "0";

		TVNTCharacterController.flipVerticalInput = flipVerticalInput;
		TVNTCharacterController.flipHorizontalInput = flipHorizontalInput;

		debugText.text = PatternSettings.patternPath;
	}

	void Update () {
		if (minGameTitleDisplayTime < 0) {
			if (patternsLoaded == false) {
				if (PatternLoader.instance.patternsLoaded && PatternSettings.patternPath.Length > 0) {
					patternsLoaded = true;
					ShowGameMenu ();
				}
			}
		} else {
			minGameTitleDisplayTime -= Time.deltaTime;
		}
	}

	//bool setCamPos = false;
	void LateUpdate() {
		//Move camera to fit board and player
		if (activeCharacter && floorOverflowsCameraBounds && interFloorTransition==false) {
			float zDiff = activeCharacter.position.z - initialCharacterZPosition;
			if (zDiff < 0) {
				zDiff = 0;
			}
			currBoardProgressRate = Mathf.Lerp (prevBoardProgressRate, zDiff/ (newPatternStartZ * PatternSettings.tiledSize), Time.deltaTime * 0.75f);
			prevBoardProgressRate = currBoardProgressRate;
			Camera.main.transform.position = Vector3.Lerp (minCameraPosition, maxCameraPosition, currBoardProgressRate);
			//Camera.main.transform.position = new Vector3 (minCameraPosition.x, 17,0);
		}
	}

	private void ShowGameMenu() {
		//Spawn the introductory pattern that shows up on the main menu
		SpawnIntroductoryPattern();

		SetupCamera ();
		//Camera.main.transform.position = new Vector3(0,18,0);

		TweenY.Add (gameTitle, 0.65f, 250f).EaseInBackWith (2).Delay (0.125f);
		TweenA.Add (mainMenuOverlay.gameObject, 1f, 0).EaseInOutExpo ().Then (HideOverlay);
		StartCoroutine (FadeSoundIn (0.65f));
	}

	private void HideOverlay() {
		mainMenuOverlay.gameObject.SetActive (false);
	}

	private void SpawnIntroductoryPattern() {
		Transform selectedPatternPrefab = PatternLoader.instance.GetIntroductoryPattern ();
		Transform selectedPattern = (Transform)Instantiate (selectedPatternPrefab);
		selectedPattern.name = selectedPatternPrefab.name;
		//debugText.text = selectedPattern.name;
		Pattern patternScript = selectedPattern.GetComponent<Pattern> ();

		bool evenGridZ = patternScript.gridZ % 2 == 0 ? true : false;
		float halfGridZ = patternScript.gridZ * 0.5f;
		if (evenGridZ) {
			newPatternStartZ -= 0.5f;
		}
		newPatternStartZ += halfGridZ;

		selectedPattern.parent = patternContainers[currentPatternContainer];
		selectedPattern.localPosition = new Vector3 (0, 0, newPatternStartZ * PatternSettings.tiledSize);
		//Rotate the pattern since in this game the character moves from the bottom to the top
		selectedPattern.localRotation = Quaternion.Euler (0, 180, 0);


		levelTiles = selectedPattern.GetComponentsInChildren<LevelTiles> ();
		for (int i = 0; i < levelTiles.Length; i++) {
			levelTiles [i].Initialize ();
		}

		newPatternStartZ += halfGridZ;
		if (evenGridZ) {
			newPatternStartZ += 0.5f;
		}

		spawnedPatterns.Add (selectedPattern);
		spawnedPatternEntrances.Add (patternScript.topEntrances);

		if (activeCharacter == null) {
			activeCharacter = (Transform)Instantiate (characterPrefab, Vector3.zero, Quaternion.Euler (initialCharacterRotation));
			activeCharacter.parent = patternContainers[currentPatternContainer];
			TVNTPlayerController playerControllerScript = activeCharacter.GetComponent<TVNTPlayerController> ();
			playerControllerScript.movementStyle = movementStyle;
			Vector3 startPosition = GameObject.Find ("Start_Symbol").transform.position;
			activeCharacter.position = new Vector3 (startPosition.x, startPosition.y + PatternSettings.playerYOffset, startPosition.z);
			initialCharacterZPosition = activeCharacter.position.z;
			playerControllerScript.lives = playerLives;
			//Don't active the character here. Activate the character after the start button is clicked
			//activeCharacter.GetComponent<TVNTCharacterController> ().activate = true;
		}
	}

	private void SetupCamera() {
		floorOverflowsCameraBounds = false;
		xMax = -float.MaxValue;
		xMin = float.MaxValue;
		yMax = -float.MaxValue;
		yMin = float.MaxValue;

		//Move the active pattern container to Vector3.zero to facilitate picking up the camera bounds
		Vector3 initialPatternContainerPosition = patternContainers[currentPatternContainer].position;
		patternContainers[currentPatternContainer].position = Vector3.zero;

		//Move the camera so that the generated level is centered in its view
		Vector3 prevCameraPosition = Camera.main.transform.position;
		Camera.main.transform.position = initialCameraPosition + new Vector3 (0, 0, newPatternStartZ * 0.5f * PatternSettings.tiledSize);

		//Go through all the level tiles in the level and get the value for Xmin, Ymin, Xmax, Ymax
		levelTiles = patternContainers[currentPatternContainer].GetComponentsInChildren<LevelTiles> ();
		for (int i = 0; i < levelTiles.Length; i++) {
			Vector3 levelTileViewportPosition = Camera.main.WorldToViewportPoint (levelTiles [i].transform.position);
			if (levelTileViewportPosition.x > xMax) {
				xMax = levelTileViewportPosition.x;
				xMaxPosition = levelTiles [i].transform.position;
			}
			if (levelTileViewportPosition.x < xMin) {
				xMin = levelTileViewportPosition.x;
				xMinPosition = levelTiles [i].transform.position;
			}
			if (levelTileViewportPosition.y > yMax) {
				yMax = levelTileViewportPosition.y;
				yMaxPosition = levelTiles [i].transform.position;
			}
			if (levelTileViewportPosition.y < yMin) {
				yMin = levelTileViewportPosition.y;
				yMinPosition = levelTiles [i].transform.position;
			}
		}

		bool xOverflow = false;
		if (xMin < camPadXMin || xMax > 1-camPadXMax) {
			xOverflow = true;
		}

		bool yOverflow = false;
		if(yMin < camPadYMin || yMax > 1-camPadYMax) {
			yOverflow = true;
		}

		Vector3 originalCameraPosition = Camera.main.transform.position;
		
		if (xOverflow) {
			//Setup the camera to cover the xMin and yMin position
			Camera.main.transform.position += new Vector3(0,0,1);
			float newXMin = Camera.main.WorldToViewportPoint (xMinPosition).x;
			float xAmountToCorrect = (camPadXMin-xMin);
			float zAmountToMove = (xAmountToCorrect / (newXMin - xMin));
			Camera.main.transform.position = originalCameraPosition;
			Camera.main.transform.position += new Vector3 (0, 0, zAmountToMove);

			originalCameraPosition = Camera.main.transform.position;
			
			yMin = Camera.main.WorldToViewportPoint(yMinPosition).y;
			Camera.main.transform.position += new Vector3(0,1,0);
			float newYMin = Camera.main.WorldToViewportPoint(yMinPosition).y;
			float yAmountToCorrect = (camPadYMin-yMin);
			float yAmountToMove = (yAmountToCorrect/(newYMin-yMin));
			Camera.main.transform.position = originalCameraPosition;
			Camera.main.transform.position += new Vector3(0,yAmountToMove,0);

			minCameraPosition = Camera.main.transform.position;

			//Setup the camera to cover the xMax and yMax position
			originalCameraPosition = Camera.main.transform.position;

			xMax = Camera.main.WorldToViewportPoint(xMaxPosition).x;
			Camera.main.transform.position += new Vector3(0,0,1);
			float newXMax = Camera.main.WorldToViewportPoint (xMaxPosition).x;
			xAmountToCorrect = ((1-camPadXMax)-xMax);
			zAmountToMove = (xAmountToCorrect / (newXMax - xMax));
			Camera.main.transform.position = originalCameraPosition;
			Camera.main.transform.position += new Vector3 (0, 0, zAmountToMove);

			originalCameraPosition = Camera.main.transform.position;
			
			yMax = Camera.main.WorldToViewportPoint(yMaxPosition).y;
			Camera.main.transform.position += new Vector3(0,1,0);
			float newYMax = Camera.main.WorldToViewportPoint(yMaxPosition).y;
			yAmountToCorrect = ((1-camPadYMax)-yMax);
			yAmountToMove = (yAmountToCorrect/(newYMax-yMax));
			Camera.main.transform.position = originalCameraPosition;
			Camera.main.transform.position += new Vector3(0,yAmountToMove,0);

			maxCameraPosition = Camera.main.transform.position;

			floorOverflowsCameraBounds = true;
		} else if (yOverflow) {
			originalCameraPosition = Camera.main.transform.position;
			
			yMin = Camera.main.WorldToViewportPoint(yMinPosition).y;
			Camera.main.transform.position += new Vector3(0,1,0);
			float newYMin = Camera.main.WorldToViewportPoint(yMinPosition).y;
			float yAmountToCorrect = (camPadYMin-yMin);
			float yAmountToMove = (yAmountToCorrect/(newYMin-yMin));
			Camera.main.transform.position = originalCameraPosition;
			Camera.main.transform.position += new Vector3(0,yAmountToMove,0);

			minCameraPosition = Camera.main.transform.position;

			originalCameraPosition = Camera.main.transform.position;
			
			yMax = Camera.main.WorldToViewportPoint(yMaxPosition).y;
			Camera.main.transform.position += new Vector3(0,1,0);
			float newYMax = Camera.main.WorldToViewportPoint(yMaxPosition).y;
			yAmountToCorrect = ((1-camPadYMax)-yMax);
			yAmountToMove = (yAmountToCorrect/(newYMax-yMax));
			Camera.main.transform.position = originalCameraPosition;
			Camera.main.transform.position += new Vector3(0,yAmountToMove,0);

			maxCameraPosition = Camera.main.transform.position;

			floorOverflowsCameraBounds = true;
		}

		if (interFloorTransition) {
			Camera.main.transform.position = prevCameraPosition;
			patternContainers [currentPatternContainer].position = initialPatternContainerPosition;
		}
	}

	public void StartGame() {
		TweenY.Add (gameTitle, 0.5f, 500f).EaseInElasticWith (20, 0.625f);
		activeCharacter.GetComponent<TVNTCharacterController> ().activate = true;
		tapToStartButton.SetActive (false);
		settingsButton.SetActive (false);
		textBestFloorMainMenu.gameObject.SetActive (false);
		textFloor.SetActive (true);
		hearts.SetActive (true);
	}

	public void NextLevel() {
		currentPatternContainer = (currentPatternContainer + 1) % 2;
		CreateFloor ();
		interFloorTransition = true;

		SetupCamera ();
		//Camera.main.transform.position = new Vector3(0,18,0);

		floor++;
		StopAudioWhileLevelTransition ();

		TweenZ.Add(patternContainers[(currentPatternContainer+1)%2].gameObject, interFloorTransitionDuration, -patternTransitionAmount * PatternSettings.tiledSize).EaseInOutBackWith(1f);
		TweenZ.Add(patternContainers[currentPatternContainer].gameObject, interFloorTransitionDuration, 0).EaseInOutBackWith(1f).Then(ClearPrevPattern);
		Invoke ("MoveCharacter", interFloorTransitionDuration * 0.5f);
	}

	private void StopAudioWhileLevelTransition() {
		Transform prevPatternContainer = patternContainers [(currentPatternContainer + 1) % 2];
		AudioSource[] allAudioSources = prevPatternContainer.GetComponentsInChildren<AudioSource> ();
		for (int i = 0; i < allAudioSources.Length; i++) {
			allAudioSources [i].Stop ();
		}
	}

	private void MoveCharacter() {
		GameObject[] startPositions = GameObject.FindGameObjectsWithTag ("Start");
		Vector3 startPosition = Vector3.zero;
		for (int i = 0; i < startPositions.Length; i++) {
			if (startPositions [i].transform.root == patternContainers [currentPatternContainer]) {
				startPosition = startPositions [i].transform.position;
				break;
			}
		}
		activeCharacter.position = new Vector3 (startPosition.x, startPosition.y + PatternSettings.playerYOffset, startPosition.z);
		activeCharacter.GetComponent<TVNTPlayerController> ().setFirstParent = true;
		activeCharacter.GetComponent<TVNTPlayerController> ().CheckGround ();
		Camera.main.transform.position = minCameraPosition;
		floorText.text = "FLOOR " + floor;
	}

	private void ClearPrevPattern() {
		for (int i = spawnedPatterns.Count - 1; i > -1; i--) {
			Transform selectedPattern = spawnedPatterns [i];
			if (selectedPattern.parent == patternContainers [(currentPatternContainer + 1) % 2]) {
				//Destroy enemies
				TVNTAIController[] activeEnemies = spawnedPatterns[i].GetComponentsInChildren<TVNTAIController>();
				for (int j = 0; j < activeEnemies.Length; j++) {
					Destroy (activeEnemies [j].gameObject);
				}
				//Destroy bullets
				Bullet[] bullets = GameObject.FindObjectsOfType<Bullet> ();
				for (int j = 0; j < bullets.Length; j++) {
					TVNTObjectPool.instance.ReleaseObject (bullets [j].transform);
				}
				//Make sure the ground colliders are unoccupied
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
				spawnedPatterns.RemoveAt (i);
				spawnedPatternEntrances.RemoveAt (i);
				Destroy (selectedPattern.gameObject);
			}
		}
		currBoardProgressRate = 0;
		prevBoardProgressRate = 0;
		initialCharacterZPosition = activeCharacter.position.z;
		interFloorTransition = false;
		activeCharacter.GetComponent<TVNTCharacterController> ().activate = true;

		//System.GC.Collect ();
	}

	private void CreateFloor() {
		patternContainers [currentPatternContainer].localPosition = new Vector3 (0, 0, patternTransitionAmount * PatternSettings.tiledSize);
		newPatternStartZ = 0;
		int enemyCount = (int)Mathf.Log (floor, 2f);
		//int enemyCount = 0; //<-- for testing purposes
		int currentBoardLength = 0;
		bool spawnStartPattern = true;
		bool spawnEndPattern = true;
		bool moveCharacter = true;

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
					bool spawnEnemy = Random.Range (0f, 1f * enemyCount) > 0.5f ? true : false;
					if (spawnEnemy) {
						if (floor <= easyDifficultyIndex) {
							//SPAWN EASY DIFFICULTY PATTERNS
							if (floor % floorIntervalTillBoss != 0 || bossSpawned) {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyEasyPattern (spawnedPatternEntrances [spawnedPatternEntrances.Count - 1]);
							} else {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyEasyBossPattern (spawnedPatternEntrances [spawnedPatternEntrances.Count - 1]);
								bossSpawned = true;
							}
						} else if (floor <= easyDifficultyIndex + mediumDifficultyIndex) {
							//SPAWN MEDIUM DIFFICULTY PATTERNS
							if (floor % floorIntervalTillBoss != 0 || bossSpawned) {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyMediumPattern (spawnedPatternEntrances [spawnedPatternEntrances.Count - 1]);
							} else {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyMediumBossPattern (spawnedPatternEntrances [spawnedPatternEntrances.Count - 1]);
								bossSpawned = true;
							}
						} else {
							//SPAWN HARD DIFFICULTY PATTERNS
							if (floor % floorIntervalTillBoss != 0 || bossSpawned) {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyHardPattern (spawnedPatternEntrances [spawnedPatternEntrances.Count - 1]);
							} else {
								selectedPatternPrefab = PatternLoader.instance.GetEnemyHardBossPattern (spawnedPatternEntrances [spawnedPatternEntrances.Count - 1]);
								bossSpawned = true;
							}
						}
						enemyCount -= selectedPatternPrefab.GetComponent<Pattern> ().enemyCount;
					}
				}
				if (selectedPatternPrefab == null) {
					if (floor < easyDifficultyIndex) {
						selectedPatternPrefab = PatternLoader.instance.GetEasyPattern (spawnedPatternEntrances[spawnedPatternEntrances.Count-1]);
					} else if (floor < easyDifficultyIndex + mediumDifficultyIndex) {
						selectedPatternPrefab = PatternLoader.instance.GetMediumPattern (spawnedPatternEntrances[spawnedPatternEntrances.Count-1]);
					} else {
						selectedPatternPrefab = PatternLoader.instance.GetHardPattern (spawnedPatternEntrances[spawnedPatternEntrances.Count-1]);
					}
				}
			}

			Transform selectedPattern = (Transform)Instantiate (selectedPatternPrefab);
			selectedPattern.name = selectedPatternPrefab.name; //to prevent the clone thing that just irritates me personally :)
			Pattern patternScript = selectedPattern.GetComponent<Pattern>();

			bool evenGridZ = patternScript.gridZ % 2 == 0 ? true : false;
			float halfGridZ = patternScript.gridZ * 0.5f;

			if (evenGridZ) {
				newPatternStartZ -= 0.5f;
			}
			newPatternStartZ += halfGridZ;

			selectedPattern.parent = patternContainers[currentPatternContainer];
			selectedPattern.localPosition = new Vector3 (0, 0, newPatternStartZ * PatternSettings.tiledSize);
			//Rotate the pattern since in this game the character moves from the bottom to the top
			selectedPattern.localRotation = Quaternion.Euler (0, 180, 0);


			levelTiles = selectedPattern.GetComponentsInChildren<LevelTiles> ();
			for (int i = 0; i < levelTiles.Length; i++) {
				levelTiles [i].Initialize ();
			}

			newPatternStartZ += halfGridZ;
			if (evenGridZ) {
				newPatternStartZ += 0.5f;
			}

			spawnedPatterns.Add (selectedPattern);
			spawnedPatternEntrances.Add (patternScript.topEntrances);

			currentBoardLength += patternScript.gridZ;
		}
	}

	public void PlayerLifeLost(int currentLives) {
		switch (currentLives) {
		case 1:
			individualHearts [1].enabled = false;
			break;
		case 2:
			individualHearts [2].enabled = false;
			break;
		}
	}

	public void PlayerDead() {
		individualHearts [0].enabled = false;
		individualHearts [1].enabled = false;
		individualHearts [2].enabled = false;

		//Set current score
		textCurrentFloor.text = floor.ToString();
		//Get highscore
		textBestFloor.text = GetHighScore(floor).ToString();

		//Show game over menu
		TweenX.Add (gameOverMenu, 0.5f, 0f).EaseOutBackWith (2);
		mainMenuOverlay.color = new Color (0, 0, 0, 0);
		mainMenuOverlay.gameObject.SetActive (true);
		TweenA.Add (mainMenuOverlay.gameObject, 0.5f, 0.5f).EaseInOutExpo ();
	}

	private int GetHighScore(int currentScore) {
		int highScore = PlayerPrefs.GetInt ("Highscore", 0);
		if (highScore < currentScore) {
			highScore = currentScore;
			PlayerPrefs.SetInt ("Highscore", highScore);
			PlayerPrefs.Save ();
		}
		return highScore;
	}

	public void ShowSettings() {
		TweenX.Add (gameSettingsMenu, 0.5f, 0f).EaseOutBackWith (2);
		mainMenuOverlay.color = new Color (0, 0, 0, 0);
		mainMenuOverlay.gameObject.SetActive (true);
		TweenA.Add (mainMenuOverlay.gameObject, 0.5f, 0.5f).EaseInOutExpo ();
		//settingsButton.SetActive (false);
		gameTitle.SetActive (false);
		tapToStartButton.SetActive (false);
	}

	public void HideSettings() {
		TweenX.Add (gameSettingsMenu, 0.5f, -2000f).EaseOutBackWith (2);
		TweenA.Add (mainMenuOverlay.gameObject, 0.5f, 0).EaseInOutExpo ().Then (HideOverlay);
		//settingsButton.SetActive (true);
		gameTitle.SetActive (true);
		tapToStartButton.SetActive (true);
	}

	public void ResetHighscore() {
		PlayerPrefs.SetInt ("Highscore", 0);
		textBestFloorMainMenu.text = "BEST 0";
	}

	public void SmashedPot(Vector3 position) {
		gold += potScore;
		goldText.text = gold.ToString ();
		CreateFloatingText (position, potScore.ToString());
		if (myAudio) {
			myAudio.clip = potSmashAudioClip;
			myAudio.Play ();
		}
	}

	public void PickedUpCoin(Vector3 position) {
		gold += coinScore;
		goldText.text = gold.ToString ();
		CreateFloatingText (position, coinScore.ToString());
		if (myAudio) {
			myAudio.clip = coinPickupAudioClip;
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

	public void SetGameVolume(float currentVolume) {
		AudioListener.volume = currentVolume;
	}

	public void RestartGame() {
		screenFader.EndScene ("Game3");
		StartCoroutine (FadeSoundOut (0.5f));
	}

	private IEnumerator FadeSoundIn(float fadeTime) {
		float t;
		for (t = 0; t < 1; t += Time.deltaTime / fadeTime) {
			AudioListener.volume = Mathf.Lerp (0, initialAudioListenerSound, t);
			yield return null;
		}
		AudioListener.volume = initialAudioListenerSound;
		volumeSlider.value = AudioListener.volume;
	}

	private IEnumerator FadeSoundOut(float fadeTime) {
		float t;
		initialAudioListenerSound = AudioListener.volume;
		for (t = 0; t < 1; t += Time.deltaTime / fadeTime) {
			AudioListener.volume = Mathf.Lerp (initialAudioListenerSound, 0, t);
			yield return null;
		}
		AudioListener.volume = 0;
	}

}
