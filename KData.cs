#if UNITY_EDITOR
using System.IO;
using UnityEditorInternal;
using UnityEngine;

namespace Kingfisher.KSetting
{
    public static class KData
    {
        #region Fields

        private const string FolderName = ".KData";

        private const string GitIgnoreFileName = ".gitignore";

        private const string GitIgnoreContents = "# Kingfisher Tools editor data - local to this machine.\n" +
                                                 "# Delete this file to commit the folder instead.\n" +
                                                 "*\n";

        private static string _folderPath;

        #endregion

        #region Properties

        public static string FolderPath => _folderPath ??= Application.dataPath.GetParentPath().CombinePath(FolderName);

        public static string RelativeFolderPath => FolderName;

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

            EnsureFolder();

            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { asset }, GetFilePath(fileName), allowTextSerialization: true);
        }

        #endregion

        #region Private Methods

        private static void EnsureFolder()
        {
            Directory.CreateDirectory(FolderPath);

            var gitIgnorePath = FolderPath.CombinePath(GitIgnoreFileName);

            if (File.Exists(gitIgnorePath)) return;

            File.WriteAllText(gitIgnorePath, GitIgnoreContents);
        }

        private static string GetParentPath(this string path) => path.LastIndexOf('/') is var i && i > 0 ? path.Substring(0, i) : path;

        private static string CombinePath(this string path, string name) => path + "/" + name;

        #endregion
    }
}
#endif
