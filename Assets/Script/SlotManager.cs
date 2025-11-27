using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems; 
using System.Linq;

[RequireComponent(typeof(AudioSource))] 
public class SlotMachine4D : MonoBehaviour
{
    [Header("Digit Displays")]
    public TextMeshProUGUI digit1Text;
    public TextMeshProUGUI digit2Text;
    public TextMeshProUGUI digit3Text;
    public TextMeshProUGUI digit4Text;
    public Button spinButton;
    public TextMeshProUGUI spinButtonText;

    [Header("UI Interaction")]
    [Tooltip("Drag buttons here that should be unclickable while spinning (e.g., TopUp, Settings, Credit Buttons).")]
    public Button[] buttonsToDisableDuringSpin;

    [Header("Win References")]
    public GameObject winPopup;
    public Image winImage;
    public float popupDuration = 3f;
    
    [Header("Managers")]
    public Credit creditSystem;
    public NumberPadManager numberPadManager;
    public JackpotManager jackpotManager;
    public PrizeTableManager prizeTableManager;
    public AutoSpinPanel autoSpinPanel;
    public WinAnimationManager winAnimationManager;
    public RTPManager rtpManager;
    
    [Header("Kill Numbers System")]
    [Tooltip("Digits that will be excluded from random generation (0-9)")]
    public int[] killNumbers = new int[0];
    [Tooltip("Enable/disable kill numbers system")]
    public bool enableKillNumbers = false;
    [Tooltip("Should kill numbers affect jackpot generation?")]
    public bool applyKillNumbersToJackpot = false;
    
    [Header("Automated Kill Numbers Settings")]
    [Tooltip("Enable automated kill number management")]
    public bool enableAutomatedKillNumbers = true;
    [Tooltip("Target RTP for automated adjustments")]
    [Range(0.8f, 0.99f)] public float targetRTP = 0.95f;
    [Tooltip("RTP deviation threshold to trigger kill number changes")]
    [Range(0.01f, 0.1f)] public float rtpDeviationThreshold = 0.03f;
    [Tooltip("Number of spins between automated checks")]
    public int checkCycleSpins = 50;
    [Tooltip("Maximum number of digits to kill (0-9)")]
    [Range(0, 9)] public int maxKillDigits = 4;
    [Tooltip("Minimum spins before first automated adjustment")]
    public int minSpinsForAdjustment = 20;

    [Header("Display - Spin Debug")]
    [ReadOnly] public string currentFinalNumber = "0000";
    
    [Header("Display - Current Kill Numbers")]
    [ReadOnly] public string currentKillNumbersDisplay = "None";
    [ReadOnly] public string killNumbersReason = "Initial Setup";
    [ReadOnly] public float currentRTP = 0f;
    [ReadOnly] public int spinsSinceLastAdjustment = 0;
    
    [Header("Prize-Aware Spinning")]
    public bool enablePrizeTracking = true;
    public Color normalDigitColor = Color.white;
    public Color closeDigitColor = Color.yellow;
    public Color matchDigitColor = Color.green;
    public Color jackpotDigitColor = Color.red;
    public AudioClip digitMatchSound;
    public AudioClip spinButtonPressSound; 
    public AudioClip shufflingSoundLoop; 
    public AudioClip[] digitStopSounds = new AudioClip[4]; 
    
    [Header("Spinning Settings")]
    public float ultraFastDuration = 1.0f;
    public float digitStopDelay = 0.5f;
    public float slowDownFactor = 0.7f;
    
    [Header("Dimming Timing Settings")]
    public float restoreDelayAfterSpin = 1.0f;
    public float quickRestoreDelay = 0.5f;

    [Header("Button Press Effect")]
    [Tooltip("How small the button gets when pressed (e.g., 0.9 = 90% of original size).")]
    public float pressedScale = 0.9f;

    [Header("Trainer Settings")]
    [Tooltip("If true, allows spinning without checking credits.")]
    public bool isCreditCheckIgnored = false;
    
    private bool isSpinning = false;
    private string finalNumber = "";
    private Coroutine spinCoroutine;
    private bool digit1Stopped = false, digit2Stopped = false, digit3Stopped = false, digit4Stopped = false;
    private TextMeshProUGUI[] digitTexts;
    private List<string> allPrizeNumbers = new List<string>();
    
    private AudioSource audioSource; 
    private AudioSource loopAudioSource; 
    
