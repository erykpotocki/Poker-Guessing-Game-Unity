using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MobileInputFieldUX : MonoBehaviour
{
    private readonly List<TMP_InputField> fields = new();
    private readonly Dictionary<RectTransform, Vector2> basePositions = new();
    private Canvas parentCanvas;
    private float currentKeyboardLift;

    public void Configure(float initialLift, params TMP_InputField[] inputFields)
    {
        fields.Clear();
        basePositions.Clear();
        parentCanvas = GetComponentInParent<Canvas>();

        foreach (TMP_InputField field in inputFields)
        {
            if (field == null || fields.Contains(field))
                continue;

            fields.Add(field);
            RectTransform rect = field.transform as RectTransform;
            if (rect == null)
                continue;

            Vector2 liftedPosition = rect.anchoredPosition + Vector2.up * initialLift;
            rect.anchoredPosition = liftedPosition;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.contentType = TMP_InputField.ContentType.Custom;
            field.inputType = TMP_InputField.InputType.Standard;
            field.richText = false;
        }

        BuildSharedLiftGroup();
    }

    private void BuildSharedLiftGroup()
    {
        if (parentCanvas == null)
            return;

        List<RectTransform> formParts = new List<RectTransform>();
        foreach (TMP_InputField field in fields)
        {
            if (field != null && field.transform is RectTransform fieldRect)
                formParts.Add(fieldRect);
        }

        foreach (RectTransform rect in parentCanvas.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect == null || formParts.Contains(rect))
                continue;

            bool isAvatar = rect.name == "AvatarPreview";
            bool isRollButton = false;
            Button button = rect.GetComponent<Button>();
            if (button != null)
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                isRollButton = label != null &&
                    label.text.Trim().Equals("Losuj", System.StringComparison.OrdinalIgnoreCase);
            }

            if (isAvatar || isRollButton)
                formParts.Add(rect);
        }

        if (formParts.Count == 0)
            return;

        Transform existing = parentCanvas.transform.Find("__MobileFormLiftGroup");
        RectTransform groupRect;
        if (existing is RectTransform existingRect)
        {
            groupRect = existingRect;
        }
        else
        {
            GameObject groupObject = new GameObject("__MobileFormLiftGroup", typeof(RectTransform));
            groupRect = groupObject.GetComponent<RectTransform>();
            groupRect.SetParent(parentCanvas.transform, false);
            groupRect.anchorMin = Vector2.zero;
            groupRect.anchorMax = Vector2.one;
            groupRect.offsetMin = Vector2.zero;
            groupRect.offsetMax = Vector2.zero;
            groupRect.pivot = new Vector2(0.5f, 0.5f);
        }

        int earliestSibling = groupRect.GetSiblingIndex();
        foreach (RectTransform part in formParts)
        {
            if (part.parent == parentCanvas.transform)
                earliestSibling = Mathf.Min(earliestSibling, part.GetSiblingIndex());
            part.SetParent(groupRect, true);
        }
        groupRect.SetSiblingIndex(earliestSibling);
        groupRect.anchoredPosition = Vector2.zero;
        basePositions[groupRect] = Vector2.zero;
    }

    private void LateUpdate()
    {
        TMP_InputField focusedField = null;
        foreach (TMP_InputField field in fields)
        {
            if (field != null && field.isFocused)
            {
                focusedField = field;
                break;
            }
        }

        float targetLift = focusedField != null
            ? CalculateKeyboardLift(focusedField)
            : 0f;
        currentKeyboardLift = Mathf.Lerp(
            currentKeyboardLift, targetLift, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));

        foreach (KeyValuePair<RectTransform, Vector2> pair in basePositions)
        {
            if (pair.Key != null)
                pair.Key.anchoredPosition = pair.Value + Vector2.up * currentKeyboardLift;
        }
    }

    private float CalculateKeyboardLift(TMP_InputField focusedField)
    {
        Rect keyboardArea = TouchScreenKeyboard.area;
        float keyboardTop = keyboardArea.height > 1f
            ? keyboardArea.yMax
            : Screen.height * 0.46f;

        RectTransform fieldRect = focusedField.transform as RectTransform;
        if (fieldRect == null)
            return 0f;

        Vector3[] corners = new Vector3[4];
        fieldRect.GetWorldCorners(corners);
        Camera camera = parentCanvas != null &&
            parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;
        float canvasScale = parentCanvas != null
            ? Mathf.Max(0.01f, parentCanvas.scaleFactor)
            : 1f;
        float fieldBottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y;
        float baseFieldBottom = fieldBottom - currentKeyboardLift * canvasScale;
        float requiredPixels = Mathf.Max(0f, keyboardTop + Screen.height * 0.035f - baseFieldBottom);
        return requiredPixels / canvasScale;
    }
}
