using UnityEngine;
using UnityEngine.UI;

public class DealtCardView : MonoBehaviour
{
    [SerializeField] private Image cardImage;

    public void ShowFront(Sprite frontSprite)
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();

        if (frontSprite == null)
        {
            Debug.LogWarning("DealtCardView: frontSprite jest null.");
            return;
        }

        cardImage.sprite = frontSprite;
        cardImage.color = Color.white;
        cardImage.preserveAspect = true;
        cardImage.enabled = true;
    }
}
