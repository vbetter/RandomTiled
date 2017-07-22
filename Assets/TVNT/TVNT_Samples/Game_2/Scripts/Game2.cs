using UnityEngine;
using UnityEngine.UI;
using TVNT;
using System.Collections;

public class Game2 : MonoBehaviour {

	public static Game2 instance = null;

	public Transform floatingTextPrefab;

	//Sounds
	//The sound clip to play when the player picks up a coin
	public AudioClip pickup1;
	//The sound clip to play when the player smashes a pot
	public AudioClip pickup2;
	//The sound clip to play when the player clicks on the click to start button
	public AudioClip buttonClip;
	public AudioSource myAudio = null;

	private BoardManager boardScript;
	private Transform uiCanvas;
	private Transform menuPanel;
	private Transform levelPanel;
	private Text levelText;
	private Text goldText;
	private Text scoreText;
	private Transform gameOverPanel;

	[HideInInspector]
	public int level = 1;
	private bool doingSetup = true;
	private int gold = 0;
	private int potScore = 20;
	private int coinScore = 5;
	private bool gameStarted = false;

	//Camera target script
	Vector3 initialCameraPosition;

	void Awake() {
		if (instance == null) {
			instance = this;
		} else if (instance != this) {
			Destroy (gameObject);
		}
		DontDestroyOnLoad (gameObject);
		initialCameraPosition = transform.position;
	}

	void Start() {
		System.GC.Collect ();
		menuPanel = GameObject.Find ("MenuPanel").transform;
		menuPanel.localPosition = Vector3.zero;
		levelText = GameObject.Find ("Text_LevelNumber").GetComponent<Text> ();
		levelText.text = "Level " + level;
	}

	public void StartGame() {
		gameStarted = true;
		menuPanel.localPosition = new Vector3 (0, -800, 0);
		Invoke("InitGame",1);
		if (myAudio) {
			myAudio.clip = buttonClip;
			myAudio.Play ();
		}
	}

	public void RestartGame() {
		gameOverPanel.localPosition = new Vector3 (0, -800, 0);
		level = 0;
		gold = 0;
		boardScript.ClearBoard ();

		//Set the tiledSize back to its original size
		//PatternSettings.tiledSize = 0;

		boardScript = null;
		Invoke ("ReloadLevel", 0.125f);
		if (myAudio) {
			myAudio.clip = buttonClip;
			myAudio.Play ();
		}
	}

	private void InitGame() {
		doingSetup = false;
		boardScript = GameObject.Find("BoardManager").GetComponent<BoardManager> ();
		uiCanvas = GameObject.Find ("UI").transform;
		levelPanel = GameObject.Find ("LevelPanel").transform;
		gameOverPanel = GameObject.Find ("GameOver").transform;
		scoreText = GameObject.Find ("Text_Score").GetComponent<Text> ();
		scoreText.text = level.ToString ();
		goldText = GameObject.Find ("Text_Gold").GetComponent<Text> ();
		goldText.text = gold.ToString ();
		PatternLoader.instance.LoadPatterns ();

		//Update the tiledSize based on the level
		//PatternSettings.tiledSize = 0;

		boardScript.SetupScene (Game2.instance.level);
		boardScript.activeCharacter.GetComponent<TVNTPlayerController> ().activate = true;
		levelPanel.gameObject.SetActive (false);
	}

	void OnLevelWasLoaded(int index) {
		if (instance == this) {
			if (gameStarted) {
				Game2.instance.level++;
				transform.position = initialCameraPosition;
				levelText = GameObject.Find ("Text_LevelNumber").GetComponent<Text> ();
				levelText.text = "Level " + Game2.instance.level;
				Invoke ("InitGame", 1f);
			}
		}
	}

	public void ReachedGoal() {
		levelText.text = "";
		levelPanel.gameObject.SetActive (true);
		doingSetup = true;
		boardScript.ClearBoard ();
		boardScript = null;
		Invoke ("ReloadLevel", 0.5f);
	}

	private void ReloadLevel() {
		Application.LoadLevel ("Game2");
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

	void Update() {
		if (doingSetup==false) {
			if (!boardScript.activeCharacter) {
				GameOver ();
			}
		}
	}

	private void GameOver() {
		doingSetup = true;
		gameOverPanel.localPosition = Vector3.zero;
	}

	public void Pause() {
		if (Mathf.Approximately (Time.timeScale, 0)) {
			Time.timeScale = 1;
		} else {
			Time.timeScale = 0;
		}
	}
}
