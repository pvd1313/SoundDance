using System;
using System.CodeDom;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Pingle.EditorTools
{
    public class AudioUtilReflectorWindow : EditorWindow
    {
        private const string ResultClassName = "AudioUtil";
        private const string ResultNamespace = "Pingle.EditorTools";

        private string _scanResult;
        private Vector2 _textAreaScroll;

        [MenuItem("Tools/AudioUtilReflector")]
        private static void OpenWindow()
        {
            EditorWindow.GetWindow(typeof(AudioUtilReflectorWindow));
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Scan"))
            {
                _scanResult = Scan();
            }

            _textAreaScroll = GUILayout.BeginScrollView(_textAreaScroll);
            GUILayout.TextArea(_scanResult, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        private string Scan()
        {
            var result = new StringBuilder();

            result.AppendLine("using UnityEditor;");
            result.AppendLine("using System.Reflection;");
            result.AppendLine("using UnityEngine;");
            result.AppendLine("using System;");

            result.AppendLine();
            result.AppendLine($"namespace {ResultNamespace}");
            result.AppendLine("{");
            result.AppendLine($"\tpublic static class {ResultClassName}");
            result.AppendLine("\t{");

            var assembly = typeof(AudioImporter).Assembly;
            var audioUtil = assembly.GetType("UnityEditor.AudioUtil");
            var methods = audioUtil.GetMethods(BindingFlags.Static | BindingFlags.Public);

            foreach (var method in methods)
            {
                var fieldName = MethodNameToFieldName(method.Name);
                result.AppendLine($"\t\tprivate static readonly MethodInfo {fieldName};");
            }

            result.AppendLine();
            result.AppendLine($"\t\tstatic {ResultClassName}()");
            result.AppendLine("\t\t{");
            result.AppendLine("\t\t\tvar assembly = typeof(AudioImporter).Assembly;");
            result.AppendLine("\t\t\tvar audioUtil = assembly.GetType(\"UnityEditor.AudioUtil\");");
            result.AppendLine("\t\t\tvar flags = BindingFlags.Static | BindingFlags.Public;");
            result.AppendLine();

            foreach (var method in methods)
            {
                var fieldName = MethodNameToFieldName(method.Name);
                var paramNames = string.Join(", ", method.GetParameters().Select(p => $"typeof({GetTypeName(p.ParameterType)})"));
                var paramArray = $"new Type[] {{ {paramNames} }}";
                result.AppendLine($"\t\t\t{fieldName} = audioUtil.GetMethod(\"{method.Name}\", flags, null, {paramArray}, null);");
            }

            result.AppendLine("\t\t}");

            foreach (var method in methods)
            {
                var methodParams = method.GetParameters().Select(p => GetTypeName(p.ParameterType) + " " + p.Name);
                var paramsText = string.Join(", ", methodParams);
                var fieldName = MethodNameToFieldName(method.Name);
                var invokeParams = string.Join(", ", method.GetParameters().Select(p => p.Name));
                var returnTypeName = GetTypeName(method.ReturnType);
                var callReturn = method.ReturnType == typeof(void) ? "" : $"return ({returnTypeName})";

                result.AppendLine($"\t\tpublic static {returnTypeName} {method.Name}({paramsText})");
                result.AppendLine("\t\t{");
                result.AppendLine($"\t\t\t{callReturn} {fieldName}.Invoke(null, new object[]{{ {invokeParams} }});");
                result.AppendLine("\t\t}");
            }

            result.AppendLine("\t}");
            result.AppendLine("}");

            return result.ToString();
        }

        private string MethodNameToFieldName(string text)
        {
            return "_" + Char.ToLower(text[0]) + text.Substring(1);
        }

        private string GetTypeName(Type type)
        {
            if (type == typeof(Int32)) return "int";
            if (type == typeof(Boolean)) return "bool";
            if (type == typeof(Double)) return "double";
            if (type == typeof(Single)) return "float";
            if (type == typeof(void)) return "void";
            if (type == typeof(Single[])) return "float[]";
            if (type == typeof(String)) return "string";
            if (type == typeof(String[])) return "string[]";

            return type.Name;
        }
    }
}
