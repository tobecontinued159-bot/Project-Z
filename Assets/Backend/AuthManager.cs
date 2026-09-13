using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

// โครงสร้างข้อมูลสำหรับแปลงเป็น JSON ส่งไป Server
[System.Serializable]
public class UserData
{
    public string username;
    public string password;
}

// โครงสร้างข้อมูลรับตอบกลับจาก Server
[System.Serializable]
public class ServerResponse
{
    public string message;
}

public class AuthManager : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text statusText;

    // URL ของ Server Node.js (หากทดสอบในเครื่องใช้ http://localhost:3000/api)
    private string baseUrl = "http://localhost:3000/api";

    // ผูกฟังก์ชันนี้กับปุ่ม Login
    public void OnLoginClick()
    {
        StartCoroutine(SendAuthRequest("/login"));
    }

    // ผูกฟังก์ชันนี้กับปุ่ม Register
    public void OnRegisterClick()
    {
        StartCoroutine(SendAuthRequest("/register"));
    }

    private IEnumerator SendAuthRequest(string endpoint)
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        // UX Validation: เช็คว่ากรอกข้อมูลครบถ้วนไหม
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            statusText.text = "กรุณากรอกชื่อผู้ใช้และรหัสผ่านให้ครบถ้วน";
            yield break;
        }

        statusText.text = "กำลังดำเนินการ...";

        // 1. แปลงข้อมูลเป็นรูปแบบ JSON
        UserData data = new UserData { username = username, password = password };
        string jsonPayload = JsonUtility.ToJson(data);

        // 2. สร้าง UnityWebRequest ส่งไปหา Node.js API
        using (UnityWebRequest www = new UnityWebRequest(baseUrl + endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            // 3. จัดการข้อความตอบกลับเพื่อแสดงผล UX บนหน้าจอ
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                // อ่านผลลัพธ์ Error จาก JSON ของ Server
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    ServerResponse errorRes = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);
                    ShowStatusMessage(errorRes.message);
                }
                else
                {
                    statusText.text = "ไม่สามารถเชื่อมต่อเซิร์ฟเวอร์ได้";
                }
            }
            else
            {
                ServerResponse successRes = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);
                ShowStatusMessage(successRes.message);
            }
        }
    }

    // แสดงผลข้อความแจ้งเตือนผู้ใช้งาน (UX Feedback)
    private void ShowStatusMessage(string messageCode)
    {
        switch (messageCode)
        {
            case "REGISTER_SUCCESS":
                statusText.text = "<color=green>สมัครสมาชิกสำเร็จ! สามารถเข้าสู่ระบบได้เลย</color>";
                break;
            case "LOGIN_SUCCESS":
                statusText.text = "<color=green>เข้าสู่ระบบสำเร็จ!</color>";
                // TODO: คำสั่งย้ายไปหน้าเมนูหลักของเกม เช่น SceneManager.LoadScene("MainMenu");
                break;
            case "USERNAME_EXISTS":
                statusText.text = "<color=red>ชื่อผู้ใช้นี้ถูกใช้งานแล้ว</color>";
                break;
            case "USER_NOT_FOUND":
            case "WRONG_PASSWORD":
                statusText.text = "<color=red>ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง</color>";
                break;
            default:
                statusText.text = "<color=red>เกิดข้อผิดพลาด: " + messageCode + "</color>";
                break;
        }
    }
}