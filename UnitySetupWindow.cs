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

        readonly HashSet<string> _selected = new();
        readonly List<string> _log = new();
        Vector2 _optionsScroll;
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
            GUILayout.Label("Select what to set up", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (Logo != null)
                GUILayout.Label(Logo, GUILayout.Width(48), GUILayout.Height(48));
            GUILayout.EndHorizontal();
            
            _optionsScroll = GUILayout.BeginScrollView(_optionsScroll, GUILayout.MaxHeight(200));
            foreach (var step in ProjectSetup.Steps)
            {
                bool wasSelected = _selected.Contains(step.Name);
                bool isSelected = EditorGUILayout.ToggleLeft(step.Name, wasSelected, EditorStyles.boldLabel);
                if (isSelected != wasSelected)
                {
                    if (isSelected) _selected.Add(step.Name);
                    else _selected.Remove(step.Name);
                }

                EditorGUILayout.LabelField(step.Description, EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All")) SelectAll(true);
            if (GUILayout.Button("Select None")) SelectAll(false);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            using (new EditorGUI.DisabledScope(_running || _selected.Count == 0))
            {
                if (GUILayout.Button("Run Selected", GUILayout.Height(28)))
                    Run();
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

        void SelectAll(bool select)
        {
            _selected.Clear();
            if (!select) return;

            foreach (var step in ProjectSetup.Steps)
                _selected.Add(step.Name);
        }

        async void Run()
        {
            _running = true;
            _log.Clear();

            foreach (var step in ProjectSetup.Steps)
            {
                if (!_selected.Contains(step.Name)) continue;

                Log($"── {step.Name} ──");
                try
                {
                    await step.Run(Log);
                }
                catch (Exception e)
                {
                    Log($"✗ {step.Name} failed: {e.Message}");
                }
            }

            Log("Done.");
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
