using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WinAnimationManager : MonoBehaviour
{
    [System.Serializable]
    public class FloatingConfig
    {
        public float floatSpeed = 2f;
        public float floatAmplitude = 15f;
    }

    // --- HEADERS ---

    [Header("--- Big Win Setup ---")]
    public GameObject normalWinPopup; 
    public Transform bigWinBackground; 
    public FloatingConfig bigWinBgFloat; 
    public Transform bigWinTitle;      
    public FloatingConfig bigWinTitleFloat; 
    public Transform bigWinEffect; 
    public TextMeshProUGUI normalWinAmountText;
    public FloatingConfig bigWinTextFloat; 
    public Button normalCloseButton;

    [Header("--- Entry Animation Settings ---")]
    [Tooltip("The starting scale for Title and Text (e.g. 100)")]
    public float hugeScaleStart = 100f; 
    public float entryScaleDuration = 0.5f;
    public float titleShakeDuration = 0.5f;
    public float titleShakeIntensity = 20f;
    
    [Header("--- Epic Win Setup ---")]
    public GameObject epicWinPopup;   
    public Transform epicWinBackground; 
    public FloatingConfig epicWinBgFloat; 
    public Transform epicWinTitle;      
    public FloatingConfig epicWinTitleFloat; 
    
    public Transform epicWinEffect;
    // --- NEW: Extra Effect Slot ---
    public Transform epicWinEffect2; 
    // ------------------------------

    public TextMeshProUGUI epicWinAmountText;
    public FloatingConfig epicWinTextFloat; 
    public Button epicCloseButton;

    [Header("--- Animation Settings (Global) ---")]
    public float popInSpeed = 10f;
    public float settleScale = 0.9f;
    
    [Header("Effect Breathing Settings")]
    public float breathingSpeed = 3f;
    public float breathingScaleMin = 0.95f;
    public float breathingScaleMax = 1.05f;
    
    [Header("Background Rotation (Big Win Only)")]
    public float bgRotationAngle = 90f;
    public float bgRotationInterval = 3f; 

    [Header("Text Pulse Effect (On Finish)")]
    public float textFinishPulseScale = 1.5f;
    public float textPulseDuration = 0.3f;
    public float countingShakeIntensity = 10f; 

    [Header("Count Up Settings")]
    public float countUpDuration = 2f;
    public float countUpSpeed = 1f; 
    public AnimationCurve countSpeedCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Header("Sound Effects")]
    public AudioClip normalWinSound;  
    public AudioClip epicWinSound;    
    public AudioClip winBGM;          
    public AudioClip countUpSound; 
    public AudioClip closeSound;
    
    [Header("Pause Settings")]
    public bool pauseGameDuringAnimation = true;
    
    private AudioSource sfxAudioSource;       
    private AudioSource bgmAudioSource;       
    private AudioSource countAudioSource;   

    // Coroutines
    private Coroutine countCoroutine;
    private Coroutine bigWinAnimCoroutine;
    private Coroutine epicWinAnimCoroutine;
    private Coroutine bgRotationCoroutine;
    private Coroutine textFloatCoroutine; 
    private Coroutine effectBreathingCoroutine; 
    private Coroutine effectBreathingCoroutine2; // --- NEW Tracker ---

    // State
    private float currentDisplayedAmount = 0f, currentWinAmount = 0f;
    private bool isEpicWin = false;
    private TextMeshProUGUI activeWinText;
    private Vector3 activeTextOriginalLocalPos; 
    private Vector3 activeTitleOriginalLocalPos; 
    private Vector3 activeBgOriginalLocalPos;
    
    private bool wasAutoSpinning = false;
    private SlotMachine4D slotMachine;
    private AutoSpinPanel autoSpinPanel;
    private Credit creditSystem;
    private NumberPadManager numberPadManager;
    private bool isAnimationPlaying = false;

    void Start()
    {
        InitializeAudioSources();
        
        slotMachine = FindObjectOfType<SlotMachine4D>();
        autoSpinPanel = FindObjectOfType<AutoSpinPanel>();
        creditSystem = FindObjectOfType<Credit>();
        numberPadManager = FindObjectOfType<NumberPadManager>();
        
        InitializeUI();
    }

    void InitializeAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length < 3)
        {
            while (sources.Length < 3)
            {
                gameObject.AddComponent<AudioSource>();
                sources = GetComponents<AudioSource>();
            }
        }
        sfxAudioSource = sources[0];
        bgmAudioSource = sources[1];
        countAudioSource = sources[2];

        sfxAudioSource.playOnAwake = false;
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.loop = true; 
        countAudioSource.playOnAwake = false;
        countAudioSource.loop = false; 
    }

    void InitializeUI()
    {
        if (normalWinPopup != null) normalWinPopup.SetActive(false);
        if (epicWinPopup != null) epicWinPopup.SetActive(false);
        
        if (normalCloseButton != null)
        {
            normalCloseButton.onClick.RemoveAllListeners();
            normalCloseButton.onClick.AddListener(CloseWinPopup);
            normalCloseButton.gameObject.SetActive(false);
        }
        
        if (epicCloseButton != null)
        {
            epicCloseButton.onClick.RemoveAllListeners();
            epicCloseButton.onClick.AddListener(CloseWinPopup);
            epicCloseButton.gameObject.SetActive(false);
        }
    }

    public void ShowWinAnimation(float totalWinAmount) => ShowWinAnimation(totalWinAmount, false);
    public void ShowEpicWinAnimation(float totalWinAmount) => ShowWinAnimation(totalWinAmount, true);

    void ShowWinAnimation(float totalWinAmount, bool isEpic)
    {
        isAnimationPlaying = true;
        if (pauseGameDuringAnimation) PauseBackgroundActivities();
        
        isEpicWin = isEpic;
        currentWinAmount = totalWinAmount; 
        
        StopAllAnimationCoroutines();
        
        currentDisplayedAmount = 0f;
        SetupWinDisplay(isEpic); 
        
        PlayWinSound(isEpic); 
        
        Debug.Log($"🎉 WIN ANIMATION: Displaying TOTAL win amount: {totalWinAmount}");
    }

    void StopAllAnimationCoroutines()
    {
        if (countCoroutine != null) StopCoroutine(countCoroutine);
        if (bigWinAnimCoroutine != null) StopCoroutine(bigWinAnimCoroutine);
        if (epicWinAnimCoroutine != null) StopCoroutine(epicWinAnimCoroutine);
        if (bgRotationCoroutine != null) StopCoroutine(bgRotationCoroutine);
        if (textFloatCoroutine != null) StopCoroutine(textFloatCoroutine);
        if (effectBreathingCoroutine != null) StopCoroutine(effectBreathingCoroutine);
        if (effectBreathingCoroutine2 != null) StopCoroutine(effectBreathingCoroutine2); // --- NEW ---
    }

    void SetupWinDisplay(bool isEpic)
    {
        if (normalCloseButton != null) normalCloseButton.gameObject.SetActive(false);
        if (epicCloseButton != null) epicCloseButton.gameObject.SetActive(false);

        if (isEpic)
        {
            if (normalWinPopup != null) normalWinPopup.SetActive(false);
            if (epicWinPopup != null)
            {
                epicWinPopup.SetActive(true);
                activeWinText = epicWinAmountText;
                
                ResetTransform(epicWinBackground);
                ResetTransform(epicWinTitle);
                ResetTransform(epicWinEffect); 
                ResetTransform(epicWinEffect2); // --- NEW ---
                
                if (activeWinText != null)
                {
                    activeWinText.transform.localScale = Vector3.one * hugeScaleStart;
                    activeWinText.text = ""; 
                }
                
                epicWinAnimCoroutine = StartCoroutine(AnimateEpicWinElements());
            }
        }
        else // Big Win
        {
            if (epicWinPopup != null) epicWinPopup.SetActive(false);
            if (normalWinPopup != null)
            {
                normalWinPopup.SetActive(true);
                activeWinText = normalWinAmountText;

                PrepareEntryElement(bigWinBackground, false); 
                PrepareEntryElement(bigWinEffect, false);     
                PrepareEntryElement(bigWinTitle, true);       
                
                if (activeWinText != null)
                {
                    activeWinText.transform.localScale = Vector3.one * hugeScaleStart;
                    activeWinText.text = ""; 
                }

                bigWinAnimCoroutine = StartCoroutine(AnimateBigWinElements());
            }
        }

        if (activeWinText != null)
        {
            activeTextOriginalLocalPos = activeWinText.transform.localPosition;
        }
        
        Transform currentTitle = isEpic ? epicWinTitle : bigWinTitle;
        Transform currentBg = isEpic ? epicWinBackground : bigWinBackground;

        if (currentTitle != null) activeTitleOriginalLocalPos = currentTitle.localPosition;
        if (currentBg != null) activeBgOriginalLocalPos = currentBg.localPosition;
    }

    void PrepareEntryElement(Transform t, bool isZoomElement)
    {
        if (t == null) return;
        if (isZoomElement)
        {
            t.localScale = Vector3.one * hugeScaleStart;
            SetAlpha(t, 1f);
        }
        else
        {
            t.localScale = Vector3.one; 
            SetAlpha(t, 0f);
        }
        t.localRotation = Quaternion.identity;
    }

    void ResetTransform(Transform t)
    {
        if (t != null)
        {
            t.localScale = Vector3.zero;
            t.localRotation = Quaternion.identity;
            SetAlpha(t, 1f); 
        }
    }

    void SetAlpha(Transform t, float alpha)
    {
        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
        CanvasGroup cg = t.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = alpha;
        }
    }

    // --- Complex Animations ---

    IEnumerator AnimateBigWinElements()
    {
        StartCoroutine(FadeIn(bigWinBackground));
        StartCoroutine(FadeIn(bigWinEffect));

        bgRotationCoroutine = StartCoroutine(RotateBackgroundClockwise(bigWinBackground));
        if (bigWinEffect != null)
            effectBreathingCoroutine = StartCoroutine(PopInAndBreathe(bigWinEffect, false)); 

        StartCoroutine(FloatObject(bigWinBackground, bigWinBgFloat));

        yield return StartCoroutine(ScaleFromHugeToNormal(bigWinTitle, entryScaleDuration));
        yield return StartCoroutine(ShakeObject(bigWinTitle, activeTitleOriginalLocalPos, titleShakeDuration, titleShakeIntensity));
        StartCoroutine(FloatObject(bigWinTitle, bigWinTitleFloat));

        if (activeWinText != null)
        {
            activeWinText.text = "0"; 
            yield return StartCoroutine(ScaleFromHugeToNormal(activeWinText.transform, entryScaleDuration));
        }

        countCoroutine = StartCoroutine(CountUpAnimation());
    }

    IEnumerator AnimateEpicWinElements()
    {
        yield return new WaitForSeconds(0.5f);

        if (epicWinBackground != null)
        {
            // Effect 1
            if (epicWinEffect != null)
            {
                epicWinEffect.localScale = Vector3.one;
                effectBreathingCoroutine = StartCoroutine(PopInAndBreathe(epicWinEffect, true)); 
            }
            // --- NEW: Effect 2 ---
            if (epicWinEffect2 != null)
            {
                epicWinEffect2.localScale = Vector3.one;
                effectBreathingCoroutine2 = StartCoroutine(PopInAndBreathe(epicWinEffect2, true)); 
            }
            // ---------------------

            yield return StartCoroutine(ScaleFromHugeToNormal(epicWinBackground, entryScaleDuration));
            yield return StartCoroutine(ShakeObject(epicWinBackground, activeBgOriginalLocalPos, titleShakeDuration, titleShakeIntensity));
            StartCoroutine(FloatObject(epicWinBackground, epicWinBgFloat));
        }

        yield return new WaitForSeconds(0.5f);

        if (epicWinTitle != null)
        {
            yield return StartCoroutine(ScaleFromHugeToNormal(epicWinTitle, entryScaleDuration));
            yield return StartCoroutine(ShakeObject(epicWinTitle, activeTitleOriginalLocalPos, titleShakeDuration, titleShakeIntensity));
            StartCoroutine(FloatObject(epicWinTitle, epicWinTitleFloat));
        }

        yield return new WaitForSeconds(0.5f);

        if (activeWinText != null)
        {
            activeWinText.text = "0"; 
            yield return StartCoroutine(ScaleFromHugeToNormal(activeWinText.transform, entryScaleDuration));
        }

        countCoroutine = StartCoroutine(CountUpAnimation());
    }

    // --- Helper Animation Logic ---

    IEnumerator FadeIn(Transform target)
    {
        if (target == null) yield break;
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * 3f; 
            SetAlpha(target, Mathf.Lerp(0f, 1f, timer));
            yield return null;
        }
        SetAlpha(target, 1f);
    }

    IEnumerator ScaleFromHugeToNormal(Transform target, float duration)
    {
        if (target == null) yield break;
        Vector3 startScale = Vector3.one * hugeScaleStart;
        Vector3 endScale = Vector3.one;
        
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime / duration;
            target.localScale = Vector3.Lerp(startScale, endScale, timer);
            yield return null;
        }
        target.localScale = endScale;
    }

    IEnumerator ShakeObject(Transform target, Vector3 originalPos, float duration, float intensity)
    {
        if (target == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity),
                0);
            target.localPosition = originalPos + randomOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localPosition = originalPos;
    }

    IEnumerator PopInAndBreathe(Transform target, bool doPopIn)
    {
        if (target == null) yield break;

        if (doPopIn)
        {
            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime * popInSpeed;
                float scale = Mathf.Lerp(0f, 1f, timer);
                target.localScale = Vector3.one * scale;
                yield return null;
            }
        }
        else
        {
            target.localScale = Vector3.one; 
        }

        float breatheTimer = 0f;
        while (true)
        {
            breatheTimer += Time.deltaTime * breathingSpeed;
            float scale = Mathf.Lerp(breathingScaleMin, breathingScaleMax, (Mathf.Sin(breatheTimer) + 1f) / 2f);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
    }

    IEnumerator FloatObject(Transform target, FloatingConfig config)
    {
        if (target == null) yield break;

        Vector3 startPos = target.localPosition;
        float time = 0f;

        while (true)
        {
            time += Time.deltaTime * config.floatSpeed;
            float newY = startPos.y + Mathf.Sin(time) * config.floatAmplitude;
            target.localPosition = new Vector3(startPos.x, newY, startPos.z);
            yield return null;
        }
    }

    IEnumerator RotateBackgroundClockwise(Transform target)
    {
        if (target == null) yield break;

        while (true)
        {
            yield return new WaitForSeconds(bgRotationInterval);

            Quaternion startRot = target.localRotation;
            Quaternion endRot = target.localRotation * Quaternion.Euler(0, 0, -bgRotationAngle); 
            
            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime * 2f; 
                target.localRotation = Quaternion.Slerp(startRot, endRot, timer);
                yield return null;
            }
            target.localRotation = endRot;
        }
    }

    // --- Count Up with High Intensity Shake ---

    IEnumerator CountUpAnimation()
    {
        if (countUpSound != null)
        {
            countAudioSource.clip = countUpSound;
            countAudioSource.loop = true; 
            countAudioSource.Play();
        }

        if (activeWinText != null)
        {
            FloatingConfig textConfig = isEpicWin ? epicWinTextFloat : bigWinTextFloat;
            textFloatCoroutine = StartCoroutine(FloatObject(activeWinText.transform, textConfig));
        }
        
        float startTime = Time.time;
        float startAmount = currentDisplayedAmount;
        float safeSpeed = (countUpSpeed <= 0) ? 1f : countUpSpeed;
        float duration = countUpDuration / safeSpeed; 
        
        while (Time.time - startTime < duration)
        {
            float progress = (Time.time - startTime) / duration;
            float curvedProgress = countSpeedCurve.Evaluate(progress);
            currentDisplayedAmount = Mathf.Lerp(startAmount, currentWinAmount, curvedProgress);
            
            if (activeWinText != null)
            {
                activeWinText.text = currentDisplayedAmount.ToString("F0");
                
                Vector3 currentPos = activeWinText.transform.localPosition;
                float shakeX = Random.Range(-countingShakeIntensity, countingShakeIntensity);
                float shakeY = Random.Range(-countingShakeIntensity, countingShakeIntensity);
                
                activeWinText.transform.localPosition = new Vector3(
                    activeTextOriginalLocalPos.x + shakeX, 
                    currentPos.y + shakeY, 
                    currentPos.z
                );
            }
            yield return null;
        }
        
        currentDisplayedAmount = currentWinAmount;
        if (activeWinText != null)
        {
            activeWinText.text = currentDisplayedAmount.ToString("F0");
            Vector3 currentPos = activeWinText.transform.localPosition;
            activeWinText.transform.localPosition = new Vector3(activeTextOriginalLocalPos.x, currentPos.y, currentPos.z);
            
            StartCoroutine(PulseAndFinalShake(activeWinText.transform));
        }
        
        countAudioSource.Stop();
        countAudioSource.loop = false; 

        ShowCloseButton();
        countCoroutine = null;
    }

    IEnumerator PulseAndFinalShake(Transform textTransform)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 bigScale = originalScale * textFinishPulseScale;
        
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime / (textPulseDuration / 2);
            textTransform.localScale = Vector3.Lerp(originalScale, bigScale, timer);
            yield return null;
        }
        
        timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime / (textPulseDuration / 2);
            textTransform.localScale = Vector3.Lerp(bigScale, originalScale, timer);
            yield return null;
        }
        textTransform.localScale = originalScale;

        // Final Shake
        yield return StartCoroutine(ShakeObject(textTransform, activeTextOriginalLocalPos, 0.5f, 10f));
    }

    void PlayWinSound(bool isEpic)
    {
        AudioClip sfx = isEpic ? epicWinSound : normalWinSound;
        if (sfx != null) sfxAudioSource.PlayOneShot(sfx);
        if (winBGM != null)
        {
            bgmAudioSource.clip = winBGM;
            bgmAudioSource.Play();
        }
    }

    void ShowCloseButton()
    {
        if (isEpicWin && epicCloseButton != null) epicCloseButton.gameObject.SetActive(true);
        else if (normalCloseButton != null) normalCloseButton.gameObject.SetActive(true);
    }

    public void CloseWinPopup()
    {
        isAnimationPlaying = false;
        ResumeBackgroundActivities();
        
        if (closeSound != null) sfxAudioSource.PlayOneShot(closeSound);
        
        if (normalWinPopup != null) normalWinPopup.SetActive(false);
        if (epicWinPopup != null) epicWinPopup.SetActive(false);
        
        StopAllAnimationCoroutines();
        
        if (activeWinText != null)
        {
            activeWinText.transform.localPosition = activeTextOriginalLocalPos;
        }

        if (normalCloseButton != null) normalCloseButton.gameObject.SetActive(false);
        if (epicCloseButton != null) epicCloseButton.gameObject.SetActive(false);
        
        bgmAudioSource.Stop();
        countAudioSource.Stop();
    }

    void PauseBackgroundActivities()
    {
        if (autoSpinPanel != null)
        {
            wasAutoSpinning = autoSpinPanel.IsAutoSpinning();
            if (wasAutoSpinning) autoSpinPanel.PauseAutoSpin();
        }
        
        if (slotMachine != null && slotMachine.spinButton != null)
            slotMachine.spinButton.interactable = false;
        
        if (creditSystem != null)
        {
            if (creditSystem.addButton != null) creditSystem.addButton.interactable = false;
            if (creditSystem.subtractButton != null) creditSystem.subtractButton.interactable = false;
        }
        
        if (numberPadManager != null && numberPadManager.numberPadPanel != null)
            numberPadManager.numberPadPanel.SetActive(false);
            
        if (autoSpinPanel != null && autoSpinPanel.autoSpinPanel != null)
            autoSpinPanel.autoSpinPanel.SetActive(false);
            
        Debug.Log("🎯 Game paused for win animation");
    }

    void ResumeBackgroundActivities()
    {
        if (!pauseGameDuringAnimation) return;
        
        if (autoSpinPanel != null && wasAutoSpinning)
            autoSpinPanel.ResumeAutoSpin();
        
        if (slotMachine != null) slotMachine.UpdateSpinButtonState();
        if (creditSystem != null) creditSystem.UpdateButtonStates();
        
        Debug.Log("🎯 Game resumed after win animation");
    }

    public bool IsWinAnimationPlaying() => isAnimationPlaying;
    public void HideWinPopup() => CloseWinPopup();
    public float GetCurrentWinAmount() => currentWinAmount;
    public bool IsEpicWin() => isEpicWin;
}