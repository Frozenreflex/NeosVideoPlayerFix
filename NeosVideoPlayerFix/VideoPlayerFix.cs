using System;
using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
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
        public override string Version => "3.0.0";

        private static bool _first_trigger = false;

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
                UniLog.Log("Removed Unity Native from valid playback engines.");
            }
            harmony.PatchAll();
            
            if (Engine.Current.Platform != Platform.Linux) return;
            
            //patch internal setupprepare method
            var ydlClass = AccessTools.AllTypes().First(i => i.ToString().EndsWith("Services.PreparationService"));
            var ydlMethod = ydlClass.GetMethod("SetupPrepare", BindingFlags.NonPublic | BindingFlags.Static);
            harmony.Patch(ydlMethod, new HarmonyMethod(typeof(YoutubeDLPatch).GetMethod("SetupPreparePatch")));
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
                UniLog.Log("Patched library path: " + __result);
                return false;
            }
        }
        class YoutubeDLPatch
        {
            public static void SetupPreparePatch(YoutubeDL ydl)
            {
                //NYoutubeDL has logic to set the youtube-dl path according to the environment PATH,
                //...but it doesn't seem to work for some reason, so it gets set to just "youtube-dl"
                //this will manually set it to a valid youtube-dl location
                //if this doesn't work on your distro, change the location and recompile
                //TODO: set this up to use NML's config system
                ydl.YoutubeDlPath = "/usr/bin/youtube-dl";
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
                UniLog.Log("Forced libVLC in a video player");
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