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

    private void Start() => Refresh();

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

    private void Refresh()
    {
        if (container == null || rowPrefab == null) return;

        if (!PhotonNetwork.InRoom)
        {
            if (playersCountText != null) playersCountText.text = "Gracze: -/-";
            ClearRows();
            return;
        }

        if (playersCountText != null && PhotonNetwork.CurrentRoom != null)
        {
            int count = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            playersCountText.text = $"Gracze: {count}/{max}";
        }

        ClearRows();

        int iRow = 1;
        List<Player> roomPlayers = new List<Player>(
            PhotonNetwork.CurrentRoom.Players.Values);
        roomPlayers.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        foreach (Player p in roomPlayers)
        {
            var go = Instantiate(rowPrefab, container);
            spawned.Add(go);
            rowsByActorNumber[p.ActorNumber] = go;

            var nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var avatarImg = go.transform.Find("AvatarImage")?.GetComponent<Image>();

            if (nameText != null)
                nameText.text = p.IsInactive
                    ? $"{iRow}. {p.NickName} (wróci za chwilę…)"
                    : $"{iRow}. {p.NickName}";

            int idx = 0;
            if (p.CustomProperties != null && p.CustomProperties.ContainsKey(AvatarKey))
                idx = (int)p.CustomProperties[AvatarKey];

            if (avatarImg != null && avatarDatabase != null && avatarDatabase.avatars != null && avatarDatabase.avatars.Length > 0)
            {
                idx = Mathf.Clamp(idx, 0, avatarDatabase.avatars.Length - 1);
                avatarImg.sprite = avatarDatabase.avatars[idx];
            }

            iRow++;
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
