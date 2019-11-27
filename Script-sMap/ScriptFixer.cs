using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Script_sMap
{
    internal class FixedScripts
    {
        public static string FixedSlimTreeNode()
        {
            StreamReader sr = new StreamReader(typeof(MainWindow).Assembly.GetManifestResourceStream("SlimTreeNode.TypeExtensions"));
            string text = sr.ReadToEnd();

            text = text.Replace("namespace SlimTreeNode", "");
            text = text.Remove(text.IndexOf("{"), 1);
            text = text.Remove(text.LastIndexOf("}"), 1);

            string typeExtensions = text.Substring(text.IndexOf("public static class TypeExtensions"));
            typeExtensions = typeExtensions.Substring(0, typeExtensions.IndexOf("//End TypeExtensions"));

            text = text.Replace(typeExtensions, "");

            typeExtensions = typeExtensions.Replace("public static class TypeExtensions", "");
            typeExtensions = typeExtensions.Remove(typeExtensions.IndexOf("{"), 1);
            typeExtensions = typeExtensions.Remove(typeExtensions.LastIndexOf("}"), 1);
            typeExtensions = typeExtensions.Replace("this", "");

            Regex containsAllReg = new Regex(@"([ (])(!?)([^ (][^ ]*)\.ContainsAll\(([^)]*)\)");
            text = containsAllReg.Replace(text, "$1$2TreeNode.ContainsAll($3, $4)");

            Regex containsAnyReg = new Regex(@"([ (])(!?)([^ (][^ ]*)\.ContainsAny\(([^)]*)\)");
            text = containsAnyReg.Replace(text, "$1$2TreeNode.ContainsAny($3, $4)");

            int ind = text.IndexOf("class TreeNode");
            ind = text.IndexOf("{", ind);
            text = text.Insert(ind + 1, typeExtensions);

            return text;
        }
    }
}
