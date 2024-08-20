
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KC
{
    [CustomEditor(typeof(BaseBinderView), true)]
    public class BinderGenerator : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (GUILayout.Button("Build"))
            {
                Build();
            }

            if (GUILayout.Button("Import"))
            {
                Import();
            }
        }

        private void Build()
        {
            if (!target.CheckValid())
            {
                return;
            }

            Debug.Log(target.GetType().Assembly);
            
            var info = target.GetClassInfo();
            if (info == null)
            {
                return;
            }
            
            
            string code = ((BaseBinderView)target).transform.ToCode(info);

            string path = $"{Path.Combine(Application.dataPath, info.Path, info.ClassName)}.cs";
            BinderGenerateHelper.ToFile(path,code);
            
            AssetDatabase.Refresh();
            Debug.Log($"构建 {info.ClassName} 到路径:{path} 成功");
        }

        private void Import()
        {
            if (!target.CheckValid())
            {
                return;
            }

            var import = target.GetImportCodeObject();
            if (import == null)
            {
                return;
            }

            var message = import.ImportToView(target);
            Debug.Log(message);
        }
    }
}