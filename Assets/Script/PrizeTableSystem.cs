using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PrizeTableManager : MonoBehaviour
{
    [Header("Special Prize Table References")]
    public TextMeshProUGUI[] specialRow1Columns;
    public TextMeshProUGUI[] specialRow2Columns;

    [Header("Consolation Prize Table References")]
    public TextMeshProUGUI[] consolationRow1Columns;
    public TextMeshProUGUI[] consolationRow2Columns;

    [Header("Prize Settings")]
    public int specialPrizeCount = 10;
    public int consolationPrizeCount = 10;

    [Header("Dimming Settings")]
    public float dimmedAlpha = 0.3f;
    private List<Color> originalSpecialColors = new List<Color>();
    private List<Color> originalConsolationColors = new List<Color>();

    // This is the clean data array. We will read from this.
    private string[] specialPrizes;
    private string[] consolationPrizes;
    private int spinCount = 0;

    void Start()
    {
        specialPrizes = new string[specialPrizeCount];
        consolationPrizes = new string[consolationPrizeCount];
        
        InitializeOriginalColors();
        GenerateAllPrizes();
    }

    void InitializeOriginalColors()
    {
        foreach (TextMeshProUGUI text in specialRow1Columns)
        {
            if (text != null) originalSpecialColors.Add(text.color);
        }
        foreach (TextMeshProUGUI text in specialRow2Columns)
        {
            if (text != null) originalSpecialColors.Add(text.color);
        }
        
        foreach (TextMeshProUGUI text in consolationRow1Columns)
        {
            if (text != null) originalConsolationColors.Add(text.color);
        }
        foreach (TextMeshProUGUI text in consolationRow2Columns)
        {
            if (text != null) originalConsolationColors.Add(text.color);
        }
    }

    public void RestorePrizeNumbersWithMatch(string partialNumber)
    {
        if (string.IsNullOrEmpty(partialNumber))
        {
            UpdateSpecialPrizeDisplay();
            UpdateConsolationPrizeDisplay();
            DimAllPrizeNumbers();
            return;
        }
        
        RestorePrizeRowWithPartialMatch(partialNumber, specialRow1Columns, 0, originalSpecialColors, true);
        RestorePrizeRowWithPartialMatch(partialNumber, specialRow2Columns, specialRow1Columns.Length, originalSpecialColors, true);
        RestorePrizeRowWithPartialMatch(partialNumber, consolationRow1Columns, 0, originalConsolationColors, false);
        RestorePrizeRowWithPartialMatch(partialNumber, consolationRow2Columns, consolationRow1Columns.Length, originalConsolationColors, false);
    }

    private void RestorePrizeRowWithPartialMatch(string partialNumber, TextMeshProUGUI[] row, int startIndex, List<Color> originalColors, bool isSpecial)
    {
        for (int i = 0; i < row.Length; i++)
        {
            TextMeshProUGUI text = row[i];
            if (text == null) continue;
            
            int colorIndex = startIndex + i;
            if (colorIndex >= originalColors.Count) continue;
            
            string prizeNumber = "";
            int prizeIndex = startIndex + i;

            if(isSpecial && prizeIndex < specialPrizes.Length)
                prizeNumber = specialPrizes[prizeIndex];
            else if (!isSpecial && prizeIndex < consolationPrizes.Length)
                prizeNumber = consolationPrizes[prizeIndex];
            
            if (string.IsNullOrEmpty(prizeNumber) || prizeNumber.Length != 4)
                continue; 
            
            int matchingDigits = CountConsecutiveMatchesFromLeft(partialNumber, prizeNumber);
            
            if (matchingDigits > 0)
            {
                string formattedText = FormatNumberWithPartialMatchHighlight(prizeNumber, matchingDigits, originalColors[colorIndex]);
                text.text = formattedText;
                text.color = originalColors[colorIndex];
            }
            else
            {
                Color dimmedColor = originalColors[colorIndex];
                dimmedColor.a = dimmedAlpha;
                text.color = dimmedColor;
                text.text = prizeNumber; 
            }
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

    public void DimAllPrizeNumbers()
    {
        int colorIndex = 0;
        
        for (int i = 0; i < specialRow1Columns.Length; i++)
        {
            if (specialRow1Columns[i] != null && colorIndex < originalSpecialColors.Count)
            {
                Color dimmedColor = originalSpecialColors[colorIndex];
                dimmedColor.a = dimmedAlpha;
                specialRow1Columns[i].color = dimmedColor;
                if(i < specialPrizes.Length) specialRow1Columns[i].text = specialPrizes[i]; 
                colorIndex++;
            }
        }
        for (int i = 0; i < specialRow2Columns.Length; i++)
        {
            if (specialRow2Columns[i] != null && colorIndex < originalSpecialColors.Count)
            {
                Color dimmedColor = originalSpecialColors[colorIndex];
                dimmedColor.a = dimmedAlpha;
                specialRow2Columns[i].color = dimmedColor;
                int prizeIndex = specialRow1Columns.Length + i;
                if(prizeIndex < specialPrizes.Length) specialRow2Columns[i].text = specialPrizes[prizeIndex]; 
                colorIndex++;
            }
        }
        
        colorIndex = 0;
        for (int i = 0; i < consolationRow1Columns.Length; i++)
        {
            if (consolationRow1Columns[i] != null && colorIndex < originalConsolationColors.Count)
            {
                Color dimmedColor = originalConsolationColors[colorIndex];
                dimmedColor.a = dimmedAlpha;
                consolationRow1Columns[i].color = dimmedColor;
                if(i < consolationPrizes.Length) consolationRow1Columns[i].text = consolationPrizes[i]; 
                colorIndex++;
            }
        }
        for (int i = 0; i < consolationRow2Columns.Length; i++)
        {
            if (consolationRow2Columns[i] != null && colorIndex < originalConsolationColors.Count)
            {
                Color dimmedColor = originalConsolationColors[colorIndex];
                dimmedColor.a = dimmedAlpha;
                consolationRow2Columns[i].color = dimmedColor;
                int prizeIndex = consolationRow1Columns.Length + i;
                if(prizeIndex < consolationPrizes.Length) consolationRow2Columns[i].text = consolationPrizes[prizeIndex]; 
                colorIndex++;
            }
        }
    }

    public void RestoreAllPrizeNumbers()
    {
        int colorIndex = 0;
        
        for (int i = 0; i < specialRow1Columns.Length; i++)
        {
            if (specialRow1Columns[i] != null && colorIndex < originalSpecialColors.Count)
            {
                specialRow1Columns[i].color = originalSpecialColors[colorIndex];
                if(i < specialPrizes.Length) specialRow1Columns[i].text = specialPrizes[i]; 
                colorIndex++;
            }
        }
        for (int i = 0; i < specialRow2Columns.Length; i++)
        {
            if (specialRow2Columns[i] != null && colorIndex < originalSpecialColors.Count)
            {
                specialRow2Columns[i].color = originalSpecialColors[colorIndex];
                int prizeIndex = specialRow1Columns.Length + i;
                if(prizeIndex < specialPrizes.Length) specialRow2Columns[i].text = specialPrizes[prizeIndex]; 
                colorIndex++;
            }
        }
        
        colorIndex = 0;
        for (int i = 0; i < consolationRow1Columns.Length; i++)
        {
            if (consolationRow1Columns[i] != null && colorIndex < originalConsolationColors.Count)
            {
                consolationRow1Columns[i].color = originalConsolationColors[colorIndex];
                if(i < consolationPrizes.Length) consolationRow1Columns[i].text = consolationPrizes[i]; 
                colorIndex++;
            }
        }
        for (int i = 0; i < consolationRow2Columns.Length; i++)
        {
            if (consolationRow2Columns[i] != null && colorIndex < originalConsolationColors.Count)
            {
                consolationRow2Columns[i].color = originalConsolationColors[colorIndex];
                int prizeIndex = consolationRow1Columns.Length + i;
                if(prizeIndex < consolationPrizes.Length) consolationRow2Columns[i].text = consolationPrizes[prizeIndex]; 
                colorIndex++;
            }
        }
    }

    // --- MODIFIED: Only increment count, do NOT generate here ---
    public void OnSpinCompleted()
    {
        spinCount++;
        Debug.Log($"📊 Spin Cycle Count: {spinCount}/3");
    }

    // --- NEW: Called at the START of a spin to randomize if needed ---
    public void CheckAndRefreshPrizes()
    {
        if (spinCount >= 3)
        {
            spinCount = 0;
            GenerateAllPrizes();
            Debug.Log("🔄 New 3-spin cycle started! Prizes Refreshed.");
        }
    }
    // --- END NEW ---

    public void GenerateAllPrizes()
    {
        GenerateSpecialPrizes();
        GenerateConsolationPrizes();
    }

    public void GenerateSpecialPrizes()
    {
        for (int i = 0; i < specialPrizeCount; i++)
        {
            int randomNumber = Random.Range(0, 10000);
            specialPrizes[i] = randomNumber.ToString("D4");
        }
        UpdateSpecialPrizeDisplay();
    }

    public void GenerateConsolationPrizes()
    {
        for (int i = 0; i < consolationPrizeCount; i++)
        {
            int randomNumber = Random.Range(0, 10000);
            consolationPrizes[i] = randomNumber.ToString("D4");
        }
        UpdateConsolationPrizeDisplay();
    }

    void UpdateSpecialPrizeDisplay()
    {
        int halfCount = Mathf.CeilToInt(specialPrizeCount / 2f);
        for (int i = 0; i < halfCount; i++)
        {
            if (i < specialRow1Columns.Length && specialRow1Columns[i] != null)
                specialRow1Columns[i].text = specialPrizes[i];
        }
        for (int i = halfCount; i < specialPrizeCount; i++)
        {
            int rowIndex = i - halfCount;
            if (rowIndex < specialRow2Columns.Length && specialRow2Columns[rowIndex] != null)
                specialRow2Columns[rowIndex].text = specialPrizes[i];
        }
    }

    void UpdateConsolationPrizeDisplay()
    {
        int halfCount = Mathf.CeilToInt(consolationPrizeCount / 2f);
        for (int i = 0; i < halfCount; i++)
        {
            if (i < consolationRow1Columns.Length && consolationRow1Columns[i] != null)
                consolationRow1Columns[i].text = consolationPrizes[i];
        }
        for (int i = halfCount; i < consolationPrizeCount; i++)
        {
            int rowIndex = i - halfCount;
            if (rowIndex < consolationRow2Columns.Length && consolationRow2Columns[rowIndex] != null)
                consolationRow2Columns[rowIndex].text = consolationPrizes[i];
        }
    }
    
    public string[] GetAllSpecialPrizes()
    {
        return specialPrizes;
    }

    public string[] GetAllConsolationPrizes()
    {
        return consolationPrizes;
    }
}