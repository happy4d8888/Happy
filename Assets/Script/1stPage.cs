using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// This script handles loading a single target scene,
/// displaying a loading bar, and then animating a "Click to Continue" prompt.
/// </summary>
public class LoadingSceneController : MonoBehaviour
{
    [Header("Scene To Load")]
    [Tooltip("The exact name of your one game scene (e.g., 'MyGameScene' or 'Level1')")]
    public string sceneToLoad;

    [Header("UI Groups")]
    [Tooltip("The parent object of all loading UI (slider, text, etc.)")]
    public GameObject loadingUIGroup;
    [Tooltip("The parent object for the 'Click to Continue' UI. MUST have an Animator.")]
    public GameObject clickToContinueUIGroup; 

    [Header("Loading Elements")]
    [Tooltip("The Slider component to use as a loading bar.")]
    public Slider loadingSlider;
    [Tooltip("The TextMeshPro text element to display the loading percentage.")]
    public TextMeshProUGUI progressText;

    [Header("Animation Settings")]
    [Tooltip("The Animator component on your ClickToContinueUIGroup.")]
    public Animator clickToContinueAnimator;
    [Tooltip("The name of the trigger parameter in your Animator to play the 'appear' animation.")]
    public string appearTriggerName = "Appear"; // Default trigger name for the animation

    // Private variables
    private AsyncOperation loadingOperation;
    private bool loadingComplete = false;
    private bool clickedToStart = false; 

    void Start()
    {
        // Initial state: Show loading UI, hide click UI
        loadingUIGroup.SetActive(true);
        clickToContinueUIGroup.SetActive(false);

        if (loadingSlider != null)
        {
            loadingSlider.value = 0;
        }

        // Safety check
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("ERROR: 'Scene To Load' is not set in the Inspector for LoadingSceneController!");
            return;
        }

        // Start the actual loading
        StartCoroutine(LoadSceneAsync(sceneToLoad));
    }

    void Update()
    {
        // Check for click *only* if loading is done and we haven't already clicked
        if (loadingComplete && !clickedToStart)
        {
            // Check for mouse click OR screen touch
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                clickedToStart = true;
                
                // --- THIS IS THE "CLOSE DIRECTLY" LOGIC ---
                // It instantly activates the new scene.
                if (loadingOperation != null)
                {
                    loadingOperation.allowSceneActivation = true;
                }
            }
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        loadingOperation = SceneManager.LoadSceneAsync(sceneName);
        loadingOperation.allowSceneActivation = false;

        while (!loadingOperation.isDone)
        {
            float progressValue = Mathf.Clamp01(loadingOperation.progress / 0.9f);

            if (loadingSlider != null)
            {
                loadingSlider.value = progressValue;
            }

            if (progressText != null)
            {
                progressText.text = "Loading... " + (progressValue * 100f).ToString("F0") + "%";
            }

            // Check if the 90% "fake" complete is done
            if (loadingOperation.progress >= 0.9f)
            {
                // Loading is finished
                loadingComplete = true;

                // Hide the loading bar/text
                loadingUIGroup.SetActive(false);
                
                // Show the click-to-continue group
                clickToContinueUIGroup.SetActive(true);
                
                // Trigger the fade-in animation
                if (clickToContinueAnimator != null)
                {
                    clickToContinueAnimator.SetTrigger(appearTriggerName);
                }
                
                break; // Exit the loop
            }

            yield return null;
        }
    }
}