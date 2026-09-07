using System;
using System.Xml;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CustomLocalization
{
    private static Dictionary<string, LanguageType> languageTypeTable = new Dictionary<string, LanguageType>() 
    {
        { "Chinese", LanguageType.CHINESE },
    };

    static Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();

    static Dictionary<LanguageType, Dictionary<string, string>> data = new Dictionary<LanguageType, Dictionary<string, string>>();

    static Dictionary<string, string> currentLanguageData;

    static Font currentFont;

    public static event Action<LanguageType> UpdateLanguageEvent;

    public static void Load()
    {
        string text = null;

        TextAsset asset = Resources.Load<TextAsset>("language");
        if (asset != null) 
            text = asset.text;

        LoadXML(text);
    }

    static bool LoadXML(string xmlString)
    {
        if (string.IsNullOrEmpty(xmlString)) return false;

        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(xmlString);

        if (xDoc.ChildNodes.Count < 2) 
            return false;

        var root = xDoc.ChildNodes[1];
        if (root.ChildNodes.Count < 2)
            return false;

        dictionary.Clear();
        for (int i = 0; i < root.ChildNodes.Count; i++) 
            AddDictionary(root.ChildNodes[i]);

        var languages = dictionary["KEY"];

        data.Clear();

        foreach (var keypair in dictionary)
        {
            var strs = keypair.Value;
            var key = keypair.Key;

            int i = 0;
            foreach (var value in keypair.Value)
            {
                var languageType = languageTypeTable[languages[i]];
                if (!data.TryGetValue(languageType, out var dict)) 
                {
                    dict = new Dictionary<string, string>();
                    data.Add(languageType, dict);
                }

                dict.Add(key, value);
                i++;                
            }
        }

        SetLanguage(DateTimeUtility.GetLanguageType());

        return true;
    }

    static void AddDictionary(XmlNode node)
    {
        if (node.NodeType == XmlNodeType.Comment) return;
        string[] temp = new string[node.ChildNodes.Count];
        for (int i = 0; i < node.ChildNodes.Count; ++i)
        {
            if (node.ChildNodes[i].NodeType != XmlNodeType.Comment) temp[i] = node.ChildNodes[i].InnerXml;
        }

        try
        {
            dictionary.Add(node.Attributes["key"].Value, temp);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Node: " + node.OuterXml +"ex"+ ex.ToString());
        }
    }

    public static void SetLanguage(LanguageType languageType)
    {
        if (data.TryGetValue(languageType, out currentLanguageData))
        {
            currentFont = LanguageFontTable.Instance.GetFont(languageType);
            UpdateLanguageEvent?.Invoke(languageType);
        }
    }

    static public string GetKey(string value, out LanguageType language)
    {
        foreach (var languageData in data)
        {
            foreach (var keyData in languageData.Value)
            {
                if (keyData.Value == value)
                {
                    language = languageData.Key;
                    return keyData.Key;
                }
            }
        }

        language = LanguageType.UNKNOWN;

        return null;
    }

    static public string Get(string key)
    {
        if (currentLanguageData != null)
        {
            if (currentLanguageData.TryGetValue(key, out string value))
            {
                return value;
            }   
        }

        return null;
    }

    static public Font GetFont()
    {
        return currentFont;
    }

    static public Font GetFont(LanguageType languageType)
    {
        return LanguageFontTable.Instance.GetFont(languageType);
    }

    static public string Get(string key, LanguageType languageType)
    {
        if (data.TryGetValue(languageType, out Dictionary<string, string> languageData))
        {
            if (languageData != null)
            {
                if (languageData.TryGetValue(key, out string value))
                {
                    return value;
                }
            }
        }

        return null;
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Load Language")]
    static void LoadLanguage()
    {
        Load();
    }
#endif
}
