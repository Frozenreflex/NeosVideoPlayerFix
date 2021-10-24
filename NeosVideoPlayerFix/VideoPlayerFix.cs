using HarmonyLib; // HarmonyLib comes included with a NeosModLoader install
using NeosModLoader;
using FrooxEngine;
using BaseX;

namespace VideoPlayerFix
{
    public class VideoPlayerFix : NeosMod
    {
        public override string Name => "VideoPlayerFix";
        public override string Author => "Fro Zen";
        public override string Version => "1.1.0";

        private static bool _first_trigger = false;

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("NeosVideoPlayerFixHarmony");
            harmony.PatchAll();
            //as an absolute backup, Unity Native isn't even a thing anymore, so we don't have to worry about it loading
            //if one wanted to limit this to Proton/Wine, this can be encapsulated with "if (Engine.Current.IsWine){}"
            var engines = UnityNeos.PlaybackEngine.PlaybackEngines;
            var nativePlayer = engines.Find(i => i.Name == "Unity Native");
            if (nativePlayer != default)
            {
                engines.Remove(nativePlayer);
                UniLog.Log("Removed Unity Native from valid playback engines.");
            }
        }
        [HarmonyPatch(typeof(VideoTextureProvider))]
        class VideoTextureProviderPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetPlaybackEngine", typeof(string))]
            public static void GetPlaybackEnginePatch(ref string mime)
            {
                mime = null;
                UniLog.Log("Forced libVLC in a video player");
            }
        }
    }
}