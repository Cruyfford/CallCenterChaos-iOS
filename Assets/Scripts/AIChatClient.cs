using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// A live AI call is an HTTPS request to OUR backend, never directly to the model API.
// Backend URL is user-configurable; no provider API keys ship with the game.
public sealed class AIChatClient : MonoBehaviour
{
    private const string SettingsKey = "ccc.ai.backend.url.v1";
    public const string EditorDefault = "http://127.0.0.1:8787";

    [Serializable]
    private class TurnRequest
    {
        public string sessionId;
        public string callerId;
        public string message;
    }

    [Serializable]
    public class TurnReply
    {
        public bool ok;
        public string reply;
        public int trustDelta;
        public string mood;
        public string mode;
        public string error;
    }

    [Serializable]
    private class HealthReply
    {
        public bool ok;
        public string status;
        public string mode;
    }

    public static string BackendUrl
    {
        get
        {
            string saved = PlayerPrefs.GetString(SettingsKey, "").Trim().TrimEnd('/');
#if UNITY_EDITOR
            return !PlayerPrefs.HasKey(SettingsKey) ? EditorDefault : saved;
#else
            return saved;
#endif
        }
    }

    public static bool SetBackendUrl(string url, out string error)
    {
        url = (url ?? "").Trim().TrimEnd('/');
        error = "";
        if (url.Length > 250) { error = "Server address is too long."; return false; }
        if (url.Length > 0)
        {
            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed) ||
                !string.IsNullOrEmpty(parsed.UserInfo) ||
                (!string.IsNullOrEmpty(parsed.AbsolutePath) && parsed.AbsolutePath != "/") ||
                !string.IsNullOrEmpty(parsed.Query) || !string.IsNullOrEmpty(parsed.Fragment))
            { error = "Enter the base server URL (no path or password)."; return false; }
            bool localEditor = false;
#if UNITY_EDITOR
            localEditor = parsed.Scheme == "http" &&
                (parsed.Host == "localhost" || parsed.Host == "127.0.0.1");
#endif
            if (parsed.Scheme != "https" && !localEditor)
            { error = "Use HTTPS; HTTP works only with local Unity Editor tests."; return false; }
        }
        PlayerPrefs.SetString(SettingsKey, url);
        PlayerPrefs.Save();
        return true;
    }

    public void Send(string sessionId, string callerId, string text, Action<TurnReply> callback)
    {
        if (string.IsNullOrEmpty(BackendUrl))
        {
            callback(new TurnReply { ok=false, error="لم تتم إضافة خادم AI." });
            return;
        }
        StartCoroutine(PostTurn(new TurnRequest { sessionId=sessionId, callerId=callerId, message=text }, callback));
    }

    private IEnumerator PostTurn(TurnRequest body, Action<TurnReply> done)
    {
        string url = BackendUrl + "/v1/call";
        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
        using (var request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            request.timeout = 18;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                done(new TurnReply { ok=false, error="الخادم غير متاح: " + request.error });
                yield break;
            }
            TurnReply result = null;
            try { result = JsonUtility.FromJson<TurnReply>(request.downloadHandler.text); }
            catch (Exception ex) { Debug.LogWarning("AI response parse failure: " + ex.Message); }
            if (result == null || !result.ok || string.IsNullOrEmpty(result.reply))
                done(new TurnReply { ok=false, error=result == null ? "Invalid server response." : result.error });
            else
                done(result);
        }
    }

    public void CheckHealth(Action<bool, string> done)
    {
        if (string.IsNullOrEmpty(BackendUrl)) { done(false,"أدخل رابط خادم أولًا."); return; }
        StartCoroutine(CheckHealthRoutine(done));
    }

    private IEnumerator CheckHealthRoutine(Action<bool, string> done)
    {
        using (var request = UnityWebRequest.Get(BackendUrl + "/health"))
        {
            request.timeout = 8;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            { done(false,"فشل الاتصال: " + request.error); yield break; }
            HealthReply status = null;
            try { status = JsonUtility.FromJson<HealthReply>(request.downloadHandler.text); }
            catch (Exception) { }
            bool ready = status != null && status.ok && (status.mode == "live" || status.mode == "mock");
            done(ready, status == null ? "Invalid health response." :
                status.mode == "mock" ? "تجربة وهمية وليست AI مباشر" :
                status.mode == "live" ? "خادم AI المباشر جاهز" : "الخادم يحتاج إعداد AI");
        }
    }
}
