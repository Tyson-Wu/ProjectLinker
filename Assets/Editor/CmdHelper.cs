using System.Diagnostics;
using UnityEditor;

namespace ProjectLinker
{
    static class CmdHelper
    {
        public static void LinkFolder(string orgPath, string linkPath)
        {
            ProcessStartInfo start = new ProcessStartInfo("cmd.exe");
            start.CreateNoWindow = true;
            start.UseShellExecute = false;
            start.RedirectStandardInput = true;
            var process = Process.Start(start);
            var sw = process.StandardInput;
            sw.WriteLine($"mklink /D \"{linkPath}\" \"{orgPath}\"");
            sw.Close();
            process.WaitForExit();
            process.Close();
        }
        public static void LaunchUnityProject(string projectFullPath, string buildTarget)
        {
            string editorPath = EditorApplication.applicationPath;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate (object state)
            {
                Process p = null;
                try
                {
                    string arg = $"-projectPath \"{projectFullPath}\"";
                    if (!string.IsNullOrEmpty(buildTarget))
                    {
                        arg += $" -buildTarget {buildTarget}";
                    }
                    ProcessStartInfo start = new ProcessStartInfo(editorPath, arg);
                    start.CreateNoWindow = false;
                    start.UseShellExecute = false;

                    p = Process.Start(start);
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    if (p != null)
                    {
                        p.Close();
                    }
                }
            });

        }
    }
}