    private float currentPlacedBet = 0f;
    private float holdStartTime = 0f;
    private const float HOLD_DURATION = 0.8f;
    private Coroutine holdCheckCoroutine;
    private List<int> allowedDigits = new List<int>();
    
    private int totalSpins = 0;
    private int fourDigitWins = 0;
    private int lastAdjustmentSpin = 0;
    private List<int> recentWinningDigits = new List<int>();
    private const int RECENT_DIGITS_MEMORY = 20;

    private bool[] digitRestored = new bool[4];
    private string currentPartialNumber = "";

    private bool isHoldAction = false;
    private Vector3 spinButtonOriginalScale; 

    void Start()
    {
        digitTexts = new TextMeshProUGUI[] { digit1Text, digit2Text, digit3Text, digit4Text };
        
        AudioSource[] sources = GetComponents<AudioSource>();
        audioSource = sources[0]; 
        if (sources.Length < 2)
        {
            loopAudioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            loopAudioSource = sources[1]; 
        }
        loopAudioSource.playOnAwake = false;
        loopAudioSource.loop = true; 
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        
        spinButton.onClick.AddListener(OnSpinButtonClicked);
        if (winPopup != null) winPopup.SetActive(false);
        
        if (spinButton != null)
        {
            spinButtonOriginalScale = spinButton.transform.localScale;
        }

        SetupSpinButtonEvents();
        UpdateSpinButtonState();
        
        if (enableAutomatedKillNumbers)
        {
            InitializeAutomatedKillNumbers();
        }
        else
        {
            UpdateAllowedDigits();
        }
        
        Debug.Log("🎰 Slot Machine Initialized");
        LogKillNumbersStatus();
    }

    // ... (InitializeAutomatedKillNumbers, SetRandomKillNumbers, etc. UNCHANGED) ...
    // Keeping code concise, assume standard methods exist here
    
    void InitializeAutomatedKillNumbers()
    {
        SetRandomKillNumbers(2, "Initial random setup");
        enableKillNumbers = true;
        Debug.Log("🤖 Automated Kill Numbers System Activated");
    }

    void SetRandomKillNumbers(int count, string reason)
    {
        List<int> newKillNumbers = new List<int>();
        List<int> availableDigits = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        foreach (int recentDigit in recentWinningDigits) availableDigits.Remove(recentDigit);
        count = Mathf.Min(count, Mathf.Min(maxKillDigits, availableDigits.Count - 1));
        for (int i = 0; i < count; i++)
        {
            if (availableDigits.Count == 0) break;
            int randomIndex = Random.Range(0, availableDigits.Count);
            newKillNumbers.Add(availableDigits[randomIndex]);
            availableDigits.RemoveAt(randomIndex);
        }
        killNumbers = newKillNumbers.ToArray();
        killNumbersReason = reason;
        UpdateKillNumbersDisplay();
        UpdateAllowedDigits();
    }

    void UpdateKillNumbersDisplay()
    {
        if (killNumbers.Length == 0) currentKillNumbersDisplay = "None";
        else currentKillNumbersDisplay = string.Join(", ", killNumbers);
    }

    void UpdateAllowedDigits()
    {
        allowedDigits.Clear();
        for (int i = 0; i < 10; i++) allowedDigits.Add(i);
        if (enableKillNumbers)
        {
            foreach (int killDigit in killNumbers) allowedDigits.Remove(killDigit);
        }
    }

    void LogKillNumbersStatus()
    {
        if (enableKillNumbers) Debug.Log($"🎯 KILL NUMBERS ACTIVE: {string.Join(", ", killNumbers)}");
        else Debug.Log("🎯 Kill Numbers system disabled");
    }

    public void OnWinDetected(string winningNumber, float winAmount, bool isFourDigitWin)
    {
        if (isFourDigitWin)
        {
            fourDigitWins++;
            foreach (char digitChar in winningNumber)
            {
                int digit = int.Parse(digitChar.ToString());
                recentWinningDigits.Add(digit);
                if (recentWinningDigits.Count > RECENT_DIGITS_MEMORY) recentWinningDigits.RemoveAt(0);
            }
        }
        if (enableAutomatedKillNumbers && totalSpins >= minSpinsForAdjustment) CheckForKillNumbersAdjustment(winAmount, isFourDigitWin);
    }

