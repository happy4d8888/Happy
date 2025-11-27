using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))] // Ensure AudioSource exists
public class JackpotManager : MonoBehaviour
{
    [Header("Manager References")]
    public NumberPadManager numberPadManager;
    public SlotMachine4D slotMachine;
    public PrizeTableManager prizeTableManager;
    public WinAnimationManager winAnimationManager;
    public RTPManager rtpManager;

    [Header("UI References - Prize Displays")]
    public TextMeshProUGUI firstPrizeText;
    public TextMeshProUGUI secondPrizeText;
    public TextMeshProUGUI thirdPrizeText;
    public TextMeshProUGUI winAmountText;

    [Header("Multiplier Rules")]
    public float twoDigitMultiplier = 2f;
    public float threeDigitMultiplier = 12f;
    
    [Header("Fixed Prize Amounts - 4 Digit Wins")]
    public float firstPrizeAmount = 8888f;
    public float secondPrizeAmount = 2000f;
    public float thirdPrizeAmount = 1000f;
    public float specialPrizeAmount = 200f;
    public float consolationPrizeAmount = 60f;

    [Header("Jackpot Settings")]
    public float jackpotChance = 1f;
    public string jackpotNumber = "";

    [Header("Display Settings")]
    public bool useRandomDisplayNumbers = true;
    public string firstPrizeDisplay = "8888";
    public string secondPrizeDisplay = "2000";
    public string thirdPrizeDisplay = "1000";

    [Header("Dimming Settings")]
    public float dimmedAlpha = 0.3f;
    private Color originalFirstPrizeColor;
    private Color originalSecondPrizeColor;
    private Color originalThirdPrizeColor;

    // --- NEW: Sound Effect for Small Win ---
    [Header("Sound Effects")]
    public AudioClip smallWinSound; // Assign this in Inspector
    private AudioSource audioSource;
    // --- END NEW ---

    private float currentWinAmount = 0f;
    private float currentPlacedBet = 0f;
    private bool isJackpotWin = false;
    private List<string> winningPrizeTypes = new List<string>();

    void Start()
    {
        // --- NEW: Initialize Audio ---
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        // --- END NEW ---

        InitializePrizeDisplayColors();
        LoadOrGenerateJackpot();
        if (useRandomDisplayNumbers) GenerateRandomDisplayNumbers();
        
        OverwriteDisplaysWithSavedNumbers();

        UpdatePrizeDisplays();
    }

    void OverwriteDisplaysWithSavedNumbers()
    {
        string s1 = PlayerPrefs.GetString("SavedNumber1", "");
        string s2 = PlayerPrefs.GetString("SavedNumber2", "");
        string s3 = PlayerPrefs.GetString("SavedNumber3", "");

        if (!string.IsNullOrEmpty(s1)) firstPrizeDisplay = s1;
        if (!string.IsNullOrEmpty(s2)) secondPrizeDisplay = s2;
        if (!string.IsNullOrEmpty(s3)) thirdPrizeDisplay = s3;
        
        Debug.Log($"🎯 Prize Displays Overwritten: 1st={firstPrizeDisplay}, 2nd={secondPrizeDisplay}, 3rd={thirdPrizeDisplay}");
    }

    void InitializePrizeDisplayColors()
    {
        if (firstPrizeText != null) originalFirstPrizeColor = firstPrizeText.color;
        if (secondPrizeText != null) originalSecondPrizeColor = secondPrizeText.color;
        if (thirdPrizeText != null) originalThirdPrizeColor = thirdPrizeText.color;
    }

    public void RestorePrizeDisplaysWithMatch(string partialNumber)
    {
        if (string.IsNullOrEmpty(partialNumber))
        {
            UpdatePrizeDisplays(); 
            DimPrizeDisplays(); 
            return;
        }
        
        RestorePrizeDisplayWithPartialMatch(partialNumber, firstPrizeText, firstPrizeDisplay, originalFirstPrizeColor);
        RestorePrizeDisplayWithPartialMatch(partialNumber, secondPrizeText, secondPrizeDisplay, originalSecondPrizeColor);
        RestorePrizeDisplayWithPartialMatch(partialNumber, thirdPrizeText, thirdPrizeDisplay, originalThirdPrizeColor);
    }

