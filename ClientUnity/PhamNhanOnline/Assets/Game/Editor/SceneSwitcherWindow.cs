using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhamNhanOnline.Client.Editor
{
    public sealed class SceneSwitcherWindow : EditorWindow
    {
        private const string ScenesRoot = "Assets/Game/Scenes";

        private readonly List<SceneEntry> scenes = new List<SceneEntry>();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Game/Scene Switcher")]
        public static void OpenWindow()
        {
            var window = GetWindow<SceneSwitcherWindow>("Scene Switcher");
            window.minSize = new Vector2(320f, 160f);
            window.RefreshScenes();
        }

        private void OnEnable()
        {
            RefreshScenes();
            EditorApplication.projectChanged += RefreshScenes;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= RefreshScenes;
        }

        private void OnFocus()
        {
            RefreshScenes();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4f);

            if (scenes.Count == 0)
            {
                EditorGUILayout.HelpBox($"No scenes found under '{ScenesRoot}'.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            var activeScenePath = SceneManager.GetActiveScene().path;

            for (var i = 0; i < scenes.Count; i++)
            {
                DrawSceneEntry(scenes[i], string.Equals(activeScenePath, scenes[i].Path, StringComparison.OrdinalIgnoreCase));
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Quick scene switcher for Assets/Game/Scenes", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    RefreshScenes();
            }
        }

        private void DrawSceneEntry(SceneEntry entry, bool isActive)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(isActive);
                    var pressed = GUILayout.Toggle(isActive, entry.DisplayName, "Button", GUILayout.Height(28f));
                    EditorGUI.EndDisabledGroup();

                    if (!isActive && pressed)
                        OpenScene(entry.Path);
                }

                EditorGUILayout.LabelField(entry.Path, EditorStyles.miniLabel);
            }
        }

        private void RefreshScenes()
        {
            scenes.Clear();

            var absoluteRoot = Path.GetFullPath(ScenesRoot);
            if (!Directory.Exists(absoluteRoot))
            {
                Repaint();
                return;
            }

            var sceneFiles = Directory.GetFiles(absoluteRoot, "*.unity", SearchOption.AllDirectories);
            Array.Sort(sceneFiles, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < sceneFiles.Length; i++)
            {
                var relativePath = ToAssetPath(sceneFiles[i]);
                scenes.Add(new SceneEntry(relativePath, Path.GetFileNameWithoutExtension(relativePath)));
            }

            Repaint();
        }

        private static void OpenScene(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static string ToAssetPath(string absolutePath)
        {
            var normalizedPath = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            return "Assets" + normalizedPath.Substring(dataPath.Length);
        }

        private readonly struct SceneEntry
        {
            public SceneEntry(string path, string displayName)
            {
                Path = path;
                DisplayName = displayName;
            }

            public string Path { get; }

            public string DisplayName { get; }
        }
    }
}
