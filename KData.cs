#if UNITY_EDITOR
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditorInternal;
using UnityEngine;

namespace Kingfisher.KSetting
{
    public static class KData
    {
        #region Fields

        private const string InstallFolderPath = "Assets/ThirdParty/KingfisherTools";

        private static string _folderPath;

        #endregion

        #region Properties

        public static string FolderPath => _folderPath ??= KSettingFolderPath().GetParentPath().CombinePath(".KData");

        #endregion

        #region Public Methods

        public static string GetFilePath(string fileName) => FolderPath.CombinePath(fileName);

        public static bool Exists(string fileName) => File.Exists(GetFilePath(fileName));

        public static T Load<T>(string fileName) where T : ScriptableObject
        {
            var filePath = GetFilePath(fileName);

            if (!File.Exists(filePath)) return null;

            var loaded = InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            for (var i = 0; i < loaded.Length; i++)
                if (loaded[i] is T typed)
                    return typed;

            return null;
        }

        public static T LoadOrCreate<T>(string fileName) where T : ScriptableObject
        {
            if (Load<T>(fileName) is { } loaded) return loaded;

            var created = ScriptableObject.CreateInstance<T>();

            created.hideFlags = HideFlags.HideAndDontSave;

            return created;
        }

        public static void Save(ScriptableObject asset, string fileName)
        {
            if (!asset) return;

            Directory.CreateDirectory(FolderPath);

            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { asset }, GetFilePath(fileName), allowTextSerialization: true);
        }

        #endregion

        #region Private Methods

        private static string KSettingFolderPath([CallerFilePath] string callerPath = "")
        {
            var path = callerPath.Replace('\\', '/').GetParentPath();

            var assetsIndex = path.LastIndexOf("/Assets/", System.StringComparison.Ordinal);

            if (assetsIndex != -1) return path.Substring(assetsIndex + 1);

            // Installed as a package, so callerPath points inside
            // Library/PackageCache - which Unity wipes on every package update.
            // Save into the project at the same place the .unitypackage installs
            // use, so data survives updates and carries over between the two.
            return InstallFolderPath.CombinePath(path.Substring(path.LastIndexOf('/') + 1));
        }

        private static string GetParentPath(this string path) => path.LastIndexOf('/') is var i && i > 0 ? path.Substring(0, i) : path;

        private static string CombinePath(this string path, string name) => path + "/" + name;

        #endregion
    }
}
#endif
