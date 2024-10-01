using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour, UI_Instance
{
    public static LobbyListUI Instance { get; private set; }

    [SerializeField] private GameObject lobbySingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button joinWithCodeButton;
    [SerializeField] private Button createLobbyButton;

    private void Awake()
    {
        Instance = this;

        refreshButton.onClick.AddListener(RefreshButtonClick);
        joinWithCodeButton.onClick.AddListener(() =>
        {
            UI_InputWindow.Show_Static("Join Code", "Join", "ABCDEFGHIJKLMNOPQRSTUVXYWZ1234567890", 6,
                () =>
                {
                    // Cancel
                },
                (string joinCode) => { JoinWithCodeButtonClick(joinCode); },
                this);
        });
        createLobbyButton.onClick.AddListener(CreateLobbyButtonClick);
    }

    private void Start()
    {
        LobbyManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged;
        LobbyManager.Instance.OnQueueJoinLobby += LobbyManager_OnQueueJoinLobby;
        LobbyManager.Instance.OnLeaveLobby += LobbyManager_OnLeftLobby;
    }

    private void LobbyManager_OnLeftLobby(object sender, EventArgs e)
    {
        Show();
    }

    private void LobbyManager_OnQueueJoinLobby(object sender, EventArgs e)
    {
        LoadingAnimationUI.Instance.Show(false, true, "Joining lobby");
        Hide();
    }

    private void LobbyManager_OnLobbyListChanged(object sender, LobbyManager.OnLobbyListChangedEventArgs e)
    {
        // Debug.Log("Lobby list change event recieved");
        UpdateLobbyList(e.lobbyList);
    }

    private void UpdateLobbyList(List<Lobby> lobbyList)
    {
        ClearList();

        foreach (Lobby lobby in lobbyList)
        {
            // Debug.Log("Adding lobby");
            GameObject lobbySingleTransform = Instantiate(lobbySingleTemplate, container);
            LobbyListSingleUI lobbyListSingleUI = lobbySingleTransform.GetComponent<LobbyListSingleUI>();
            lobbyListSingleUI.UpdateLobby(lobby);
        }
    }

    private void RefreshButtonClick()
    {
        LobbyManager.Instance.RefreshLobbyList();
    }

    private void JoinWithCodeButtonClick(string joinCode)
    {
        LobbyManager.Instance.JoinLobby(joinCode, false);
    }

    public void CreateLobbyButtonClick()
    {
        LobbyCreateUI.Instance.Show(this);
        Hide();
    }
    
    private void ClearList()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    private void Hide()
    {
        ClearList();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        LobbyManager.Instance.RefreshLobbyList();
        gameObject.SetActive(true);
    }
}