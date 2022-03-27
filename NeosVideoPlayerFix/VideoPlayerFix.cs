using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using UnityNeos;
using System.IO;
using UMP;
namespace VideoPlayerFix
{
    public class VideoPlayerFix : NeosMod
    {
        public override string Name => "VideoPlayerFix";
        public override string Author => "Fro Zen";
        public override string Version => "2.0.0";

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