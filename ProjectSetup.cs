using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;

namespace gishadev
{
    public readonly struct SetupStep
    {
        public readonly string Name;
        public readonly string Description;
        public readonly Func<Action<string>, Task> Run;

        public SetupStep(string name, string description, Func<Action<string>, Task> run)
        {
            Name = name;
            Description = description;
            Run = run;
        }
    }

    public static class ProjectSetup
    {
        public static readonly SetupStep[] Steps =
        {
            new("Create Folders",
                "Scaffold the standard _Project folder structure.",
                CreateFolders),
            new("Import Essentials",
                "Packages every project needs: 2D Animation, Cinemachine, Input System, UniTask, TextMeshPro.",
                ImportEssentials),
            new("Import Main Tools",
                "VContainer, PrimeTween, com.gishadev.tools and generated pool enums.",
                ImportMainTools),
            new("Import Odin",
                "Odin Inspector & Serializer (from Asset Store cache).",
                ImportOdin),
            new("Import Editor Helpers",
                "vFolders 2 and vFavorites 2 (from Asset Store cache).",
                ImportEditorHelpers),
        };

        static async Task CreateFolders(Action<string> log)
        {
            log("Creating folder structure...");

            Folders.Create("_Project", "Materials", "Prefabs", "Scripts");
            Refresh();
            Folders.Move("_Project", "Scenes");
            Folders.Move("_Project", "Settings");
            Folders.Delete("TutorialInfo");
            Refresh();
            MoveAsset("Assets/InputSystem_Actions.inputactions",
                "Assets/_Project/Settings/InputSystem_Actions.inputactions");
            DeleteAsset("Assets/Readme.asset");
            Refresh();

            log("✓ Folder structure created.");
            await Task.CompletedTask;
        }

        static async Task ImportEssentials(Action<string> log)
        {
            await Packages.InstallPackages(new[]
            {
                "com.unity.2d.animation",
                "com.unity.cinemachine",
                "com.unity.inputsystem",
                "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
            }, log);

            await Assets.ImportTmpEssentials(log);
        }

        static async Task ImportMainTools(Action<string> log)
        {
            await Assets.ImportAsset("PrimeTween High-Performance Animations and Sequences.unitypackage",
                "Kyrylo Kuzyk/Editor ExtensionsAnimation", log);

            Folders.CreateEnum("MusicAudioEnum", log);
            Folders.CreateEnum("SFXAudioEnum", log);
            Folders.CreateEnum("SFXPoolEnum", log);
            Folders.CreateEnum("VFXPoolEnum", log);
            Folders.CreateEnum("OtherPoolEnum", log);
            Folders.CreateExtensionsClass(log);

            await Packages.InstallPackages(new[]
            {
                "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.16.9",
                "https://github.com/gishadev/gishadev-tools.git"
            }, log);
        }

        static async Task ImportOdin(Action<string> log)
        {
            await Assets.ImportAsset("Odin Inspector and Serializer.unitypackage", "Sirenix/Editor ExtensionsSystem",
                log);
        }

        static async Task ImportEditorHelpers(Action<string> log)
        {
            await Assets.ImportAsset("vFolders 2.unitypackage", "kubacho lab/Editor ExtensionsUtilities", log);
            await Assets.ImportAsset("vFavorites 2.unitypackage", "kubacho lab/Editor ExtensionsUtilities", log);
        }

        static class Assets
        {
            public static Task ImportAsset(string asset, string folder, Action<string> log)
            {
                string basePath;
#if UNITY_EDITOR_WIN
                string defaultPath = Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Unity");
                basePath = Combine(EditorPrefs.GetString("AssetStoreCacheRootPath", defaultPath),
                    "Asset Store-5.x");
#elif UNITY_EDITOR_OSX
                    string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    basePath = Combine(homeDirectory, "Library/Unity/Asset Store-5.x");
#else
                string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                basePath = Combine(EditorPrefs.GetString("AssetStoreCacheRootPath",
                        Combine(homeDirectory, ".local/share/unity3d")),
                    "Asset Store-5.x");
#endif

                asset = asset.EndsWith(".unitypackage") ? asset : asset + ".unitypackage";
                string fullPath = Combine(basePath, folder, asset);
                string displayName = GetFileNameWithoutExtension(asset);

                if (!File.Exists(fullPath))
                {
                    log($"✗ {displayName} not found at: {fullPath}");
                    return Task.CompletedTask;
                }

                return AwaitImport(displayName, () => ImportPackage(fullPath, false), log);
            }

            public static Task ImportTmpEssentials(Action<string> log)
            {
                return AwaitImport("TMP Essential Resources",
                    () => TMP_PackageResourceImporter.ImportResources(true, false, false), log);
            }

