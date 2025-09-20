using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilkSongTextSheetOverride;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class TextSheetOverridePlugin : BaseUnityPlugin
{
    internal static ManualLogSource TextSheetOverrideLogger = new ManualLogSource("TextSheetOverride");
    private static readonly string ModDir = Path.Combine(Application.dataPath, "Mods", "TextSheetOverride");

    private static bool _patched = false;
    
    private void Awake()
    {
        if (!Directory.Exists(ModDir)) Directory.CreateDirectory(ModDir);
        
        BepInEx.Logging.Logger.Sources.Add(TextSheetOverrideLogger);
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
        TextSheetOverrideLogger.LogInfo("Harmony patching complete");
    }
    
    [HarmonyPatch]
    class Patch_Language_GetLanguageFileContents
    {
        
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Language), "GetLanguageFileContents", [typeof(string)]);
        }
        
        static void Postfix(ref string __result, object[] __args)
        {
            string sheetTitle = __args[0] as string;
            TextSheetOverrideLogger.LogInfo($"Loading sheet '{sheetTitle}'");
            if (!Directory.Exists(ModDir)) return;

            var matchingModdedPlainTextFiles = Directory.GetFiles(ModDir).Where(x => Path.GetFileName(x).Equals(sheetTitle + ".txt")).ToList();
            if (!matchingModdedPlainTextFiles.Any()) return;

            TextSheetOverrideLogger.LogInfo($"Found external sheet to override '{sheetTitle}'");
            var replacementTextContent = File.ReadAllText(matchingModdedPlainTextFiles.First());

            __result = replacementTextContent;
        }
    }
}