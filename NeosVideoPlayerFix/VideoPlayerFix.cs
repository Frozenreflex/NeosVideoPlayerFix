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
        public override string Version => "1.0.0";

        private static bool _first_trigger = false;

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("NeosVideoPlayerFixHarmony");
            harmony.PatchAll();
        }
        [HarmonyPatch(typeof(VideoTextureProvider))]
		class VideoPlayerFixPatch
		{
            [HarmonyPrefix]
			[HarmonyPatch("GetPlaybackEngine", typeof(string))]
			public static void GetPlaybackEnginePatch(ref string mime)
			{
                if (Engine.Current.IsWine)
                {
                    mime = null;
                }
                UniLog.Log("Forced libVLC in a video player");
			}
		}
    }
}