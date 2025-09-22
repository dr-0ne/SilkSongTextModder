using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilkSongTextModder;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class TextModderPlugin : BaseUnityPlugin
{
    internal static ManualLogSource TextModderLogger = new ManualLogSource("TextModder");
    private static readonly string ModDir = Path.Combine(Application.dataPath, "Mods", "TextModder");

    private static bool _patched;
    
    private void Awake()
    {
        if (!Directory.Exists(ModDir)) Directory.CreateDirectory(ModDir);
        
        BepInEx.Logging.Logger.Sources.Add(TextModderLogger);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        if (_patched) return;
        DoPatch();
    }

    private static void DoPatch()
    {
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        _patched = true;
        TextModderLogger.LogInfo("Harmony patching complete");
    }
    
    [HarmonyPatch]
    class Patch_Language_DoSwitch
    {
        
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Language), "DoSwitch", [typeof(LanguageCode)]);
        }
        
        static void Postfix()
        {
            // Language -> sheet -> entryId -> value
            // A little gross I'm aware
            Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>> customSheetsByLanguage = ReadInCustomText();
            
            FieldInfo? currentEntrySheetsFieldInfo = typeof(Language).GetField("_currentEntrySheets", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (currentEntrySheetsFieldInfo == null)
            {
                TextModderLogger.LogError("Failed to find '_currentEntrySheets' field");
                return;
            }
            
            Dictionary<string, Dictionary<string, string>>? currentEntrySheets = currentEntrySheetsFieldInfo?.GetValue(null) as Dictionary<string, Dictionary<string, string>>;

            if (currentEntrySheets == null)
            {
                TextModderLogger.LogError("Failed to get value of '_currentEntrySheets'");
                return;
            }

            FieldInfo? currentLanguageFieldInfo = typeof(Language).GetField("_currentLanguage", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (currentLanguageFieldInfo == null)
            {
                TextModderLogger.LogError("Failed to find '_currentLanguage' field");
                return;
            }
            
            LanguageCode currentLanguage = currentLanguageFieldInfo.GetValue(null) is LanguageCode ? (LanguageCode)currentLanguageFieldInfo.GetValue(null) : LanguageCode.N;

            if (!customSheetsByLanguage.ContainsKey(currentLanguage))
            {
                TextModderLogger.LogWarning("There is no custom text for the game's current language code: '" + currentLanguage +"'");
                return;
            }
            
            // Copy each custom value into the game's text data
            foreach (var sheet in customSheetsByLanguage[currentLanguage])
            {
                if (!currentEntrySheets.ContainsKey(sheet.Key))
                {
                    currentEntrySheets.Add(sheet.Key, sheet.Value);
                }
                foreach (var entry in sheet.Value)
                {
                    currentEntrySheets[sheet.Key][entry.Key] = entry.Value;
                }
            }
            
            currentLanguageFieldInfo.SetValue(null,currentEntrySheets);
            
            TextModderLogger.LogInfo("Replaced all text entries");
        }

        static Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>> ReadInCustomText()
        {
            /*var newDict = new Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>>();
            newDict.Add(LanguageCode.EN, new Dictionary<string, Dictionary<string, string>>());
            newDict[LanguageCode.EN].Add("MainMenu", new Dictionary<string, string>());
            newDict[LanguageCode.EN]["MainMenu"]["BUTTON_CAST"] = "LOCK IN";
            return newDict;*/
            
            var returnDict = new Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>>();
            foreach (var filePath in Directory.GetFiles(ModDir))
            {
                TextModderLogger.LogInfo($"Reading Text From File: {filePath}");
                DeserializeCustomText(filePath,returnDict);
                TextModderLogger.LogInfo($"Finshed reading Text From File: {filePath}");
            }
            
            return returnDict;
        }

        static void DeserializeCustomText(string filePathToDeserialize, Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>> dictToAddTo)
        {
            TextModderLogger.LogInfo($"something??");
            //XDocument doc = XDocument.Load(File.ReadAllText(filePathToDeserialize));

            XmlReader xmlReader = XmlReader.Create(new StringReader( filePathToDeserialize));
            
            while (xmlReader.ReadToFollowing("entry"))
            {
                xmlReader.MoveToFirstAttribute();
                string value = xmlReader.Value;
                xmlReader.MoveToElement();
                string text2 = xmlReader.ReadElementContentAsString().Trim();
                text2 = text2.UnescapeXml();
                TextModderLogger.LogInfo($"{text2}");
            }
            
            //if (doc.Root == null) return;
            
            /*foreach (var languageElem in doc.Root.Elements("language"))
            {
                string? langName = languageElem.Attribute("name")?.Value;
                TextModderLogger.LogInfo($"{langName}");
                if (langName == null || !Enum.TryParse<LanguageCode>(langName, true, out var langCode))
                {
                    continue;
                }
                TextModderLogger.LogInfo($"{langCode}");

                if (!dictToAddTo.ContainsKey(langCode))
                {
                    dictToAddTo.Add(langCode, new Dictionary<string, Dictionary<string, string>>());
                }
                
                foreach (var sheetElem in languageElem.Elements("sheet"))
                {
                    string? sheetName = sheetElem.Attribute("name")?.Value;
                    if (sheetName == null) continue;

                    if (!dictToAddTo[langCode].ContainsKey(sheetName))
                    {
                        dictToAddTo[langCode].Add(sheetName, new Dictionary<string, string>());
                    }
                    
                    foreach (var entryElem in sheetElem.Elements("entry"))
                    {
                        string? entryName = entryElem.Attribute("name")?.Value;
                        if (entryName == null) continue;
                        
                        TextModderLogger.LogInfo($"Found Custom Text: ({langCode}:{sheetName}:{entryName}:{entryElem.Value})");
                        dictToAddTo[langCode][sheetName][entryName] = entryElem.Value;
                    }
                }
            }*/
        }
    }
}