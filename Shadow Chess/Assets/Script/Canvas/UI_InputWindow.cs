/*
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UI_InputWindow : MonoBehaviour
{
    private static UI_InputWindow instance;
    private static UI_Instance prevInstance;

    private Button okBtn;
    private Action onEsc;
    private Action<string> onOk;
    private TMP_InputField inputField;

    private void Awake()
    {
        instance = this;

        okBtn = transform.Find("okBtn").GetComponent<Button>();
        inputField = transform.Find("inputField").GetComponent<TMP_InputField>();

        Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            prevInstance.Show();
            onOk(inputField.text);
            Hide();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            prevInstance.Show();
            onEsc();
            Hide();
        }
    }

    private void Show(string titleString, string buttonText, string validCharacters, int characterLimit, Action onEsc, Action<string> onOk,
        UI_Instance uiInstance)
    {
        gameObject.SetActive(true);
        prevInstance = uiInstance;
        transform.SetAsLastSibling();
        okBtn.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;

        inputField.placeholder.GetComponent<TextMeshProUGUI>().text = titleString;

        inputField.characterLimit = characterLimit;
        inputField.onValidateInput = (string text, int charIndex, char addedChar) =>
        {
            return ValidateChar(validCharacters, addedChar);
        };
        inputField.Select();

        okBtn.onClick.AddListener(() =>
        {
            onOk(inputField.text);
            prevInstance.Show();
            Hide();
        });

        this.onEsc = onEsc;
        this.onOk = onOk;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private char ValidateChar(string validCharacters, char addedChar)
    {
        if (validCharacters.IndexOf(addedChar) != -1)
        {
            // Valid
            return addedChar;
        }
        else if (validCharacters.IndexOf(char.ToUpper(addedChar)) != -1)
        {
            return char.ToUpper(addedChar);
        }
        else
        {
            // Invalid
            return '\0';
        }
    }

    public static void Show_Static(string titleString, string buttonText, string validCharacters, int characterLimit,
        Action onCancel, Action<string> onOk, UI_Instance uiInstance)
    {
        instance.Show(titleString, buttonText, validCharacters, characterLimit, onCancel, onOk, uiInstance);
    }

    public static void Show_Static(string titleString, string buttonText, Action onCancel, Action<int> onOk, UI_Instance uiInstance)
    {
        instance.Show(titleString, buttonText, "0123456789", 20, onCancel, (string inputText) =>
            {
                // Try to Parse input string
                if (int.TryParse(inputText, out int _i))
                {
                    onOk(_i);
                }
            }, uiInstance
        );
    }
}