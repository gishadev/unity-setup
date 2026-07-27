using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace gishadev
{
    public class UnitySetupWindow : EditorWindow
    {
        static Texture2D _logo;

        readonly List<string> _log = new();
        Vector2 _logScroll;
        bool _running;

        [MenuItem("Tools/gishadev/Unity Setup")]
        public static void Open()
        {
            var window = GetWindow<UnitySetupWindow>("Unity Setup");
            window.minSize = new Vector2(420, 480);
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Unity Setup", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (Logo != null)
                GUILayout.Label(Logo, GUILayout.Width(48), GUILayout.Height(48));
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            foreach (var step in ProjectSetup.Steps)
            {
                GUILayout.BeginVertical("box");

                GUILayout.BeginHorizontal();
                GUILayout.Label(step.Name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_running))
                {
                    if (GUILayout.Button("Run", GUILayout.Width(60)))
                        Run(step);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField(step.Description, EditorStyles.wordWrappedMiniLabel);

                GUILayout.EndVertical();
            }

            GUILayout.Space(8);
            GUILayout.Label("Log", EditorStyles.boldLabel);

            _logScroll = GUILayout.BeginScrollView(_logScroll, "box", GUILayout.ExpandHeight(true));
            foreach (var line in _log)
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            GUILayout.EndScrollView();

            if (GUILayout.Button("Clear Log"))
                _log.Clear();
        }

        async void Run(SetupStep step)
        {
            _running = true;
            Log($"── {step.Name} ──");

            // Installing a package (e.g. VContainer, tools-polish) that brings new scripts queues a
            // domain reload once it compiles. That reload wipes this running method mid-flight, so any
            // later package in the same step (e.g. tools-polish right after VContainer) never installs.
            // Locking defers the reload until we're done with the whole step.
            EditorApplication.LockReloadAssemblies();
            try
            {
                await step.Run(Log);
                Log("Done.");
            }
            catch (Exception e)
            {
                Log($"✗ {step.Name} failed: {e.Message}");
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }

            _running = false;
        }

        void Log(string line)
        {
            _log.Add(line);
            _logScroll.y = Mathf.Infinity;
            Repaint();
        }

        static Texture2D Logo
        {
            get
            {
                if (_logo == null)
                    _logo = AssetDatabase.LoadAssetAtPath<Texture2D>(GetAssetPath("logo.png"));

                return _logo;
            }
        }

        static string GetAssetPath(string fileName, [CallerFilePath] string sourceFilePath = "")
        {
            string directory = Path.GetDirectoryName(sourceFilePath)?.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            string relativeDirectory = "Assets" + directory?.Substring(dataPath.Length);
            return $"{relativeDirectory}/{fileName}";
        }
    }
}
