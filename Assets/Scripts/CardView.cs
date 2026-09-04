using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image cardImage;

    public void SetBack(CardBackDatabase backDatabase, int backIndex = 0)
    {
        if (cardImage == null)
        {
            Debug.LogError("CardView: cardImage nie jest przypisany.");
            return;
        }

        if (backDatabase == null)
        {
            Debug.LogError("CardView: backDatabase jest null.");
            return;
        }

        Sprite backSprite = backDatabase.GetBackSprite(backIndex);

        if (backSprite == null)
        {
            Debug.LogError("CardView: nie udało się pobrać sprite rewersu.");
            return;
        }

        cardImage.sprite = backSprite;
        cardImage.preserveAspect = true;
    }

    public void SetMultiplayerBack(CardBackDatabase backDatabase, int backIndex = 0)
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();

        if (cardImage == null || backDatabase == null)
            return;

        Sprite backSprite = backDatabase.GetMultiplayerBackSprite(backIndex);
        if (backSprite == null)
            return;

        cardImage.sprite = backSprite;
        cardImage.color = Color.white;
        cardImage.preserveAspect = true;
        cardImage.enabled = true;
    }
}
