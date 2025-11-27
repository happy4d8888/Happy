using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AutoSpinPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject autoSpinPanel;
    public Button[] autoSpinButtons;
    public TextMeshProUGUI spinButtonText;
    public Button spinButton;
    
    [Header("Auto Spin Settings")]
    public int[] autoSpinOptions = { 10, 25, 50, 100 };
    public string infiniteSpinText = "∞";
    public float spinDelay = 1.5f;
    public float pauseAfterSpinComplete = 1.2f;
    
    [Header("RTP Integration")]
    public RTPManager rtpManager;
    public TextMeshProUGUI autoSpinStatsText;
    
    [Header("Pop Animation")]
    [Tooltip("The time (in seconds) between each button popping in.")]
    public float popInDelay = 0.08f;
    [Tooltip("How fast the button scales in (Lerp speed). Higher is faster.")]
    public float popInSpeed = 12f;
    [Tooltip("The time (in seconds) between each button popping out.")]
    public float popOutDelay = 0.05f;
    [Tooltip("How fast the button scales out (Lerp speed). Higher is faster.")]
    public float popOutSpeed = 15f;

    [Header("Sound Effects")]
    public AudioClip showPanelSound;
    public AudioClip closePanelSound;
    public AudioClip buttonClickSound;

    private AudioSource audioSource;

    private int currentAutoSpinCount = 0;
    private int selectedAutoSpinOption = 0;
    private bool isInfiniteSpin = false;
    private Coroutine autoSpinCoroutine;
    private SlotMachine4D slotMachine;
    
    // Auto-spin statistics
    private float autoSpinTotalWagered = 0f;
    private float autoSpinTotalWon = 0f;
    private int autoSpinTotalSpins = 0;
    private int autoSpinTotalWins = 0;
    
    // Pause system variables
    private bool isPaused = false;
    private bool wasAutoSpinningBeforePause = false;

    // Coroutine trackers for animations
    private Coroutine popInCoroutine;
    private Coroutine popOutCoroutine;

    void Start()
    {
        slotMachine = FindObjectOfType<SlotMachine4D>();
        rtpManager = FindObjectOfType<RTPManager>();
        autoSpinPanel.SetActive(false);
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        SetupButtonListeners();
        UpdateSpinButtonText();
    }

    void SetupButtonListeners()
    {
        for (int i = 0; i < autoSpinButtons.Length; i++)
        {
            int index = i;
            autoSpinButtons[i].onClick.AddListener(() => OnAutoSpinOptionSelected(index));
        }
        
        if (spinButton != null) spinButton.onClick.AddListener(OnSpinButtonClicked);
    }

    public void ShowPanel()
    {
        if (IsGameplayBlockedByWinAnimation()) return;
        if (autoSpinPanel.activeSelf || popInCoroutine != null) return; 

        if (showPanelSound != null) audioSource.PlayOneShot(showPanelSound);

        if (popOutCoroutine != null)
        {
            StopCoroutine(popOutCoroutine);
            popOutCoroutine = null;
        }

        foreach (Button btn in autoSpinButtons)
        {
            btn.transform.localScale = Vector3.zero;
            btn.gameObject.SetActive(true);
        }

        autoSpinPanel.SetActive(true);
        UpdateAutoSpinStats();

        popInCoroutine = StartCoroutine(AnimatePopIn());
    }

    public void HidePanel()
    {
        if (!autoSpinPanel.activeSelf || popOutCoroutine != null) return;

        if (closePanelSound != null) audioSource.PlayOneShot(closePanelSound);

        if (popInCoroutine != null)
        {
            StopCoroutine(popInCoroutine);
            popInCoroutine = null;
        }

        popOutCoroutine = StartCoroutine(AnimatePopOut());
    }

    void OnAutoSpinOptionSelected(int optionIndex)
    {
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);

        if (IsGameplayBlockedByWinAnimation()) return;
        
        if (optionIndex < autoSpinOptions.Length)
        {
            selectedAutoSpinOption = autoSpinOptions[optionIndex];
            currentAutoSpinCount = 0;
            isInfiniteSpin = false;
        }
        else
        {
            isInfiniteSpin = true;
            selectedAutoSpinOption = 0;
            currentAutoSpinCount = 0;
        }
        
        ResetAutoSpinStats();
        StartAutoSpin();
        HidePanel(); 
    }

    void StartAutoSpin()
    {
        if (autoSpinCoroutine != null) StopCoroutine(autoSpinCoroutine);
        autoSpinCoroutine = StartCoroutine(AutoSpinRoutine());
        UpdateSpinButtonText();
    }

    IEnumerator AutoSpinRoutine()
    {
        if (isInfiniteSpin)
        {
            while (true)
            {
                while (IsGameplayBlockedByWinAnimation() || isPaused)
                {
                    yield return null;
                }
                
                // Perform the spin (Checks credit inside here)
                if (!ExecuteSpinCycle()) yield break;
                
                while (slotMachine.IsSpinning() || IsGameplayBlockedByWinAnimation())
                {
                    yield return null;
                }
                
                yield return new WaitForSeconds(pauseAfterSpinComplete);
            }
        }
        else
        {
            for (int i = 0; i < selectedAutoSpinOption; i++)
            {
                while (IsGameplayBlockedByWinAnimation() || isPaused)
                {
                    yield return null;
                }
                
                // Perform the spin (Checks credit inside here)
                if (!ExecuteSpinCycle()) yield break;
                
                while (slotMachine.IsSpinning() || IsGameplayBlockedByWinAnimation())
                {
                    yield return null;
                }
                
                yield return new WaitForSeconds(pauseAfterSpinComplete);
            }
            StopAutoSpin();
        }
    }

    // --- MODIFIED: Strict Check for CanSpin ---
    bool ExecuteSpinCycle()
    {
        // Check if the SlotMachine allows spinning (Checks Credit, Animation State, etc.)
        if (slotMachine != null && slotMachine.CanSpin())
        {
            slotMachine.InitiateAutoSpin();
            currentAutoSpinCount++;
            UpdateSpinButtonText();
            UpdateAutoSpinStats();
            return true;
        }
        else
        {
            // If we can't spin (Insufficient Credit), FORCE STOP immediately.
            Debug.Log("⛔ AutoSpin Forced Stop: Insufficient Credits or Cannot Spin.");
            StopAutoSpin();
            return false;
        }
    }
    // --- END MODIFIED ---

    void UpdateAutoSpinStats()
    {
        if (autoSpinStatsText != null && rtpManager != null)
        {
            float autoSpinRTP = autoSpinTotalWagered > 0 ? autoSpinTotalWon / autoSpinTotalWagered : 0f;
            autoSpinStatsText.text = $"Auto Spin Stats:\n" +
                                     $"Spins: {autoSpinTotalSpins}\n" +
                                     $"RTP: {autoSpinRTP:P1}\n" +
                                     $"Won: {autoSpinTotalWon:F0}";
        }
    }

    void ResetAutoSpinStats()
    {
        autoSpinTotalWagered = 0f;
        autoSpinTotalWon = 0f;
        autoSpinTotalSpins = 0;
        autoSpinTotalWins = 0;
        UpdateAutoSpinStats();
    }

    public void StopAutoSpin()
    {
        if (autoSpinCoroutine != null)
        {
            StopCoroutine(autoSpinCoroutine);
            autoSpinCoroutine = null;
        }
        
        currentAutoSpinCount = 0;
        isInfiniteSpin = false;
        isPaused = false;
        UpdateSpinButtonText(); // This resets text to "SPIN" immediately
    }

    public void PauseAutoSpin()
    {
        if (IsAutoSpinning() && !isPaused)
        {
            isPaused = true;
            wasAutoSpinningBeforePause = true;
            Debug.Log("⏸️ Auto-spin paused for win animation");
        }
    }

    public void ResumeAutoSpin()
    {
        if (isPaused && wasAutoSpinningBeforePause)
        {
            isPaused = false;
            wasAutoSpinningBeforePause = false;
            Debug.Log("▶️ Auto-spin resumed after win animation");
        }
    }

    void UpdateSpinButtonText()
    {
        if (spinButtonText != null)
        {
            if (isInfiniteSpin) spinButtonText.text = "∞";
            else if (currentAutoSpinCount > 0) spinButtonText.text = $"{selectedAutoSpinOption - currentAutoSpinCount}";
            else spinButtonText.text = "SPIN";
        }
    }

    void OnSpinButtonClicked()
    {
        if (IsGameplayBlockedByWinAnimation()) return;
        
        if (IsAutoSpinning()) StopAutoSpin();
    }

    bool IsGameplayBlockedByWinAnimation()
    {
        WinAnimationManager winAnimManager = FindObjectOfType<WinAnimationManager>();
        return winAnimManager != null && winAnimManager.IsWinAnimationPlaying();
    }

    public bool IsAutoSpinning() => autoSpinCoroutine != null && !isPaused;
    public bool IsPaused() => isPaused;
    public bool IsInfiniteSpin() => isInfiniteSpin;
    public int GetRemainingSpins() => selectedAutoSpinOption - currentAutoSpinCount;

    private IEnumerator AnimatePopIn()
    {
        for (int i = 0; i < autoSpinButtons.Length; i++)
        {
            StartCoroutine(ScaleButton(autoSpinButtons[i], Vector3.one, popInSpeed));
            yield return new WaitForSeconds(popInDelay);
        }
        popInCoroutine = null;
    }

    private IEnumerator AnimatePopOut()
    {
        for (int i = autoSpinButtons.Length - 1; i >= 0; i--)
        {
            StartCoroutine(ScaleButton(autoSpinButtons[i], Vector3.zero, popOutSpeed));
            yield return new WaitForSeconds(popOutDelay);
        }

        yield return new WaitForSeconds(0.2f); 
        
        autoSpinPanel.SetActive(false); 
        popOutCoroutine = null;
    }

    private IEnumerator ScaleButton(Button btn, Vector3 targetScale, float speed)
    {
        Transform btnTransform = btn.transform;
        
        if (targetScale == Vector3.one)
            btn.gameObject.SetActive(true);

        while (Mathf.Abs(btnTransform.localScale.x - targetScale.x) > 0.01f)
        {
            btnTransform.localScale = Vector3.Lerp(btnTransform.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }

        btnTransform.localScale = targetScale; 
        
        if (targetScale == Vector3.zero)
            btn.gameObject.SetActive(false);
    }
}