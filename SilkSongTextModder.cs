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
            var returnDict = new Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>>();
            foreach (var filePath in Directory.GetFiles(ModDir))
            {
                TextModderLogger.LogInfo($"Reading Text From File: {filePath}");
                foreach (var line in File.ReadAllLines(filePath))
                {
                    DeserializeCustomTextLine(line,returnDict);
                }
            }
            
            return returnDict;
        }

        static void DeserializeCustomTextLine(string lineToDeserialize, Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>> dictToAddTo)
        {
            TextModderLogger.LogInfo(lineToDeserialize);

            if (string.IsNullOrWhiteSpace(lineToDeserialize)) return;

            // Split into four parts
            var parts = lineToDeserialize.Split(new[] { '>' }, 4); // limit to 4 parts in case the value contains colons
            if (parts.Length != 4)
            {
                TextModderLogger.LogInfo($"Invalid format: {lineToDeserialize}");
                return;
            }

            // Parse language code
            if (!Enum.TryParse(parts[0], out LanguageCode langCode))
            {
                TextModderLogger.LogInfo($"Unknown language code: {parts[0]}");
                return;
            }

            string topKey = parts[1];
            string subKey = parts[2];
            string value = parts[3];

            if (!dictToAddTo.ContainsKey(langCode))
                dictToAddTo[langCode] = new Dictionary<string, Dictionary<string, string>>();

            if (!dictToAddTo[langCode].ContainsKey(topKey))
                dictToAddTo[langCode][topKey] = new Dictionary<string, string>();

            // Add or update the value
            dictToAddTo[langCode][topKey][subKey] = value;
        }
    }
}