using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Transporting;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour, UI_Instance
{
    public static LobbyUI Instance { get; private set; }


    [SerializeField] private Transform playerSingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button readyUpButton;
    private TextMeshProUGUI readyUpButtonText;
    [SerializeField] private bool allReady;


    private void Awake()
    {
        Instance = this;

        leaveLobbyButton.onClick.AddListener(() => { LobbyManager.Instance.LeaveLobby(); });

        readyUpButton.onClick.AddListener(OnReadyClick);
        readyUpButtonText = readyUpButton.gameObject.GetComponentInChildren<TextMeshProUGUI>();
        
        InstanceFinder.ClientManager.RegisterBroadcast<LobbyManagerNetworker.StartGameEvent>(LobbyManager_OnStartGame);
        InstanceFinder.ClientManager.RegisterBroadcast<LobbyManagerNetworker.KickPlayerEvent>(LobbyManager_OnKickedFromLobby);
    }

    private void Start()
    {
        LobbyManager.Instance.OnJoinLobby += JoinLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.OnLeaveLobby += LobbyManager_OnLeftLobbyEvent;

        Hide();
    }

    private void LobbyManager_OnLeftLobbyEvent(object sender, LobbyManager.LobbyEventArgs e)
    {
        ClearLobby();
        Hide();
        
    }

    private void LobbyManager_OnStartGame(LobbyManagerNetworker.StartGameEvent startGameEvent, Channel channel)
    {
        ClearLobby();
        Hide();
    }

    private void LobbyManager_OnKickedFromLobby(LobbyManagerNetworker.KickPlayerEvent kickedPlayerEvent, Channel channel)
    {
        if(kickedPlayerEvent.PlayerId == LobbyManager.Instance.PlayerId)
        {
            LobbyManager.Instance.LeaveLobby();
            ClearLobby();
            Hide();
        }
    }

    private void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
    {
        UpdateLobby(e.lobby);
    }

    private void JoinLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
    {
        UpdateLobby(e.lobby);
        Show();
    }

    private void UpdateLobby(Lobby lobby)
    {
        ClearLobby();
        allReady = true;

        foreach (Unity.Services.Lobbies.Models.Player player in lobby.Players)
        {
            Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
            playerSingleTransform.gameObject.SetActive(true);
            LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();
            bool playerIsReady = bool.Parse(player.Data[LobbyManager.k_playerReady].Value);

            allReady = allReady && (playerIsReady ||
                                    LobbyManager.Instance.IsLobbyHost(player.Id));

            lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                LobbyManager.Instance.IsLobbyHost() &&
                player.Id != AuthenticationService.Instance.PlayerId
            );

            lobbyPlayerSingleUI.UpdatePlayer(player, playerIsReady, LobbyManager.Instance.IsLobbyHost(player.Id));
        }

        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        if (LobbyManager.Instance.IsLobbyHost())
        {
            readyUpButton.interactable = allReady;
        }
    }

    private void ClearLobby()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    private void OnReadyClick()
    {
        if (LobbyManager.Instance.IsLobbyHost())
        {
            LobbyManager.Instance.StartGame();
        }
        else
        {
            LobbyManager.Instance.ReadyUp();
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (LobbyManager.Instance.IsLobbyHost())
        {
            readyUpButtonText.text = "Start";
        }
        else
        {
            readyUpButtonText.text = "Ready";
        }

        gameObject.SetActive(true);
    }
}