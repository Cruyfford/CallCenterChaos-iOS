using System;
using System.Collections.Generic;
using UnityEngine;

// Original, fictional callers. All portraits are generated in this project.
[Serializable]
public class CallerProfile
{
    public string id;
    public string displayName;
    public string portrait;
    public string personality;
    public string occupation;
    public string topic;
    public string greeting;
    public string dialogueHint;
    public string highTrustReply;
    public string mediumTrustReply;
    public string lowTrustReply;
    public int patience;
    public string gender;
    public int age;
    public string ageGroup;
    public string language;
    public int voiceVariant;
    public float voicePitch;
    public float voiceRate;
}

[Serializable]
public class CallerRosterFile
{
    public CallerProfile[] callers;
}

public sealed class CallerRoster
{
    private readonly CallerProfile[] profiles;
    private readonly Dictionary<string, Texture2D> portraitCache = new Dictionary<string, Texture2D>();
    private readonly Queue<string> cacheOrder = new Queue<string>();
    private readonly HashSet<int> recentlySelected = new HashSet<int>();
    private const int CacheLimit = 24; // Keep mobile texture memory bounded.
    public int Count { get { return profiles.Length; } }

    public CallerRoster()
    {
        var source = Resources.Load<TextAsset>("Callers/caller_profiles");
        CallerRosterFile file = null;
        if (source != null)
        {
            try { file = JsonUtility.FromJson<CallerRosterFile>(source.text); }
            catch (Exception ex) { Debug.LogError("Caller roster JSON error: " + ex.Message); }
        }
        profiles = file != null && file.callers != null ? file.callers : new CallerProfile[0];
        if (profiles.Length == 0) Debug.LogWarning("No callers loaded. Re-run Tools/generate_callers.py to restore portraits and JSON.");
    }

    public CallerProfile Get(int index)
    {
        return index < 0 || index >= profiles.Length ? null : profiles[index];
    }

    public CallerProfile GetRandom()
    {
        if (profiles.Length == 0) return null;
        if (recentlySelected.Count >= profiles.Length) recentlySelected.Clear();
        int index = UnityEngine.Random.Range(0, profiles.Length);
        for (int tries = 0; tries < 25 && recentlySelected.Contains(index); tries++)
            index = UnityEngine.Random.Range(0, profiles.Length);
        if (recentlySelected.Contains(index))
            for (int i=0;i<profiles.Length;i++) if (!recentlySelected.Contains(i)) { index=i; break; }
        recentlySelected.Add(index);
        return profiles[index];
    }

    public Texture2D Portrait(CallerProfile profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.portrait)) return null;
        Texture2D texture;
        if (portraitCache.TryGetValue(profile.portrait, out texture)) return texture;
        texture = Resources.Load<Texture2D>(profile.portrait);
        if (texture == null) return null;
        portraitCache[profile.portrait] = texture;
        cacheOrder.Enqueue(profile.portrait);
        while (cacheOrder.Count > CacheLimit)
        {
            var removeKey = cacheOrder.Dequeue();
            portraitCache.Remove(removeKey); // Resources owns imported textures; do not Destroy shared assets.
        }
        return texture;
    }
}