    private void RestorePrizeDisplayWithPartialMatch(string partialNumber, TextMeshProUGUI prizeText, string displayNumber, Color originalColor)
    {
        if (prizeText == null || string.IsNullOrEmpty(displayNumber) || displayNumber.Length != 4) return;
        
        int matchingDigits = CountConsecutiveMatchesFromLeft(partialNumber, displayNumber);
        
        if (matchingDigits > 0)
        {
            string formattedText = FormatNumberWithPartialMatchHighlight(displayNumber, matchingDigits, originalColor);
            prizeText.text = formattedText;
            prizeText.color = originalColor;
        }
        else
        {
            Color dimmedColor = originalColor;
            dimmedColor.a = dimmedAlpha;
            prizeText.color = dimmedColor;
            prizeText.text = displayNumber;
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

    public void DimPrizeDisplays()
    {
        if (firstPrizeText != null)
        {
            Color dimmedColor = originalFirstPrizeColor;
            dimmedColor.a = dimmedAlpha;
            firstPrizeText.color = dimmedColor;
            firstPrizeText.text = firstPrizeDisplay; 
        }
        if (secondPrizeText != null)
        {
            Color dimmedColor = originalSecondPrizeColor;
            dimmedColor.a = dimmedAlpha;
            secondPrizeText.color = dimmedColor;
            secondPrizeText.text = secondPrizeDisplay; 
        }
        if (thirdPrizeText != null)
        {
            Color dimmedColor = originalThirdPrizeColor;
            dimmedColor.a = dimmedAlpha;
            thirdPrizeText.color = dimmedColor;
            thirdPrizeText.text = thirdPrizeDisplay; 
        }
    }

    public void RestorePrizeDisplays()
    {
        if (firstPrizeText != null) 
        {
            firstPrizeText.color = originalFirstPrizeColor;
            firstPrizeText.text = firstPrizeDisplay; 
        }
        if (secondPrizeText != null)
        {
            secondPrizeText.color = originalSecondPrizeColor;
            secondPrizeText.text = secondPrizeDisplay; 
        }
        if (thirdPrizeText != null)
        {
            thirdPrizeText.color = originalThirdPrizeColor;
            thirdPrizeText.text = thirdPrizeDisplay; 
        }
    }

    public void CheckForWins(string spunNumber, float placedBet)
    {
        currentWinAmount = 0f;
        currentPlacedBet = placedBet;
        isJackpotWin = false;
        winningPrizeTypes.Clear();
        float totalWinMultiplierAmount = 0f;
        
        if (PlayerPrefs.GetInt("JackpotUsed", 0) == 0)
        {
            int jackpotMatches = CountConsecutiveMatches_4Digit(spunNumber, jackpotNumber); 
            if (jackpotMatches >= 2)
            {
                float winAmount = CalculateWinnings(placedBet, jackpotMatches, 0, true);
                totalWinMultiplierAmount += winAmount;
                winningPrizeTypes.Add($"JACKPOT ({jackpotMatches}-digit)");
                
                if (jackpotMatches == 4)
                {
                    isJackpotWin = true;
                    PlayerPrefs.SetInt("JackpotUsed", 1);
                    PlayerPrefs.Save();
                }
            }
        }

        float savedNumbersWin = CheckSavedNumbers(spunNumber, placedBet);
        totalWinMultiplierAmount += savedNumbersWin;
        
        float specialPrizesWin = CheckSpecialPrizes(spunNumber, placedBet);
        totalWinMultiplierAmount += specialPrizesWin;
        
        float consolationPrizesWin = CheckConsolationPrizes(spunNumber, placedBet);
        totalWinMultiplierAmount += consolationPrizesWin;
        
        currentWinAmount = totalWinMultiplierAmount;
        
        // --- MODIFIED: Check win type before showing animation ---
        if (totalWinMultiplierAmount > 0)
        {
            bool hasFourDigitWin = winningPrizeTypes.Exists(prize => prize.Contains("4-digit"));
            
            // Check if we have any "Big" wins (3 or 4 digits)
            bool hasBigWin = winningPrizeTypes.Exists(prize => prize.Contains("3-digit") || prize.Contains("4-digit"));

            if (hasBigWin)
            {
                // Play the full popup animation for 3 or 4 digit wins
                ShowWinAnimation(currentWinAmount, hasFourDigitWin, isJackpotWin);
            }
            else
            {
                // It's only a 2-digit win - Just play SFX and don't block gameplay
                if (smallWinSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(smallWinSound);
                }
                Debug.Log("💰 Small Win detected (2-digit only) - Playing SFX without Popup");
            }

            // Always award money and report stats
            AwardWinnings(totalWinMultiplierAmount, hasFourDigitWin);
            ReportWinToAutomatedSystem(spunNumber, totalWinMultiplierAmount, hasFourDigitWin);
        }
        // --- END MODIFIED ---
            
        UpdateWinAmountDisplay();
    }

    public void ReportWinToAutomatedSystem(string winningNumber, float winAmount, bool isFourDigitWin)
    {
        if (slotMachine != null)
        {
            slotMachine.OnWinDetected(winningNumber, winAmount, isFourDigitWin);
        }
    }

    float CheckSavedNumbers(string spunNumber, float placedBet)
    {
        float totalWin = 0f;
        for (int i = 0; i < 3; i++)
        {
            string savedNumber = numberPadManager.GetSavedNumberByIndex(i);
            if (!string.IsNullOrEmpty(savedNumber))
            {
                int matches = CountConsecutiveMatches_4Digit(spunNumber, savedNumber);
                if (matches >= 2)
                {
                    float winAmount = CalculateWinnings(placedBet, matches, i, false);
                    totalWin += winAmount;
                    string prizeName = i == 0 ? "1st Prize" : i == 1 ? "2nd Prize" : "3rd Prize";
                    winningPrizeTypes.Add($"{prizeName} ({matches}-digit)");
                }
            }
        }
        return totalWin;
    }

    float CheckSpecialPrizes(string spunNumber, float placedBet)
    {
        float totalWin = 0f;
        string[] specialPrizes = prizeTableManager?.GetAllSpecialPrizes();
        if (specialPrizes != null)
        {
            foreach (string prize in specialPrizes)
            {
                if (!string.IsNullOrEmpty(prize))
                {
                    int matches = CountConsecutiveMatches_4Digit(spunNumber, prize);
                    if (matches >= 2)
                    {
                        float winAmount = CalculateWinnings(placedBet, matches, -1, false);
                        totalWin += winAmount;
                        winningPrizeTypes.Add($"Special Prize ({matches}-digit)");
                    }
                }
            }
        }
        return totalWin;
    }

    float CheckConsolationPrizes(string spunNumber, float placedBet)
    {
        float totalWin = 0f;
        string[] consolationPrizes = prizeTableManager?.GetAllConsolationPrizes();
        if (consolationPrizes != null)
        {
            foreach (string prize in consolationPrizes)
            {
                if (!string.IsNullOrEmpty(prize))
                {
                    int matches = CountConsecutiveMatches_4Digit(spunNumber, prize);
                    if (matches >= 2)
                    {
                        float winAmount = CalculateWinnings(placedBet, matches, -2, false);
                        totalWin += winAmount;
                        winningPrizeTypes.Add($"Consolation Prize ({matches}-digit)");
                    }
                }
            }
        }
        return totalWin;
    }

    void ShowWinAnimation(float totalWinAmount, bool isEpicWin, bool isJackpot)
    {
        if (winAnimationManager != null)
        {
            if (isEpicWin) winAnimationManager.ShowEpicWinAnimation(totalWinAmount);
            else winAnimationManager.ShowWinAnimation(totalWinAmount);
        }
    }

    void AwardWinnings(float multiplierAmount, bool isJackpot)
    {
        if (slotMachine != null && slotMachine.creditSystem != null)
        {
            slotMachine.creditSystem.AddWinnings(multiplierAmount);
        }
        if (rtpManager != null)
        {
            rtpManager.RecordWin(multiplierAmount, isJackpot);
        }
    }

    int CountConsecutiveMatches_4Digit(string spun, string target)
    {
        if (string.IsNullOrEmpty(spun) || string.IsNullOrEmpty(target) || spun.Length != 4 || target.Length != 4)
            return 0;

        int matches = 0;
        for (int i = 0; i < 4; i++)
        {
            if (spun[i] == target[i]) matches++;
            else break;
        }
        return matches;
    }

    float CalculateWinnings(float bet, int matches, int prizeSlotIndex, bool isJackpot)
    {
        float baseWin = 0f;
        float fixedAmount = 0f;
        
        if (matches == 2)
        {
            baseWin = bet * twoDigitMultiplier;
        }
        else if (matches == 3)
        {
            baseWin = bet * threeDigitMultiplier;
        }
        else if (matches == 4)
        {
            baseWin = bet * threeDigitMultiplier;
            if (isJackpot || prizeSlotIndex == 0)
                fixedAmount = firstPrizeAmount;
            else if (prizeSlotIndex == 1)
                fixedAmount = secondPrizeAmount;
            else if (prizeSlotIndex == 2)
                fixedAmount = thirdPrizeAmount;
            else if (prizeSlotIndex == -1) 
                fixedAmount = specialPrizeAmount;
            else if (prizeSlotIndex == -2) 
                fixedAmount = consolationPrizeAmount;
        }
        return baseWin + fixedAmount;
    }

    void LoadOrGenerateJackpot()
    {
        jackpotNumber = PlayerPrefs.GetString("JackpotNumber", "");
        if (string.IsNullOrEmpty(jackpotNumber))
        {
            jackpotNumber = Random.Range(0, 10000).ToString("D4");
            PlayerPrefs.SetString("JackpotNumber", jackpotNumber);
            PlayerPrefs.SetInt("JackpotUsed", 0);
            PlayerPrefs.Save();
        }
    }

    void GenerateRandomDisplayNumbers()
    {
        firstPrizeDisplay = Random.Range(0, 10000).ToString("D4");
        secondPrizeDisplay = Random.Range(0, 10000).ToString("D4");
        thirdPrizeDisplay = Random.Range(0, 10000).ToString("D4");
    }

    void UpdatePrizeDisplays()
    {
        if (firstPrizeText != null) firstPrizeText.text = firstPrizeDisplay;
        if (secondPrizeText != null) secondPrizeText.text = secondPrizeDisplay;
        if (thirdPrizeText != null) thirdPrizeText.text = thirdPrizeDisplay;
    }

    void UpdateWinAmountDisplay()
    {
        if (winAmountText != null) winAmountText.text = currentWinAmount.ToString("0");
    }
}