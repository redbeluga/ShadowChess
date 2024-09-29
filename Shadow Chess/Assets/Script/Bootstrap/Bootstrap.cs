using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Managing;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private LobbyManager _lobbyManager;
    [SerializeField] private LobbyManagerNetworker _lobbyManagerNetworker;

    private void Awake()
    {
        // DontDestroyOnLoad(_networkManager.gameObject); // done in networkmanager inspector
        DontDestroyOnLoad(_lobbyManager.gameObject);
        // _lobbyManager.gameObject.SetActive(true);
        // Debug.Log("ENABLE MANAGER");
        
        LoadLobby();
    }

    void LoadLobby()
    {
        // Debug.Log("Loading MainMenu");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }
}
