using System.Collections.Generic;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayersListUI : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private Transform container;            // PlayersListContainer
    [SerializeField] private GameObject rowPrefab;           // prefab PlayerRow
    [SerializeField] private TMP_Text playersCountText;      // PlayerCountText (Gracze: x/6)

    [Header("Avatars")]
    [SerializeField] private AvatarDatabase avatarDatabase;  // wspólny asset

    private const string AvatarKey = "avatarIndex";
    private readonly List<GameObject> spawned = new();
    private readonly Dictionary<int, GameObject> rowsByActorNumber = new();
    private float currentRowHeight = 108f;
    private float currentAvatarSize = 88f;

    public int AvatarCount => avatarDatabase != null && avatarDatabase.avatars != null
        ? avatarDatabase.avatars.Length
        : 0;

    private void Start()
    {
        ConfigureModernListLayout(1);
        Refresh();
    }

    public override void OnJoinedRoom() => Refresh();
    public override void OnPlayerEnteredRoom(Player newPlayer) => Refresh();
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer != null && otherPlayer.IsInactive)
        {
            StartCoroutine(RefreshAfterPlayerListUpdate());
            return;
        }

        if (otherPlayer != null &&
            rowsByActorNumber.TryGetValue(otherPlayer.ActorNumber, out GameObject row))
        {
            rowsByActorNumber.Remove(otherPlayer.ActorNumber);
            spawned.Remove(row);
            if (row != null)
            {
                row.SetActive(false);
                Destroy(row);
            }
        }

        StartCoroutine(RefreshAfterPlayerListUpdate());
    }

    public override void OnLeftRoom()
    {
        ClearRows();
        if (playersCountText != null)
            playersCountText.text = "Gracze: -/-";
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) => Refresh();

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged != null &&
            propertiesThatChanged.ContainsKey(LobbyBotRegistry.RoomPropertyKey))
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (container == null || rowPrefab == null) return;

        if (!PhotonNetwork.InRoom)
        {
            if (playersCountText != null) playersCountText.text = "Gracze: -/-";
            ClearRows();
            return;
        }

        int count = PhotonNetwork.CurrentRoom != null
            ? PhotonNetwork.CurrentRoom.PlayerCount + LobbyBotRegistry.GetBots().Count
            : 0;
        ConfigureModernListLayout(count);

        if (playersCountText != null && PhotonNetwork.CurrentRoom != null)
        {
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            playersCountText.text = $"GRACZE: {Mathf.Min(count, max)}/{max}";
        }

        ClearRows();

        int iRow = 1;
        List<Player> roomPlayers = new List<Player>(
            PhotonNetwork.CurrentRoom.Players.Values);
        roomPlayers.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        foreach (Player p in roomPlayers)
        {
            int idx = 0;
            if (p.CustomProperties != null && p.CustomProperties.ContainsKey(AvatarKey))
                idx = (int)p.CustomProperties[AvatarKey];

            string playerName = p.IsInactive
                ? $"{p.NickName} (WRÓCI ZA CHWILĘ…)"
                : p.NickName;
            SpawnRow(p.ActorNumber, iRow, playerName, idx);

            iRow++;
        }

        List<LobbyBotInfo> bots = LobbyBotRegistry.GetBots();
        for (int i = 0; i < bots.Count; i++)
        {
            LobbyBotInfo bot = bots[i];
            SpawnRow(bot.ActorNumber, iRow, bot.Name, bot.AvatarIndex);

            iRow++;
        }
    }

    private void SpawnRow(int actorNumber, int rowNumber, string displayName, int avatarIndex)
    {
        GameObject go = Instantiate(rowPrefab, container);
        spawned.Add(go);
        rowsByActorNumber[actorNumber] = go;

        TMP_Text nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
        Image avatarImg = go.transform.Find("AvatarImage")?.GetComponent<Image>();

        if (nameText != null)
        {
            nameText.text = $"{rowNumber}. {displayName}";
            nameText.fontStyle = FontStyles.Bold;
            nameText.fontWeight = FontWeight.Bold;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 28f;
            nameText.fontSizeMax = 36f;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (avatarImg != null && avatarDatabase != null &&
            avatarDatabase.avatars != null && avatarDatabase.avatars.Length > 0)
        {
            avatarIndex = Mathf.Clamp(avatarIndex, 0, avatarDatabase.avatars.Length - 1);
            avatarImg.sprite = avatarDatabase.avatars[avatarIndex];
            avatarImg.preserveAspect = true;

            if (avatarImg.transform is RectTransform avatarRect)
                avatarRect.sizeDelta = new Vector2(currentAvatarSize, currentAvatarSize);

            LayoutElement avatarLayout = avatarImg.GetComponent<LayoutElement>();
            if (avatarLayout != null)
            {
                avatarLayout.minWidth = currentAvatarSize;
                avatarLayout.preferredWidth = currentAvatarSize;
                avatarLayout.minHeight = currentAvatarSize;
                avatarLayout.preferredHeight = currentAvatarSize;
            }
        }

        if (go.transform is RectTransform rowRect)
            rowRect.sizeDelta = new Vector2(560f, currentRowHeight);

        LayoutElement rowLayout = go.GetComponent<LayoutElement>();
        if (rowLayout != null)
        {
            rowLayout.minHeight = currentRowHeight;
            rowLayout.preferredHeight = currentRowHeight;
        }

        HorizontalLayoutGroup horizontal = go.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            horizontal.padding = new RectOffset(20, 20, 10, 10);
            horizontal.spacing = 24f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
        }
    }

    private void ConfigureModernListLayout(int participantCount)
    {
        if (container is not RectTransform rect)
            return;

        if (participantCount <= 3)
        {
            currentRowHeight = 108f;
            currentAvatarSize = 88f;
        }
        else if (participantCount == 4)
        {
            currentRowHeight = 96f;
            currentAvatarSize = 78f;
        }
        else
        {
            currentRowHeight = 82f;
            currentAvatarSize = 66f;
        }

        rect.sizeDelta = new Vector2(600f, 640f);

        VerticalLayoutGroup vertical = container.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
        {
            vertical.padding = new RectOffset(20, 20, 12, 12);
            vertical.spacing = participantCount <= 3 ? 20f : participantCount == 4 ? 14f : 10f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;
        }
    }

    private void ClearRows()
    {
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null) Destroy(spawned[i]);
        spawned.Clear();
        rowsByActorNumber.Clear();
    }

    private IEnumerator RefreshAfterPlayerListUpdate()
    {
        yield return null;
        Refresh();
    }
}
