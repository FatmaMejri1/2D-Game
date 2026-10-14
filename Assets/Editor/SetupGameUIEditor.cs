#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Menu: Escape Room > Create Game Scene. Makes Assets/Scenes/EscapeRoom.unity ready to play.</summary>
public static class SetupGameUIEditor
{
    const string ScenePath = "Assets/Scenes/EscapeRoom.unity";

    [MenuItem("Escape Room/Create Game Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(8f, 0f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.07f);
        camGO.AddComponent<AudioListener>();

        new GameObject("Level Setup").AddComponent<LevelSetupHelper>();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == ScenePath))
        {
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Escape Room",
            "Scene created at " + ScenePath + " and added to Build Settings.\n\nPress Play to start the game.", "OK");
    }
}
#endif
