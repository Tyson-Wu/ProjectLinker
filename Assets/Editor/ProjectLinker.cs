using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.IMGUI.Controls;
namespace ProjectLinker
{
    public class ProjectLinker : EditorWindow
    {
        [MenuItem("Window/Project Linker")]
        private static void Open()
        {
            var win = EditorWindow.GetWindow<ProjectLinker>("关联项目快捷启动");
            win.Show();
        }

        private const string _CACHE_TABLE_HEADER = "ProjectLinker_Table_";
        private string _copyProjectName = "";
        private string _defaultCopyProjectName = "";
        private string _curProjectPath = "";
        private string _rootPath = "";
        [SerializeField] TreeViewState _treeViewState;
        ProjectTreeView _treeView;

        CacheDataTable _cacheDataTable = null;
        int _selectedElemIndex = -1;

        CacheDataElem selectedElem
        {
            get
            {
                if (_selectedElemIndex > -1 && _selectedElemIndex < _cacheDataTable.elems.Count)
                {
                    return _cacheDataTable.elems[_selectedElemIndex];
                }
                return null;
            }
        }
        private void OnEnable()
        {
            _curProjectPath = Path.GetDirectoryName(Application.dataPath);
            _rootPath = Path.GetDirectoryName(_curProjectPath);
            _defaultCopyProjectName = Path.GetFileName(_curProjectPath) + "_copy";
            _copyProjectName = _defaultCopyProjectName;
            LoadCache();

            if (_treeViewState == null)
                _treeViewState = new TreeViewState();
            _treeView = new ProjectTreeView(_cacheDataTable, _treeViewState);
            _treeView.OnSelected -= OnSelected;
            _treeView.OnSelected += OnSelected;
            _treeView.OnDataChange -= SaveCache;
            _treeView.OnDataChange += SaveCache;
        }
        private void OnSelected(int index)
        {
            _selectedElemIndex = index;
        }
        private void DrawTree()
        {
            Rect rect = GUILayoutUtility.GetRect(1000, 10000, 0, 10000);
            _treeView.OnGUI(rect);
        }
        private void DrawElemInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("选中项目详情", EditorStyles.label);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField(selectedElem.buildTarget, GUILayout.Width(100));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            selectedElem.name = EditorGUILayout.TextField("项目名称", selectedElem.name);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("项目路径", selectedElem.projectPath);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
            {
                SaveCache();
                _treeView.Reload();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(50));
            if (GUILayout.Button("删除"))
            {
                _cacheDataTable.elems.RemoveAt(_selectedElemIndex);
                SaveCache();
                _treeView.Reload();
            }
            if (GUILayout.Button("重置"))
            {
                selectedElem.name = Path.GetFileName(selectedElem.projectPath);
                SaveCache();
                _treeView.Reload();
            }
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("打开", GUILayout.Width(50), GUILayout.ExpandHeight(true)))
            {
                CmdHelper.LaunchUnityProject(selectedElem.projectPath, selectedElem.buildTarget);
                Close();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        private void OnGUI()
        {
            DrawTree();
            if (selectedElem != null)
            {
                DrawElemInfo();
            }
            GUILayout.FlexibleSpace();
            DrawButton();
        }
        private void DrawButton()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("关联已有项目", GUILayout.Width(100), GUILayout.ExpandHeight(true)))
            {
                var folder = EditorUtility.OpenFolderPanel("", _rootPath, "");
                if (!string.IsNullOrEmpty(folder))
                {
                    folder = folder.Replace('/', '\\');
                    if (folder == _curProjectPath)
                    {
                        Debug.LogError("不能关联当前项目本身");
                    }
                    else
                    {
                        DirectoryInfo folderInfo = new DirectoryInfo(folder);
                        var childDirec = folderInfo.GetDirectories("Assets", SearchOption.TopDirectoryOnly);
                        bool valide = childDirec.Length == 1;
                        if (valide)
                        {
                            bool hasExisted = false;
                            for (int i = 0; i < _cacheDataTable.elems.Count; ++i)
                            {
                                if (_cacheDataTable.elems[i].projectPath == folderInfo.FullName)
                                {
                                    hasExisted = true;
                                    _selectedElemIndex = i + 1;
                                    _treeView.SetSelection(new List<int> { _selectedElemIndex });
                                    break;
                                }
                            }
                            if (!hasExisted)
                            {
                                AddItem(folderInfo);
                            }
                        }
                        else
                        {
                            Debug.LogError("关联的目录不是Unity项目");
                        }
                    }
                }
            }
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("当前项目复制版");
            _copyProjectName = EditorGUILayout.TextField("项目名称:", _copyProjectName);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_copyProjectName));
            if (GUILayout.Button("创建"))
            {
                var folder = EditorUtility.OpenFolderPanel("", _rootPath, "");
                if (!string.IsNullOrEmpty(folder))
                {
                    string path = Path.Combine(folder, _copyProjectName);
                    if (Directory.Exists(path))
                    {
                        Debug.LogError("工程名已经存在");
                    }
                    else
                    {
                        DirectoryInfo folderInfo = new DirectoryInfo(path);
                        try
                        {
                            folderInfo.Create();
                            LinkProject(folderInfo);
                            AddItem(folderInfo, true);
                        }
                        catch
                        {
                            Debug.LogError("Error");
                        }
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }
        private void LinkProject(DirectoryInfo folderInfo)
        {
            string orgPath = Path.Combine(_curProjectPath, "Assets");
            string copyPath = Path.Combine(folderInfo.FullName, "Assets");
            CmdHelper.LinkFolder(orgPath, copyPath);
            orgPath = Path.Combine(_curProjectPath, "ProjectSettings");
            copyPath = Path.Combine(folderInfo.FullName, "ProjectSettings");
            CmdHelper.LinkFolder(orgPath, copyPath);
            orgPath = Path.Combine(_curProjectPath, "Packages");
            copyPath = Path.Combine(folderInfo.FullName, "Packages");
            CmdHelper.LinkFolder(orgPath, copyPath);
        }
        private void AddItem(DirectoryInfo folderInfo, bool copyTarget = false)
        {
            CacheDataElem elem = new CacheDataElem
            {
                name = folderInfo.Name,
                projectPath = folderInfo.FullName
            };
            if (copyTarget)
            {
                //elem.buildTarget = BuildPipeline.GetBuildTargetName(EditorUserBuildSettings.activeBuildTarget);
            }
            _cacheDataTable.elems.Add(elem);
            SaveCache();
            _treeView.Reload();
            _treeView.SetSelection(new List<int> { _cacheDataTable.elems.Count });
            _selectedElemIndex = _cacheDataTable.elems.Count - 1;
        }
        private void LoadCache()
        {
            var tableStr = EditorPrefs.GetString(_CACHE_TABLE_HEADER + Application.dataPath, null);
            if (string.IsNullOrEmpty(tableStr))
            {
                _cacheDataTable = new CacheDataTable();
            }
            else
            {
                _cacheDataTable = JsonUtility.FromJson<CacheDataTable>(tableStr);
            }
        }
        private void SaveCache()
        {
            var tableStr = JsonUtility.ToJson(_cacheDataTable);
            EditorPrefs.SetString(_CACHE_TABLE_HEADER + Application.dataPath, tableStr);
        }
    }
}