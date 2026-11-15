#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DiGiorgioLabs.DroneDefenseVR.Core;

namespace DiGiorgioLabs.DroneDefenseVR.Editor
{
    public static class DroneDefenseVRSetup
    {
        [MenuItem("Tools/DiGiorgioLabs/1. Build Drone Defense VR Scene")]
        public static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var appGo = new GameObject("DroneDefenseVR_App");
            appGo.AddComponent<DroneDefenseBootstrap>();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            string path = "Assets/Scenes/DroneDefenseVR.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            Debug.Log($"[DiGiorgioLabs] Saved DroneDefenseVR scene to {path}");
        }

        [MenuItem("Tools/DiGiorgioLabs/2. Configure Meta Quest Android Settings")]
        public static void ConfigureQuestAndroid()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.digiorgiolabs.dronedefensevr");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Debug.Log("[DiGiorgioLabs] Configured Android IL2CPP ARM64 for Meta Quest.");
        }
    }
}
#endif
