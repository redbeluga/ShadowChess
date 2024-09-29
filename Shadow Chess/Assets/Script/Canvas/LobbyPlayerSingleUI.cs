using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

public class LobbyPlayerSingleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Button kickPlayerButton;
    [SerializeField] private TextMeshProUGUI isHostText;

    private Unity.Services.Lobbies.Models.Player player;

    private void Awake()
    {
        kickPlayerButton.onClick.AddListener(KickPlayer);
    }

    public void SetKickPlayerButtonVisible(bool visible)
    {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    public void UpdatePlayer(Unity.Services.Lobbies.Models.Player player, bool ready, bool isHost)
    {
        this.player = player;
        string isReady = isHost ? "" : ready ? ": Ready" : ": Not Ready";
        playerNameText.text = $"{player.Data[LobbyManager.k_playerName].Value}{isReady}";
        isHostText.gameObject.SetActive(isHost);
    }

    private void KickPlayer() {
        if (player != null) {
            Debug.Log(player.Id);
            LobbyManager.Instance.KickPlayer(player.Id);
        }
    }
}