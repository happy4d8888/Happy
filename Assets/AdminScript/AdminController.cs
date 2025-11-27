using UnityEngine;
using UnityEngine.UI;

public class AdminController : MonoBehaviour
{
    public InputField jackpotInput;
    public InputField chanceInput;
    public Button saveButton;
    public Button resetButton;

    void Start()
    {
        saveButton.onClick.AddListener(SaveSettings);
        resetButton.onClick.AddListener(ResetSettings);
    }

    void SaveSettings()
    {
        string jackpot = jackpotInput.text;
        float chance;

        if (jackpot.Length != 4 || !int.TryParse(jackpot, out _))
        {
            Debug.LogWarning("Jackpot number must be a 4-digit number.");
            return;
        }

        if (!float.TryParse(chanceInput.text, out chance) || chance < 0f || chance > 100f)
        {
            Debug.LogWarning("Chance must be between 0 and 100.");
            return;
        }

        PlayerPrefs.SetString("JackpotNumber", jackpot);
        PlayerPrefs.SetFloat("JackpotChance", chance);
        PlayerPrefs.SetInt("JackpotUsed", 0); // 0 = not used yet
        PlayerPrefs.Save();

        Debug.Log($"Saved Jackpot: {jackpot}, Chance: {chance}%, (Usable once)");
    }

    void ResetSettings()
    {
        PlayerPrefs.DeleteKey("JackpotNumber");
        PlayerPrefs.DeleteKey("JackpotChance");
        PlayerPrefs.DeleteKey("JackpotUsed");
        PlayerPrefs.Save();

        Debug.Log("Jackpot settings reset.");
    }
}
