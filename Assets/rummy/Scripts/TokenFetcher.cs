using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class User
{
    public string avatar;
    public string username;
    public string createdAt;
    public string updatedAt;
}

[System.Serializable]
public class SessionData
{
    public User user;
    public string expires;
    public string token;
}

[System.Serializable]
public class PlayerData
{
    public string avatar;
    public int balance;
    public string username;
}

public class TokenFetcher : MonoBehaviour
{
    private const string SessionApiUrl = "http://localhost:3000/api/auth/session";
    private const string PlayerApiUrl = "http://backend.chkobbaking.com:8000/player";

    private IEnumerator Start()
    {
        yield return FetchSessionData();
    }

    private IEnumerator FetchSessionData()
    {
        Debug.Log("Requesting session data...");

        using (UnityWebRequest request = UnityWebRequest.Get(SessionApiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Session Response: " + jsonResponse);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    ParseSessionData(jsonResponse);
                }
                else
                {
                    Debug.LogError("Empty session response.");
                }
            }
            else
            {
                Debug.LogError($"Session request failed: {request.error} (Code: {request.responseCode})");
            }
        }
    }

    private void ParseSessionData(string jsonResponse)
    {
        try
        {
            SessionData sessionData = JsonUtility.FromJson<SessionData>(jsonResponse);

            if (sessionData?.user != null)
            {
                Debug.Log($"Session Token: {sessionData.token}");
                Debug.Log($"User: {sessionData.user.username}, Avatar: {sessionData.user.avatar}");

                // Fetch player data using the token
                StartCoroutine(FetchPlayerData(sessionData.token));
            }
            else
            {
                Debug.LogError("Invalid session data structure.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Session JSON Parsing Error: {ex.Message}");
        }
    }

    private IEnumerator FetchPlayerData(string token)
    {
        Debug.Log("Requesting player data...");

        using (UnityWebRequest request = UnityWebRequest.Get(PlayerApiUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Player Response: " + jsonResponse);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    ParsePlayerData(jsonResponse);
                }
                else
                {
                    Debug.LogError("Empty player response.");
                }
            }
            else
            {
                Debug.LogError($"Player request failed: {request.error} (Code: {request.responseCode})");
            }
        }
    }

    private void ParsePlayerData(string jsonResponse)
    {
        try
        {
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(jsonResponse);

            if (playerData != null)
            {
                Debug.Log($"Player: {playerData.username}, Balance: {playerData.balance}, Avatar: {playerData.avatar}");
            }
            else
            {
                Debug.LogError("Invalid player data structure.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Player JSON Parsing Error: {ex.Message}");
        }
    }
}
