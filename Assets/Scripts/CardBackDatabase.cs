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
                    GetCardArtworkRect(texture),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }
        }

        return resourceBackSprites.Length > 0
            ? resourceBackSprites
            : backSprites ?? System.Array.Empty<Sprite>();
    }

    private static Rect GetCardArtworkRect(Texture2D texture)
    {
        float left = 0f;
        float bottom = 0f;
        float width = 1f;
        float height = 1f;

        if (texture.name.Contains("Ornate"))
        {
            left = 0.105f;
            bottom = 0.115f;
            width = 0.79f;
            height = 0.77f;
        }
        else if (texture.name.Contains("RedDiamond"))
        {
            left = 0.135f;
            bottom = 0.125f;
            width = 0.73f;
            height = 0.75f;
        }
        else if (texture.name.Contains("Suits"))
        {
            left = 0.07f;
            bottom = 0.045f;
            width = 0.86f;
            height = 0.91f;
        }

        return new Rect(
            texture.width * left,
            texture.height * bottom,
            texture.width * width,
            texture.height * height
        );
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
