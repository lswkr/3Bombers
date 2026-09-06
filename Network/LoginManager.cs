using Cysharp.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    private enum LoginType
    {
        ID,
        Password,
        Nickname
    };

    private FirebaseDatabase database;
    private DatabaseReference reference;

    //È¸¿ø°¡ÀÔ 
    [SerializeField] private GameObject registerPanel;

    [SerializeField] private TMP_InputField registerPlayerIDInput;
    [SerializeField] private TMP_InputField registerPlayerPasswordInput;
    [SerializeField] private TMP_InputField registerPlayerNicknameInput;

    [SerializeField] private TextMeshProUGUI registerIDInfoText;
    [SerializeField] private TextMeshProUGUI registerPasswordInfoText;
    [SerializeField] private TextMeshProUGUI registerNicknameInfoText;
    [SerializeField] private TextMeshProUGUI commonRegisterInfoText;

    [SerializeField] private TMP_InputField loginPlayerIDInput;
    [SerializeField] private TMP_InputField loginPlayerPasswordInput;

    [SerializeField] private TextMeshProUGUI loginInfoText;
    [SerializeField] private GameObject loginInfo;

    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button createButton;

    [SerializeField] private Button idCheckButton;
    [SerializeField] private Button passwordCheckButton;
    [SerializeField] private Button nicknameCheckButton;

    bool idAllowed = false;
    bool passwordAllowed = false;
    bool nicknameAllowed = false;

    private string loginPlayerID;
    private string loginPlayerPassword;
    private string loginPlayerNickname;

    private string registerPlayerID;
    private string registerPlayerPassword;
    private string registerPlayerNickname;


    private void Start()
    {
        registerPanel.SetActive(false);
        loginInfo.SetActive(false);

        database = FirebaseDatabase.GetInstance(Constants.FIREBASEURL);
        reference = database.RootReference;

        loginButton.onClick.AddListener(() => Login().Forget());

        createButton.onClick.AddListener(() => Create().Forget());

        registerButton.onClick.AddListener(() => { if (!registerPanel.activeSelf) { registerPanel.SetActive(true); } });

        idCheckButton.onClick.AddListener(() => { IDRuleCheck().Forget(); });

        passwordCheckButton.onClick.AddListener(() => { PasswordRuleChecker(); });

        nicknameCheckButton.onClick.AddListener(() => { NicknameRuleCheck().Forget(); });
    }

    private void SetLoginInfoText(string inputText, Color color)
    {
        loginInfoText.text = inputText;
        loginInfoText.color = color;

        if (!loginInfo.activeSelf)
            loginInfo.SetActive(true);
    }

    private void SetRegisterInfoText(TextMeshProUGUI textMeshText, string inputText, Color color)
    {
        textMeshText.text = inputText;
        textMeshText.color = color;

        if (!textMeshText.gameObject.activeSelf)
            textMeshText.gameObject.SetActive(true);
    }

    private async UniTaskVoid Login()
    {
        loginPlayerID = loginPlayerIDInput.text;
        loginPlayerPassword = loginPlayerPasswordInput.text;

        if (string.IsNullOrEmpty(loginPlayerID))
        {
            SetLoginInfoText("ID¸¦ ÀÔ·ÂÇØÁÖ¼¼¿ä.", Color.red);
            return;
        }

        SetLoginInfoText("µ¥ÀÌÅÍ È®ÀÎ Áß...", Color.black);
        loginButton.interactable = false; //Áßº¹ Å¬¸¯ ¹æÁö

        try
        {
            DataSnapshot snapshot = await reference.Child("UserInfo").OrderByChild("playerID").EqualTo(loginPlayerID).GetValueAsync();

            if (snapshot.Exists && snapshot.HasChildren) //¾ÆÀÌµð Á¸Àç
            {
                foreach (var child in snapshot.Children)
                {
                    string password = child.Child("playerPassword").Value.ToString();

                    if (password == GetHashedPassword(loginPlayerPassword))
                    {
                        SetLoginInfoText("·Î±×ÀÎ Áß..", Color.black);
                        loginButton.interactable = false;
                        registerButton.interactable = false;

                        loginPlayerNickname = child.Child("playerNickname").Value.ToString();

                        LoginInfoManager.Instance.Init(loginPlayerID, loginPlayerNickname);
                        SceneManager.LoadScene("MenuScene");
                        return;
                    }
                    else
                    {
                        SetLoginInfoText("ºñ¹Ð¹øÈ£°¡ ÀÏÄ¡ÇÏÁö ¾Ê½À´Ï´Ù.", Color.red);
                        loginButton.interactable = true;
                        return;
                    }
                }
            }
            else
            {
                SetLoginInfoText("Á¸ÀçÇÏÁö ¾Ê´Â ¾ÆÀÌµðÀÔ´Ï´Ù.", Color.red);
                loginButton.interactable = true;
                return;
            }
        }

        catch (Exception ex)
        {
            Debug.LogError($"¿¡·¯°¡ ¹ß»ýÇß½À´Ï´Ù.{ex}");
            SetLoginInfoText($"¿À·ù°¡ ¹ß»ýÇÏ¿´½À´Ï´Ù. ¿À·ù:{ex}", Color.red);

            loginButton.interactable = true;
        }
    }


    private async UniTaskVoid IDRuleCheck() // ID ±ÔÄ¢ ¹× Áßº¹ Ã¼Å©
    {
        if (string.IsNullOrEmpty(registerPlayerIDInput.text))
        {
            SetRegisterInfoText(registerIDInfoText, "ID¸¦ ÀÔ·ÂÇØÁÖ¼¼¿ä.", Color.red);
            idCheckButton.interactable = true;
            return;
        }
        else if (!CheckStringCondition(registerPlayerIDInput.text, LoginType.ID))
        {
            SetRegisterInfoText(registerIDInfoText, "¾ÆÀÌµð´Â 6ÀÚ ÀÌ»ó 12ÀÚ ÀÌÇÏÀÇ \n¿µ¹®°ú ¼ýÀÚ·Î ±¸¼ºµÇ¾î¾ß ÇÕ´Ï´Ù", Color.red);
            idCheckButton.interactable = true;
            return;
        }

        try
        {
            DataSnapshot snapshot = await reference.Child("UserInfo").OrderByChild("playerID").EqualTo(registerPlayerIDInput.text).GetValueAsync();

            if (snapshot.HasChildren)
            {
                SetRegisterInfoText(registerIDInfoText, "ÀÌ¹Ì Á¸ÀçÇÏ´Â IDÀÔ´Ï´Ù.", Color.red);
                idCheckButton.interactable = true;
                return;
            }
            else
            {
                registerPlayerID = registerPlayerIDInput.text;
                SetRegisterInfoText(registerIDInfoText, "»ç¿ë°¡´ÉÇÑ IDÀÔ´Ï´Ù!.", Color.green);
                idCheckButton.interactable = true;
                idAllowed = true;
                return;
            }
        }

        catch (Exception ex)
        {
            Debug.LogError($"¿¡·¯°¡ ¹ß»ýÇß½À´Ï´Ù.{ex}");
            SetRegisterInfoText(registerIDInfoText, $"¿À·ù°¡ ¹ß»ýÇÏ¿´½À´Ï´Ù. ¿À·ù:{ex}", Color.red);
            idCheckButton.interactable = true;
        }
    }

    private async UniTaskVoid NicknameRuleCheck() // ID ±ÔÄ¢ ¹× Áßº¹ Ã¼Å©
    {
        if (string.IsNullOrEmpty(registerPlayerNicknameInput.text))
        {
            SetRegisterInfoText(registerNicknameInfoText, "´Ð³×ÀÓÀ» ÀÔ·ÂÇØÁÖ¼¼¿ä.", Color.red);
            idCheckButton.interactable = true;
            return;
        }
        else if (!CheckStringCondition(registerPlayerNicknameInput.text, LoginType.Nickname))
        {
            SetRegisterInfoText(registerNicknameInfoText, "´Ð³×ÀÓÀº 2ÀÚ ÀÌ»ó 8ÀÚ ÀÌÇÏÀÇ ¹®ÀÚ·Î ±¸¼ºµÇ¾î¾ß ÇÕ´Ï´Ù", Color.red);
            idCheckButton.interactable = true;
            return;
        }

        try
        {
            DataSnapshot nicknameSnapshot = await reference.Child("UserInfo").OrderByChild("playerNickname").EqualTo(registerPlayerNicknameInput.text).GetValueAsync();

            if (nicknameSnapshot.HasChildren)
            {
                SetRegisterInfoText(registerNicknameInfoText, "ÀÌ¹Ì Á¸ÀçÇÏ´Â ´Ð³×ÀÓÀÔ´Ï´Ù.", Color.red);
                nicknameCheckButton.interactable = true;
                return;
            }
            else
            {
                registerPlayerNickname = registerPlayerNicknameInput.text;

                SetRegisterInfoText(registerNicknameInfoText, "»ç¿ë °¡´ÉÇÑ ´Ð³×ÀÓÀÔ´Ï´Ù!", Color.green);
                nicknameAllowed = true;
                nicknameCheckButton.interactable = true;
                return;

            }
        }

        catch (Exception ex)
        {
            Debug.LogError($"¿¡·¯°¡ ¹ß»ýÇß½À´Ï´Ù.{ex}");
            SetRegisterInfoText(registerNicknameInfoText, $"¿À·ù°¡ ¹ß»ýÇÏ¿´½À´Ï´Ù. ¿À·ù:{ex}", Color.red);
            nicknameCheckButton.interactable = true;
        }
    }

    private void PasswordRuleChecker()
    {
        passwordCheckButton.interactable = false;

        if (string.IsNullOrEmpty(registerPlayerPasswordInput.text))
        {
            SetRegisterInfoText(registerPasswordInfoText, "ºñ¹Ð¹øÈ£¸¦ ÀÔ·ÂÇØÁÖ¼¼¿ä.", Color.red);
            passwordCheckButton.interactable = true;
            return;
        }
        else if (!CheckStringCondition(registerPlayerPasswordInput.text, LoginType.Password))
        {
            SetRegisterInfoText(registerPasswordInfoText, "ºñ¹Ð¹øÈ£´Â ¿µ¾î, ¼ýÀÚ, Æ¯¼ö¹®ÀÚ¸¦ Æ÷ÇÔÇØ\n6ÀÚ ÀÌ»ó 20ÀÚ ÀÌÇÏ·Î ¸¸µé¾îÁÖ¼¼¿ä", Color.red);
            passwordCheckButton.interactable = true;
            return;
        }
        else
        {
            registerPlayerPassword = registerPlayerPasswordInput.text;
            SetRegisterInfoText(registerPasswordInfoText, "»ç¿ë°¡´ÉÇÑ ºñ¹Ð¹øÈ£ÀÔ´Ï´Ù!", Color.green);
            passwordCheckButton.interactable = true;
            passwordAllowed = true;
            return;
        }
    }

    private async UniTaskVoid Create()
    {

        if (!idAllowed || !passwordAllowed || !nicknameAllowed)
        {
            SetRegisterInfoText(commonRegisterInfoText, "Á¤º¸ ÀÔ·ÂÀÌ ¿Ï·áµÇÁö ¾Ê¾Ò½À´Ï´Ù.", Color.red);
            return;
        }
        try
        {
            UserData userData = new UserData(registerPlayerID, GetHashedPassword(registerPlayerPassword), registerPlayerNickname);
            string jsonData = JsonConvert.SerializeObject(userData);

            await reference.Child("UserInfo").Push().SetRawJsonValueAsync(jsonData);
            SetRegisterInfoText(commonRegisterInfoText, "°èÁ¤ »ý¼ºÀÌ ¿Ï·áµÇ¾ú½À´Ï´Ù.", Color.green);
            idCheckButton.interactable = false;
            passwordCheckButton.interactable = false;
            nicknameCheckButton.interactable = false;
            createButton.interactable = false;

            //ÆÐ³ÎÀº X¹öÆ°À¸·Î ²ø ¼ö ÀÖÀ½
            return;
        }

        catch (Exception ex)
        {
            Debug.LogError($"¿¡·¯°¡ ¹ß»ýÇß½À´Ï´Ù.{ex}");
            SetRegisterInfoText(commonRegisterInfoText, $"¿À·ù°¡ ¹ß»ýÇÏ¿´½À´Ï´Ù. ¿À·ù:{ex}", Color.red);
            createButton.interactable = true;
        }
    }

    private bool CheckStringCondition(string inputString, LoginType loginType)
    {
        string pattern;
        int maxLength = 0;
        int minLength = 0;

        
        if (loginType == LoginType.ID)
        {
            minLength = 6;
            maxLength = 12;
            Debug.Log($"ÀÔ·ÂµÈ ID: {inputString}");
            pattern = @"^[a-zA-Z0-9]+$";
        }

        else if (loginType == LoginType.Password)
        {
            minLength = 6;
            maxLength = 20;
            Debug.Log($"ÀÔ·ÂµÈ Password: {inputString}");
            pattern = @"^[a-zA-Z0-9!@#$%^&*]+$";
        }

        else if (loginType == LoginType.Nickname)
        {
            minLength = 2;
            maxLength = 8;
            Debug.Log($"ÀÔ·ÂµÈ Nickname: {inputString}");
            pattern = @"^[a-zA-Z0-9°¡-ÆR¤¡-¤¾¤¿-¤Ó!@#$%^&*]+$";
        }
        else
        {
            return false;
        }

        if (inputString.Length < minLength || inputString.Length > maxLength)
        {
            return false;
        }
        return Regex.IsMatch(inputString, pattern);
    }

    private string GetHashedPassword(string password)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }



}
