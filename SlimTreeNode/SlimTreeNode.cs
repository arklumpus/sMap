using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SlimTreeNode
{
    //Useful extension methods
    public static class TypeExtensions
    {
        //Returns true if haystack contains all of the elements that are in needle (NOTE: this returns true also if needle is empty, regardless of haystack)
        public static bool ContainsAll<T>(this IEnumerable<T> haystack, IEnumerable<T> needle)
        {
            foreach (T t in needle)
            {
                if (!haystack.Contains(t))
                {
                    return false;
                }
            }
            return true;
        }

        //Returns true if haystack contains at least one of the elements that are in needle (NOTE: this returns false if needle is empty, regardless of haystack)
        public static bool ContainsAny<T>(this IEnumerable<T> haystack, IEnumerable<T> needle)
        {
            foreach (T t in needle)
            {
                if (haystack.Contains(t))
                {
                    return true;
                }
            }
            return false;
        }
    } //End TypeExtensions

    public class Split
    {
        public readonly string Name;
        public readonly double Length;
        public readonly double Support;
        public readonly LengthTypes LengthType;


        public enum LengthTypes
        {
            Length, Age
        }

        public Split(string name, double length, LengthTypes lengthType, double support)
        {
            this.Name = name;
            this.Length = length;
            this.Support = support;
            this.LengthType = lengthType;
        }



        public static bool AreCompatible(Split s1, Split s2)
        {
            if (!s1.Name.Contains("|") && !s2.Name.Contains("|"))
            {
                string[] leaves1 = s1.Name.Split(',');
                string[] leaves2 = s2.Name.Split(',');

                return !leaves1.ContainsAny(leaves2) || leaves1.ContainsAll(leaves2) || leaves2.ContainsAll(leaves1);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public bool IsCompatible(IEnumerable<Split> splits)
        {
            foreach (Split split in splits)
            {
                if (!AreCompatible(split, this))
                {
                    return false;
                }
            }
            return true;
        }
    }

    [Serializable]
    public class TreeNode
    {
        public TreeNode Parent;
        public List<TreeNode> Children;

        public double Length = -1;

        public double Support = -1;

        //Node name (e.g. species name for the leaf nodes)
        public string Name;

        //Univocal identifier
        public string Guid;

        //Constructor (parent can be null, and is expected to be for the root node)
        public TreeNode(TreeNode parent)
        {
            Parent = parent;
            Guid = System.Guid.NewGuid().ToString();
            Children = new List<TreeNode>();
        }

        public static TreeNode BuildRooted(IEnumerable<Split> splits)
        {
            List<Split> sortedSplits = (from el in splits orderby el.Name.Split(',').Length descending select el).ToList();

            List<TreeNode> nodes = new List<TreeNode>();

            bool clockLike = true;

            for (int i = 0; i < sortedSplits.Count; i++)
            {
                int parent = -1;

                string[] children = sortedSplits[i].Name.Split(',');

                if (sortedSplits[i].LengthType != Split.LengthTypes.Age)
                {
                    clockLike = false;
                }

                for (int j = 0; j < i; j++)
                {
                    if (sortedSplits[j].Name.Split(',').ContainsAll(children))
                    {
                        parent = j;
                    }
                }

                string name = sortedSplits[i].Name.Contains(",") ? null : sortedSplits[i].Name;

                if (parent < 0)
                {
                    TreeNode child = new TreeNode(null) { Name = name, Support = sortedSplits[i].Support, Length = sortedSplits[i].Length };
                    nodes.Add(child);
                }
                else
                {
                    TreeNode child = new TreeNode(nodes[parent]) { Name = name, Support = sortedSplits[i].Support, Length = sortedSplits[i].Length };
                    nodes.Add(child);
                    nodes[parent].Children.Add(child);
                }
            }

            if (clockLike)
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    if (nodes[i].Parent != null)
                    {
                        nodes[i].Length = nodes[i].Parent.Length - nodes[i].Length;
                    }
                    else
                    {
                        nodes[i].Length = -1;
                    }
                }
            }

            return nodes[0];
        }

        public static TreeNode Parse(string source)
        {
            return Parse(source, null);
        }

        //Parse a Newick string, and add the resulting node to the specified parent (which can be null)
        public static TreeNode Parse(string source, TreeNode parent)
        {
            source = source.Trim();
            //Remove comments from the source tree
            source = source.Replace("[]", "");
            if (source.Contains("["))
            {
                source = RemoveComments(source);
            }

            //Having apostrophes as identifier delimiters allows the use of special characters and leading numbers in the identifiers. If there are no special characters in the identifiers, it should be possible to determine the boundaries of the identifiers even in the absence of apostrophes.
            if (!source.Contains("'"))
            {
                //Determine identifier boundaries and insert apostrophe delimiters accordingly
                source = InsertApostrophe(source);
            }

            //We don't care for the final semicolon
            if (source.EndsWith(";"))
            {
                source = source.Substring(0, source.Length - 1);
            }

            //The tree should either start with a parenthesis (internal node) or an apostrophe (leaf node); if it does not, there may be some garbage left from previous parsing steps
            if (!source.StartsWith("(") && !source.StartsWith("'") && source.Contains("("))
            {
                source = source.Substring(source.IndexOf("("));
            }

            if (!source.Contains("("))
            {
                //If there are no parentheses, we are parsing a leaf node
                TreeNode node = new TreeNode(parent);

                //The only relevant parameter is the name of the leaf (which is the source string, minus the apostrophe delimiters)
                source = source.Substring(source.IndexOf("'") + 1);
                node.Name = source.Substring(0, source.IndexOf("'"));

                if (!source.Contains("/"))
                {
                    if (source.IndexOf(":") < 0 || !double.TryParse(source.Substring(0, source.IndexOf(":")), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out node.Support))
                    {
                        node.Support = -1;
                    }
                }
                else
                {
                    if (source.IndexOf(":") < 0 || !double.TryParse(source.Substring(0, source.IndexOf(":")).Replace("/", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out node.Support))
                    {
                        node.Support = -1;
                    }
                }

                source = source.Substring(source.IndexOf(":") + 1);

                if (!source.Contains("{"))
                {
                    if (!double.TryParse(source, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out node.Length))
                    {
                        node.Length = -1;
                    }
                }

                return node;
            }

            if (source.IndexOf("(") == source.LastIndexOf("("))
            {
                //If there is exactly one pair of parentheses, we are parsing a subterminal node (with children that are leaves)
                TreeNode node = new TreeNode(parent);

                string source1 = source.Substring(source.IndexOf(")") + 1);

                if (!source.Contains("/"))
                {
                    if (!double.TryParse(source1.Substring(0, source1.Contains(":") ? source1.IndexOf(":") : source1.Length), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out node.Support))
                    {
                        node.Support = -1;
                    }
                }
                else
                {
                    if (!double.TryParse(source1.Substring(0, source1.Contains(":") ? source1.IndexOf(":") : source1.Length).Replace("/", ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out node.Support))
                    {
                        node.Support = -1;
                    }
                }

                source1 = source1.Substring(source1.IndexOf(":") + 1);

                if (!double.TryParse(source1, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out node.Length))
                {
                    node.Length = -1;
                }

                //The children can be found by removing the leading and trailing parentheses and splitting the source string at each comma (",")
                source = source.Substring(source.IndexOf("(") + 1);
                source = source.Substring(0, source.IndexOf(")"));
                string[] children = source.Split(',');

                //The children are parsed separately and added to the current node
                for (int i = 0; i < children.Length; i++)
                {
                    node.Children.Add(TreeNode.Parse(children[i], node));
                }
                return node;
            }
            else
            {
                //We are parsing an internal node
                List<TreeNode> parsedNodes = new List<TreeNode>();

                //We parse the nodes in the source string, replacing each node string with its guid. This loop runs until there is exactly one pair for parentheses
                while (source.IndexOf("(") != source.LastIndexOf("("))
                {
                    //Start from the innermost node
                    string src = source.Substring(source.LastIndexOf("("));
                    string postSrc = src.Substring(src.IndexOf(")") + 1);

                    //This is the source string for the node
                    src = src.Substring(0, src.IndexOf(")") + 1);

                    //Include in the source string additional information about the node (e.g. branch length, support value...)
                    postSrc = postSrc.Substring(0, Math.Min(postSrc.IndexOf(",") < 0 ? int.MaxValue : postSrc.IndexOf(","), postSrc.IndexOf(")") < 0 ? int.MaxValue : postSrc.IndexOf(")")));
                    src += postSrc;

                    //Parse the node
                    TreeNode nd = TreeNode.Parse(src, null);
                    parsedNodes.Add(nd);

                    //Replace the parsed node with its guid in the source string
                    source = source.Replace(src, "'" + nd.Guid + "'");
                }

                //Parse the source string (which, by now, should only consist of a single pair of parentheses with single nodes within)
                TreeNode node = TreeNode.Parse(source, parent);

                //Replace the guids with the nodes they point to
                replaceChildrenRecursive(node, parsedNodes);

                return node;
            }
        }

        //Remove comments from a Newick tree
        public static string RemoveComments(string source)
        {
            Regex reg = new Regex(@"\[[^]]*\]");
            return reg.Replace(source, "");
        }

        //Add apostrophe delimiters to the node and leaf names in the source string
        public static string InsertApostrophe(string source)
        {
            int pos = 0;

            List<int> apostrophe = new List<int>();

            pos = source.IndexOf("(") + 1;

            while (pos < source.Length)
            {
                //Scan the source string up to the first non-deliminter character
                while (pos < source.Length && (source[pos] == '(' || source[pos] == ',' || source[pos] == ':' || source[pos] == '/' || source[pos] == ')' || source[pos] == ';'))
                {
                    pos++;
                }
                int t;

                bool added = false;

                //If the character that was found is not a number, it must be an identifier
                if (pos < source.Length && !int.TryParse(source[pos].ToString(), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out t))
                {
                    //Take note of the position where the apostrophe should be inserted
                    apostrophe.Add(pos);
                    added = true;
                }

                //Scan the source string up to the first delimiter character
                while (pos < source.Length && (source[pos] != '(' && source[pos] != ',' && source[pos] != ':' && source[pos] != '/' && source[pos] != ')' && source[pos] != ';'))
                {
                    pos++;
                }

                //If an apostrophe was added earlier, the delimiter character indicates the end of the identifier
                if (pos < source.Length && added)
                {
                    apostrophe.Add(pos);
                }
            }

            //For each identifier, two apostrophes should have been added
            if (apostrophe.Count % 2 != 0)
            {
                throw new Exception("It was impossible to correctly guess identifier boundaries.");
            }

            apostrophe.Sort();

            //Insert the apostrophes in the source string, from the last one to the first one (so that the positions do not need to be updated after each insertion)
            for (int i = apostrophe.Count - 1; i >= 0; i--)
            {
                source = source.Insert(apostrophe[i], "'");
            }

            return source;
        }

        //Recursively replace nodes with guid names with the nodes the guids refer to
        private static void replaceChildrenRecursive(TreeNode parent, List<TreeNode> children)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                for (int d = 0; d < children.Count; d++)
                {
                    //If the name of a child is the guid of a parsed node, replace the child with the corresponding parsed node
                    if (parent.Children[i].Name == children[d].Guid)
                    {
                        parent.Children[i] = children[d];
                        children[d].Parent = parent;

                        //Again, replace the children of the found child with the nodes corresponding to the guids
                        replaceChildrenRecursive(children[d], children);
                    }
                }
            }
        }

        //Get an unrooted version of a rooted tree
        public TreeNode GetUnrootedTree()
        {
            //A tree is unrooted if the root node has 3 children
            if (this.Children.Count == 3)
            {
                //If the tree is already unrooted, just return a clone
                return this.Clone();
            }
            else
            {
                //At this point, assume that the root node has 2 children

                //If the second child of the root node is not a leaf node (i.e. it has 2 children), we can take the first child of the root node and graft it onto the second child; the second child will now have 3 children and will be the root node of the unrooted tree
                if (this.Children[1].Children.Count == 2)
                {
                    TreeNode child1 = this.Children[1].Clone();
                    TreeNode child0 = this.Children[0].Clone();
                    child0.Parent = child1;
                    child1.Children.Add(child0);
                    child1.Parent = null;
                    child1.Name = this.Name;
                    return child1;
                }
                else
                {
                    //If the second child of the root node is a leaf node, then the first child must not be a leaf node; thus we do the same as before, but swapping the two children 
                    TreeNode child0 = this.Children[1].Clone();
                    TreeNode child1 = this.Children[0].Clone();
                    child0.Parent = child1;
                    child1.Children.Add(child0);
                    child1.Parent = null;
                    child1.Name = this.Name;
                    return child1;
                }
            }
        }

        //Recursively clone a node
        public TreeNode Clone()
        {
            TreeNode nd = new TreeNode(this.Parent);
            nd.Guid = this.Guid;
            nd.Name = this.Name;
            foreach (TreeNode nd2 in this.Children)
            {
                TreeNode nd22 = nd2.Clone();
                nd22.Parent = nd;
                nd.Children.Add(nd22);
            }

            nd.Length = this.Length;

            nd.Support = this.Support;

            return nd;
        }

        //Recursively get all of the nodes that descend from the specified node (including the parent node)
        public List<TreeNode> GetChildrenRecursive()
        {
            List<TreeNode> tbr = new List<TreeNode>();
            tbr.Add(this);
            for (int i = 0; i < this.Children.Count; i++)
            {
                tbr.AddRange(this.Children[i].GetChildrenRecursive());
            }
            return tbr;
        }

        //Get all the leaves that descend (directly or indirectly) from the current node instance
        public List<TreeNode> GetLeaves()
        {
            return new List<TreeNode>(from el in this.GetChildrenRecursive() where el.Children.Count == 0 select el);
        }

        //Get the names of all the leaves that descend (directly or indirectly) from the current node instance
        public List<string> GetLeafNames()
        {
            return new List<string>(from el in this.GetChildrenRecursive() where el.Children.Count == 0 select el.Name);
        }

        public TreeNode GetBranchFromName(string branchName)
        {
            foreach (TreeNode nd in this.GetChildrenRecursive())
            {
                if (nd.Name == branchName)
                {
                    return nd;
                }
            }
            return null;
        }

        public double UpstreamLength()
        {
            double tbr = 0;

            TreeNode nd = this;

            while (nd.Parent != null)
            {
                tbr += nd.Length;
                nd = nd.Parent;
            }

            return tbr;
        }

        public double DownstreamLength()
        {
            if (this.Children.Count == 0)
            {
                return 0;
            }
            else
            {
                double maxLen = 0;
                for (int i = 0; i < this.Children.Count; i++)
                {
                    maxLen = Math.Max(maxLen, this.Children[i].DownstreamLength() + this.Children[i].Length);
                }

                return maxLen;
            }
        }

        public double TotalLength()
        {
            double tbr = 0;

            foreach (TreeNode nd in this.GetChildrenRecursive())
            {
                tbr += nd.Length;
            }

            return tbr;
        }

        public void SortNodes(bool ascending)
        {
            for (int i = 0; i < this.Children.Count; i++)
            {
                this.Children[i].SortNodes(ascending);
            }

            if (this.Children.Count > 0)
            {
                this.Children.Sort((a, b) =>
                {
                    int val = (a.GetLevels(true)[1] - b.GetLevels(true)[1]) * (ascending ? 1 : -1);
                    if (val != 0)
                    {
                        return val;
                    }
                    else
                    {
                        return a.GetLeafNames()[0].CompareTo(b.GetLeafNames()[0]);
                    }
                });
            }
        }

        public int[] GetLevels(bool ignoreTotal = false)
        {
            int upperCount = 0;
            TreeNode prnt = this.Parent;
            TreeNode lastPrnt = null;
            while (prnt != null)
            {
                lastPrnt = prnt;
                upperCount++;
                prnt = prnt.Parent;
            }

            int lowerCount = 0;
            if (this.Children.Count > 0)
            {
                for (int i = 0; i < this.Children.Count; i++)
                {
                    TreeNode ch = this.Children[i];
                    lowerCount = Math.Max(lowerCount, 1 + ch.GetLevels(true)[1]);
                }
            }

            if (this.Parent != null && !ignoreTotal)
            {
                return new int[] { upperCount, lowerCount, lastPrnt.GetLevels()[2] };
            }
            else
            {
                return new int[] { upperCount, lowerCount, lowerCount };
            }

        }

        public override string ToString()
        {
            return this.ToString(true);
        }

        public string ToString(bool withApostrophe)
        {
            if (this.Children.Count == 0)
            {
                string tbr = "";
                if (withApostrophe)
                {
                    tbr += "'" + this.Name + "'";
                }
                else
                {
                    tbr += this.Name;
                }
                if (this.Length != -1 && !double.IsNaN(this.Length))
                {
                    tbr += ":" + this.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                if (this.Parent == null)
                {
                    tbr += ";";
                }
                return tbr;
            }
            else
            {
                string tbr = "(";
                for (int i = 0; i < this.Children.Count; i++)
                {
                    tbr += this.Children[i].ToString(withApostrophe);
                    if (i < this.Children.Count - 1)
                    {
                        tbr += ",";
                    }
                }
                tbr += ")";
                if (!string.IsNullOrEmpty(this.Name) && (withApostrophe || this.Support < 0))
                {
                    if (withApostrophe)
                    {
                        tbr += "'" + this.Name + "'";
                    }
                    else
                    {
                        tbr += this.Name;
                    }
                }
                if (this.Support >= 0)
                {
                    tbr += this.Support.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                if (this.Length != -1 && !double.IsNaN(this.Length))
                {
                    tbr += ":" + this.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                if (this.Parent == null)
                {
                    tbr += ";";
                }
                return tbr;
            }
        }

        public bool IsClocklike()
        {
            List<TreeNode> leaves = this.GetLeaves();

            double len = leaves[0].UpstreamLength();

            foreach (TreeNode leaf in leaves)
            {
                if (Math.Abs(leaf.UpstreamLength() / len - 1) > 0.001)
                {
                    return false;
                }
            }

            return true;
        }

        public TreeNode GetMonophyleticGroup(string[] monophyleticConstraint)
        {
            if (monophyleticConstraint.Length > 0)
            {
                TreeNode seed = this.GetBranchFromName(monophyleticConstraint[0]);

                while (seed != null && !seed.GetLeafNames().ContainsAll(monophyleticConstraint))
                {
                    seed = seed.Parent;
                }

                return seed;
            }
            else
            {
                return null;
            }
        }

        public List<Split> GetSplits(Split.LengthTypes lengthType)
        {
            List<Split> tbr = new List<Split>();

            List<TreeNode> nodes = this.GetChildrenRecursive();

            if (this.Children.Count == 2)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    List<string> nodeLeaves = nodes[i].GetLeafNames();
                    nodeLeaves.Sort();

                    tbr.Add(new Split(nodeLeaves.Aggregate((a, b) => a + "," + b), lengthType == Split.LengthTypes.Length ? nodes[i].Length : nodes[i].DownstreamLength(), lengthType, 1));
                }
            }
            else
            {
                List<string> allLeaves = this.GetLeafNames();



                for (int i = 0; i < nodes.Count; i++)
                {
                    List<string> nodeLeaves = nodes[i].GetLeafNames();

                    List<string> diffLeaves = (from el in allLeaves where !nodeLeaves.Contains(el) select el).ToList();

                    nodeLeaves.Sort();
                    diffLeaves.Sort();

                    List<string> splitTerminals = new List<string>() { nodeLeaves.Aggregate((a, b) => a + "," + b), diffLeaves.Aggregate((a, b) => a + "," + b) };
                    splitTerminals.Sort();

                    tbr.Add(new Split(splitTerminals.Aggregate((a, b) => a + "|" + b), lengthType == Split.LengthTypes.Length ? nodes[i].Length : (nodes[i].DownstreamLength() + nodes[i].Length), lengthType, 1));
                }

            }
            return tbr;
        }

        public void Drop(bool keepInternalNode = false)
        {
            if (this.Parent == null)
            {
                return;
            }
            else
            {
                TreeNode prnt = this.Parent;
                for (int i = 0; i < prnt.Children.Count; i++)
                {
                    if (prnt.Children[i] == this)
                    {
                        prnt.Children.RemoveAt(i);
                        break;
                    }
                }
                if (prnt.Children.Count == 1 && !keepInternalNode)
                {
                    TreeNode child = prnt.Children[0];
                    if (child.Length >= 0 && prnt.Length >= 0)
                    {
                        child.Length += prnt.Length;
                    }
                    child.Support = prnt.Support;
                    child.Parent = prnt.Parent;

                    if (prnt.Parent != null)
                    {
                        for (int i = 0; i < prnt.Parent.Children.Count; i++)
                        {
                            if (prnt.Parent.Children[i] == prnt)
                            {
                                prnt.Parent.Children.RemoveAt(i);
                                break;
                            }
                        }
                        prnt.Parent.Children.Add(child);
                    }
                    else
                    {
                        prnt.Guid = child.Guid;
                        prnt.Name = child.Name;
                        prnt.Length = child.Length;
                        prnt.Support = child.Support;
                        prnt.Children = child.Children;

                        foreach (TreeNode nd2 in prnt.Children)
                        {
                            nd2.Parent = prnt;
                        }
                    }
                }

            }
        }
    }
}