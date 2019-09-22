using System.Collections;
using UnityEngine;
using MadLevelManager;

//Added for Restful API call
using Proyecto26;
using System.Collections.Generic;
using UnityEngine.Networking;


//List of all the posible gamestates
public enum GameState
{
    NotStarted,
    Playing,
    Completed,
    Failed
}

//Make sure there is always an AudioSource component on the GameObject where this script is added.
[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    //Text element to display certain messages on
    public GUIText FeedbackText;

    //Text to be displayed when entering one of the gamestates
    public string GameNotStartedText;
    public string GameCompletedText;
    public string GameFailedText;

    //Sounds to be played when entering one of the gamestates
    public AudioClip StartSound;
    public AudioClip FailedSound;
	public AudioClip KittySound;
    private GameState currentState = GameState.NotStarted;
    //All the blocks found in this level, to keep track of how many are left
    private Block[] allBlocks;
    private Ball[] allBalls;
	public CuCat HoldCat;
	public Cube HoldCube;
	public GUITexture GuiLevel1;
	public float Timer=0.0f;
	private int minutes;
	private int seconds;
	public string niceTime;
	public GUIText timeHolder;
    
    //Added for RestAPI Call to HighScore Chain
    private readonly string basePath = "https://ephemeral.api.factom.com/v1/chains/5e3ee9da3394d85ee1ff57c23a0bb1aab45a0fee60f208f3e756a8523ea7e0bc";
	private RequestHelper currentRequest;

    // Use this for initialization
    void Start()
    {
	

		Time.timeScale=1;

        //Find all the blocks in this scene
        allBlocks = FindObjectsOfType(typeof(Block)) as Block[];

        //Find all the balls in this scene
        allBalls = FindObjectsOfType(typeof(Ball)) as Ball[];

        //Prepare the start of the level
        SwitchTo(GameState.NotStarted);
		GameObject.Find("Help").GetComponent<GUITexture>().enabled = false;

	 }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case GameState.NotStarted:
                    //Check if the player taps/clicks.
                    if (Input.GetMouseButtonDown(0))    //Note: on mobile this will translate to the first touch/finger so perfectly multiplatform!
                    {
                        for (int i = 0; i < allBalls.Length; i++)
                            allBalls[i].Launch();

                        SwitchTo(GameState.Playing);
                    }
                break;
            case GameState.Playing:
                {
					Timer += Time.deltaTime;
					minutes= Mathf.FloorToInt(Timer/60F);
					seconds= Mathf.FloorToInt(Timer-minutes *60);
					niceTime=string.Format("{0:0}:{1:00}", minutes, seconds);
					timeHolder.text = niceTime;

                    bool allBlocksDestroyed = true;

                    //Check if all blocks have been destroyed
                    for (int i = 0; i < allBlocks.Length; i++)
                    {
                        if (!allBlocks[i].BlockIsDestroyed)
                        {
                            allBlocksDestroyed = false;
                            break;
                        }
                    }

                    //Are there no balls left?
                    if (FindObjectOfType(typeof(Ball)) == null)
                        SwitchTo(GameState.Failed);

                    if (allBlocksDestroyed)
                        SwitchTo(GameState.Completed);
                }
                break;
            //Both cases do the same: restart the game
            case GameState.Failed:
            case GameState.Completed:
				 bool allBlocksDestroyedFinal = true;
				
				//Destroy all the balls
				Ball[] others = FindObjectsOfType(typeof(Ball)) as Ball[];
				
				foreach(Ball other in others) {
				//if (FindObjectOfType(typeof(Ball)) != null)
					Destroy(other.gameObject);
				}
				 //Check if all blocks have been destroyed
                    for (int i = 0; i < allBlocks.Length; i++)
                    {
                        if (!allBlocks[i].BlockIsDestroyed)
                        {
                            allBlocksDestroyedFinal = false;
                            break;
                        }
                    }
					
                //Check if the player taps/clicks.
                //if (Input.GetMouseButtonDown(0) && !allBlocksDestroyedFinal)    //Note: on mobile this will translate to the first touch/finger so perfectly multiplatform!
                 //   Restart();
                break;
        }
    }

    //Do the appropriate actions when changing the gamestate
    public void SwitchTo(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            default:
            case GameState.NotStarted:
                DisplayText(GameNotStartedText);
                break;
            case GameState.Playing:
				GetComponent<AudioSource>().PlayOneShot(StartSound);
                DisplayText("");
				GuiLevel1.enabled = false;
			GuiLevel1.GetComponent<ParticleSystem>().enableEmission=false;
				HoldCat.Catbool =false;
                break;
            case GameState.Completed:
                GetComponent<AudioSource>().PlayOneShot(StartSound);
                //StartCoroutine(NewLevelAfter(StartSound.length));
				HoldCat.Catbool=true;
				HoldCube.CubeRender=false;
                BlockChainHighScore(niceTime);
				DisplayText(GameCompletedText);
			//	StartCoroutine(NewLevelAfter(4.0f));
				break;
            case GameState.Failed:
				GetComponent<AudioSource>().PlayOneShot(KittySound);
				GetComponent<AudioSource>().PlayOneShot(FailedSound);
				DisplayText(GameFailedText);
                BlockChainHighScore(niceTime);
				//StartCoroutine(RestartAfter(FailedSound.length));
				break;
        }
    }

    //Make call to Restful API
    private void BlockChainHighScore(string strHighscore){
        Debug.Log(strHighscore);
        
        // We can add default query string params for all requests
		//RestClient.DefaultRequestParams["param1"] = "My first param";
		//RestClient.DefaultRequestParams["param3"] = "My other param";
        
        // We can add default request headers for all requests
		RestClient.DefaultRequestHeaders["app_id"] = "1301a567";
		RestClient.DefaultRequestHeaders["app_key"] = "e6e2b598793069e4d24287b04b0f6606";

		currentRequest = new RequestHelper {
			Uri = basePath + "/entries",
			/*Params = new Dictionary<string, string> {
				{ "param1", "value 1" },
				{ "param2", "value 2" }
			},*/
			Body = new Post {
				content = "MjowMA==",
				external_ids = '["aGlnaCBzY29yZQ==", "Z2FtaW5n"]'
			},
			EnableDebug = true
		};
		RestClient.Post<Post>(currentRequest)
		.Then(res => {

			// And later we can clear the default query string params for all requests
			RestClient.ClearDefaultParams();

			this.LogMessage("Success", JsonUtility.ToJson(res, true));
		})
		.Catch(err => this.LogMessage("Error", err.Message));
    }
    
    //Helper to display some text
    private void DisplayText(string text)
    {
        FeedbackText.text = text;
	}
	
	//Coroutine which waits and then restarts the level
    //Note: You need to call this method with StartRoutine(RestartAfter(seconds)) else it won't restart
   // private IEnumerator RestartAfter(float seconds)
   // {
    //    yield return new WaitForSeconds(seconds);

    //    Restart();
    //}
	/*private IEnumerator NewLevelAfter(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		
		NewLevel();
	}*/

	//Helper to GIVE the NEW level
	public void NewLevel()
	{
	if (MadLevel.HasNext(MadLevel.Type.Level)) 
		{
		MadLevel.LoadNext(MadLevel.Type.Level);
		} 
		else
		{
		MadLevel.LoadFirst(MadLevel.Type.Level);
		}
	}
    //Helper to restart the level
    public void Restart()
    {
		MadLevel.ReloadCurrent();
    }

	/*void OnGUI () {
		// Make a background box
		GUI.Box(new Rect(10,10,100,90), "Choose a Level");
		
		// Make the first button. If it is pressed, Application.Loadlevel (1) will be executed
		if(GUI.Button(new Rect(20,40,80,20), "Level 1")) {
			Application.LoadLevel(1);
		}
		
		// Make the second button.
		if(GUI.Button(new Rect(20,70,80,20), "Level 2")) {
			Application.LoadLevel(2);
		}
		
		// Make the first button. If it is pressed, Application.Loadlevel (1) will be executed
		if(GUI.Button(new Rect(20,100,80,20), "Level 3")) {
			Application.LoadLevel(3);
		}
		
		// Make the second button.
		if(GUI.Button(new Rect(20,130,80,20), "Level 4")) {
			Application.LoadLevel(4);
		}
	}*/
}