using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class RTPManager : MonoBehaviour
{
    [Header("RTP Configuration")]
    [Range(0.85f, 0.99f)] public float targetRTP = 0.95f;
    [Range(1f, 3f)] public float volatility = 2f;
    public bool enableRTPBalancing = false; // Disabled to prevent bonus issues
    
    [Header("Live Statistics Display")]
    public TextMeshProUGUI rtpDisplayText;
    public bool showLiveStatistics = true;
    
    [Header("Game Statistics")]
    public float totalWagered = 0f;
    public float totalWon = 0f;
    public int totalSpins = 0;
    public int totalWins = 0;
    public int totalJackpots = 0;
    
    [Header("Session Statistics")]
    public float sessionWagered = 0f;
    public float sessionWon = 0f;
    public int sessionSpins = 0;
    public int sessionWins = 0;
    
    private JackpotManager jackpotManager;
    
    void Start()
    {
        jackpotManager = FindObjectOfType<JackpotManager>();
        LoadStatistics();
        
        // Disable RTP balancing to prevent automatic bonus calculations
        if (enableRTPBalancing)
        {
            Debug.LogWarning("⚠️ RTP Balancing is disabled to prevent 1% bonus issues");
            enableRTPBalancing = false;
        }
        
        UpdateDisplay();
    }
    
    public void RecordSpin(float betAmount)
    {
        totalWagered += betAmount;
        totalSpins++;
        sessionWagered += betAmount;
        sessionSpins++;
        SaveStatistics();
        UpdateDisplay();
    }
    
    public void RecordWin(float winAmount, bool isJackpot = false)
    {
        // FIXED: No automatic bonuses - record exact win amount
        totalWon += winAmount;
        totalWins++;
        sessionWon += winAmount;
        sessionWins++;
        if (isJackpot) totalJackpots++;
        SaveStatistics();
        UpdateDisplay();
        
        Debug.Log($"📊 RTP Record: Win={winAmount}, TotalWon={totalWon}, TotalWagered={totalWagered}");
    }
    
    public float GetCurrentRTP()
    {
        if (totalWagered == 0) return 0f;
        return totalWon / totalWagered;
    }
    
    public float GetSessionRTP()
    {
        if (sessionWagered == 0) return 0f;
        return sessionWon / sessionWagered;
    }
    
    public float GetHitFrequency()
    {
        if (totalSpins == 0) return 0f;
        return (float)totalWins / totalSpins;
    }
    
    void UpdateDisplay()
    {
        if (rtpDisplayText != null && showLiveStatistics)
        {
            float currentRTP = GetCurrentRTP();
            float sessionRTP = GetSessionRTP();
            float frequency = GetHitFrequency();
            
            rtpDisplayText.text = $"RTP: {currentRTP:P1} (Session: {sessionRTP:P1})\n" +
                                 $"Hit Rate: {frequency:P1} | Spins: {totalSpins}\n" +
                                 $"Jackpots: {totalJackpots}";
        }
    }
    
    void SaveStatistics()
    {
        PlayerPrefs.SetFloat("TotalWagered", totalWagered);
        PlayerPrefs.SetFloat("TotalWon", totalWon);
        PlayerPrefs.SetInt("TotalSpins", totalSpins);
        PlayerPrefs.SetInt("TotalWins", totalWins);
        PlayerPrefs.SetInt("TotalJackpots", totalJackpots);
        PlayerPrefs.Save();
    }
    
    void LoadStatistics()
    {
        totalWagered = PlayerPrefs.GetFloat("TotalWagered", 0f);
        totalWon = PlayerPrefs.GetFloat("TotalWon", 0f);
        totalSpins = PlayerPrefs.GetInt("TotalSpins", 0);
        totalWins = PlayerPrefs.GetInt("TotalWins", 0);
        totalJackpots = PlayerPrefs.GetInt("TotalJackpots", 0);
    }
    
    public void ResetSession()
    {
        sessionWagered = 0f;
        sessionWon = 0f;
        sessionSpins = 0;
        sessionWins = 0;
        UpdateDisplay();
    }
    
    public void ResetAllStatistics()
    {
        totalWagered = 0f;
        totalWon = 0f;
        totalSpins = 0;
        totalWins = 0;
        totalJackpots = 0;
        ResetSession();
        SaveStatistics();
    }
}