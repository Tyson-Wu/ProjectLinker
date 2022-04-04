using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
namespace ProjectLinker
{
    class ProjectTreeView : TreeView
    {
        private CacheDataTable _tableData;
        public event Action<int> OnSelected;
        public event Action OnDataChange;
        public ProjectTreeView(CacheDataTable table, TreeViewState state) : base(state)
        {
            _tableData = table;
            showAlternatingRowBackgrounds = false;
            Reload();
        }
        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem { id = 0, depth = -1 };
        }
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = GetRows() ?? new List<TreeViewItem>(10);
            rows.Clear();
            var iconTex = EditorGUIUtility.FindTexture("Folder Icon");
            for (int i = 0; i < _tableData.elems.Count; ++i)
            {
                var item = new TreeViewItem { id = i + 1, depth = 0, displayName = _tableData.elems[i].name };
                item.icon = iconTex;
                root.AddChild(item);
                rows.Add(item);
            }
            SetupDepthsFromParentsAndChildren(root);
            return rows;
        }
        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
            float width = 16f;
            Rect deleRect = new Rect(args.rowRect.width - width - 10, args.rowRect.y, width, width);
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && deleRect.Contains(evt.mousePosition))
                SelectionClick(args.item, false);
            if (GUI.Button(deleRect, "x"))
            {
                _tableData.elems.RemoveAt(args.item.id - 1);
                Reload();
                OnDataChange?.Invoke();
            }
        }
        protected override void SingleClickedItem(int id)
        {
            base.SingleClickedItem(id);
            int index = id - 1;
            OnSelected?.Invoke(index);
        }
    }
}