    void CheckForKillNumbersAdjustment(float winAmount, bool isFourDigitWin)
    {
        currentRTP = rtpManager != null ? rtpManager.GetCurrentRTP() : 0f;
        bool shouldAdjust = false;
        string adjustmentReason = "";
        if (Mathf.Abs(currentRTP - targetRTP) > rtpDeviationThreshold)
        {
            shouldAdjust = true;
            if (currentRTP > targetRTP) adjustmentReason = "RTP too high";
            else adjustmentReason = "RTP too low";
        }
        else if (spinsSinceLastAdjustment >= checkCycleSpins)
        {
            shouldAdjust = true;
            adjustmentReason = "Cycle check";
        }
        else if (isFourDigitWin && fourDigitWins % 2 == 0)
        {
            shouldAdjust = true;
            adjustmentReason = "4-digit win trigger";
        }
        else if (winAmount > 5000)
        {
            shouldAdjust = true;
            adjustmentReason = "Big win detected";
        }
        if (shouldAdjust)
        {
            AdjustKillNumbers(adjustmentReason);
            spinsSinceLastAdjustment = 0;
        }
    }

    void AdjustKillNumbers(string reason)
    {
        int newKillCount = killNumbers.Length;
        if (currentRTP > targetRTP + rtpDeviationThreshold) newKillCount = Mathf.Max(killNumbers.Length - 1, 0);
        else if (currentRTP < targetRTP - rtpDeviationThreshold) newKillCount = Mathf.Min(killNumbers.Length + 1, maxKillDigits);
        else newKillCount = Random.Range(1, maxKillDigits + 1);
        SetRandomKillNumbers(newKillCount, reason);
    }

