using System;
using System.Collections.Generic;
using System.Text;

namespace IniEdit.Services
{
    /// <summary>
    /// Represents a node in a hierarchical tree structure.
    /// </summary>
    public sealed class TreeNodeData
    {
        /// <summary>
        /// Gets the display name of this node.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the full path/name of this node.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the associated section, or null if this is just a folder node.
        /// </summary>
        public Section? Section { get; internal set; }

        /// <summary>
        /// Gets the child nodes.
        /// </summary>
        public List<TreeNodeData> Children { get; } = new();

        /// <summary>
        /// Gets whether this node has an associated section.
        /// </summary>
        public bool HasSection => Section != null;

        /// <summary>
        /// Gets whether this node has children.
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNodeData"/> class.
        /// </summary>
        /// <param name="displayName">The display name.</param>
        /// <param name="fullName">The full name/path.</param>
        /// <param name="section">The associated section, if any.</param>
        public TreeNodeData(string displayName, string fullName, Section? section = null)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
            Section = section;
        }
    }

    /// <summary>
    /// Builds hierarchical tree structures from INI document sections.
    /// </summary>
    public sealed class TreeViewBuilder
    {
        private readonly string _separator;

        /// <summary>
        /// Gets the separator used to split section names.
        /// </summary>
        public string Separator => _separator;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeViewBuilder"/> class.
        /// </summary>
        /// <param name="separator">The separator to use for splitting section names.</param>
        public TreeViewBuilder(string separator = ".")
        {
            if (string.IsNullOrEmpty(separator))
                throw new ArgumentException("Separator cannot be null or empty.", nameof(separator));

            _separator = separator;
        }

        /// <summary>
        /// Builds a tree structure from the sections in a document.
        /// </summary>
        /// <param name="document">The document containing sections.</param>
        /// <param name="globalSectionName">The name to use for the global/default section.</param>
        /// <returns>A list of root nodes representing the tree structure.</returns>
        public List<TreeNodeData> BuildTree(Document document, string globalSectionName = "(Global)")
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var rootNodes = new List<TreeNodeData>();

            // Add global section node
            var globalNode = new TreeNodeData(globalSectionName, globalSectionName, null);
            rootNodes.Add(globalNode);

            // Build tree from sections
            foreach (var section in document)
            {
                AddSectionToTree(rootNodes, section);
            }

            return rootNodes;
        }

        /// <summary>
        /// Adds a section to the tree, creating intermediate nodes as needed.
        /// </summary>
        /// <param name="rootNodes">The root nodes collection.</param>
        /// <param name="section">The section to add.</param>
        private void AddSectionToTree(List<TreeNodeData> rootNodes, Section section)
        {
            var parts = section.Name.Split(new[] { _separator }, StringSplitOptions.None);
            var currentLevel = rootNodes;
            var partialNameBuilder = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                var displayName = parts[i];

                // Build partial name incrementally
                if (i > 0)
                    partialNameBuilder.Append(_separator);
                partialNameBuilder.Append(displayName);

                var partialName = partialNameBuilder.ToString();

                // Look for existing node at this level
                var existingNode = FindNodeByDisplayName(currentLevel, displayName);

                if (existingNode != null)
                {
                    // If this is the last part, update the section reference
                    if (i == parts.Length - 1)
                    {
                        existingNode.Section = section;
                    }
                    currentLevel = existingNode.Children;
                }
                else
                {
                    // Create new node
                    var isLastPart = i == parts.Length - 1;
                    var newNode = new TreeNodeData(
                        displayName,
                        partialName,
                        isLastPart ? section : null
                    );

                    currentLevel.Add(newNode);
                    currentLevel = newNode.Children;
                }
            }
        }

        /// <summary>
        /// Finds a node by its display name in a collection.
        /// </summary>
        /// <param name="nodes">The nodes to search.</param>
        /// <param name="displayName">The display name to find.</param>
        /// <returns>The found node, or null if not found.</returns>
        private static TreeNodeData? FindNodeByDisplayName(List<TreeNodeData> nodes, string displayName)
        {
            foreach (var node in nodes)
            {
                if (node.DisplayName == displayName)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// Counts the total number of nodes in a tree.
        /// </summary>
        /// <param name="rootNodes">The root nodes.</param>
        /// <returns>The total node count.</returns>
        public static int CountNodes(List<TreeNodeData> rootNodes)
        {
            int count = 0;
            foreach (var node in rootNodes)
            {
                count += CountNodesRecursive(node);
            }
            return count;
        }

        private static int CountNodesRecursive(TreeNodeData node)
        {
            int count = 1;
            foreach (var child in node.Children)
            {
                count += CountNodesRecursive(child);
            }
            return count;
        }

        /// <summary>
        /// Finds a node by its full name in a tree.
        /// </summary>
        /// <param name="rootNodes">The root nodes.</param>
        /// <param name="fullName">The full name to find.</param>
        /// <returns>The found node, or null if not found.</returns>
        public static TreeNodeData? FindNodeByFullName(List<TreeNodeData> rootNodes, string fullName)
        {
            foreach (var node in rootNodes)
            {
                var found = FindNodeByFullNameRecursive(node, fullName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static TreeNodeData? FindNodeByFullNameRecursive(TreeNodeData node, string fullName)
        {
            if (node.FullName == fullName)
                return node;

            foreach (var child in node.Children)
            {
                var found = FindNodeByFullNameRecursive(child, fullName);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Gets all nodes that have an associated section.
        /// </summary>
        /// <param name="rootNodes">The root nodes.</param>
        /// <returns>List of nodes with sections.</returns>
        public static List<TreeNodeData> GetNodesWithSections(List<TreeNodeData> rootNodes)
        {
            var result = new List<TreeNodeData>();
            foreach (var node in rootNodes)
            {
                CollectNodesWithSections(node, result);
            }
            return result;
        }

        private static void CollectNodesWithSections(TreeNodeData node, List<TreeNodeData> result)
        {
            if (node.HasSection)
                result.Add(node);

            foreach (var child in node.Children)
            {
                CollectNodesWithSections(child, result);
            }
        }
    }
}
