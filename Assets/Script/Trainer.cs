using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Collections;

public class Trainer : MonoBehaviour
{
    [Header("Next Shuffle Control")]
    public string nextShuffleNumber = "0000";
    public bool controlNextShuffle = false;
    public ShuffleMode shuffleMode = ShuffleMode.CustomNumber;
    
    [Header("Fixed Prize Testing")]
    public bool testFixedPrizes = true;
    public float testBetAmount = 1f;
    
    [Header("Kill Numbers Testing")]
    public int[] testKillNumbers = new int[0];
    public bool testKillNumbersEnabled = false;
    public bool testApplyToJackpot = false;

    [Header("Credit Control")]
    // --- MODIFIED: Default amount is now 1 ---
    [Tooltip("Amount to set when button is pressed")]
    public float targetCreditAmount = 1f; 
    public Button setCreditButton; 
    
    // --- NEW: Ignore Fun Credit Check ---
    [Tooltip("If checked, allows spinning even with 0 credits.")]
    public bool ignoreFunCredit = false;
    // -----------------------------------
    
    [Header("UI References")]
    public TMP_InputField shuffleInputField;
    public TMP_InputField betInputField;
    public Toggle controlShuffleToggle;
    public Dropdown shuffleModeDropdown;
    public Button testFixedPrizesButton;
    public Button testShuffleButton;
    public Button testKillNumbersButton;
    public GameObject trainerPanel;
    
    [Header("Win Animation Integration")]
    public bool respectWinAnimations = true;
    
    private SlotMachine4D slotMachine;
    private JackpotManager jackpotManager;
    private WinAnimationManager winAnimationManager;
    private Credit creditSystem;
    private FieldInfo finalNumberField;
    
    public enum ShuffleMode { CustomNumber, Random, TwoDigitWin, ThreeDigitWin, FourDigitWin }
    
