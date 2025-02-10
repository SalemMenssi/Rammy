using System;
using SimpleJSON;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RammyApi : MonoBehaviour
{
#if !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string GetURLFromPage();
#endif

    //eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mzg3OTk1MjQsImlhdCI6MTczODE5NDcyNCwiaWQiOjN9.pm28yMfN8gcjFV2kHZNSP8VBdZeu5GcP1CBRysu2eYk  Client
    //eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mzg3Nzg4MzQsImlhdCI6MTczODE3NDAzNCwiaWQiOjF9.xQn57v_VgDbm87xDk63-6SjTrZqzwq1q7kZQryLkN8o  Host
    private const string BaseUrl = "http://backend.chkobbaking.com:8000";
    // private const string HttpAPIAuthSession = "http://127.0.0.1:3000/api/auth/session";

    private const string TestToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3Mzk2MzkzMzIsImlhdCI6MTczOTAzNDUzMiwiaWQiOjF9.nVN8tx-xTjWfrEq8-8GPCBgAJfHfu2ZB0yQc3RtNuVU";

    private const string SessionToken =
        "eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..GHdr3YsRBZLI34je.ufsaT3ul_GGq1cyeJ46XgwPv5ffHJGLA57TmcP28lHaKF4iyDK_e4OZhLK52emK1eX8VeEeLi1Rbby6xFZ24WazTtfKpEGzOE-Z7WSKfeK_Hlq3ocN6rus2UGeBWBRrx8j11mAt7FT0pv_AZ5oPq3zqIFpNGdqd-0uJ2L1gDBKzqO27ajX_ab1vr-BCu9oObayOS1Ht1W84G_tmUco8CTL00US7TLPu0PXAPd85aivBkspVZceh2HkGDirf2siD4XUTEoxK0hJjMdHaJEJvGlIQTc6nuJesruVpvtvxVk71iuTJcCy27bguBPl0dx5DoVsuYO3Sl0EsOE0t1GPuXmqkYJOij-uG_15p2nC3F5PLgd5-YfRrdanfTOvLcxz9JyG56MrNrC_abqfwCpAVo2qEJvblmVdGRDWtmeUOs2qeroDEF_obP2vbaeJuuRG5wg8BI8TjK3m7bcvoXHCoQExuw-X4cvA8.Il4hdMPLVKp94QNajDoRfw";

    private const float MaxCheckingTime = 3f;

    private const string StaticRoomEndpoint = "/chkobba/static-room";
    private const string StaticRoomJoin = "/chkobba/static-room/join/";
    private const string QueueEndpoint = "/chkobba/queue/static";
    private const string SyncHostCodeEndpoint = "/chkobba/sync/";
    private const string AssignPlayerEndpoint = "/chkobba/assign";
    private const string StaticStartEndpoint = "/chkobba/static-start";
    private const string EndGameEndpoint = "/chkobba/end";
    private const string PlayerEndpoint = "/player";

    private const string ApplicationJsonHeaderValue = "application/json";
    private const string ContentTypeHeaderName = "Content-Type";

    private const string CookieHeaderName = "Cookie";
    private const string BearerHeadName = "Bearer ";
    private const string AuthorizationHeaderName = "Authorization";
    private const string AmountFieldName = "amount";
    private const string ModeFieldName = "mode";
    private const string TokenParameterName = "?token=";

    public bool isHosted;
    public string code;
    [SerializeField] private Text joinCodeText;
    [SerializeField] private bool _isTestedToken;

    private string CookieToken;
    private string mod;
    private int numberOfHostes;

    private float timer;
    private bool isGetActiveRoomBusy;
    private bool isRoomHostedBusy;
    private string playerToken;

    private bool numberOfHostesIsOk;

    public string CurrentUserName { get; private set; }
    public string CurrentAvatar { get; private set; }

    private void Awake()
    {
        mod = string.Empty;
        if (_isTestedToken)
        {
            playerToken = TestToken;
        }
        else
        {
            playerToken = string.Empty;
            // StartCoroutine(GetToken());
        }

        CookieToken = string.Empty;
        code = string.Empty;

        TryGetTokenFromUrl();
        StartCoroutine(GetProfile());
        
    }

    private void Update()
    {
        if (numberOfHostesIsOk)
        {
            return;
        }

        if (numberOfHostes == 2)
        {
            //Debug.Log("Room is ready");
        }

        timer += Time.deltaTime;
        if (timer > MaxCheckingTime)
        {
            timer = 0f;
            if (!isGetActiveRoomBusy)
            {
                //TestGetActiveGameRoom();
            }

            if (!isRoomHostedBusy && isHosted)
            {
                TryHost();
            }
        }
    }

    #region Tests

    [ContextMenu("Create game Room")]
    public void TestCreateGameRoom()
    {
        StartCoroutine(CreateGameRoom("1v1", 43));
    }

    [ContextMenu("Destroy game Room")]
    public void TestDestroyGameRoom()
    {
        StartCoroutine(DestroyGameRoom());
    }

    [ContextMenu("Join game Room")]
    public void TestJoinGameRoom(int roomID)
    {
        StartCoroutine(JoinGameRoom(roomID));
    }

    [ContextMenu("Join random Game")]
    public void TestJoinRandomGame()
    {
        StartCoroutine(JoinRandomGame());
    }

    [ContextMenu("Try sync host code")]
    public void TestSyncHostCode()
    {
        StartCoroutine(SyncHostCode("host-code"));
    }

    [ContextMenu("Assign random player")]
    public void TestAssignRandomPlayer()
    {
        StartCoroutine(AssignRandomPlayer());
    }

    [ContextMenu("Start game")]
    public void TestStartGame()
    {
        StartCoroutine(StartGame());
    }

    [ContextMenu("End game")]
    public void TestEndGame()
    {
        string[] winners = { "Player1", "Player2" };
        StartCoroutine(EndGame(winners));
    }

    #endregion

    public void GameStart()
    {
        StartCoroutine(StartGame());
    }

    public void GameOver(string[] winners)
    {
        StartCoroutine(EndGame(winners));
    }

    public void SyncCode(string relayCode)
    {
        StartCoroutine(SyncHostCode(relayCode));
    }

    public void TestGetActiveGameRoom()
    {
        StopCoroutine(nameof(GetActiveGameRoom));
        StartCoroutine(nameof(GetActiveGameRoom));
    }

    private void TryHost()
    {
        switch (mod)
        {
            case "1V1":
                if (numberOfHostes < 2)
                {
                    StartCoroutine(AssignRandomPlayer());
                    Debug.Log("looking for Player In queu");
                }
                else
                {
                    numberOfHostesIsOk = true;
                    //StartCoroutine(SyncHostCode(joinCodeText.text));
                }

                break;
            case "2V2":
                if (numberOfHostes < 4)
                {
                    StartCoroutine(AssignRandomPlayer());
                }
                else
                {
                    numberOfHostesIsOk = true;
                    Debug.Log("Game Ready");
                }

                break;
            default:
                break;
        }
    }

    private IEnumerator CreateGameRoom(string mode, float amount)
    {
        string url = BaseUrl + StaticRoomEndpoint;
        WWWForm form = new WWWForm();
        form.AddField(ModeFieldName, mode);
        form.AddField(AmountFieldName, amount.ToString());

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();

        CheckRequestResult(request, nameof(CreateGameRoom), "Game room created successfully");
    }




    private IEnumerator DestroyGameRoom()
    {
        string url = BaseUrl + StaticRoomEndpoint;
        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(DestroyGameRoom), "Game room deleted successfully");
    }

    private IEnumerator GetActiveGameRoom()
    {
        isGetActiveRoomBusy = true;
        string url = BaseUrl + StaticRoomEndpoint;
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();

        CheckRequestResult(request, nameof(GetActiveGameRoom), "Success", () =>
        {
            string jsonResponse = request.downloadHandler.text;
            JSONNode jsonNode = JSON.Parse(jsonResponse);

            mod = jsonNode["room"]["mode"];
            isHosted = jsonNode["room"]["is_host"];
            JSONArray guests = jsonNode["room"]["guests"].AsArray;
            numberOfHostes = guests.Count;
            code = jsonNode["room"]["code"];
            Debug.Log("Dev code :" + jsonNode);
        });

        isGetActiveRoomBusy = false;
    }

    private IEnumerator JoinGameRoom(int roomId)
    {
        string url = BaseUrl + StaticRoomJoin + roomId;
        UnityWebRequest request = UnityWebRequest.Put(url, "");
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(JoinGameRoom), "Game game room successfully");
    }

    private IEnumerator JoinRandomGame()
    {
        string url = BaseUrl + QueueEndpoint;
        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "");
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(JoinRandomGame), "Game Random game room successfully");
    }

    private IEnumerator SyncHostCode(string hostCode)
    {
        isRoomHostedBusy = true;
        string url = BaseUrl + SyncHostCodeEndpoint + hostCode;
        Debug.Log($"[SYNC HOST CODE] Code: {hostCode}\r\nEmpty PUT to the end point: {url}");

        UnityWebRequest request = UnityWebRequest.Put(url, "");
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(SyncHostCode), "Sync host successfully");
        isRoomHostedBusy = false;
    }

    private IEnumerator AssignRandomPlayer()
    {
        isRoomHostedBusy = true;
        string url = BaseUrl + AssignPlayerEndpoint;
        UnityWebRequest request = UnityWebRequest.Put(url, "");
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(AssignRandomPlayer), "AssignRandomPlayer Response: " + request.downloadHandler.text);
        isRoomHostedBusy = false;
    }

    private IEnumerator StartGame()
    {
        string url = BaseUrl + StaticStartEndpoint;
        UnityWebRequest request = UnityWebRequest.Put(url, "");
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(StartGame), "StartGame Response: " + request.downloadHandler.text);
    }
    public IEnumerator StartGameRammy()
    {
        string url = BaseUrl + "/rummy/room/static/v2/start";
        UnityWebRequest request = UnityWebRequest.Put(url, "");
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(StartGame), "StartGame Response: " + request.downloadHandler.text);
        Debug.Log(request.downloadHandler.text);
    }
    private IEnumerator EndGame(string[] winners)
    {
        string url = BaseUrl + EndGameEndpoint;
        string jsonBody = JsonUtility.ToJson(winners);
        UnityWebRequest request = UnityWebRequest.Put(url, jsonBody);
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        request.SetRequestHeader(ContentTypeHeaderName, ApplicationJsonHeaderValue);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(EndGame), "EndGame Response: " + request.downloadHandler.text);
    }

    public IEnumerator EndGameRammy(string winner)
    {
        string url = BaseUrl + "/rummy/room/static/v2/end";
        var payload = new { winner };
        string jsonBody = JsonUtility.ToJson(payload);
        UnityWebRequest request = UnityWebRequest.Put(url, jsonBody);
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        request.SetRequestHeader(ContentTypeHeaderName, ApplicationJsonHeaderValue);
        yield return request.SendWebRequest();
        CheckRequestResult(request, nameof(EndGame), "EndGame Response: " + request.downloadHandler.text);
        Debug.Log(request.downloadHandler.text);
    }

    // private IEnumerator GetToken()
    // {
    //     UnityWebRequest request = UnityWebRequest.Get("");
    //     request.SetRequestHeader(ContentTypeHeaderName, ApplicationJsonHeaderValue);
    //     request.SetRequestHeader(CookieHeaderName, $"next-auth.session-token={SessionToken}");
    //     yield return request.SendWebRequest();
    //
    //     CheckRequestResult(request, nameof(GetToken), "Get token successfully: ", () =>
    //     {
    //         string jsonResponse = request.downloadHandler.text;
    //         Debug.Log("Full Response: " + jsonResponse);
    //
    //         // Parse JSON
    //         JSONNode jsonNode = JSON.Parse(jsonResponse);
    //         playerToken = jsonNode["token"].ToString(); // Extract user token
    //         Debug.Log("Extracted User Token: " + playerToken);
    //     });
    // }

    public IEnumerator GetProfile()
    {
        UnityWebRequest request = UnityWebRequest.Get(BaseUrl + PlayerEndpoint);
        request.SetRequestHeader(AuthorizationHeaderName, BearerHeadName + playerToken);
        yield return request.SendWebRequest();
        Debug.Log(request.downloadHandler.text);
        CheckRequestResult(request, nameof(GetProfile), "Profile Response: " + request.downloadHandler.text, () =>
        {
            CurrentUserName = GetUserName(request.downloadHandler.text);
            CurrentAvatar = GetUserAvatar(request.downloadHandler.text);
            Debug.Log($"Current user name: {CurrentUserName}");
        });
    }

    private IEnumerator Cookie()
    {
        string BaseURL = "http://127.0.0.1:3000/api/session";
        UnityWebRequest request = UnityWebRequest.Get(BaseURL);

        yield return request.SendWebRequest();

        CheckRequestResult(request, nameof(Cookie), "Cookie Response: " + CookieToken, () =>
        {
            string jsonResponse = request.downloadHandler.text;
            JSONNode jsonNode = JSON.Parse(jsonResponse);
            string token = jsonNode["token"].ToString();
            CookieToken = token.Substring(1, token.Length - 2);
        });
    }

    private string GetUserName(string json)
    {
        JSONNode jsonNode = JSON.Parse(json);
        var ss = jsonNode["player"].ToString();
        JSONNode jsonNode2 = JSON.Parse(ss);
        return jsonNode2["username"].ToString();
    }
    private string GetUserAvatar(string json)
    {
        JSONNode jsonNode = JSON.Parse(json);
        var ss = jsonNode["player"].ToString();
        JSONNode jsonNode2 = JSON.Parse(ss);
        return jsonNode2["avatar"].ToString();
    }

    private void CheckRequestResult(UnityWebRequest request, string methodName, string successMessage, Action successCallback = null,
        Action notSuccessCallback = null)
    {
        if (request.result == UnityWebRequest.Result.Success)
        {
            successCallback?.Invoke();
            Debug.Log($"[{methodName}]: {successMessage}");
        }
        else
        {
            notSuccessCallback?.Invoke();
            Debug.LogError($"[{methodName}] Response Code: {request.responseCode}, Response: {request.downloadHandler.text}");
        }
    }

    private void TryGetTokenFromUrl()
    {
#if !UNITY_EDITOR
        string url = GetURLFromPage();
        playerToken = TryGetToken(url);
        Debug.Log($"Current URL: {url} | Token: {playerToken} ");
#endif
    }

#if !UNITY_EDITOR
    private string TryGetToken(string url)
    {
        int index = url.IndexOf(TokenParameterName, StringComparison.Ordinal);
        string token = String.Empty;
        if (index != -1)
        {
            token = url.Substring(index + TokenParameterName.Length).Trim();
        }

        return token;
    }
#endif


}