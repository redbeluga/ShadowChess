using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Managing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyHudCanvas : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private GameObject _leaveLobbyButton; 
    [SerializeField] private GameObject _createLobbyButton; 
    [SerializeField] private GameObject _joinLobbyButton; 
    [SerializeField] private GameObject _lobbyCodeInputField; 
    [SerializeField] private GameObject _lobbyInfoTextObject;
    // [SerializeField] private GameObject _lobbyPlayerInfoTextObject;
    
    private TMP_Text _lobbyInfoText;
    private bool inLobby = false;


    private void Awake()
    {
        _lobbyInfoText = _lobbyInfoTextObject.GetComponentInChildren<TMP_Text>();
    }

    private void Start()
    {
        LobbyManager.Instance.OnLeaveLobby += OnLeaveLobby;
        LobbyManager.Instance.OnJoinLobby += OnEnterLobby;
    }

    public async void OnClickCreate()
    {
        Debug.Log("Creating lobby");
        // await LobbyManager.Instance.CreateLobby();
    }

    public async void OnClickJoin()
    {
        try
        {
            if (string.IsNullOrEmpty(_inputField.text))
            {
                await LobbyManager.Instance.QuickJoinLobby();
            }
            else
            {
                await LobbyManager.Instance.JoinLobby(_inputField.text, false);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
    
    public async void OnClickLeave()
    {
        try
        {
            await LobbyManager.Instance.LeaveLobby();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public void OnEnterLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        Debug.Log("Changing Lobby Hud UI to Lobby");
        _inputField.text = "";
        
        inLobby = true;
        _leaveLobbyButton.SetActive(true);
        _lobbyInfoTextObject.SetActive(true);
        // _lobbyPlayerInfoTextObject.SetActive(true);
        
        _createLobbyButton.SetActive(false);
        _joinLobbyButton.SetActive(false);
        _lobbyCodeInputField.SetActive(false);
        
        // _lobbyInfoText.text = LobbyManager.Instance.getLobbyName();
    }

    public void OnLeaveLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        Debug.Log("Changing Lobby Hud UI to Find Lobbies");
        inLobby = false;
        _leaveLobbyButton.SetActive(false);
        _lobbyInfoTextObject.SetActive(false);
        // _lobbyPlayerInfoTextObject.SetActive(false);
        
        _createLobbyButton.SetActive(true);
        _joinLobbyButton.SetActive(true);
        _lobbyCodeInputField.SetActive(true);
    }

    public void UpdatePlayerList()
    {
        
    }
}