using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class LobbyDebugFakePlayers : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI playersListText;   // PlayersListText
    [SerializeField] private TextMeshProUGUI playersCountText;  // PlayerCountText (Gracze: x/6)

    [Header("Debug (Editor/Host only)")]
    [SerializeField] private int maxPlayers = 6;

    private readonly List<string> bots = new List<string>();
    private Button addBotButton;

    public static int BotCount { get; private set; }

    private void Start()
    {
        BotCount = 0;
        CreateAddBotButton();
        RefreshUI();
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom) return;

        if (!PhotonNetwork.IsMasterClient) return;
        RefreshUI();
    }

    public void AddBot()
    {
        int real = PhotonNetwork.CurrentRoom.PlayerCount;
        int freeSlotsForBots = Mathf.Max(0, maxPlayers - real);
        if (bots.Count >= freeSlotsForBots) return;

        bots.Add("Bot_" + Random.Range(100, 999));
        BotCount = bots.Count;
        RefreshUI();
    }

    private void RemoveBot()
    {
        if (bots.Count == 0) return;
        bots.RemoveAt(bots.Count - 1);
        BotCount = bots.Count;
    }

    private void CreateAddBotButton()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        Button startButton = null;
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.text.ToUpperInvariant().Contains("ROZPOCZNIJ"))
            {
                startButton = button;
                break;
            }
        }

        if (startButton == null)
            return;

        addBotButton = Instantiate(startButton, startButton.transform.parent);
        addBotButton.name = "AddTestBotButton";
        RectTransform rect = addBotButton.transform as RectTransform;
        if (rect != null)
            rect.anchoredPosition = (startButton.transform as RectTransform).anchoredPosition + Vector2.up * 125f;

        TMP_Text buttonLabel = addBotButton.GetComponentInChildren<TMP_Text>(true);
        if (buttonLabel != null)
            buttonLabel.text = "DODAJ BOTA";

        addBotButton.onClick.RemoveAllListeners();
        addBotButton.onClick.AddListener(AddBot);
        PokerButtonTheme.ApplyTo(addBotButton);
    }

    private void RefreshUI()
    {
        // COUNT
        if (playersCountText != null)
        {
            int real = PhotonNetwork.CurrentRoom.PlayerCount;
            int shown = Mathf.Clamp(real + bots.Count, 0, maxPlayers);
            playersCountText.text = $"Gracze: {shown}/{maxPlayers}";
        }

        // LISTA
        if (playersListText != null)
        {
            var sb = new StringBuilder();
            int i = 1;

            foreach (var p in PhotonNetwork.PlayerList)
                sb.AppendLine($"{i++}. {p.NickName}");

            foreach (var b in bots)
                sb.AppendLine($"{i++}. {b}");

            if (i == 1) sb.AppendLine("(brak graczy)");

            playersListText.text = sb.ToString();
        }
    }
}
