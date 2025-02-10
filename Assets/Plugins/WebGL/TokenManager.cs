using System.Runtime.InteropServices;
using UnityEngine;

public class TokenManager : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string GetLocalStorageToken();

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string token = GetLocalStorageToken();
        Debug.Log("Retrieved Token: " + token);
#endif
    }
}