    void Start()
    {
        slotMachine = FindObjectOfType<SlotMachine4D>();
        jackpotManager = FindObjectOfType<JackpotManager>();
        winAnimationManager = FindObjectOfType<WinAnimationManager>();
        creditSystem = FindObjectOfType<Credit>();
        
        if (slotMachine != null)
        {
            System.Type type = slotMachine.GetType();
            finalNumberField = type.GetField("finalNumber", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        
        InitializeUI();
        
        if (trainerPanel != null) trainerPanel.SetActive(false);
    }
    
    void InitializeUI()
    {
        if (shuffleInputField != null)
        {
            shuffleInputField.text = nextShuffleNumber;
            shuffleInputField.onValueChanged.AddListener(OnShuffleInputChanged);
        }
        
        if (betInputField != null)
        {
            betInputField.text = testBetAmount.ToString("0.0");
            betInputField.onValueChanged.AddListener(OnBetInputChanged);
        }
        
        if (controlShuffleToggle != null)
        {
            controlShuffleToggle.isOn = controlNextShuffle;
            controlShuffleToggle.onValueChanged.AddListener(OnControlShuffleToggleChanged);
        }
        
        if (shuffleModeDropdown != null)
        {
            shuffleModeDropdown.ClearOptions();
            shuffleModeDropdown.AddOptions(new System.Collections.Generic.List<string> 
            { 
                "Custom Number", 
                "Random", 
                "2-Digit Win", 
                "3-Digit Win", 
                "4-Digit Win" 
            });
            shuffleModeDropdown.value = (int)shuffleMode;
            shuffleModeDropdown.onValueChanged.AddListener(OnShuffleModeChanged);
        }
        
        if (testFixedPrizesButton != null)
        {
            testFixedPrizesButton.onClick.AddListener(OnTestFixedPrizesButtonClicked);
        }
        
        if (testShuffleButton != null)
        {
            testShuffleButton.onClick.AddListener(OnTestShuffleButtonClicked);
        }
        
        if (testKillNumbersButton != null)
        {
            testKillNumbersButton.onClick.AddListener(OnTestKillNumbersButtonClicked);
        }

        if (setCreditButton != null)
        {
            setCreditButton.onClick.AddListener(OnSetCreditClicked);
        }
    }
    
    void OnSetCreditClicked()
    {
        if (creditSystem != null)
        {
            creditSystem.SetTotalCredit(targetCreditAmount);
        }
    }

    void Update()
    {
        // --- NEW: Sync Ignore Credit setting to SlotMachine ---
        if (slotMachine != null)
        {
            if (slotMachine.isCreditCheckIgnored != ignoreFunCredit)
            {
                slotMachine.isCreditCheckIgnored = ignoreFunCredit;
                slotMachine.UpdateSpinButtonState(); // Refresh button state immediately
            }
        }
        // -----------------------------------------------------

        if (respectWinAnimations && IsWinAnimationPlaying()) return;
        
        if (controlNextShuffle && slotMachine != null && finalNumberField != null)
        {
            string nextNumber = GenerateNextShuffleNumber();
            finalNumberField.SetValue(slotMachine, nextNumber.PadLeft(4, '0'));
            
            if (shuffleMode == ShuffleMode.CustomNumber && shuffleInputField != null)
            {
                if (shuffleInputField.text != nextNumber)
                    shuffleInputField.text = nextNumber;
            }
        }
    }
    
    string GenerateNextShuffleNumber()
    {
        switch (shuffleMode)
        {
            case ShuffleMode.CustomNumber: return nextShuffleNumber;
            case ShuffleMode.Random: return Random.Range(0, 10000).ToString("D4");
            case ShuffleMode.TwoDigitWin: return GenerateWinningNumber(2);
            case ShuffleMode.ThreeDigitWin: return GenerateWinningNumber(3);
            case ShuffleMode.FourDigitWin: return GenerateWinningNumber(4);
            default: return nextShuffleNumber;
        }
    }
    
    string GenerateWinningNumber(int matches)
    {
        NumberPadManager numberPad = FindObjectOfType<NumberPadManager>();
        if (numberPad != null)
        {
            for (int i = 0; i < 3; i++)
            {
                string savedNum = numberPad.GetSavedNumberByIndex(i);
                if (!string.IsNullOrEmpty(savedNum) && savedNum.Length == 4)
                {
                    string baseNumber = savedNum.Substring(0, Mathf.Min(matches, savedNum.Length));
                    string randomSuffix = "";
                    for (int j = matches; j < 4; j++)
                    {
                        randomSuffix += Random.Range(0, 10).ToString();
                    }
                    return baseNumber + randomSuffix;
                }
            }
        }
        
        string randomBase = "";
        for (int i = 0; i < matches; i++)
        {
            randomBase += Random.Range(0, 10).ToString();
        }
        string fallbackSuffix = "";
        for (int i = matches; i < 4; i++)
        {
            fallbackSuffix += Random.Range(0, 10).ToString();
        }
        return randomBase + fallbackSuffix;
    }
    
    void OnShuffleInputChanged(string newValue)
    {
        string cleanedValue = CleanNumberInput(newValue, 4);
        nextShuffleNumber = cleanedValue;
        if (shuffleInputField != null && shuffleInputField.text != cleanedValue)
            shuffleInputField.text = cleanedValue;
    }
    
    void OnBetInputChanged(string newValue)
    {
        if (float.TryParse(newValue, out float result))
        {
            testBetAmount = Mathf.Clamp(result, 0.5f, 100f);
            if (betInputField != null && betInputField.text != testBetAmount.ToString("0.0"))
                betInputField.text = testBetAmount.ToString("0.0");
        }
    }
    
    void OnControlShuffleToggleChanged(bool isOn)
    {
        controlNextShuffle = isOn;
        Debug.Log($"Next shuffle control {(isOn ? "enabled" : "disabled")}");
    }
    
    void OnShuffleModeChanged(int value)
    {
        shuffleMode = (ShuffleMode)value;
        Debug.Log($"Shuffle mode set to: {shuffleMode}");
    }
    
    void OnTestFixedPrizesButtonClicked()
    {
        TestFixedPrizeCalculations();
    }
    
    void OnTestShuffleButtonClicked()
    {
        if (!controlNextShuffle)
        {
            controlNextShuffle = true;
            if (controlShuffleToggle != null) controlShuffleToggle.isOn = true;
        }
        
        TriggerTestSpin();
    }
    
    void OnTestKillNumbersButtonClicked()
    {
        TestKillNumbersSystem();
    }
    
    string CleanNumberInput(string input, int maxLength)
    {
        if (input.Length > maxLength) input = input.Substring(0, maxLength);
        
        string cleanedValue = "";
        foreach (char c in input)
        {
            if (char.IsDigit(c)) cleanedValue += c;
        }
        return cleanedValue.PadLeft(maxLength, '0');
    }
    
    bool IsWinAnimationPlaying()
    {
        return winAnimationManager != null && winAnimationManager.IsWinAnimationPlaying();
    }
    
    public void TriggerTestSpin()
    {
        if (IsWinAnimationPlaying())
        {
            Debug.Log("⏸️ Cannot spin during win animation");
            return;
        }
        
        if (slotMachine == null || !slotMachine.CanSpin())
        {
            Debug.LogWarning("Cannot spin: slot machine not available or insufficient credits");
            return;
        }
        
        if (creditSystem != null && Mathf.Abs(creditSystem.CurrentBet - testBetAmount) > 0.1f)
        {
            SetBetAmount(testBetAmount);
        }
        
        string nextNumber = GenerateNextShuffleNumber();
        
        if (finalNumberField != null)
        {
            finalNumberField.SetValue(slotMachine, nextNumber.PadLeft(4, '0'));
        }
        
        slotMachine.InitiateAutoSpin();
        
        Debug.Log($"🎯 Trainer spin: {nextNumber} (Bet: {testBetAmount}, Mode: {shuffleMode})");
    }
    
    public void TestKillNumbersSystem()
    {
        if (slotMachine != null)
        {
            slotMachine.SetKillNumbers(testKillNumbers);
            slotMachine.EnableKillNumbers(testKillNumbersEnabled);
            slotMachine.SetApplyToJackpot(testApplyToJackpot);
            
            Debug.Log($"🎯 KILL NUMBERS TEST:");
            Debug.Log($"Enabled: {testKillNumbersEnabled}");
            Debug.Log($"Apply to Jackpot: {testApplyToJackpot}");
            Debug.Log($"Kill Numbers: {string.Join(", ", testKillNumbers)}");
            
            Debug.Log($"🎯 TEST GENERATED NUMBERS:");
            for (int i = 0; i < 5; i++)
            {
                string testNumber = slotMachine.GenerateNumberWithoutKillNumbers(); 
                Debug.Log($"  {i+1}. {testNumber}");
            }
        }
        else
        {
            Debug.LogError("❌ SlotMachine4D not found!");
        }
    }
    
    public void TestFixedPrizeCalculations()
    {
        Debug.Log("🧪 FIXED PRIZE CALCULATION TEST:");
        Debug.Log("=================================");
        
        float bet = testBetAmount;
        
        Debug.Log($"Testing with bet amount: {bet}");
        Debug.Log("");
        
        Debug.Log("4-DIGIT WINS (12x Bet + Fixed Prize):");
        Debug.Log($"1st Prize: 12×{bet} + {jackpotManager.firstPrizeAmount} = {12*bet + jackpotManager.firstPrizeAmount}");
        Debug.Log($"2nd Prize: 12×{bet} + {jackpotManager.secondPrizeAmount} = {12*bet + jackpotManager.secondPrizeAmount}");
        Debug.Log($"3rd Prize: 12×{bet} + {jackpotManager.thirdPrizeAmount} = {12*bet + jackpotManager.thirdPrizeAmount}");
        Debug.Log("");
        
        Debug.Log("MULTIPLIER-ONLY WINS:");
        Debug.Log($"2-digit win: 2×{bet} = {2*bet}");
        Debug.Log($"3-digit win: 12×{bet} = {12*bet}");
        Debug.Log($"4-digit Special: (12+15)×{bet} = {27*bet}");
        Debug.Log($"4-digit Consolation: (12+8)×{bet} = {20*bet}");
        Debug.Log("");
        
        Debug.Log("YOUR SCENARIO (Bet=1, 1st Prize 4-digit match):");
        Debug.Log($"12×1 + {jackpotManager.firstPrizeAmount} = {12*1 + jackpotManager.firstPrizeAmount} win amount");
        Debug.Log($"Plus returned bet: +1");
        Debug.Log($"TOTAL CREDIT INCREASE: {12*1 + jackpotManager.firstPrizeAmount + 1}");
        Debug.Log("=================================");
    }
    
    void SetBetAmount(float amount)
    {
        Debug.Log($"💎 Setting bet amount to: {amount}");
    }
    
    public void SetNextShuffleNumber(string number)
    {
        if (number.Length == 4 && int.TryParse(number, out _))
        {
            nextShuffleNumber = number;
            shuffleMode = ShuffleMode.CustomNumber;
            
            if (shuffleInputField != null) shuffleInputField.text = nextShuffleNumber;
            if (shuffleModeDropdown != null) shuffleModeDropdown.value = (int)ShuffleMode.CustomNumber;
            
            Debug.Log($"Trainer next shuffle set to: {nextShuffleNumber}");
        }
    }
    
    public void SetShuffleMode(ShuffleMode mode)
    {
        shuffleMode = mode;
        if (shuffleModeDropdown != null) shuffleModeDropdown.value = (int)mode;
        Debug.Log($"Shuffle mode set to: {mode}");
    }
    
    public void EnableShuffleControl(bool enable)
    {
        controlNextShuffle = enable;
        if (controlShuffleToggle != null) controlShuffleToggle.isOn = enable;
    }
    
    public void QuickTestFourDigitFirstPrize()
    {
        EnableShuffleControl(true);
        SetShuffleMode(ShuffleMode.FourDigitWin);
        TriggerTestSpin();
    }
}