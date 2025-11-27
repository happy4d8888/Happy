using UnityEngine;
using UnityEngine.UI;

public class TopUpManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject topUpPanel;
    public Button openTopUpButton; // The button in your main game to open this panel
    public Button closeButton; // The button inside the panel to close it

    [Header("Manager References")]
    private WinAnimationManager winAnimationManager;

    void Start()
    {
        // Find manager references
        winAnimationManager = FindObjectOfType<WinAnimationManager>();

        // Hide the panel by default
        if (topUpPanel != null)
        {
            topUpPanel.SetActive(false);
        }

        // Add button listeners
        if (openTopUpButton != null)
        {
            openTopUpButton.onClick.AddListener(ShowTopUpPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideTopUpPanel);
        }
    }

    public void ShowTopUpPanel()
    {
        // Don't show if a win animation is playing
        if (IsGameplayBlockedByWinAnimation())
        {
            return;
        }

        if (topUpPanel != null)
        {
            topUpPanel.SetActive(true);
        }
    }

    public void HideTopUpPanel()
    {
        if (topUpPanel != null)
        {
            topUpPanel.SetActive(false);
        }
    }
    
    // Checks if the game is busy with a win animation
    bool IsGameplayBlockedByWinAnimation()
    {
        return winAnimationManager != null && winAnimationManager.IsWinAnimationPlaying();
    }
}