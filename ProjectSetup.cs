using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Codice.Utils;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;

namespace gishadev
{
    public static class ProjectSetup
    {
        [MenuItem("Tools/Setup/Create Folders")]
        public static void CreateFolders()
        {
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
        }

        [MenuItem("Tools/Setup/Import Essentials")]
        static void ImportEssentials()
        {
            Packages.InstallPackages(new[]
            {
                "com.unity.2d.animation",
                "com.unity.cinemachine",
                "com.unity.inputsystem"
            });
        }

        [MenuItem("Tools/Setup/Import polishing tools")]
        static void ImportPolishingTools()
        {
            // Assets.ImportAsset("PrimeTween High-Performance Animations and Sequences.unitypackage", "Kyrylo Kuzyk/Editor ExtensionsAnimation");
            Packages.InstallPackages(new[]
            {
                "com.kyrylokuzyk.primetween",
                "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.16.9",
                "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                "https://github.com/gishadev/gishadev-tools-polish.git"
            });
        }

        [MenuItem("Tools/Setup/Import Walking Sim Template")]
        static void ImportWalkingSim()
        {
            Packages.InstallPackages(new[]
            {
                "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.16.9",
                "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
            });
        }
        
        [MenuItem("Tools/Setup/Import Odin")]
        static void ImportOdin()
        {
            Assets.ImportAsset("Odin Inspector and Serializer.unitypackage", "Sirenix/Editor ExtensionsSystem");
        }
        
        [MenuItem("Tools/Setup/Import Editor Helpers")]
        static void ImportEditorHelpers()
        {
            Assets.ImportAsset("vFolders 2.unitypackage", "kubacho lab/Editor ExtensionsUtilities");
            Assets.ImportAsset("vFavorites 2.unitypackage", "kubacho lab/Editor ExtensionsUtilities");
        }

        static class Assets
        {
            public static void ImportAsset(string asset, string folder)
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
#endif

                asset = asset.EndsWith(".unitypackage") ? asset : asset + ".unitypackage";
                string fullPath = Combine(basePath, folder, asset);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"The asset package was not found at the path: {fullPath}");

                ImportPackage(fullPath, false);
            }
        }

        static class Packages
        {
            static AddRequest _request;
            static readonly Queue<string> PackagesToInstall = new();

            public static void InstallPackages(string[] packages)
            {
                foreach (var package in packages)
                    PackagesToInstall.Enqueue(package);

                if (PackagesToInstall.Count > 0)
                    StartNextPackageInstallation();
            }

            static async void StartNextPackageInstallation()
            {
                _request = Client.Add(PackagesToInstall.Dequeue());

                while (!_request.IsCompleted) await Task.Delay(10);

                if (_request.Status == StatusCode.Success) Debug.Log("Installed: " + _request.Result.packageId);
                else if (_request.Status >= StatusCode.Failure) Debug.LogError(_request.Error.message);

                if (PackagesToInstall.Count > 0)
                {
                    await Task.Delay(1000);
                    StartNextPackageInstallation();
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
        }
    }
}