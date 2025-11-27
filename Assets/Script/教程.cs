using System.Collections;
using UnityEngine;
using UnityEngine.UI; 

public class SlidingPanelManager : MonoBehaviour
{
    [Header("Assigned Objects")]
    [Tooltip("The panel that will slide up.")]
    public GameObject tutorialPanel; 

    [Tooltip("The button that opens the panel.")]
    public Button openButton; 

    [Tooltip("The button that closes the panel.")]
    public Button closeButton; 

    [Header("Animation Settings")]
    [Tooltip("The distance (in pixels) the panel will slide up.")]
    public float slideUpDistance = 800f;

    [Tooltip("The time (in seconds) it takes to complete the slide.")]
    public float slideDuration = 0.5f;

    // --- NEW: Audio Section ---
    [Header("Audio Settings")]
    [Tooltip("The AudioSource component to play sounds from.")]
    public AudioSource uiAudioSource;

    [Tooltip("Sound to play when opening.")]
    public AudioClip openSound;

    [Tooltip("Sound to play when closing.")]
    public AudioClip closeSound;

    // --- Private Variables ---
    private RectTransform _panelRectTransform;
    private Vector2 _hiddenPosition;
    private Vector2 _shownPosition;
    private bool _isMoving = false;

    void Start()
    {
        _panelRectTransform = tutorialPanel.GetComponent<RectTransform>();
        _hiddenPosition = _panelRectTransform.anchoredPosition;

        _shownPosition = new Vector2(
            _hiddenPosition.x,
            _hiddenPosition.y + slideUpDistance
        );

        if (openButton != null) openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        _panelRectTransform.anchoredPosition = _hiddenPosition;
        
        // Safety Check: Try to find AudioSource on this object if the user forgot to assign it
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
        }
    }

    public void OpenPanel()
    {
        if (!_isMoving && _panelRectTransform.anchoredPosition != _shownPosition)
        {
            // Play Open Sound
            PlaySound(openSound);
            StartCoroutine(MovePanel(_shownPosition));
        }
    }

    public void ClosePanel()
    {
        if (!_isMoving && _panelRectTransform.anchoredPosition != _hiddenPosition)
        {
            // Play Close Sound
            PlaySound(closeSound);
            StartCoroutine(MovePanel(_hiddenPosition));
        }
    }

    // Helper function to play sounds safely
    private void PlaySound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator MovePanel(Vector2 targetPosition)
    {
        _isMoving = true;
        float elapsedTime = 0f;
        Vector2 startPosition = _panelRectTransform.anchoredPosition;

        while (elapsedTime < slideDuration)
        {
            float t = elapsedTime / slideDuration;
            // Optional: smooth easing
            // t = t * t * (3f - 2f * t); 
            
            _panelRectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _panelRectTransform.anchoredPosition = targetPosition;
        _isMoving = false;
    }
}