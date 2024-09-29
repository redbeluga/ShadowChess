using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadingAnimationUI : MonoBehaviour
{
    public static LoadingAnimationUI Instance { get; private set; }
    
    private GameObject animation;
    
    [SerializeField] private TextMeshProUGUI loadingTextObject;
    private bool textShouldLoad;
    private string loadingText;
    private float timer;
    private float count;

    private void Awake()
    {
        Instance = this;
        
        gameObject.SetActive(false);
    }

    private void Start()
    {
        LobbyManager.Instance.OnJoinLobby += OnEnd;
    }

    private void Update()
    {
        if (textShouldLoad)
        {
            timer += Time.deltaTime;
            if (timer >= 0.5f)
            {
                count = (count + 1) % 4;
                if (count == 0)
                {
                    loadingTextObject.text = loadingText;
                }
                else
                {
                    loadingTextObject.text += ".";
                }
        
                timer = 0;
            }
        }
    }

    private void OnEnd(object sender, EventArgs e)
    {
        Hide();
    }


    public void Show(bool animationIsOn, bool textShouldLoad, string loadingText)
    {
        this.textShouldLoad = textShouldLoad;
        this.loadingText = loadingText;

        if (textShouldLoad)
        {
            timer = 0;
            count = 0;
            loadingTextObject.text = loadingText;
        }
        
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