            static Task AwaitImport(string displayName, Action startImport, Action<string> log)
            {
                var tcs = new TaskCompletionSource<bool>();

                void OnCompleted(string packageName)
                {
                    AssetDatabase.importPackageCompleted -= OnCompleted;
                    AssetDatabase.importPackageFailed -= OnFailed;
                    log($"✓ Imported {displayName}");
                    tcs.TrySetResult(true);
                }

                void OnFailed(string packageName, string errorMessage)
                {
                    AssetDatabase.importPackageCompleted -= OnCompleted;
                    AssetDatabase.importPackageFailed -= OnFailed;
                    log($"✗ Failed to import {displayName}: {errorMessage}");
                    tcs.TrySetResult(false);
                }

                AssetDatabase.importPackageCompleted += OnCompleted;
                AssetDatabase.importPackageFailed += OnFailed;

                log($"Importing {displayName}...");
                startImport();

                return tcs.Task;
            }
        }

        static class Packages
        {
            public static async Task InstallPackages(string[] packages, Action<string> log)
            {
                foreach (var package in packages)
                {
                    log($"Installing {package}...");

                    var request = Client.Add(package);
                    while (!request.IsCompleted) await Task.Delay(100);

                    if (request.Status == StatusCode.Success)
                        log($"✓ Installed {request.Result.packageId}");
                    else if (request.Status >= StatusCode.Failure)
                        log($"✗ Failed to install {package}: {request.Error.message}");
                }
            }
        }

        static class Folders
        {
            public static void Create(string root, params string[] folders)
            {
                var fullpath = Combine(Application.dataPath, root);
                if (!Directory.Exists(fullpath)) Directory.CreateDirectory(fullpath);

                foreach (var folder in folders)
                    CreateSubFolders(fullpath, folder);
            }

            static void CreateSubFolders(string rootPath, string folderHierarchy)
            {
                var folders = folderHierarchy.Split('/');
                var currentPath = rootPath;

                foreach (var folder in folders)
                {
                    currentPath = Combine(currentPath, folder);
                    if (!Directory.Exists(currentPath)) Directory.CreateDirectory(currentPath);
                }
            }

            public static void Move(string newParent, string folderName)
            {
                var sourcePath = $"Assets/{folderName}";
                if (IsValidFolder(sourcePath))
                {
                    var destinationPath = $"Assets/{newParent}/{folderName}";
                    var error = MoveAsset(sourcePath, destinationPath);

                    if (!string.IsNullOrEmpty(error))
                        Debug.LogError($"Failed to move {folderName}: {error}");
                }
            }

            public static void Delete(string folderName)
            {
                var pathToDelete = $"Assets/{folderName}";

                if (IsValidFolder(pathToDelete))
                    DeleteAsset(pathToDelete);
            }

            public static void CreateEnum(string enumName, Action<string> log)
            {
                string path = $"Assets/_Project/Scripts/Generated/{enumName}.cs";

                var str = new StringBuilder();
                str.AppendLine();
                str.AppendLine($"public enum {enumName}");
                str.AppendLine("{");
                str.AppendLine("}");

                Directory.CreateDirectory(GetDirectoryName(path) ?? string.Empty);
                File.WriteAllText(path, str.ToString());
                ImportAsset(path);

                log($"✓ Created empty enum: {path}");
            }

            public static void CreateExtensionsClass(Action<string> log)
            {
                string path = "Assets/_Project/Scripts/Generated/GeneratedExtensionMethods.cs";

                var str = new StringBuilder();
                str.AppendLine("using gishadev.tools.Audio;");
                str.AppendLine("using gishadev.tools.Effects;");
                str.AppendLine("using UnityEngine;");
                str.AppendLine();
                str.AppendLine("public static class GeneratedExtensionMethods");
                str.AppendLine("{");
                str.AppendLine("    public static void PlayMusic(this IAudioManager @this, MusicAudioEnum music) => @this.PlayMusic((int)music);");
                str.AppendLine("    public static void PlaySFX(this IAudioManager @this, SFXAudioEnum sfx) => @this.PlaySFX((int)sfx);");
                str.AppendLine("    public static GameObject EmitAt(this ISFXEmitter @this, SFXPoolEnum sfx, Vector3 position, Quaternion? rotation = null) => @this.EmitAt((int)sfx, position, rotation);");
                str.AppendLine("    public static GameObject EmitAt(this IVFXEmitter @this, VFXPoolEnum vfx, Vector3 position, Quaternion? rotation = null) => @this.EmitAt((int)vfx, position, rotation);");
                str.AppendLine("    public static GameObject EmitAt(this IOtherEmitter @this, OtherPoolEnum other, Vector3 position, Quaternion? rotation = null) => @this.EmitAt((int)other, position, rotation);");
                str.AppendLine("}");

                Directory.CreateDirectory(GetDirectoryName(path) ?? string.Empty);
                File.WriteAllText(path, str.ToString());
                ImportAsset(path);

                log($"✓ Created extension methods: {path}");
            }
        }
    }
}
