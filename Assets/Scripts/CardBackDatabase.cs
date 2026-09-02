using UnityEngine;

public class CardBackDatabase : MonoBehaviour
{
    [Header("Card back sprites")]
    [SerializeField] private Sprite[] backSprites;

    private Sprite[] resourceBackSprites;

    public int BackCount => GetAvailableBackSprites().Length;

    private Sprite[] GetAvailableBackSprites()
    {
        if (resourceBackSprites == null)
        {
            Texture2D[] textures = Resources.LoadAll<Texture2D>("CardBacks");
            resourceBackSprites = new Sprite[textures.Length];

            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                resourceBackSprites[i] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
        }

        return resourceBackSprites.Length > 0
            ? resourceBackSprites
            : backSprites ?? System.Array.Empty<Sprite>();
    }

    public Sprite GetBackSprite(int index = 0)
    {
        Sprite[] availableBackSprites = GetAvailableBackSprites();

        if (availableBackSprites.Length == 0)
        {
            Debug.LogError("CardBackDatabase: brak przypisanych rewersów kart.");
            return null;
        }

        if (index < 0 || index >= availableBackSprites.Length)
        {
            Debug.LogWarning($"CardBackDatabase: index {index} poza zakresem. Zwracam pierwszy rewers.");
            return availableBackSprites[0];
        }

        return availableBackSprites[index];
    }
}
