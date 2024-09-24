using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyHudCanvas : MonoBehaviour
{
    private LobbyManager _lobbyManager;

    [SerializeField] private TMP_InputField _inputField;

    private void OnGUI()
    {
        _lobbyManager = FindObjectOfType<LobbyManager>();
    }

    public void OnClickCreate()
    {
        Debug.Log("Creating relay");
        _lobbyManager.CreateRelay();
    }

    public void OnClickJoin()
    {
        Debug.Log("Joining relay: " + _inputField.text);
        _lobbyManager.JoinRelay(_inputField.text);
        _inputField.text = "";
    }
}