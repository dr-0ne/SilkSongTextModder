using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            return AccessTools.Method(typeof(Language), "DoSwitch", [typeof(string)]);
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
            
            // modify the text
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
            var newDict = new Dictionary<LanguageCode, Dictionary<string, Dictionary<string, string>>>();
            newDict.Add(LanguageCode.EN, new Dictionary<string, Dictionary<string, string>>());
            newDict[LanguageCode.EN].Add("MainMenu", new Dictionary<string, string>());
            newDict[LanguageCode.EN]["MainMenu"]["BUTTON_CAST"] = "LOCK IN";
            return newDict;
        }
    }
}