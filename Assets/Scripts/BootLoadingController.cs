using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootLoadingController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private TMP_Text continueText;
    [SerializeField] private BootLoadingLogoPulse logoPulse;
    [SerializeField] private BootLoadingSpinner chipSpinner;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "MainMenu";

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 3f;
    [SerializeField] private float loadingAnimationTime = 1f;
    [SerializeField] private float stopAnimationsDuration = 2.5f;
    [SerializeField] private float continueTextFadeInDuration = 1.2f;
    [SerializeField] private float continueUnlockDelay = 2f;
    [SerializeField] private float continueTextFadeOutDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.9f;
    [SerializeField] private float chipSpinReturnDuration = 0f;

    [Header("Network wait")]
    [SerializeField] private bool waitForPhotonReady = true;
    [SerializeField] private float maxPhotonWaitSeconds = 30f;

    [Header("Continue text pulse")]
    [SerializeField] private float continuePulseSpeed = 0.5f;
    [SerializeField] private float continueMinAlpha = 1.5f;
    [SerializeField] private float continueMaxAlpha = 1f;

    [Header("Continue text scale")]
    [SerializeField] private float continueStartScaleMultiplier = 1f;
    [SerializeField] private float continueEndScaleMultiplier = 1f;
    [SerializeField] private float continueHideScaleMultiplier = 1f;
    [SerializeField] private float continuePulseScaleAmount = 0.07f;

    private bool canContinue = false;
    private bool isPromptVisible = false;
    private bool isTransitioning = false;

    private Color continueTextBaseColor = Color.white;
    private Vector3 continueTextBaseScale = Vector3.one;
    private float continuePulseTimer = 0f;

    private void Start()
    {
        PrepareContinueText();
        StartCoroutine(BootFlow());
    }

    private void Update()
    {
        if (isPromptVisible && continueText != null && !isTransitioning)
        {
            continuePulseTimer += Time.unscaledDeltaTime;

            float alphaT = (Mathf.Cos(continuePulseTimer * continuePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            SetContinueTextAlpha(Mathf.Lerp(continueMinAlpha, continueMaxAlpha, alphaT));

            float scaleOffset = Mathf.Sin(continuePulseTimer * continuePulseSpeed * Mathf.PI * 2f) * continuePulseScaleAmount;
            SetContinueTextScaleMultiplier(continueEndScaleMultiplier + scaleOffset);
        }

        if (!canContinue || isTransitioning)
            return;

        if (WasContinueInputPressed())
        {
            StartCoroutine(ContinueToMenu());
        }
    }

    private IEnumerator BootFlow()
    {
        yield return FadeOverlay(1f, 0f, fadeInDuration);

        float timer = 0f;
        while (timer < loadingAnimationTime)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (ShouldWaitForPhoton())
        {
            float photonTimer = 0f;

            while (!PhotonNetwork.IsConnectedAndReady && photonTimer < maxPhotonWaitSeconds)
            {
                photonTimer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        yield return StopLoadingAnimationsSmoothly();
        yield return ShowContinuePrompt();

        float unlockTimer = 0f;
        while (unlockTimer < continueUnlockDelay)
        {
            unlockTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        canContinue = true;
    }

    private bool ShouldWaitForPhoton()
    {
        if (!waitForPhotonReady || PhotonNetwork.IsConnectedAndReady)
            return false;

        return PhotonNetwork.PhotonServerSettings != null &&
               PhotonNetwork.PhotonServerSettings.AppSettings != null &&
               !string.IsNullOrWhiteSpace(
                   PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime
               );
    }

    private IEnumerator ContinueToMenu()
    {
        isTransitioning = true;
        canContinue = false;
        isPromptVisible = false;

        if (chipSpinner != null)
            StartCoroutine(RestoreChipSpinSmoothly());

        yield return HideContinuePrompt();
        yield return FadeOverlay(0f, 1f, fadeOutDuration);

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator StopLoadingAnimationsSmoothly()
    {
        if (stopAnimationsDuration <= 0f)
        {
            if (logoPulse != null)
                logoPulse.SetAnimationMultiplier(0f);

            if (chipSpinner != null)
                chipSpinner.SetAnimationMultiplier(0f);

            yield break;
        }

        float time = 0f;

        while (time < stopAnimationsDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / stopAnimationsDuration);
            float multiplier = Mathf.Lerp(1f, 0f, t);

            if (logoPulse != null)
                logoPulse.SetAnimationMultiplier(multiplier);

            if (chipSpinner != null)
                chipSpinner.SetAnimationMultiplier(multiplier);

            yield return null;
        }

        if (logoPulse != null)
            logoPulse.SetAnimationMultiplier(0f);

        if (chipSpinner != null)
            chipSpinner.SetAnimationMultiplier(0f);
    }

    private IEnumerator RestoreChipSpinSmoothly()
    {
        if (chipSpinner == null)
            yield break;

        if (chipSpinReturnDuration <= 0f)
        {
            chipSpinner.SetAnimationMultiplier(1f);
            yield break;
        }

        float duration = chipSpinReturnDuration;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            chipSpinner.SetAnimationMultiplier(t);
            yield return null;
        }

        chipSpinner.SetAnimationMultiplier(1f);
    }

    private void PrepareContinueText()
    {
        if (continueText == null)
            return;

        continueText.gameObject.SetActive(false);
        continueTextBaseColor = continueText.color;
        continueTextBaseScale = continueText.rectTransform.localScale;

        SetContinueTextAlpha(0f);
        SetContinueTextScaleMultiplier(continueStartScaleMultiplier);
    }

    private IEnumerator ShowContinuePrompt()
    {
        if (continueText == null)
            yield break;

        continueText.gameObject.SetActive(true);
        continuePulseTimer = 0f;

        SetContinueTextAlpha(0f);
        SetContinueTextScaleMultiplier(continueStartScaleMultiplier);

        float duration = Mathf.Max(0.01f, continueTextFadeInDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            continuePulseTimer += Time.unscaledDeltaTime;

            float appearT = Mathf.Clamp01(time / duration);

            float alphaT = (Mathf.Cos(continuePulseTimer * continuePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            float pulseAlpha = Mathf.Lerp(continueMinAlpha, continueMaxAlpha, alphaT);
            SetContinueTextAlpha(appearT * pulseAlpha);

            float appearScale = Mathf.Lerp(continueStartScaleMultiplier, continueEndScaleMultiplier, appearT);
            float scaleOffset = Mathf.Sin(continuePulseTimer * continuePulseSpeed * Mathf.PI * 2f) * continuePulseScaleAmount;
            SetContinueTextScaleMultiplier(appearScale + scaleOffset);

            yield return null;
        }

        isPromptVisible = true;
    }

    private IEnumerator HideContinuePrompt()
    {
        if (continueText == null)
            yield break;

        float startAlpha = continueText.color.a;
        float duration = Mathf.Max(0.01f, continueTextFadeOutDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            SetContinueTextAlpha(Mathf.Lerp(startAlpha, 0f, t));
            SetContinueTextScaleMultiplier(Mathf.Lerp(continueEndScaleMultiplier, continueHideScaleMultiplier, t));

            yield return null;
        }

        SetContinueTextAlpha(0f);
        continueText.gameObject.SetActive(false);
    }

    private void SetContinueTextAlpha(float alpha)
    {
        if (continueText == null)
            return;

        Color color = continueTextBaseColor;
        color.a = alpha;
        continueText.color = color;
    }

    private void SetContinueTextScaleMultiplier(float multiplier)
    {
        if (continueText == null)
            return;

        continueText.rectTransform.localScale = continueTextBaseScale * multiplier;
    }

    private bool WasContinueInputPressed()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
                return true;
        }

        return false;
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeOverlay == null)
            yield break;

        Color color = fadeOverlay.color;
        color.a = from;
        fadeOverlay.color = color;

        if (duration <= 0f)
        {
            color.a = to;
            fadeOverlay.color = color;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            color.a = Mathf.Lerp(from, to, t);
            fadeOverlay.color = color;

            yield return null;
        }

        color.a = to;
        fadeOverlay.color = color;
    }
}
