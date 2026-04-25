using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements; 

public class StartMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    [SerializeField] private GameObject loadingScreen;

    void Start()
    {
        //Allows to get the uidoc
        uiDocument = GetComponent<UIDocument>();
        
        //Grabs to root visual element to manipulate
        var root = uiDocument.rootVisualElement;

        //Grabs the buttons 
        var playButton   = root.Q<Button>("Play_Btn");
        var settingsButton = root.Q<Button>("Settings_Btn");
        var creditsButton  = root.Q<Button>("Credits_Btn");
        var quitButton     = root.Q<Button>("Quit_Btn");

        //Click events
        if (playButton != null)
            playButton.clicked += OnPlayClicked;

        if (settingsButton != null)
            settingsButton.clicked += OnSettingsClicked;

        if (creditsButton != null)
            creditsButton.clicked += OnCreditsClicked;

        if (quitButton != null)
            quitButton.clicked += OnQuitClicked;
    }

    private void OnPlayClicked()
    {
        StartCoroutine(LoadGameWithScreen());
        
    }

    private System.Collections.IEnumerator LoadGameWithScreen()
    {
        // Show loading screen immediately
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        Time.timeScale = 1f;

        // Start async load (single mode replaces menu)
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
        SaveManager.Instance.LoadGame();
        SaveManager.Instance.LoadTechData();
        asyncLoad.allowSceneActivation = false; 

        // Fake progress while waiting (or use asyncLoad.progress)
       while (!asyncLoad.isDone)
        {

            if (asyncLoad.progress >= 0.9f)
            {
                // Loading complete → small delay or wait for input if you want "Press any key"
                yield return new WaitForSeconds(0.5f);  // or while(!Input.anyKeyDown) yield return null;

                asyncLoad.allowSceneActivation = true;  // now activate the new scene
            }

            yield return null;
        }

        //hide loading after activation
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    //Func to check settings button works
    private void OnSettingsClicked()
    {
        Debug.Log("Settings clicked");
       
    }

    //Func to check credits button works
    private void OnCreditsClicked()
    {
        Debug.Log("Credits clicked");
        
    }

    //Function to quit the game
    private void OnQuitClicked()
    {
        Debug.Log("Quit clicked");
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}