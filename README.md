# VideoPlayerFix

NeosVR is dead. This mod is no longer relevant. Resonite still has the same issue, but as of archiving this repository, is completely unplayable on Linux Native anyway.

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that fixes a lot of things related to video players.

On Linux Native, video players now work properly. On Linux with Proton, Unity Native is disabled by default to prevent a crash. On Windows, LibVLC is disabled by default. The media player which gets disabled can be configured.

A local install of [yt-dlp](https://github.com/yt-dlp/yt-dlp) or [YoutubeDL](https://github.com/ytdl-org/youtube-dl) is required for Youtube videos to work on Linux Native. 

If you encounter errors in-game, such as crashes and videos not working, including videos from local files, but the log still reports patching correctly, then I can't provide support. Neos is hilariously out of date and all this mod does is change two strings to make libraries load properly when they didn't before. Ask the Neos team to make this mod obsolete. 🙃

## For Neos Developers

### UMP

The library path within UMPSettings is not set to the correct directory
```cs
Msg($"Previous library path: {UMPSettings.Instance.LibrariesPath}");
var harmony = new Harmony("NeosVideoPlayerFixHarmony");
```
```
20:29:40.013 (  0 FPS)	[INFO] [NeosModLoader] loaded mod VideoPlayerFix 3.3.0 from /mnt/LocalDisk/SteamLibrary/steamapps/common/NeosVR/nml_mods/NeosVideoPlayerFix.dll
20:29:40.033 (  0 FPS)	[INFO] [NeosModLoader/VideoPlayerFix] Previous library path: 
20:29:40.035 (  0 FPS)	[INFO] [NeosModLoader/VideoPlayerFix] Removed Unity Native from valid playback engines.
```
```
[UMPSetting] Can't find LibVLC libraries, try to check the settings file in UMP 'Resources' folder.
 #0 GetStacktrace(int)
 #1 DebugStringToFile(DebugStringToFileData const&)
 #2 DebugLogHandler_CUSTOM_Internal_Log(LogType, LogOption, ScriptingBackendNativeStringPtrOpaque*, ScriptingBackendNativeObjectPtrOpaque*)
 #3  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #4  (Mono JIT Code) NeosModLoader.ModLoader:LoadMods ()
 #5  (Mono JIT Code) NeosModLoader.ExecutionHook:.cctor ()
 #6  (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void (object,intptr,intptr,intptr)
 #7 mono_print_method_from_ip
 #8 mono_perfcounter_foreach
 #9 mono_object_get_class
 #10 mono_install_unhandled_exception_hook
 #11  (Mono JIT Code) (wrapper managed-to-native) System.Reflection.MonoField:SetValueInternal (System.Reflection.FieldInfo,object,object)
```

### NYoutubeDL

Within FrooxEngine.VideoTextureProvider, YoutubeDL is initialized like this
```cs
private async Task LoadFromVideoServiceIntern(Uri url, CancellationToken cancellationToken)
{
	if (youtubeDL == null)
	{
		if (base.Engine.Platform == Platform.Linux)
		{
			youtubeDL = new YoutubeDL("youtube-dl");
		}
		else
		{
			string path = Path.Combine(base.Engine.AppPath, "RuntimeData\\yt-dlp.exe");
			if (!File.Exists(path))
			{
				return;
			}
			youtubeDL = new YoutubeDL(path);
		}
	}
```
The YoutubeDL class has two constructors
```cs
	public string YoutubeDlPath { get; set; } = new FileInfo("youtube-dl").GetFullPath();
	public YoutubeDL()
	{
		downloadTokenSource = new CancellationTokenSource();
	}

	public YoutubeDL(string path)
		: this()
	{
		YoutubeDlPath = path;
	}
```
Providing a string overrides the default initialization, and the default initialization does not search for yt-dlp, only youtube-dl. On Windows, a yt-dlp binary is provided, and correctly initializes it instead of youtube-dl, while on Linux a binary is not provided. Additionally, while it is given a valid path on Windows, it is not on Linux. To fix this, either provide a binary for yt-dlp and replace the start of the method with this
```cs
    if (youtubeDL == null)
    {
        string exec = base.Engine.Platform == Platform.Windows ? "RuntimeData\\yt-dlp.exe" : "yt-dlp"; //put executable path here
        string path = Path.Combine(base.Engine.AppPath, exec);
        if (!File.Exists(path)) return;
        youtubeDL = new YoutubeDL(path);
    }
```
Or change the default executable name in NYoutubeDL, have users provide their own yt-dlp binary, and not initialize YoutubeDL with a string
```cs
	public string YoutubeDlPath { get; set; } = new FileInfo("yt-dlp" /*"youtube-dl"*/).GetFullPath();
```
```cs
	if (youtubeDL == null)
	{
		if (base.Engine.Platform == Platform.Linux)
		{
			youtubeDL = new YoutubeDL(/*"youtube-dl"*/);
		}
		else
		{
			string path = Path.Combine(base.Engine.AppPath, "RuntimeData\\yt-dlp.exe");
			if (!File.Exists(path))
			{
				return;
			}
			youtubeDL = new YoutubeDL(path);
		}
	}
```
