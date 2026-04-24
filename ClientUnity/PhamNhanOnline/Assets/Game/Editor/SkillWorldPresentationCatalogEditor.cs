using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using PhamNhanOnline.Client.Features.Combat.Presentation;

namespace PhamNhanOnline.Client.Editor
{
    [CustomEditor(typeof(SkillWorldPresentationCatalog))]
    public sealed class SkillWorldPresentationCatalogEditor : UnityEditor.Editor
    {
        private const string DefaultCatalogAssetPath =
            "Assets/Game/Content/ScriptableObjects/Combat/SkillWorldPresentationCatalog.asset";

        [MenuItem("Tools/Game/Combat/Sync Skill World Presentation Catalog")]
        private static void SyncDefaultCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SkillWorldPresentationCatalog>(DefaultCatalogAssetPath);
            if (catalog == null)
            {
                EditorUtility.DisplayDialog(
                    "Skill World Catalog Sync",
                    $"Khong tim thay catalog tai '{DefaultCatalogAssetPath}'.",
                    "OK");
                return;
            }

            SyncCatalog(catalog);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "DB la nguon trust. Sync se them skill_group_code moi tu DB, xoa group/skill override khong con ton tai trong DB, va chuan hoa key cua override theo DB.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Sync From DB", GUILayout.Width(140f), GUILayout.Height(28f)))
                    SyncCatalog((SkillWorldPresentationCatalog)target);
            }
        }

        private static void SyncCatalog(SkillWorldPresentationCatalog catalog)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Skill World Catalog Sync", "Dang doc skill tu DB...", 0.2f);

                var projectRoot = ResolveProjectRoot();
                var toolProjectPath = Path.Combine(projectRoot, "CientTest", "SkillWorldCatalogSyncTool", "SkillWorldCatalogSyncTool.csproj");
                if (!File.Exists(toolProjectPath))
                    throw new FileNotFoundException("Khong tim thay SkillWorldCatalogSyncTool.csproj.", toolProjectPath);

                var payloadJson = RunExportCommand(projectRoot, toolProjectPath);
                var payload = JsonUtility.FromJson<SkillWorldCatalogSyncPayload>(payloadJson);
                if (payload?.skills == null)
                    throw new InvalidOperationException("Khong parse duoc payload skill tu tool sync.");

                EditorUtility.DisplayProgressBar("Skill World Catalog Sync", "Dang cap nhat catalog...", 0.8f);

                var summary = catalog.ApplyDatabaseSync(payload.skills);
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Skill World Catalog Sync",
                    BuildSummaryMessage(summary),
                    "OK");
            }
            catch (Exception exception)
            {
                EditorUtility.ClearProgressBar();
                UnityEngine.Debug.LogError($"[SkillWorldPresentationCatalogEditor] Sync that bai: {exception}");
                EditorUtility.DisplayDialog(
                    "Skill World Catalog Sync",
                    $"Sync that bai:{Environment.NewLine}{exception.Message}",
                    "OK");
            }
        }

        private static string ResolveProjectRoot()
        {
            var current = Directory.GetParent(Application.dataPath)?.FullName;
            while (!string.IsNullOrWhiteSpace(current))
            {
                var hasGameServer = Directory.Exists(Path.Combine(current, "GameServer"));
                var hasClientUnity = Directory.Exists(Path.Combine(current, "ClientUnity"));
                var hasCientTest = Directory.Exists(Path.Combine(current, "CientTest"));
                if (hasGameServer && hasClientUnity && hasCientTest)
                    return current;

                current = Directory.GetParent(current)?.FullName;
            }

            throw new DirectoryNotFoundException("Khong resolve duoc root cua repo.");
        }

        private static string RunExportCommand(string workingDirectory, string toolProjectPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{toolProjectPath}\" --no-launch-profile",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("Khong the khoi dong process dotnet.");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Tool DB sync tra ve exit code {process.ExitCode}.{Environment.NewLine}{error}");
            }

            if (string.IsNullOrWhiteSpace(output))
                throw new InvalidOperationException("Tool DB sync khong tra ve du lieu.");

            return output;
        }

        private static string BuildSummaryMessage(SkillWorldPresentationCatalog.EditorSyncSummary summary)
        {
            return
                $"DB skills: {summary.DatabaseSkillCount}{Environment.NewLine}" +
                $"DB skill groups: {summary.DatabaseSkillGroupCount}{Environment.NewLine}" +
                $"Group them moi: {summary.AddedGroupCount}{Environment.NewLine}" +
                $"Group bi xoa: {summary.RemovedGroupCount}{Environment.NewLine}" +
                $"Skill override bi xoa: {summary.RemovedOverrideCount}{Environment.NewLine}" +
                $"Skill override duoc chuan hoa: {summary.NormalizedOverrideCount}";
        }

        [Serializable]
        private sealed class SkillWorldCatalogSyncPayload
        {
            public SkillWorldPresentationCatalog.EditorDatabaseSkillRecord[] skills =
                Array.Empty<SkillWorldPresentationCatalog.EditorDatabaseSkillRecord>();
        }
    }
}
