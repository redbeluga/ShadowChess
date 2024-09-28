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
        // kickPlayerButton.onClick.AddListener(KickPlayer);
    }

    public void SetKickPlayerButtonVisible(bool visible, bool isHost)
    {
        kickPlayerButton.gameObject.SetActive(visible);
        isHostText.gameObject.SetActive(isHost);
    }

    public void UpdatePlayer(Unity.Services.Lobbies.Models.Player player)
    {
        this.player = player;
        playerNameText.text = player.Data[LobbyManager.k_playerName].Value;
    }

    // private void KickPlayer() {
    //     if (player != null) {
    //         LobbyManager.Instance.KickPlayer(player.Id);
    //     }
    // }
}