using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for press effects

[RequireComponent(typeof(AudioSource))]
public class NumberPadManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject numberPadPanel;
    public Button[] showKeypadButtons;
    public Button saveButton;
    public Button deleteButton;
    public Button closeButton; 
    public Button[] digitButtons;
    public TextMeshProUGUI displayText;
    
    [Header("Saved Number Slots")]
    public TextMeshProUGUI savedNumberSlot1;
    public TextMeshProUGUI savedNumberSlot2;
    public TextMeshProUGUI savedNumberSlot3;

    [Header("Dimming Settings")]
    public float dimmedAlpha = 0.3f;
    private Color[] originalColors = new Color[3];

    [Header("Sound Effects")]
    public AudioClip buttonClickSound; 
    private AudioSource audioSource;

    // --- NEW: Button Press Effect ---
    [Header("Button Press Effect")]
    [Tooltip("How small the button gets when pressed (e.g., 0.9 = 90% of original size).")]
    public float pressedScale = 0.9f;
    // --- END NEW ---
    
    // Public properties for external access
    public string SavedNumber1 { get; private set; } = "";
    public string SavedNumber2 { get; private set; } = "";
    public string SavedNumber3 { get; private set; } = "";
    
    private string currentInput = "";
    private const int maxDigits = 4;
    private int currentSlot = 1;
    
    private WinAnimationManager winAnimationManager;

    void Start()
    {
        winAnimationManager = FindObjectOfType<WinAnimationManager>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        if (numberPadPanel != null) numberPadPanel.SetActive(false);
        
        if (savedNumberSlot1 != null) originalColors[0] = savedNumberSlot1.color;
        if (savedNumberSlot2 != null) originalColors[1] = savedNumberSlot2.color;
        if (savedNumberSlot3 != null) originalColors[2] = savedNumberSlot3.color;
        
        LoadSavedNumbers();
        
        // --- MODIFIED: Adding Logic AND Press Effects ---

        // 1. Show Keypad Buttons
        if (showKeypadButtons.Length >= 1 && showKeypadButtons[0] != null)
        {
            showKeypadButtons[0].onClick.AddListener(() => { PlayClickSound(); ShowKeypadForSlot(1); });
            AddPressEffect(showKeypadButtons[0]); // Add Effect
        }
        
        if (showKeypadButtons.Length >= 2 && showKeypadButtons[1] != null)
        {
            showKeypadButtons[1].onClick.AddListener(() => { PlayClickSound(); ShowKeypadForSlot(2); });
            AddPressEffect(showKeypadButtons[1]); // Add Effect
        }
        
        if (showKeypadButtons.Length >= 3 && showKeypadButtons[2] != null)
        {
            showKeypadButtons[2].onClick.AddListener(() => { PlayClickSound(); ShowKeypadForSlot(3); });
            AddPressEffect(showKeypadButtons[2]); // Add Effect
        }
        
        // 2. Functional Buttons
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() => { PlayClickSound(); SaveInput(); });
            AddPressEffect(saveButton); // Add Effect
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() => { PlayClickSound(); DeleteLastDigit(); });
            AddPressEffect(deleteButton); // Add Effect
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => { PlayClickSound(); HideKeypad(); });
            AddPressEffect(closeButton); // Add Effect
        }

        // 3. Digit Buttons
        for (int i = 0; i < digitButtons.Length; i++)
        {
            if (digitButtons[i] != null)
            {
                int digit = i;
                digitButtons[i].onClick.AddListener(() => { PlayClickSound(); AddDigit(digit); });
                AddPressEffect(digitButtons[i]); // Add Effect
            }
        }

        UpdateSavedNumberDisplays();
    }

    // --- NEW: Helper function to add press effect to any button ---
    void AddPressEffect(Button btn)
    {
        if (btn == null) return;

        // Store the original scale
        Vector3 originalScale = btn.transform.localScale;

        // Get or Add EventTrigger
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        // Pointer Down (Press)
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => 
        {
            if (btn.interactable) btn.transform.localScale = originalScale * pressedScale; 
        });
        trigger.triggers.Add(pointerDown);

        // Pointer Up (Release)
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => 
        {
            btn.transform.localScale = originalScale; 
        });
        trigger.triggers.Add(pointerUp);

        // Pointer Exit (Drag off)
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => 
        {
            btn.transform.localScale = originalScale; 
        });
        trigger.triggers.Add(pointerExit);
    }
    // --- END NEW ---

    void PlayClickSound()
    {
        if (buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    void LoadSavedNumbers()
    {
        SavedNumber1 = ValidateSavedNumber(PlayerPrefs.GetString("SavedNumber1", ""));
        SavedNumber2 = ValidateSavedNumber(PlayerPrefs.GetString("SavedNumber2", ""));
        SavedNumber3 = ValidateSavedNumber(PlayerPrefs.GetString("SavedNumber3", ""));
        
        if (SavedNumber1 != PlayerPrefs.GetString("SavedNumber1", ""))
            PlayerPrefs.SetString("SavedNumber1", SavedNumber1);
        if (SavedNumber2 != PlayerPrefs.GetString("SavedNumber2", ""))
            PlayerPrefs.SetString("SavedNumber2", SavedNumber2);
        if (SavedNumber3 != PlayerPrefs.GetString("SavedNumber3", ""))
            PlayerPrefs.SetString("SavedNumber3", SavedNumber3);
            
        PlayerPrefs.Save();
    }
    
    string ValidateSavedNumber(string number)
    {
        if (string.IsNullOrEmpty(number)) return "";
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]");
        number = regex.Replace(number, "");
        if (number.Length == 4) return number;
        return "";
    }
    
    void UpdateSavedNumberDisplays()
    {
        if (savedNumberSlot1 != null) 
        {
            savedNumberSlot1.color = originalColors[0];
            savedNumberSlot1.text = string.IsNullOrEmpty(SavedNumber1) ? "----" : SavedNumber1;
        }
        if (savedNumberSlot2 != null) 
        {
            savedNumberSlot2.color = originalColors[1];
            savedNumberSlot2.text = string.IsNullOrEmpty(SavedNumber2) ? "----" : SavedNumber2;
        }
        if (savedNumberSlot3 != null) 
        {
            savedNumberSlot3.color = originalColors[2];
            savedNumberSlot3.text = string.IsNullOrEmpty(SavedNumber3) ? "----" : SavedNumber3;
        }
    }

    public void ShowKeypadForSlot(int slotNumber)
    {
        if (IsGameplayBlockedByWinAnimation()) return;
        
        currentSlot = Mathf.Clamp(slotNumber, 1, 3);
        if (numberPadPanel != null) numberPadPanel.SetActive(true);
        
        switch (currentSlot)
        {
            case 1: currentInput = SavedNumber1; break;
            case 2: currentInput = SavedNumber2; break;
            case 3: currentInput = SavedNumber3; break;
        }
        UpdateDisplay();
    }

    public void HideKeypad()
    {
        if (numberPadPanel != null) numberPadPanel.SetActive(false);
    }

    void AddDigit(int digit)
    {
        if (currentInput.Length < maxDigits)
        {
            currentInput += digit.ToString();
            UpdateDisplay();
        }
    }

    void DeleteLastDigit()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    void SaveInput()
    {
        if (currentInput.Length == maxDigits)
        {
            switch (currentSlot)
            {
                case 1:
                    SavedNumber1 = currentInput;
                    PlayerPrefs.SetString("SavedNumber1", SavedNumber1);
                    break;
                case 2:
                    SavedNumber2 = currentInput;
                    PlayerPrefs.SetString("SavedNumber2", SavedNumber2);
                    break;
                case 3:
                    SavedNumber3 = currentInput;
                    PlayerPrefs.SetString("SavedNumber3", SavedNumber3);
                    break;
            }
            PlayerPrefs.Save();
            UpdateSavedNumberDisplays();
            HideKeypad();
        }
    }

    void UpdateDisplay()
    {
        if (displayText != null)
            displayText.text = currentInput.PadRight(4, '_');
    }
    
    bool IsGameplayBlockedByWinAnimation()
    {
        return winAnimationManager != null && winAnimationManager.IsWinAnimationPlaying();
    }
    
    public string GetSavedNumberByIndex(int index)
    {
        switch (index)
        {
            case 0: return SavedNumber1;
            case 1: return SavedNumber2;
            case 2: return SavedNumber3;
            default: return "";
        }
    }
    
    public void RestoreSavedNumbersWithMatch(string partialNumber)
    {
        if (string.IsNullOrEmpty(partialNumber))
        {
            UpdateSavedNumberDisplays(); 
            DimAllSavedNumbers(); 
            return;
        }
        
        RestoreSavedNumberWithMatch(partialNumber, savedNumberSlot1, SavedNumber1, 0);
        RestoreSavedNumberWithMatch(partialNumber, savedNumberSlot2, SavedNumber2, 1);
        RestoreSavedNumberWithMatch(partialNumber, savedNumberSlot3, SavedNumber3, 2);
    }

    private void RestoreSavedNumberWithMatch(string partialNumber, TextMeshProUGUI slotText, string savedNumber, int slotIndex)
    {
        if (slotText == null || string.IsNullOrEmpty(savedNumber)) return;
        
        int matchingDigits = CountConsecutiveMatchesFromLeft(partialNumber, savedNumber);
        
        if (matchingDigits > 0)
        {
            string formattedText = FormatNumberWithPartialMatchHighlight(savedNumber, matchingDigits, originalColors[slotIndex]);
            slotText.text = formattedText;
            slotText.color = originalColors[slotIndex]; 
        }
        else
        {
            Color dimmedColor = originalColors[slotIndex];
            dimmedColor.a = dimmedAlpha;
            slotText.color = dimmedColor;
            slotText.text = savedNumber; 
        }
    }

    private int CountConsecutiveMatchesFromLeft(string partialNumber, string prizeNumber)
    {
        if (string.IsNullOrEmpty(partialNumber) || string.IsNullOrEmpty(prizeNumber)) return 0;
        
        int matches = 0;
        for (int i = 0; i < partialNumber.Length; i++)
        {
            if (i >= prizeNumber.Length) break;

            if (partialNumber[i] == prizeNumber[i])
            {
                matches++;
            }
            else
            {
                break; 
            }
        }
        return matches;
    }

    private string FormatNumberWithPartialMatchHighlight(string number, int matchingDigits, Color originalColor)
    {
        if (matchingDigits >= 4)
        {
            return number;
        }

        if (matchingDigits == 0)
        {
            Color fullDimmedColor = originalColor;
            fullDimmedColor.a = dimmedAlpha;
            string dimmedHex = ColorUtility.ToHtmlStringRGBA(fullDimmedColor);
            return $"<color=#{dimmedHex}>{number}</color>";
        }

        Color dimmedColor = originalColor;
        dimmedColor.a = dimmedAlpha;
        string dimmedColorHex = ColorUtility.ToHtmlStringRGBA(dimmedColor);
        
        string matchingPart = number.Substring(0, matchingDigits);
        string remainingPart = number.Substring(matchingDigits);

        return $"{matchingPart}<color=#{dimmedColorHex}>{remainingPart}</color>";
    }
    
    public void DimAllSavedNumbers()
    {
        if (savedNumberSlot1 != null)
        {
            Color color = originalColors[0]; 
            color.a = dimmedAlpha;
            savedNumberSlot1.color = color;
            savedNumberSlot1.text = string.IsNullOrEmpty(SavedNumber1) ? "----" : SavedNumber1; 
        }
        
        if (savedNumberSlot2 != null)
        {
            Color color = originalColors[1]; 
            color.a = dimmedAlpha;
            savedNumberSlot2.color = color;
            savedNumberSlot2.text = string.IsNullOrEmpty(SavedNumber2) ? "----" : SavedNumber2; 
        }
        
        if (savedNumberSlot3 != null)
        {
            Color color = originalColors[2]; 
            color.a = dimmedAlpha;
            savedNumberSlot3.color = color;
            savedNumberSlot3.text = string.IsNullOrEmpty(SavedNumber3) ? "----" : SavedNumber3; 
        }
    }

    public void RestoreAllSavedNumbers()
    {
        if (savedNumberSlot1 != null && originalColors.Length > 0)
        {
            savedNumberSlot1.color = originalColors[0];
            savedNumberSlot1.text = string.IsNullOrEmpty(SavedNumber1) ? "----" : SavedNumber1; 
        }
        if (savedNumberSlot2 != null && originalColors.Length > 1)
        {
            savedNumberSlot2.color = originalColors[1];
            savedNumberSlot2.text = string.IsNullOrEmpty(SavedNumber2) ? "----" : SavedNumber2; 
        }
        if (savedNumberSlot3 != null && originalColors.Length > 2)
        {
            savedNumberSlot3.color = originalColors[2];
            savedNumberSlot3.text = string.IsNullOrEmpty(SavedNumber3) ? "----" : SavedNumber3; 
        }
    }
    
    public void ClearSavedNumber(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                SavedNumber1 = "";
                PlayerPrefs.SetString("SavedNumber1", "");
                break;
            case 2:
                SavedNumber2 = "";
                PlayerPrefs.SetString("SavedNumber2", "");
                break;
            case 3:
                SavedNumber3 = "";
                PlayerPrefs.SetString("SavedNumber3", "");
                break;
        }
        PlayerPrefs.Save();
        UpdateSavedNumberDisplays();
    }
    
    public bool IsSlotEmpty(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return string.IsNullOrEmpty(SavedNumber1);
            case 2: return string.IsNullOrEmpty(SavedNumber2);
            case 3: return string.IsNullOrEmpty(SavedNumber3);
            default: return true;
        }
    }
}