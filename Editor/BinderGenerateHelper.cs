using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KC
{
    public static class BinderGenerateHelper
    {
        private const string Space4 = "    ";
        private const string Space8 = "         ";
        private const string Space12 = "            ";
        
        internal static BinderClassInfo GetClassInfo(this Object target)
        {
            var type = target.GetType();

            var attribute = type.GetCustomAttribute<BinderAttribute>();
            if (attribute == null)
            {
                Debug.LogWarning($"本次构建Binder无效,请确保目标:{target.GetType().FullName}拥有标签:BinderAttribute");
                return null;
            }

            var classInfo = new BinderClassInfo()
            {
                ClassName = type.Name[..^4],
                Annotation = attribute.Annotation,
                Path = attribute.Path,
            };

            var fields = type.GetRuntimeFields();
            if (fields == null)
            {
                Debug.LogWarning($"本次构建Binder无效,请确保目标:{target.GetType().FullName} 拥有有效字段成员");
                return null;
            }

            classInfo.MemberInfos = new List<BinderMemberInfo>();

            foreach (var fieldInfo in fields)
            {
                var memberInfo = GetMemberInfo(target,fieldInfo);
                if (memberInfo == null)
                {
                    continue;
                }

                if (memberInfo.MemberName == "Self")
                {
                    Debug.LogWarning($"本次构建Binder无效,目标:{target.GetType().FullName}中不能有成员名称为Self,Self是默认为您自动生成的,请将您的Self更换为其他名称");
                    return null;
                }

                classInfo.MemberInfos.Add(memberInfo);
            }

            return classInfo;
        }
        
        internal static bool CheckValid(this Object target)
        {
            var name = target.GetType().Name;
            if (!name.EndsWith("View"))
            {
                Debug.LogWarning($"目标:{target.GetType().FullName}需要以View结尾,如:{target.GetType().Name}View,去除View后就是使用时的名称");
                return false;
            }
            return true;
        }

        internal static string ToCode(this Transform transform,BinderClassInfo classInfo)
        {

            string annotation = @"{0}/// <summary>
{0}/// {1}
{0}/// </summary>";
            
            string annotationParam = "{0}/// <param name=\"{1}\">{2}</param>";
            
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("namespace KC");
            stringBuilder.AppendLine("{");
            if (!string.IsNullOrEmpty(classInfo.Annotation))
            {
                stringBuilder.AppendLine(string.Format(annotation,Space4,classInfo.Annotation));
            }
            stringBuilder.AppendLine($"{Space4}public class {classInfo.ClassName}");
            stringBuilder.AppendLine("    {");

            stringBuilder.AppendLine($"{Space8}public readonly UnityEngine.Transform Self;\n");
            
            foreach (var member in classInfo.MemberInfos)
            {
                if (!string.IsNullOrEmpty(member.Annotation))
                {
                    stringBuilder.AppendLine(string.Format(annotation,Space8,member.Annotation));
                }
                stringBuilder.AppendLine($"{Space8}public readonly {member.Type} {member.MemberName};\n");
            }

            stringBuilder.AppendLine(string.Format(annotation,Space8,$"构建{classInfo.Annotation}\n"+string.Format(annotationParam,Space8,"transform","根节点")));
            stringBuilder.AppendLine($"{Space8}public {classInfo.ClassName}(UnityEngine.Transform transform)");
            stringBuilder.AppendLine(Space8+"{");

            foreach (var memberInfo in classInfo.MemberInfos)
            {
                if (!memberInfo.IsArray)
                {
                    continue;
                }

                string typeString = memberInfo.Type.ToString();
                string fixedType = memberInfo.Type.ToString().Substring(0, typeString.Length - 2);
                stringBuilder.AppendLine(
                    $"{Space12}{memberInfo.MemberName} = new {fixedType}[{memberInfo.MemberInfos.Length}];");
            }
            
            var nodes = transform.GetComponentsInChildren<Transform>(true);
            
            stringBuilder.AppendLine(
                $"{Space12}Self = transform;");
            
            foreach (var member in classInfo.MemberInfos)
            {
                var node = nodes.FirstOrDefault(p => p == member.Transform);
                if (node == transform)
                {
                    stringBuilder.AppendLine(
                        $"{Space12}{member.MemberName} = transform.GetComponent<{member.Type}>();");
                    continue;
                }
                if (!member.IsArray)
                {
                    string localPath = node.name;
                    while (node.parent != transform)
                    {
                        var parent = node.parent;
                        localPath = $"{parent.name}/{localPath}";
                        node = parent;
                    }

                    stringBuilder.AppendLine(
                        $"{Space12}{member.MemberName} = transform.Find(\"{localPath}\").GetComponent<{member.Type}>();");
                }
                else
                {
                    GenerateGetComponentInfo(transform,stringBuilder,member.MemberInfos,nodes,member.MemberName,Space12);
                }
            }
            stringBuilder.AppendLine(Space8+"}");
            stringBuilder.AppendLine(Space4+"}\n}");
            
            
            return stringBuilder.ToString();
        }

        internal static void ToFile(string path, string content)
        {
            var directoryName = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryName))
            {
                if (directoryName != null) Directory.CreateDirectory(directoryName);
            }
            File.Create(path).Dispose();
            File.WriteAllText(path,content);
        }

        internal static object GetImportCodeObject(this Object target)
        {
            var type = target.GetType();
            
            var attribute = type.GetCustomAttribute<BinderAttribute>();
            if (attribute == null)
            {
                Debug.LogWarning($"导入Binder无效,请确保目标:{target.GetType().FullName}拥有标签:BinderAttribute");
                return null;
            }

            var codeClassName = type.Name[..^4];
            string path = $"{Path.Combine(Application.dataPath, attribute.Path, codeClassName)}.cs";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"导入Binder无效,请确保目标:{target.GetType().FullName}拥有在指定路径:{path} 存在脚本");
                return null;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (!assembly.CodeBase.Contains("KC"))
                {
                    continue;
                }
                var types = assembly.GetTypes();
                foreach (var temp in types)
                {
                    if (temp.Name == codeClassName)
                    {
                        var instance = Activator.CreateInstance(temp,new object[]{ ((BaseBinderView)target).transform });
                        return instance;
                    }
                }
            }

            return null;
        }

        internal static string ImportToView(this object import, Object target)
        {
            var importFields = import.GetType().GetRuntimeFields();
            var targetFields = target.GetType().GetRuntimeFields();

            string name = null;
            foreach (var targetField in targetFields)
            {
                foreach (var importField in importFields)
                {
                    if (importField.Name == targetField.Name && importField.FieldType == targetField.FieldType)
                    {
                        targetField.SetValue(target,importField.GetValue(import));
                        name += $"{targetField.FieldType} : {targetField.Name},\n";
                        break;
                    }
                }
            }

            return string.IsNullOrEmpty(name)
                ? $"{import.GetType().FullName} 导入至:{target.GetType().FullName} 完成,但无任何字段导入成功."
                : $"{import.GetType().FullName} 成功导入至:{target.GetType().FullName} ,本次导入成功字段为:\n{name.TrimEnd(',')}";
        }
        
        private static void GenerateGetComponentInfo(Transform transform,StringBuilder stringBuilder, IList<BinderMemberInfo> memberInfos,IList<Transform> nodes,string arrayMemberName,string space)
        {
            if (memberInfos==null || memberInfos.Count == 0)
            {
                return;
            }

            for (var index = 0; index < memberInfos.Count; index++)
            {
                string currentArrayMemberName = $"{arrayMemberName}[{index}]";
                var member = memberInfos[index];
                if (member == null)
                {
                    stringBuilder.AppendLine(
                        $"{space}{currentArrayMemberName} = null;");
                    continue;
                }

                var node = nodes.FirstOrDefault(p => p == member.Transform);
                string localPath = node.name;
                while (node.parent != transform)
                {
                    var parent = node.parent;
                    localPath = $"{parent.name}/{localPath}";
                    node = parent;
                }

                
                stringBuilder.AppendLine(
                    $"{space}{currentArrayMemberName} = transform.Find(\"{localPath}\").GetComponent<{member.Type}>();");
                
            }
        }
        
        private static BinderMemberInfo GetMemberInfo(Object target,FieldInfo fieldInfo)
        {
            if (fieldInfo.FieldType.BaseType != null && fieldInfo.FieldType.BaseType == typeof(Array))
            {
                var objs = fieldInfo.GetValue(target) as Object[];
                if (objs == null)
                {
                    Debug.LogWarning($"目标:{target.GetType().FullName} 数组字段{fieldInfo}不生成,其值为空");
                    return null;
                }

                if (objs.Length == 0)
                {
                    Debug.LogWarning($"目标:{target.GetType().FullName} 数组字段{fieldInfo}不生成,其值为空");
                    return null;
                }

                string nullStr = string.Empty;
                int nullCount = 0;
                for (int i = 0; i < objs.Length; i++)
                {
                    if (objs[i] == null)
                    {
                        nullStr += $"{i},";
                        nullCount++;
                    }
                }

                if (nullCount == objs.Length)
                {
                    Debug.LogWarning($"目标:{target.GetType().FullName} 数组字段{fieldInfo}不生成,其索引引用全部为空");
                    return null;
                }

                if (!string.IsNullOrEmpty(nullStr))
                {
                    Debug.LogWarning($"目标:{target.GetType().FullName} 数组字段{fieldInfo} 下标:[{nullStr.TrimEnd(',')}]的元素为空,请检查是否缺失引用");
                }

                List<BinderMemberInfo> binderMemberInfos = new List<BinderMemberInfo>(objs.Length);
                foreach (var obj in objs)
                {
                    if (obj == null)
                    {
                        binderMemberInfos.Add(null);
                        continue;
                    }
                    var info = new BinderMemberInfo()
                    {
                        MemberName = obj.name,
                        Type = obj.GetType(),
                    };

                    info.Transform = obj switch
                    {
                        GameObject gameObject => gameObject.transform,
                        Component component => component.transform,
                        _ => info.Transform
                    };

                    binderMemberInfos.Add(info);
                }

                return new BinderMemberInfo()
                {
                    MemberName = fieldInfo.Name,
                    Type = fieldInfo.FieldType,
                    Annotation = fieldInfo.GetCustomAttribute<HeaderAttribute>()?.header,
                    MemberInfos = binderMemberInfos.ToArray()
                };
            }
            
            if (GetValue(fieldInfo,target) == null)
            {
                Debug.LogWarning($"目标:{target.GetType().FullName} 字段{fieldInfo}不生成,其引用为空");
                return null;
            }
            
            return new BinderMemberInfo()
            {
                MemberName = fieldInfo.Name,
                Type = fieldInfo.FieldType,
                Annotation = fieldInfo.GetCustomAttribute<HeaderAttribute>()?.header,
                Transform = ((UnityEngine.Component)fieldInfo.GetValue(target)).transform
            };
        }

        private static object GetValue(FieldInfo info, object obj)
        {
            Component value = null;
            try
            {
                value = info.GetValue(obj) as Component;
                var a = value.transform;
            }
            catch (Exception e)
            {
                value = null;
            }
            return value;
        }
    }
}