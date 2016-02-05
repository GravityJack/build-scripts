/**
 * This script should be copied to Assets/Editor/MobileBuildRunner.cs for the Unity Project.
 */
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public class MobileBuildRunner
{
	private const string DEFAULT_BUILD_DIR = "Build/";
	private const string DEFAULT_IOS_OUTPUT_DIR = DEFAULT_BUILD_DIR + "/iOS";
	private const string DEFAULT_ANDROID_APK_FILE = DEFAULT_BUILD_DIR + "/app.apk";
	private const string MENU_PATH = "CustomTools/Build/";

	// Generally DiscoverScenes should get the scenes for the build but if necessary
	// the following arrays can be used to explicitly set which scenes to build.

	// Scenes that will be included in builds for both platforms
	private static readonly string[] COMMON_SCENES = {};
	// Scenes that should only be included for Android builds
	private static readonly string[] ANDROID_ONLY_SCENES = {};
	// Scenes that should only be include for iOS builds.
	private static readonly string[] IOS_ONLY_SCENES = {};

	// Set to true to expose the builder methods as menu options in the Unity
	// Editor. Useful when making changes to this script.
	private const bool MenuOptionsEnabled = false;

	[MenuItem (MENU_PATH + "Android", false)]
	static void PerformAndroidBuild ()
	{
		string output = ReadTargetOutputPath();
		if (output == null) {
			Directory.CreateDirectory(DEFAULT_BUILD_DIR);
			output = DEFAULT_ANDROID_APK_FILE;
		}
        
        string keystoreName = ReadKeystoreName ();
		if (keystoreName != null) {
			PlayerSettings.Android.keystoreName = keystoreName;
		}

		string keystorePass = ReadKeystorePass ();
		if (keystorePass != null) {
			PlayerSettings.Android.keystorePass = keystorePass;
		}

		string keyAliasName = ReadKeyAliasName ();
		if (keyAliasName != null) {
			PlayerSettings.Android.keyaliasName = keyAliasName;
		}

		string keyAliasPass = ReadKeyAliasPass ();
		if (keyAliasPass != null) {
			PlayerSettings.Android.keyaliasPass = keyAliasPass;
		}

		new BuildRequest {
			Output = output,
			Scenes = GetAndroidScenes(),
			TargetPlatform = BuildTarget.Android
		}.Build ();
	}

	[MenuItem (MENU_PATH + "iOS", false)]
	static void PerformIOSBuild()
	{
		string output = ReadTargetOutputPath();
		if (output == null) {
			Directory.CreateDirectory(DEFAULT_IOS_OUTPUT_DIR);
			output = DEFAULT_IOS_OUTPUT_DIR;
		}

		new BuildRequest {
			Output = output,
			Scenes = GetIosScenes(),
			TargetPlatform = BuildTarget.iOS
		}.Build ();
	}

	[MenuItem (MENU_PATH + "Android", true)]
	static bool ShouldEnableAndroidBuildItem ()
	{
		// Suppress unreachable expression warning.
#pragma warning diable 0429
		return MenuOptionsEnabled && !BuildPipeline.isBuildingPlayer 
			&& EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Android 
			&& EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
#pragma warning restore 0429
	}

	[MenuItem (MENU_PATH + "iOS", true)]
	static bool ShouldEnableIOSBuildItem ()
	{
		// Suppress unreachable expression warning.
#pragma warning diable 0429
		return MenuOptionsEnabled && !BuildPipeline.isBuildingPlayer 
			&& EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.iOS
			&& EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;
#pragma warning restore 0429
	}

	private static string[] GetAndroidScenes ()
	{
		if (COMMON_SCENES.Length == 0 && ANDROID_ONLY_SCENES.Length == 0) {
			return DiscoverScenes ();
		} else {
			return COMMON_SCENES.Union (ANDROID_ONLY_SCENES).ToArray ();
		}
	}

	private static string[] GetIosScenes ()
	{
		if (COMMON_SCENES.Length == 0 && IOS_ONLY_SCENES.Length == 0) {
			return DiscoverScenes ();
		} else {
			return COMMON_SCENES.Union (IOS_ONLY_SCENES).ToArray ();
		}
	}

	private static string[] DiscoverScenes ()
	{
		return (from editorScene in EditorBuildSettings.scenes
			where editorScene.enabled
			select editorScene.path).ToArray ();
	}

	private static string ReadTargetOutputPath()
	{
		return CommandLineReader.GetCustomArgument("outputPath");
	}
    
    private static string ReadKeystoreName()
	{
		return CommandLineReader.GetCustomArgument("keystoreName");
	}

	private static string ReadKeystorePass()
	{
		return CommandLineReader.GetCustomArgument("keystorePass");
	}

	private static string ReadKeyAliasName()
	{
		return CommandLineReader.GetCustomArgument("keyAliasName");
	}

	private static string ReadKeyAliasPass()
	{
		return CommandLineReader.GetCustomArgument("keyAliasPass");
	}

	class BuildRequest
	{
		public string Output;
		public string[] Scenes;
		public BuildTarget TargetPlatform;

		public void Build ()
		{
			if (BuildPipeline.isBuildingPlayer)
				return;
			Debug.Log (DateTime.Now.ToString() + ": Build " + this.ToString());
			EditorUserBuildSettings.SwitchActiveBuildTarget (TargetPlatform);
			AssetDatabase.Refresh();

			if (Scenes.Length == 0) {
				throw new InvalidOperationException ("No scenes to build.");
			}
			var msg = BuildPipeline.BuildPlayer (Scenes, Output, TargetPlatform, BuildOptions.None);
			if (!String.IsNullOrEmpty (msg)) {
				// Kill the build.
				throw new Exception (msg);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[BuildRequest: Output={0}, Scenes=[{1}], TargetPlatform={2}]", Output, string.Join(", ", Scenes), TargetPlatform);
		}
	}

	class CommandLineReader {
		//Config
		private const string CUSTOM_ARGS_PREFIX = "-gj.mobilebuild:";
		private const char CUSTOM_ARGS_SEPARATOR = ';';

		public static string[] GetCommandLineArgs()
		{
			return Environment.GetCommandLineArgs();
		}

		public static string GetCommandLine()
		{
			string[] args = GetCommandLineArgs();

			if (args.Length > 0)
			{
				return string.Join(" ", args);
			}
			else
			{
				UnityEngine.Debug.LogError("CommandLineReader.cs - GetCommandLine() - Can't find any command line arguments!");
				return "";
			}
		}

		public static Dictionary<string,string> GetCustomArguments()
		{
			Dictionary<string, string> customArgsDict = new Dictionary<string, string>();
			string[] commandLineArgs = GetCommandLineArgs();
			string[] customArgs;
			string[] customArgBuffer;
			string customArgsStr = "";

			try
			{
				customArgsStr = commandLineArgs.SingleOrDefault(arg => arg.StartsWith(CUSTOM_ARGS_PREFIX)) ?? "";
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError("Error processing arguments [" + commandLineArgs + "]. Exception: " + e);
			}

			customArgsStr = customArgsStr.Replace(CUSTOM_ARGS_PREFIX, "");
			customArgs = customArgsStr.Split(CUSTOM_ARGS_SEPARATOR);

			foreach (string customArg in customArgs)
			{
				customArgBuffer = customArg.Split('=');
				if (customArgBuffer.Length == 2)
				{
					customArgsDict.Add(customArgBuffer[0], customArgBuffer[1]);
				}
				else
				{
					UnityEngine.Debug.LogWarning("CommandLineReader.cs - GetCustomArguments() - The custom argument [" + customArg + "] seem to be malformed.");
				}
			}

			return customArgsDict;
		}

		public static string GetCustomArgument(string argumentName)
		{
			Dictionary<string, string> customArgsDict = GetCustomArguments();

			if (customArgsDict.ContainsKey(argumentName))
			{
				return customArgsDict[argumentName];
			}
			else
			{
				return null;
			}
		}
	}
}