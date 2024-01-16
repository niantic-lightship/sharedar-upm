// Copyright 2022-2024 Niantic.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Niantic.Lightship.SharedAR.Colocalization;

[CustomEditor(typeof(SharedSpaceManager))]
internal class SharedSpaceManagerEditor : Editor
{
    private GameObject GetSharedArRootPrefab(string prefabTextName)
    {
        // Get this script's path
        var scriptGuids = AssetDatabase.FindAssets($"{nameof(SharedSpaceManagerEditor)} t:Script");
        var scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
        var directories = scriptPath.Split(new char[] { '/' });
        var editorIndex = Array.IndexOf(directories, "Editor");

        if (editorIndex < 0)
        {
            throw new InvalidOperationException(
                $"Expected to find \"Editor\" in the path of this script: {scriptPath}");
        }

        // Grab our plugin's relative path from the script path
        var subfolderToSearch =
            string.Join(Path.DirectorySeparatorChar.ToString(), directories, 0, editorIndex) +
            Path.DirectorySeparatorChar;
        // Search for prefab in plugin's relative path
        var prefabGuids = AssetDatabase.FindAssets($"{prefabTextName} t:Prefab",
            new string[] {subfolderToSearch});

        if (prefabGuids.Length != 1)
        {
            throw new InvalidOperationException(
                $"SharedSpaceManagerEditor could not find unique SharedArRootPrefab with name {prefabTextName}." +
                $"FindAssets found {prefabGuids.Length} matching assets in subfolder {subfolderToSearch}.");
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGuids[0]));
    }

    private void OnVpsGUI(SharedSpaceManager.ColocalizationType previousColocType)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_arLocationManager"),
            new GUIContent("AR Location Manager (optional)"));
    }

    private void OnImageTargetGUI()
    {
    }

    private void OnMockGUI()
    {
    }

    public override void OnInspectorGUI()
    {
        var prefabProperty = serializedObject.FindProperty("_sharedArRootPrefab");
        if (prefabProperty.objectReferenceValue == null)
        {
            prefabProperty.objectReferenceValue = GetSharedArRootPrefab("SharedArRoot");
        }

        EditorGUILayout.Space(10);

        var sharedSpaceManager = (SharedSpaceManager)target;
        var colocalizationTypeProperty = serializedObject.FindProperty("_colocalizationType");
        var newColocType =
            (SharedSpaceManager.ColocalizationType)EditorGUILayout.Popup(
                new GUIContent(colocalizationTypeProperty.displayName, colocalizationTypeProperty.tooltip),
                colocalizationTypeProperty.enumValueIndex,
                Enum.GetNames(typeof(SharedSpaceManager.ColocalizationType))
            );

        switch (newColocType)
        {
          case SharedSpaceManager.ColocalizationType.VpsColocalization:
              // The manager's _colocalizationType hasn't been updated yet
              OnVpsGUI((SharedSpaceManager.ColocalizationType)
                  colocalizationTypeProperty.enumValueIndex);
              break;
          case SharedSpaceManager.ColocalizationType.ImageTrackingColocalization:
              OnImageTargetGUI();
              break;
          case SharedSpaceManager.ColocalizationType.MockColocalization:
              OnMockGUI();
              break;
        }

        colocalizationTypeProperty.enumValueIndex = (int)newColocType;
        serializedObject.ApplyModifiedProperties();
    }
}

#endif
