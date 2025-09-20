using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using TeamCherry.Localization;
using UnityEngine;

namespace SilkSongTextSheetOverride;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class SilkSongTextSheetOverride : BaseUnityPlugin
{
    private static readonly string ModDir = Path.Combine(Application.dataPath, "Mods", "TextSheetOverride");

    [HarmonyPatch]
    public static class Patch_Language_GetLanguageFileContents
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Language), "GetLanguageFileContents", new[] { typeof(string) });
        }
        
        static void Postfix(string sheetTitle, ref string __result)
        {
            if (!Directory.Exists(ModDir)) return;
            
            var matchingModdedPlainTextFiles = Directory.GetFiles(ModDir).Where(x => x.Equals(sheetTitle + ".txt")).ToList();
            if (!matchingModdedPlainTextFiles.Any()) return;

            var replacementTextContent = File.ReadAllText(matchingModdedPlainTextFiles.First());

            __result = replacementTextContent;
        }
    }

    private void Awake()
    {
        if (!Directory.Exists(ModDir)) Directory.CreateDirectory(ModDir);
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} has loaded!");
        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}