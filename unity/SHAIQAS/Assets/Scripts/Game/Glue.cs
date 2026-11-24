// SHAIQAS · Unity ↔ Core байланысы: деректер Resources/Data-дан, сақтау PlayerPrefs-ке.
using UnityEngine;

public class ResSource : ISource
{
    public string Load(string name)
    {
        TextAsset t = Resources.Load<TextAsset>("Data/" + name);
        return t == null ? null : t.text;
    }
}

public class PrefsStore : IStore
{
    public string Get(string key) { return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null; }
    public void Set(string key, string value) { PlayerPrefs.SetString(key, value); PlayerPrefs.Save(); }
}
