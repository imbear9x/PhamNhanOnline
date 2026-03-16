using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PhamNhanOnline.Client.Editor
{
    [InitializeOnLoad]
    public static class SceneSwitcherToolbar
    {
        private const string ScenesRoot = "Assets/Game/Scenes";
        private const string ToolbarElementName = "PhamNhanOnline-SceneSwitcherToolbar";

        private static readonly Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static readonly List<SceneEntry> Scenes = new List<SceneEntry>();

        private static ScriptableObject currentToolbar;
        private static IMGUIContainer toolbarContainer;

        static SceneSwitcherToolbar()
        {
            RefreshScenes();
            EditorApplication.update += TryAttachToToolbar;
            EditorApplication.projectChanged += RefreshScenes;
        }

        private static void TryAttachToToolbar()
        {
            if (ToolbarType == null)
                return;

            if (currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
                if (toolbars == null || toolbars.Length == 0)
                    return;

                currentToolbar = toolbars[0] as ScriptableObject;
            }

            if (currentToolbar == null)
                return;

            var root = GetToolbarRoot(currentToolbar);
            if (root == null)
                return;

            if (toolbarContainer != null && toolbarContainer.parent != null)
                return;

            var leftZone = root.Q("ToolbarZoneLeftAlign");
            if (leftZone == null)
                return;

            var existing = leftZone.Q<IMGUIContainer>(ToolbarElementName);
            if (existing != null)
            {
                toolbarContainer = existing;
                return;
            }

            toolbarContainer = new IMGUIContainer(DrawToolbar)
            {
                name = ToolbarElementName,
            };
            toolbarContainer.style.marginLeft = 8;
            toolbarContainer.style.marginRight = 8;
            leftZone.Add(toolbarContainer);
        }

        private static VisualElement GetToolbarRoot(ScriptableObject toolbar)
        {
            var rootField = ToolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rootField?.GetValue(toolbar) is VisualElement fieldRoot)
                return fieldRoot;

            var rootProperty = toolbar.GetType().GetProperty("rootVisualElement", BindingFlags.Public | BindingFlags.Instance);
            if (rootProperty?.GetValue(toolbar) is VisualElement propertyRoot)
                return propertyRoot;

            return null;
        }

        private static void DrawToolbar()
        {
            if (Scenes.Count == 0)
                return;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Scenes", EditorStyles.miniLabel, GUILayout.Width(42f));
                var activeScenePath = SceneManager.GetActiveScene().path;

                for (var i = 0; i < Scenes.Count; i++)
                {
                    var entry = Scenes[i];
                    var isActive = string.Equals(activeScenePath, entry.Path, StringComparison.OrdinalIgnoreCase);
                    var pressed = GUILayout.Toggle(isActive, entry.DisplayName, EditorStyles.toolbarButton, GUILayout.Height(22f));

                    if (!isActive && pressed)
                        OpenScene(entry.Path);
                }
            }
        }

        private static void RefreshScenes()
        {
            Scenes.Clear();

            var absoluteRoot = Path.GetFullPath(ScenesRoot);
            if (!Directory.Exists(absoluteRoot))
                return;

            var sceneFiles = Directory.GetFiles(absoluteRoot, "*.unity", SearchOption.AllDirectories);
            Array.Sort(sceneFiles, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < sceneFiles.Length; i++)
            {
                var relativePath = ToAssetPath(sceneFiles[i]);
                Scenes.Add(new SceneEntry(relativePath, Path.GetFileNameWithoutExtension(relativePath)));
            }

            toolbarContainer?.MarkDirtyRepaint();
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
