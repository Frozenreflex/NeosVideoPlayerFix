using System;
using System.Collections.Generic;
using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using UnityNeos;
using System.IO;
using System.Linq;
using System.Reflection;
using UMP;
using NYoutubeDL;
namespace VideoPlayerFix
{
    public class VideoPlayerFix : NeosMod
    {
        public override string Name => "VideoPlayerFix";
        public override string Author => "Fro Zen";
        public override string Version => "3.3.1";

        private static bool _first_trigger = false;
        private static string YoutubeDLPath = "";

        public override void OnEngineInit()
        {
            var harmony = new Harmony("NeosVideoPlayerFixHarmony");
            //as an absolute backup, Unity Native isn't even a thing anymore, so we don't have to worry about it loading
            //if one wanted to limit this to Proton/Wine, this can be encapsulated with "if (Engine.Current.IsWine){}"
            var engines = PlaybackEngine.PlaybackEngines;
            var nativePlayer = engines.Find(i => i.Name == "Unity Native");
            if (nativePlayer != default)
            {
                engines.Remove(nativePlayer);
                Msg("Removed Unity Native from valid playback engines.");
            }
            harmony.PatchAll();
            
            if (Engine.Current.Platform != Platform.Linux) return;

            //patch internal setupprepare method
            var ydlClass = AccessTools.AllTypes().First(i => i.ToString().EndsWith("Services.PreparationService"));
            var ydlMethod = ydlClass.GetMethod("SetupPrepare", BindingFlags.NonPublic | BindingFlags.Static);
            harmony.Patch(ydlMethod, new HarmonyMethod(typeof(YoutubeDLPatch).GetMethod("SetupPreparePatch")));
            
            //find valid YTDL locations (and add the local directory)
            var path = Environment.GetEnvironmentVariable("PATH");
            path += Path.PathSeparator + Engine.Current.AppPath;
            var paths = path.Split(Path.PathSeparator);
            var programs = new [] {"yt-dlp", "youtube-dl"};
            foreach (var p in programs)
            {
                var test = paths.FirstOrDefault(i => File.Exists($"{i}/{p}")) + $"/{p}";
                if (test == $"/{p}") continue;
                YoutubeDLPath = test;
                Msg($"Patched NYoutubeDL with {p}: {test}");
                return;
            }
            Msg("Could not find a valid program to patch NYoutubeDL with");
        }

        [HarmonyPatch(typeof(UMPSettings))]
        class WrapperPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("get_LibrariesPath")]
            public static bool WrapperLibraryPatch(ref string __result)
            {
                //Fix video players by setting the library path properly
                if (Engine.Current.Platform != Platform.Linux) return true;
                __result = Path.Combine(Engine.Current.AppPath, "Neos_Data", "Plugins");
                Msg("Patched library path: " + __result);
                return false;
            }
        }
        class YoutubeDLPatch
        {
            public static void SetupPreparePatch(YoutubeDL ydl)
            {
                //NYoutubeDL has logic to set the youtube-dl path according to the environment PATH,
                //but this doesn't work because it's set to an invalid path when initialized
                //this will set it to a valid youtube-dl location
                ydl.YoutubeDlPath = YoutubeDLPath;
            }
        }
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
}