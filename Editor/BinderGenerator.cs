
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
                Debug.LogWarning("运行时为了避免误操作,无法生成代码");
                return;
            }

            if (!GUILayout.Button("Build"))
            {
                return;
            }

            if (!target.CheckValid())
            {
                return;
            }

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
    }
}