using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TeamCherry.Localization;
using UnityEngine;

namespace SilkSongTextSheetOverride;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class TextSheetOverridePlugin : BaseUnityPlugin
{
    internal static ManualLogSource TextSheetOverrideLogger = new ManualLogSource("TextSheetOverride");
    private static readonly string ModDir = Path.Combine(Application.dataPath, "Mods", "TextSheetOverride");

    private void Awake()
    {
        if (!Directory.Exists(ModDir)) Directory.CreateDirectory(ModDir);
        
        BepInEx.Logging.Logger.Sources.Add(TextSheetOverrideLogger);
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
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
            TextSheetOverrideLogger.LogInfo($"Load sheet with ID {sheetTitle}");
            if (!Directory.Exists(ModDir)) return;

            var matchingModdedPlainTextFiles = Directory.GetFiles(ModDir).Where(x => x.Equals(sheetTitle + ".txt")).ToList();
            if (!matchingModdedPlainTextFiles.Any()) return;

            TextSheetOverrideLogger.LogInfo($"Found external sheet to override {sheetTitle}");
            var replacementTextContent = File.ReadAllText(matchingModdedPlainTextFiles.First());

            __result = replacementTextContent;
        }
    }
}