    void SetupSpinButtonEvents()
    {
        if (spinButton.gameObject.GetComponent<EventTrigger>() == null)
        {
            var eventTrigger = spinButton.gameObject.AddComponent<EventTrigger>();
            var pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) => { OnSpinButtonPointerDown((PointerEventData)data); });
            eventTrigger.triggers.Add(pointerDownEntry);
            var pointerUpEntry = new EventTrigger.Entry();
            pointerUpEntry.eventID = EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener((data) => { OnSpinButtonPointerUp((PointerEventData)data); });
            eventTrigger.triggers.Add(pointerUpEntry);
            var pointerExitEntry = new EventTrigger.Entry();
            pointerExitEntry.eventID = EventTriggerType.PointerExit;
            pointerExitEntry.callback.AddListener((data) => { OnSpinButtonPointerUp((PointerEventData)data); });
            eventTrigger.triggers.Add(pointerExitEntry);
        }
    }
    
    // --- MODIFIED: Only restrict animation if blocked by Win Animation (not low credits) ---
    public void OnSpinButtonPointerDown(PointerEventData eventData)
    {
        if (IsGameplayBlockedByWinAnimation()) return;

        if (spinButton != null) 
        {
            spinButton.transform.localScale = spinButtonOriginalScale * pressedScale;
            if (spinButtonPressSound != null) audioSource.PlayOneShot(spinButtonPressSound);
        }
        
        isHoldAction = false;
        if (isSpinning) return; 

        holdStartTime = Time.time;

        if (autoSpinPanel != null && autoSpinPanel.autoSpinPanel.activeSelf) holdCheckCoroutine = StartCoroutine(CheckForHoldToClose());
        else if (autoSpinPanel == null || !autoSpinPanel.IsAutoSpinning()) holdCheckCoroutine = StartCoroutine(CheckForHoldToOpen());
    }
    // --- END MODIFIED ---
    
    public void OnSpinButtonPointerUp(PointerEventData eventData)
    {
        if (spinButton != null) spinButton.transform.localScale = spinButtonOriginalScale;
        holdStartTime = 0f;
        if (holdCheckCoroutine != null)
        {
            StopCoroutine(holdCheckCoroutine);
            holdCheckCoroutine = null;
        }
    }
    
    IEnumerator CheckForHoldToOpen()
    {
        while (holdStartTime > 0 && Time.time - holdStartTime < HOLD_DURATION) yield return null;
        if (holdStartTime > 0 && Time.time - holdStartTime >= HOLD_DURATION)
        {
            isHoldAction = true; 
            if (autoSpinPanel != null) autoSpinPanel.ShowPanel();
        }
    }

    IEnumerator CheckForHoldToClose()
    {
        while (holdStartTime > 0 && Time.time - holdStartTime < HOLD_DURATION) yield return null;
        if (holdStartTime > 0 && Time.time - holdStartTime >= HOLD_DURATION)
        {
            isHoldAction = true; 
            if (autoSpinPanel != null) autoSpinPanel.HidePanel();
        }
    }
    
    void OnSpinButtonClicked()
    {
        if (IsGameplayBlockedByWinAnimation()) return;
        if (isHoldAction)
        {
            isHoldAction = false; 
            return; 
        }
        if (isSpinning) StopSpinImmediately();
        else if (autoSpinPanel != null && autoSpinPanel.IsAutoSpinning()) autoSpinPanel.StopAutoSpin();
        else if (CanSpin()) Generate4DNumber();
        UpdateSpinButtonText();
    }
    
    void SetExternalButtonsState(bool state)
    {
        if (buttonsToDisableDuringSpin != null)
        {
            foreach (Button btn in buttonsToDisableDuringSpin)
            {
                if (btn != null) btn.interactable = state;
            }
        }
    }

    void Generate4DNumber()
    {
        for (int i = 0; i < digitRestored.Length; i++) digitRestored[i] = false;
        currentPartialNumber = "";

        if (prizeTableManager != null) prizeTableManager.CheckAndRefreshPrizes();

        SetAllPrizeNumbersDimmed(true);
        SetExternalButtonsState(false);

        currentPlacedBet = creditSystem.PlaceBetAndDeduct();
        
        if(currentPlacedBet <= 0) 
        {
            if (isCreditCheckIgnored && creditSystem != null)
            {
                currentPlacedBet = creditSystem.GetCurrentBet();
                Debug.Log("⚠️ FUN CREDIT IGNORED: Spinning with 0 credits!");
            }
            else
            {
                StartCoroutine(RestorePrizeNumbersAfterDelay(quickRestoreDelay, ""));
                SetExternalButtonsState(true); 
                return;
            }
        }

        if (shufflingSoundLoop != null && !loopAudioSource.isPlaying)
        {
            loopAudioSource.clip = shufflingSoundLoop;
            loopAudioSource.Play();
        }

        GenerateFinalNumber(currentPlacedBet);
        UpdatePrizeNumbersList();
        
        digit1Stopped = digit2Stopped = digit3Stopped = digit4Stopped = false;
        ResetDigitColors();
        
        spinCoroutine = StartCoroutine(PrizeAwareSpinAnimation(currentPlacedBet));
        UpdateSpinButtonText();
        
        totalSpins++;
        spinsSinceLastAdjustment++;
        
        Debug.Log($"🎰 SPIN INITIATED - Bet: {currentPlacedBet}, Final Number: {finalNumber}");
    }

    private void SetAllPrizeNumbersDimmed(bool isDimmed)
    {
        if (numberPadManager != null) numberPadManager.DimAllSavedNumbers();
        if (prizeTableManager != null) prizeTableManager.DimAllPrizeNumbers();
        if (jackpotManager != null) jackpotManager.DimPrizeDisplays();
    }

    private IEnumerator RestorePrizeNumbersAfterDelay(float delay, string spunNumber)
    {
        yield return new WaitForSeconds(delay);
        RestorePrizeNumbersBasedOnMatch(spunNumber);
    }

    private void RestorePrizeNumbersBasedOnMatch(string spunNumber)
    {
        if (numberPadManager != null) numberPadManager.RestoreSavedNumbersWithMatch(spunNumber);
        if (prizeTableManager != null) prizeTableManager.RestorePrizeNumbersWithMatch(spunNumber);
        if (jackpotManager != null) jackpotManager.RestorePrizeDisplaysWithMatch(spunNumber);
    }

    void UpdatePrizeNumbersList()
    {
        allPrizeNumbers.Clear();
        if (!string.IsNullOrEmpty(jackpotManager.jackpotNumber)) allPrizeNumbers.Add(jackpotManager.jackpotNumber);
        for (int i = 0; i < 3; i++)
        {
            string savedNum = numberPadManager.GetSavedNumberByIndex(i);
            if (!string.IsNullOrEmpty(savedNum)) allPrizeNumbers.Add(savedNum);
        }
        string[] specialPrizes = prizeTableManager.GetAllSpecialPrizes();
        string[] consolationPrizes = prizeTableManager.GetAllConsolationPrizes();
        if (specialPrizes != null) allPrizeNumbers.AddRange(specialPrizes);
        if (consolationPrizes != null) allPrizeNumbers.AddRange(consolationPrizes);
    }
    
    void GenerateFinalNumber(float placedBet)
    {
        string jackpot = PlayerPrefs.GetString("JackpotNumber", "");
        float chance = jackpotManager.jackpotChance;
        int jackpotUsed = PlayerPrefs.GetInt("JackpotUsed", 0);

        if (!string.IsNullOrEmpty(jackpot) && jackpotUsed == 0 && Random.Range(0f, 100f) <= chance)
        {
            if (applyKillNumbersToJackpot && enableKillNumbers && ContainsKillNumbers(jackpot))
            {
                finalNumber = GenerateNumberWithoutKillNumbers();
            }
            else
            {
                finalNumber = jackpot;
                PlayerPrefs.SetInt("JackpotUsed", 1);
                PlayerPrefs.Save();
            }
        }
        else
        {
            if (enableKillNumbers) finalNumber = GenerateNumberWithoutKillNumbers();
            else
            {
                int randomNumber;
                do { randomNumber = Random.Range(0, 10000); finalNumber = randomNumber.ToString("D4"); } while (finalNumber == jackpot);
            }
        }
        
        if (rtpManager != null) rtpManager.RecordSpin(placedBet);
        currentFinalNumber = finalNumber; 
    }

    public string GenerateNumberWithoutKillNumbers()
    {
        UpdateAllowedDigits();
        if (allowedDigits.Count == 0) for (int i = 0; i < 10; i++) allowedDigits.Add(i);

        string generatedNumber = "";
        for (int i = 0; i < 4; i++)
        {
            int randomIndex = Random.Range(0, allowedDigits.Count);
            generatedNumber += allowedDigits[randomIndex].ToString();
        }
        return generatedNumber;
    }

    bool ContainsKillNumbers(string number)
    {
        foreach (char digitChar in number)
        {
            int digit = int.Parse(digitChar.ToString());
            if (killNumbers.Contains(digit)) return true;
        }
        return false;
    }
    
    IEnumerator PrizeAwareSpinAnimation(float placedBet)
    {
        isSpinning = true;
        UpdateSpinButtonState();
        
        float ultraFastElapsed = 0f;
        while (ultraFastElapsed < ultraFastDuration)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!IsDigitStopped(i))
                {
                    int randomDigit;
                    if (enableKillNumbers && allowedDigits.Count > 0)
                    {
                        int randomIndex = Random.Range(0, allowedDigits.Count);
                        randomDigit = allowedDigits[randomIndex];
                    }
                    else randomDigit = Random.Range(0, 10);
                    
                    digitTexts[i].text = randomDigit.ToString();
                    if (enablePrizeTracking) CheckDigitProximity(i, randomDigit);
                }
            }
            ultraFastElapsed += Time.deltaTime;
            yield return null;
        }
        
        yield return StartCoroutine(StopDigitWithPrizeTracking(0, finalNumber[0].ToString()));
        yield return StartCoroutine(StopDigitWithPrizeTracking(1, finalNumber[1].ToString()));
        yield return StartCoroutine(StopDigitWithPrizeTracking(2, finalNumber[2].ToString()));
        yield return StartCoroutine(StopDigitWithPrizeTracking(3, finalNumber[3].ToString()));

        for (int i = 0; i < 4; i++)
        {
            digitTexts[i].text = finalNumber[i].ToString();
        }

        isSpinning = false;
        Debug.Log($"🎰 Spin completed. Checking wins for: {finalNumber}");
        CheckForWinsAfterSpin(placedBet);
        UpdateSpinButtonState();
    }
    
    IEnumerator StopDigitWithPrizeTracking(int digitIndex, string targetValue)
    {
        TextMeshProUGUI digitText = digitTexts[digitIndex];
        if (digitText == null) yield break;
        
        float stopDuration = digitStopDelay * Mathf.Pow(slowDownFactor, digitIndex);
        float elapsedTime = 0f;
        
        while (elapsedTime < stopDuration)
        {
            int randomDigit;
            if (enableKillNumbers && allowedDigits.Count > 0)
            {
                int randomIndex = Random.Range(0, allowedDigits.Count);
                randomDigit = allowedDigits[randomIndex];
            }
            else randomDigit = Random.Range(0, 10);
            
            digitText.text = randomDigit.ToString();
            if (enablePrizeTracking) CheckDigitProximity(digitIndex, randomDigit);
            UpdateNonStoppedDigits();
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        int targetDigit = int.Parse(targetValue);
        digitText.text = targetValue;
        
        SetDigitStopped(digitIndex, true);
        PlayDigitStopSound(digitIndex);

        if (enablePrizeTracking) CheckDigitProximity(digitIndex, targetDigit);
        UpdatePartialNumber(digitIndex, targetValue);
        RestorePrizeNumbersBasedOnPartialMatch();
        
        Vector3 originalScale = digitText.transform.localScale;
        digitText.transform.localScale = originalScale * 1.2f;
        
        float bounceTime = 0f;
        while (bounceTime < 0.1f)
        {
            UpdateNonStoppedDigits(); 
            bounceTime += Time.deltaTime;
            yield return null;
        }
        
        digitText.transform.localScale = originalScale;
        digitRestored[digitIndex] = true;
    }

    void SetDigitStopped(int index, bool state)
    {
        switch (index)
        {
            case 0: digit1Stopped = state; break;
            case 1: digit2Stopped = state; break;
            case 2: digit3Stopped = state; break;
            case 3: digit4Stopped = state; break;
        }
    }
    
    private void UpdatePartialNumber(int digitIndex, string digitValue)
    {
        if (digitIndex == 0) currentPartialNumber = digitValue; 
        else if (digitIndex == currentPartialNumber.Length) currentPartialNumber += digitValue;
        else
        {
            char[] partialChars = "____".ToCharArray();
            for(int i = 0; i < 4; i++)
            {
                if(i < finalNumber.Length && digitRestored[i]) partialChars[i] = finalNumber[i];
            }
            if(digitIndex < 4) partialChars[digitIndex] = digitValue[0];
            currentPartialNumber = new string(partialChars).TrimEnd('_');
        }
    }

    private void RestorePrizeNumbersBasedOnPartialMatch()
    {
        if (string.IsNullOrEmpty(currentPartialNumber)) return;
        if (numberPadManager != null) numberPadManager.RestoreSavedNumbersWithMatch(currentPartialNumber);
        if (prizeTableManager != null) prizeTableManager.RestorePrizeNumbersWithMatch(currentPartialNumber);
        if (jackpotManager != null) jackpotManager.RestorePrizeDisplaysWithMatch(currentPartialNumber);
    }

    void CheckDigitProximity(int digitPosition, int currentDigit)
    {
        bool isClose = false, isExact = false, isJackpot = false;
        foreach (string prizeNumber in allPrizeNumbers)
        {
            if (prizeNumber.Length == 4)
            {
                int prizeDigit = int.Parse(prizeNumber[digitPosition].ToString());
                if (currentDigit == prizeDigit)
                {
                    isExact = true;
                    if (prizeNumber == jackpotManager.jackpotNumber) isJackpot = true;
                    break;
                }
                int digitDiff = Mathf.Abs(currentDigit - prizeDigit);
                if (digitDiff <= 2 || digitDiff >= 8) isClose = true;
            }
        }
        
        TextMeshProUGUI digitText = digitTexts[digitPosition];
        if (digitText != null)
        {
            if (isJackpot) { digitText.color = jackpotDigitColor; digitText.transform.localScale = Vector3.one * 1.3f; }
            else if (isExact) { digitText.color = matchDigitColor; digitText.transform.localScale = Vector3.one * 1.2f; if (digitMatchSound != null) audioSource.PlayOneShot(digitMatchSound); }
            else if (isClose) { digitText.color = closeDigitColor; digitText.transform.localScale = Vector3.one * 1.1f; }
            else { digitText.color = normalDigitColor; digitText.transform.localScale = Vector3.one; }
        }
    }
    
    void UpdateNonStoppedDigits()
    {
        for (int i = 0; i < 4; i++)
        {
            if (!IsDigitStopped(i))
            {
                int randomDigit;
                if (enableKillNumbers && allowedDigits.Count > 0)
                {
                    int randomIndex = Random.Range(0, allowedDigits.Count);
                    randomDigit = allowedDigits[randomIndex];
                }
                else randomDigit = Random.Range(0, 10);
                
                digitTexts[i].text = randomDigit.ToString();
                if (enablePrizeTracking) CheckDigitProximity(i, randomDigit);
            }
        }
    }
    
    void CheckForWinsAfterSpin(float placedBet)
    {
        if (loopAudioSource.isPlaying) loopAudioSource.Stop();
        if (jackpotManager != null) jackpotManager.CheckForWins(finalNumber, placedBet);
        if (prizeTableManager != null) prizeTableManager.OnSpinCompleted();
        StartCoroutine(RestoreDigitColorsAfterDelay(restoreDelayAfterSpin));
        SetExternalButtonsState(true);
    }
    
    bool IsDigitStopped(int index)
    {
        switch (index)
        {
            case 0: return digit1Stopped;
            case 1: return digit2Stopped;
            case 2: return digit3Stopped;
            case 3: return digit4Stopped;
            default: return true;
        }
    }
    
    void ResetDigitColors()
    {
        foreach (TextMeshProUGUI digitText in digitTexts)
        {
            if (digitText != null) { digitText.color = normalDigitColor; digitText.transform.localScale = Vector3.one; }
        }
        for (int i = 0; i < digitRestored.Length; i++) digitRestored[i] = false;
        currentPartialNumber = "";
    }
    
    IEnumerator RestoreDigitColorsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetDigitColors();
    }
    
    void StopSpinImmediately()
    {
        if (isSpinning && spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            if (loopAudioSource.isPlaying) loopAudioSource.Stop();
            
            for (int i = 0; i < 4; i++)
            {
                if (!IsDigitStopped(i))
                {
                    digitTexts[i].text = finalNumber[i].ToString();
                    if (enablePrizeTracking) CheckDigitProximity(i, int.Parse(finalNumber[i].ToString()));
                    PlayDigitStopSound(i);
                    if(!digitRestored[i]) { UpdatePartialNumber(i, finalNumber[i].ToString()); digitRestored[i] = true; }
                }
            }
            
            RestorePrizeNumbersBasedOnPartialMatch();
            digit1Stopped = digit2Stopped = digit3Stopped = digit4Stopped = true;
            isSpinning = false;
            CheckForWinsAfterSpin(currentPlacedBet);
            UpdateSpinButtonState();
        }
    }

    void PlayDigitStopSound(int digitIndex)
    {
        if (digitStopSounds != null && digitIndex >= 0 && digitIndex < digitStopSounds.Length)
        {
            AudioClip clip = digitStopSounds[digitIndex];
            if (clip != null) audioSource.PlayOneShot(clip); 
        }
    }
    
    bool IsGameplayBlockedByWinAnimation() { return winAnimationManager != null && winAnimationManager.IsWinAnimationPlaying(); }

    public void SetKillNumbers(int[] newKillNumbers) { killNumbers = newKillNumbers; UpdateAllowedDigits(); LogKillNumbersStatus(); }
    public void EnableKillNumbers(bool enable) { enableKillNumbers = enable; UpdateAllowedDigits(); LogKillNumbersStatus(); }
    public void SetApplyToJackpot(bool apply) { applyKillNumbersToJackpot = apply; }
    public void EnableAutomatedKillNumbers(bool enable) { enableAutomatedKillNumbers = enable; if (enable) InitializeAutomatedKillNumbers(); }
    public void SetTargetRTP(float newTargetRTP) { targetRTP = Mathf.Clamp(newTargetRTP, 0.8f, 0.99f); }
    public void InitiateAutoSpin() { if (!isSpinning && CanSpin()) Generate4DNumber(); }
    public bool CanSpin() 
    { 
        if (IsGameplayBlockedByWinAnimation()) return false;
        if (isCreditCheckIgnored) return true;
        return creditSystem != null && creditSystem.CurrentBet >= 0.5f && creditSystem.TotalCredit >= creditSystem.CurrentBet; 
    }
    public bool IsSpinning() { return isSpinning; }
    
    // --- MODIFIED: Force true always to prevent fade out ---
    public void UpdateSpinButtonState() { if (spinButton != null) spinButton.interactable = true; }
    // --- END MODIFIED ---
    
    void UpdateSpinButtonText()
    {
        if (spinButtonText != null)
        {
            if (isSpinning) spinButtonText.text = "STOP";
            else if (autoSpinPanel != null && autoSpinPanel.IsAutoSpinning())
                spinButtonText.text = autoSpinPanel.IsInfiniteSpin() ? "∞" : $"{autoSpinPanel.GetRemainingSpins()}";
            else spinButtonText.text = "SPIN";
        }
    }
}