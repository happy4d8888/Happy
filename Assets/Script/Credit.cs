using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using System.Collections;

public class Credit : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text totalCreditText;
    public TMP_Text totalBetText;
    public TMP_Text totalWinText;
    public Button addButton;
    public Button subtractButton;
    public Button spinButton;

    [Header("Top Up Settings")]
    [Tooltip("Button that adds 100 credits when clicked")]
    public Button add100Button; // --- NEW: Reference for the Add 100 button ---

    [Header("Sound Effects")]
    public AudioClip addBetSound;
    public AudioClip subtractBetSound;

    [Header("Button Press Effect")]
    [Tooltip("How small the button gets when pressed (e.g., 0.9 = 90% of original size).")]
    public float pressedScale = 0.9f;

    [Header("Balance Settings")]
    public float startingCredit = 0f; 

    private AudioSource audioSource;
    private Vector3 addButtonOriginalScale;
    private Vector3 subtractButtonOriginalScale;

    private float totalCredit = 0f;
    private float currentBet = 0.5f;
    private float totalWin = 0f;
    private float lastWinAmount = 0f;

    private const float MIN_BET = 0.5f;
    private const float MAX_BET = 100f;
    private const float TIER_1_MAX = 5f;
    private const float TIER_2_MAX = 10f;

    public float CurrentBet { get { return currentBet; } }
    public float TotalWin { get { return totalWin; } }
    public float TotalCredit { get { return totalCredit; } }

    private WinAnimationManager winAnimationManager;

    void Start()
    {
        winAnimationManager = FindObjectOfType<WinAnimationManager>();
        
        totalCredit = startingCredit;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        UpdateTotalCreditDisplay();
        UpdateBetDisplay();
        UpdateTotalWinDisplay();
        
        // --- Setup Betting Buttons ---
        if (addButton != null)
        {
            addButtonOriginalScale = addButton.transform.localScale;
            addButton.onClick.AddListener(AddToBet);
            AddButtonPressEvents(addButton, addButtonOriginalScale);
        }
        
        if (subtractButton != null)
        {
            subtractButtonOriginalScale = subtractButton.transform.localScale;
            subtractButton.onClick.AddListener(SubtractFromBet);
            AddButtonPressEvents(subtractButton, subtractButtonOriginalScale);
        }

        // --- NEW: Setup Add 100 Button ---
        if (add100Button != null)
        {
            // 1. Store original scale for the press effect
            Vector3 add100Scale = add100Button.transform.localScale;
            
            // 2. Add the listener to add money
            add100Button.onClick.AddListener(() => {
                // Play sound if available (reusing addBetSound for feedback)
                if (addBetSound != null) audioSource.PlayOneShot(addBetSound);
                AddCredits(100f); 
            });

            // 3. Add the visual press effect
            AddButtonPressEvents(add100Button, add100Scale);
        }
        // ---------------------------------
        
        UpdateButtonStates();
        
        Debug.Log($"💰 Credit System Initialized with {totalCredit}");
    }

    // --- Helper for Trainer to set specific amount ---
    public void SetTotalCredit(float amount)
    {
        totalCredit = amount;
        UpdateTotalCreditDisplay();
        UpdateButtonStates();
        Debug.Log($"💰 Trainer set credit to: {totalCredit}");
    }

    void AddToBet()
    {
        if (IsGameplayBlockedByWinAnimation()) return;
        
        float increment = GetIncrementAmount();
        float newBet = currentBet + increment;
        
        if (newBet <= MAX_BET && totalCredit >= newBet)
        {
            if (addBetSound != null) audioSource.PlayOneShot(addBetSound);

            currentBet = newBet;
            UpdateBetDisplay();
            UpdateButtonStates();
            Debug.Log($"💰 Bet increased to: {currentBet}");
        }
    }

    void SubtractFromBet()
    {
        if (IsGameplayBlockedByWinAnimation()) return;
        
        float decrement = GetDecrementAmount();
        float newBet = currentBet - decrement;
        
        if (newBet >= MIN_BET)
        {
            if (subtractBetSound != null) audioSource.PlayOneShot(subtractBetSound);

            currentBet = newBet;
            UpdateBetDisplay();
            UpdateButtonStates();
            Debug.Log($"💰 Bet decreased to: {currentBet}");
        }
    }

    private void AddButtonPressEvents(Button button, Vector3 originalScale)
    {
        if (button == null) return;

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { OnButtonPress(button.transform, originalScale); });
        trigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { OnButtonRelease(button.transform, originalScale); });
        trigger.triggers.Add(pointerUpEntry);

        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnButtonRelease(button.transform, originalScale); });
        trigger.triggers.Add(pointerExitEntry);
    }

    private void OnButtonPress(Transform buttonTransform, Vector3 originalScale)
    {
        if (buttonTransform != null)
        {
            buttonTransform.localScale = originalScale * pressedScale;
        }
    }

    private void OnButtonRelease(Transform buttonTransform, Vector3 originalScale)
    {
        if (buttonTransform != null)
        {
            buttonTransform.localScale = originalScale;
        }
    }

    float GetIncrementAmount()
    {
        if (currentBet < TIER_1_MAX) return 0.5f;
        else if (currentBet < TIER_2_MAX) return 1f;
        else return 10f;
    }

    float GetDecrementAmount()
    {
        if (currentBet > TIER_2_MAX) return 10f;
        else if (currentBet > TIER_1_MAX) return 1f;
        else if (currentBet > MIN_BET) return 0.5f;
        else return 0f;
    }

    public float PlaceBetAndDeduct()
    {
        ResetTotalWin();
        
        if (currentBet >= MIN_BET && totalCredit >= currentBet)
        {
            totalCredit -= currentBet;
            UpdateTotalCreditDisplay();
            Debug.Log($"💰 BET PLACED: {currentBet} deducted. New balance: {totalCredit}");
            return currentBet;
        }
        Debug.LogWarning($"💰 Cannot place bet: {currentBet}. Min: {MIN_BET}, Available: {totalCredit}");
        return 0f;
    }

    public void AddWinnings(float multiplierAmount)
    {
        Debug.Log($"\n💰 === ADDING WINNINGS ===");
        Debug.Log($"💰 Received multiplierAmount: {multiplierAmount}");
        Debug.Log($"💰 Current bet amount: {currentBet}");
        
        float totalWinnings = multiplierAmount + currentBet;
        
        Debug.Log($"💰 CALCULATION: {multiplierAmount} (win) + {currentBet} (returned bet) = {totalWinnings}");
        Debug.Log($"💰 Old credit balance: {totalCredit}");
        
        totalCredit += totalWinnings;
        
        lastWinAmount = multiplierAmount;
        totalWin = totalWinnings;

        UpdateTotalCreditDisplay();
        UpdateTotalWinDisplay();
        UpdateButtonStates();
        
        Debug.Log($"💰 New credit balance: {totalCredit}");
        Debug.Log($"💰 Total added to credit: {totalWinnings}");
        Debug.Log($"💰 Win amount (excluding returned bet): {multiplierAmount}\n");
    }

    void UpdateTotalCreditDisplay()
    {
        totalCreditText.text = totalCredit.ToString("F0");
    }

    void UpdateBetDisplay()
    {
        totalBetText.text = currentBet.ToString("0.0");
    }

    void UpdateTotalWinDisplay()
    {
        if (totalWinText != null)
            totalWinText.text = totalWin.ToString("F0");
    }

    public void UpdateButtonStates()
    {
        bool isBlocked = IsGameplayBlockedByWinAnimation();
        
        if (subtractButton != null) 
            subtractButton.interactable = !isBlocked && currentBet > MIN_BET;
        
        float nextBet = currentBet + GetIncrementAmount();
        
        if (addButton != null)
            addButton.interactable = !isBlocked && nextBet <= MAX_BET && totalCredit >= nextBet;
        
        if (spinButton != null)
            spinButton.interactable = !isBlocked && currentBet >= MIN_BET && totalCredit >= currentBet;
    }
    
    bool IsGameplayBlockedByWinAnimation()
    {
        return winAnimationManager != null && winAnimationManager.IsWinAnimationPlaying();
    }
    
    public void ResetTotalWin()
    {
        totalWin = 0f;
        UpdateTotalWinDisplay();
    }
    
    public float GetCurrentBet()
    {
        return currentBet;
    }
    
    public float GetLastWinAmount()
    {
        return lastWinAmount;
    }
    
    public float GetTotalWinAmount()
    {
        return totalWin;
    }
    
    public void AddCredits(float amount)
    {
        if (amount > 0)
        {
            totalCredit += amount;
            UpdateTotalCreditDisplay();
            UpdateButtonStates(); 
            Debug.Log($"💰 TOP UP: {amount} credits added. New balance: {totalCredit}");
        }
    }
}