using System;
using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using UnityNeos;
using System.IO;
using System.Linq;
using System.Reflection;
using UMP;
using NYoutubeDL;
namespace VideoPlayerFix;

public enum PlayerMode
{
    DisableNone,
    DisableUnityNative,
    DisableLibVLC,
    DisableAll
}
public class VideoPlayerFix : NeosMod
{
    public override string Name => "VideoPlayerFix";
    public override string Author => "Fro Zen";
    public override string Version => "4.1.0";
    private static string YoutubeDLPath = "";

    [AutoRegisterConfigKey]
    public readonly ModConfigurationKey<PlayerMode> VideoPlayerMode = new("VideoPlayerMode", "Video Player Mode (Requires Restart)",
        () => Engine.Current.IsWine || Engine.Current.Platform != Platform.Windows
            ? PlayerMode.DisableUnityNative
            : PlayerMode.DisableLibVLC);

    public override void OnEngineInit()
    {
        var harmony = new Harmony("NeosVideoPlayerFixHarmony");
        var config = GetConfiguration();
        var mode = config!.GetValue(VideoPlayerMode);
        if (mode is not PlayerMode.DisableNone)
        {
            var engines = PlaybackEngine.PlaybackEngines;
            if (mode is PlayerMode.DisableAll)
            {
                engines.Clear();
                //patch internal setupprepare method to disable youtubedl
                var ydlClassRemove = AccessTools.AllTypes().First(i => i.ToString().EndsWith("Services.PreparationService"));
                var ydlMethodRemove = ydlClassRemove.GetMethod("SetupPrepare", BindingFlags.NonPublic | BindingFlags.Static);
                harmony.Patch(ydlMethodRemove, new HarmonyMethod(typeof(YoutubeDLPatch).GetMethod(nameof(YoutubeDLPatch.SetupPrepareRemovePatch))));
                Msg("Removed all valid playback engines and disabled YoutubeDL");
                Msg("Disabling YoutubeDL reduces network strain, but will cause more errors in logs");
                return;
            }
            var disable = mode == PlayerMode.DisableUnityNative ? "Unity Native" : "libVLC";
            var disablePlayer = engines.Find(i => i.Name == disable);
            if (disablePlayer != default)
            {
                engines.Remove(disablePlayer);
                Msg($"Removed {disable} from valid playback engines.");
            }
        }
        if (Engine.Current.Platform != Platform.Linux) return;

        harmony.Patch(typeof(UMPSettings).GetMethod("get_LibrariesPath"),
            new HarmonyMethod(typeof(WrapperPatch).GetMethod(nameof(WrapperPatch.WrapperLibraryPatch))));

        //patch internal setupprepare method
        var ydlClass = AccessTools.AllTypes().First(i => i.ToString().EndsWith("Services.PreparationService"));
        var ydlMethod = ydlClass.GetMethod("SetupPrepare", BindingFlags.NonPublic | BindingFlags.Static);
        harmony.Patch(ydlMethod, new HarmonyMethod(typeof(YoutubeDLPatch).GetMethod(nameof(YoutubeDLPatch.SetupPreparePatch))));
            
        //find valid YTDL locations
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var paths = path.Split(Path.PathSeparator).Append(Engine.Current.AppPath).ToList();
        var programs = new [] {"yt-dlp", "youtube-dl"};
        foreach (var p in programs)
        {
            var test = paths.Select(i => Path.Combine(i, p)).FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(test)) continue;
            YoutubeDLPath = test;
            Msg($"Patched NYoutubeDL with {p}: {test}");
            return;
        }
        Msg("Could not find a valid program to patch NYoutubeDL with");
    }

    private class WrapperPatch
    {
        public static bool WrapperLibraryPatch(ref string __result)
        {
            //Fix video players by setting the library path properly
            __result = Path.Combine(Engine.Current.AppPath, "Neos_Data", "Plugins");
            Msg("Patched library path: " + __result);
            return false;
        }
    }

    private class YoutubeDLPatch
    {
        public static void SetupPreparePatch(YoutubeDL ydl) =>
            //NYoutubeDL has logic to set the youtube-dl path according to the environment PATH,
            //but this doesn't work because it's set to an invalid path when initialized
            //this will set it to a valid youtube-dl location
            ydl.YoutubeDlPath = YoutubeDLPath;
        public static bool SetupPrepareRemovePatch(YoutubeDL ydl) => throw new Exception();
    }
    /*
    [HarmonyPatch(typeof(VideoTextureProvider))]
    class VideoTextureProviderPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("GetPlaybackEngine", typeof(string))]
        public static void GetPlaybackEnginePatch(ref string mime)
        {
            //if mime is null, this should theoretically always return libvlc
            //this seems to work properly in local worlds but doesn't work in other user's worlds
            //if mime is a specific value, it will use the force video player value, which can
            //allow unity's player to run, which we don't want
            mime = null;
            Msg("Forced libVLC in a video player");
        }
    }
    */
    /*
    [HarmonyPatch(typeof(FrooxEngine.WorldConfiguration))]
    class WorldConfigurationPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("FieldChanged")]
        public static bool FieldChanged(IChangeable obj)
        {
            //fix log spam when starting up
            //also removes confusing language when the values are set automatically
            var field = (IField)obj;
            var conflictingSyncElement = (ConflictingSyncElement)obj;
            var userMessage = conflictingSyncElement.LastModifyingUser != null ? $" LastModifyingUser: {conflictingSyncElement.LastModifyingUser}" : "";
            UniLog.Log($"{field.Name} set to {field.BoxedValue}.{userMessage}", false);
            return false;
        }
    }
    */
}