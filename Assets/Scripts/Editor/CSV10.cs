using System.IO;
using Codice.Client.Common.Threading;
using UnityEditor;
using UnityEngine;

namespace RaytracingUnity.Editor
{
    public class Csv10
    {
        private static FileSystemWatcher _watcher;

        [InitializeOnLoadMethod]
        public static void OnLoad()
        {
            _watcher = new FileSystemWatcher(Path.Combine(Application.dataPath, ".."));

            _watcher.NotifyFilter =   NotifyFilters.Attributes
                                      | NotifyFilters.CreationTime
                                      | NotifyFilters.DirectoryName
                                      | NotifyFilters.FileName
                                      | NotifyFilters.LastAccess
                                      | NotifyFilters.LastWrite
                                      | NotifyFilters.Security
                                      | NotifyFilters.Size;
            
            _watcher.Changed += OnChanged;

            _watcher.Filter = "*.csproj";
            _watcher.IncludeSubdirectories = false;
            _watcher.EnableRaisingEvents = true;
           }
        
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed) return;
            
            var content = File.ReadAllText(e.FullPath);
            
            if (!content.Contains("<LangVersion>9.0</LangVersion>")) return;
            
            content = content.Replace("<LangVersion>9.0</LangVersion>", "<LangVersion>10</LangVersion>");
            File.WriteAllText(e.FullPath, content);
        }
    }
}