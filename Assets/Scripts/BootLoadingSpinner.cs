using UnityEngine;

public class BootLoadingSpinner : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;

    private float animationMultiplier = 1f;

    private void Awake()
    {
        RectTransform rectTransform = transform as RectTransform;

        if (rectTransform == null)
            return;

        // Żeton był obracany względem prawego dolnego rogu. Przenosimy pivot
        // na środek, jednocześnie zachowując jego pozycję na ekranie.
        Vector2 oldPivot = rectTransform.pivot;
        Vector2 centeredPivot = new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition += Vector2.Scale(
            centeredPivot - oldPivot,
            rectTransform.rect.size
        );
        rectTransform.pivot = centeredPivot;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, -rotationSpeed * animationMultiplier * Time.unscaledDeltaTime);
    }

    public void SetAnimationMultiplier(float value)
    {
        animationMultiplier = Mathf.Clamp01(value);
    }
}
