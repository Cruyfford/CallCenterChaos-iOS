using System.Runtime.InteropServices;
using UnityEngine;

// Offline phone dialogue works without voice. iOS Speech and speech synthesis are optional enhancements.
public static class VoiceBridge
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void CCC_StartSpeech(string gameObjectName);
    [DllImport("__Internal")] private static extern void CCC_StopSpeech();
    [DllImport("__Internal")] private static extern void CCC_Speak(string text);
    [DllImport("__Internal")] private static extern void CCC_SetVoice(int voiceIndex, float pitch, float rate, int gender, string locale);
    [DllImport("__Internal")] private static extern void CCC_SetLanguage(string locale);
#endif
    public static void SetLanguage(string locale)
    {
#if UNITY_IOS && !UNITY_EDITOR
        CCC_SetLanguage(locale);
#endif
        PlayerPrefs.SetString("ccc.voice.locale", locale);
        PlayerPrefs.Save();
    }

    public static void SetVoice(int voiceIndex, float pitch, float rate, string gender, string locale)
    {
#if UNITY_IOS && !UNITY_EDITOR
        CCC_SetVoice(voiceIndex, pitch, rate, gender == "female" ? 2 : gender == "male" ? 1 : 0, locale);
#endif
    }

    public static void Listen(string receiver)
    {
#if UNITY_IOS && !UNITY_EDITOR
        CCC_StartSpeech(receiver);
#else
        Debug.Log("Speech recognition runs on iOS device builds. Use choice buttons in Editor.");
#endif
    }
    public static void Speak(string text)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if(!string.IsNullOrEmpty(text)) CCC_Speak(text);
#else
        Debug.Log("Caller: " + text);
#endif
    }
    public static void Stop()
    {
#if UNITY_IOS && !UNITY_EDITOR
        CCC_StopSpeech();
#endif
    }
}
