using System;
using IniEdit;
using IniEdit.GUI.Services;

namespace IniEdit.GUI.Tests.Services
{
    [TestFixture]
    public class TreeViewBuilderTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithDefaultSeparator_UsesDot()
        {
            var builder = new TreeViewBuilder();
            Assert.That(builder.Separator, Is.EqualTo("."));
        }

        [Test]
        public void Constructor_WithCustomSeparator_UsesThatSeparator()
        {
            var builder = new TreeViewBuilder("/");
            Assert.That(builder.Separator, Is.EqualTo("/"));
        }

        [Test]
        public void Constructor_WithNullSeparator_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TreeViewBuilder(null!));
        }

        [Test]
        public void Constructor_WithEmptySeparator_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TreeViewBuilder(string.Empty));
        }

        #endregion

        #region BuildTree Tests

        [Test]
        public void BuildTree_EmptyDocument_ReturnsOnlyGlobalNode()
        {
            var doc = new Document();
            var builder = new TreeViewBuilder();

            var result = builder.BuildTree(doc);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].DisplayName, Is.EqualTo("(Global)"));
            Assert.That(result[0].Section, Is.Null);
        }

        [Test]
        public void BuildTree_WithCustomGlobalName_UsesCustomName()
        {
            var doc = new Document();
            var builder = new TreeViewBuilder();

            var result = builder.BuildTree(doc, "Default Section");

            Assert.That(result[0].DisplayName, Is.EqualTo("Default Section"));
        }

        [Test]
        public void BuildTree_SingleFlatSection_CreatesOneNode()
        {
            var doc = new Document();
            doc.AddSection(new Section("Database"));
            var builder = new TreeViewBuilder();

            var result = builder.BuildTree(doc);

            Assert.That(result.Count, Is.EqualTo(2)); // Global + Database
            Assert.That(result[1].DisplayName, Is.EqualTo("Database"));
            Assert.That(result[1].Section, Is.Not.Null);
            Assert.That(result[1].Section!.Name, Is.EqualTo("Database"));
        }

        [Test]
        public void BuildTree_NestedSection_CreatesHierarchy()
        {
            var doc = new Document();
            doc.AddSection(new Section("Database.Connection"));
            var builder = new TreeViewBuilder();

            var result = builder.BuildTree(doc);

            Assert.That(result.Count, Is.EqualTo(2)); // Global + Database
            var databaseNode = result[1];
            Assert.That(databaseNode.DisplayName, Is.EqualTo("Database"));
            Assert.That(databaseNode.Section, Is.Null); // Intermediate node
            Assert.That(databaseNode.Children.Count, Is.EqualTo(1));

            var connectionNode = databaseNode.Children[0];
            Assert.That(connectionNode.DisplayName, Is.EqualTo("Connection"));
            Assert.That(connectionNode.Section, Is.Not.Null);
        }

        [Test]
        public void BuildTree_MultipleSectionsWithCommonParent_SharesParentNode()
        {
            var doc = new Document();
            doc.AddSection(new Section("Database.Connection"));
            doc.AddSection(new Section("Database.Settings"));
            var builder = new TreeViewBuilder();

            var result = builder.BuildTree(doc);

            Assert.That(result.Count, Is.EqualTo(2)); // Global + Database
            var databaseNode = result[1];
            Assert.That(databaseNode.Children.Count, Is.EqualTo(2));
            Assert.That(databaseNode.Children[0].DisplayName, Is.EqualTo("Connection"));
            Assert.That(databaseNode.Children[1].DisplayName, Is.EqualTo("Settings"));
        }

        [Test]
        public void BuildTree_ParentAndChildBothSections_BothHaveSectionReference()
        {
            var doc = new Document();
            var parentSection = new Section("Database");
            var childSection = new Section("Database.Connection");
            doc.AddSection(parentSection);
            doc.AddSection(childSection);
            var builder = new TreeViewBuilder();

            var result = builder.BuildTree(doc);

            var databaseNode = result[1];
            Assert.That(databaseNode.Section, Is.SameAs(parentSection));
            Assert.That(databaseNode.Children[0].Section, Is.SameAs(childSection));
        }

        [Test]
        public void BuildTree_DeepNesting_CreatesCorrectHierarchy()
        {
            var doc = new Document();
            doc.AddSection(new Section("A.B.C.D"));
            var builder = new TreeViewBuilder();

            var result = builder.BuildTree(doc);

            var aNode = result[1];
            Assert.That(aNode.DisplayName, Is.EqualTo("A"));
            Assert.That(aNode.FullName, Is.EqualTo("A"));

            var bNode = aNode.Children[0];
            Assert.That(bNode.DisplayName, Is.EqualTo("B"));
            Assert.That(bNode.FullName, Is.EqualTo("A.B"));

            var cNode = bNode.Children[0];
            Assert.That(cNode.DisplayName, Is.EqualTo("C"));
            Assert.That(cNode.FullName, Is.EqualTo("A.B.C"));

            var dNode = cNode.Children[0];
            Assert.That(dNode.DisplayName, Is.EqualTo("D"));
            Assert.That(dNode.FullName, Is.EqualTo("A.B.C.D"));
            Assert.That(dNode.Section, Is.Not.Null);
        }

        [Test]
        public void BuildTree_WithSlashSeparator_ParsesCorrectly()
        {
            var doc = new Document();
            doc.AddSection(new Section("Config/Database/Settings"));
            var builder = new TreeViewBuilder("/");

            var result = builder.BuildTree(doc);

            Assert.That(result[1].DisplayName, Is.EqualTo("Config"));
            Assert.That(result[1].Children[0].DisplayName, Is.EqualTo("Database"));
            Assert.That(result[1].Children[0].Children[0].DisplayName, Is.EqualTo("Settings"));
        }

        [Test]
        public void BuildTree_NullDocument_ThrowsArgumentNullException()
        {
            var builder = new TreeViewBuilder();
            Assert.Throws<ArgumentNullException>(() => builder.BuildTree(null!));
        }

        [Test]
        public void BuildTree_SectionWithConsecutiveSeparators_SkipsEmptyParts()
        {
            var doc = new Document();
            doc.AddSection(new Section("A..B")); // Consecutive dots
            var builder = new TreeViewBuilder(".");

            var result = builder.BuildTree(doc);

            // Should have Global + A node (with B as child)
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[1].DisplayName, Is.EqualTo("A"));
            Assert.That(result[1].Children.Count, Is.EqualTo(1));
            Assert.That(result[1].Children[0].DisplayName, Is.EqualTo("B"));
        }

        [Test]
        public void BuildTree_SectionWithLeadingSeparator_SkipsEmptyParts()
        {
            var doc = new Document();
            doc.AddSection(new Section(".A.B")); // Leading separator
            var builder = new TreeViewBuilder(".");

            var result = builder.BuildTree(doc);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[1].DisplayName, Is.EqualTo("A"));
        }

        [Test]
        public void BuildTree_SectionWithTrailingSeparator_SkipsEmptyParts()
        {
            var doc = new Document();
            doc.AddSection(new Section("A.B.")); // Trailing separator
            var builder = new TreeViewBuilder(".");

            var result = builder.BuildTree(doc);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[1].DisplayName, Is.EqualTo("A"));
            Assert.That(result[1].Children[0].DisplayName, Is.EqualTo("B"));
            Assert.That(result[1].Children[0].Children.Count, Is.EqualTo(0)); // No empty child
        }

        #endregion

        #region CountNodes Tests

        [Test]
        public void CountNodes_EmptyTree_ReturnsZero()
        {
            var nodes = new System.Collections.Generic.List<TreeNodeData>();
            Assert.That(TreeViewBuilder.CountNodes(nodes), Is.EqualTo(0));
        }

        [Test]
        public void CountNodes_SingleNode_ReturnsOne()
        {
            var nodes = new System.Collections.Generic.List<TreeNodeData>
            {
                new TreeNodeData("Test", "Test")
            };
            Assert.That(TreeViewBuilder.CountNodes(nodes), Is.EqualTo(1));
        }

        [Test]
        public void CountNodes_NestedNodes_ReturnsCorrectCount()
        {
            var doc = new Document();
            doc.AddSection(new Section("A.B.C"));
            doc.AddSection(new Section("A.D"));
            var builder = new TreeViewBuilder();

            var result = builder.BuildTree(doc);
            var count = TreeViewBuilder.CountNodes(result);

            // Global + A + B + C + D = 5
            Assert.That(count, Is.EqualTo(5));
        }

        #endregion

        #region FindNodeByFullName Tests

        [Test]
        public void FindNodeByFullName_ExistingNode_ReturnsNode()
        {
            var doc = new Document();
            doc.AddSection(new Section("Database.Connection"));
            var builder = new TreeViewBuilder();
            var tree = builder.BuildTree(doc);

            var found = TreeViewBuilder.FindNodeByFullName(tree, "Database.Connection");

            Assert.That(found, Is.Not.Null);
            Assert.That(found!.FullName, Is.EqualTo("Database.Connection"));
        }

        [Test]
        public void FindNodeByFullName_IntermediateNode_ReturnsNode()
        {
            var doc = new Document();
            doc.AddSection(new Section("Database.Connection"));
            var builder = new TreeViewBuilder();
            var tree = builder.BuildTree(doc);

            var found = TreeViewBuilder.FindNodeByFullName(tree, "Database");

            Assert.That(found, Is.Not.Null);
            Assert.That(found!.FullName, Is.EqualTo("Database"));
        }

        [Test]
        public void FindNodeByFullName_NonExistentNode_ReturnsNull()
        {
            var doc = new Document();
            doc.AddSection(new Section("Database"));
            var builder = new TreeViewBuilder();
            var tree = builder.BuildTree(doc);

            var found = TreeViewBuilder.FindNodeByFullName(tree, "NonExistent");

            Assert.That(found, Is.Null);
        }

        #endregion

        #region GetNodesWithSections Tests

        [Test]
        public void GetNodesWithSections_ReturnsOnlyNodesWithSections()
        {
            var doc = new Document();
            doc.AddSection(new Section("A.B")); // A has no section, B has
            var builder = new TreeViewBuilder();
            var tree = builder.BuildTree(doc);

            var nodesWithSections = TreeViewBuilder.GetNodesWithSections(tree);

            Assert.That(nodesWithSections.Count, Is.EqualTo(1));
            Assert.That(nodesWithSections[0].FullName, Is.EqualTo("A.B"));
        }

        [Test]
        public void GetNodesWithSections_AllNodesHaveSections_ReturnsAll()
        {
            var doc = new Document();
            doc.AddSection(new Section("A"));
            doc.AddSection(new Section("A.B"));
            var builder = new TreeViewBuilder();
            var tree = builder.BuildTree(doc);

            var nodesWithSections = TreeViewBuilder.GetNodesWithSections(tree);

            Assert.That(nodesWithSections.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetNodesWithSections_EmptyDocument_ReturnsEmpty()
        {
            var doc = new Document();
            var builder = new TreeViewBuilder();
            var tree = builder.BuildTree(doc);

            var nodesWithSections = TreeViewBuilder.GetNodesWithSections(tree);

            Assert.That(nodesWithSections, Is.Empty);
        }

        #endregion

        #region TreeNodeData Tests

        [Test]
        public void TreeNodeData_Constructor_SetsProperties()
        {
            var section = new Section("Test");
            var node = new TreeNodeData("Display", "Full.Name", section);

            Assert.That(node.DisplayName, Is.EqualTo("Display"));
            Assert.That(node.FullName, Is.EqualTo("Full.Name"));
            Assert.That(node.Section, Is.SameAs(section));
            Assert.That(node.HasSection, Is.True);
            Assert.That(node.HasChildren, Is.False);
        }

        [Test]
        public void TreeNodeData_WithoutSection_HasSectionIsFalse()
        {
            var node = new TreeNodeData("Display", "Name");

            Assert.That(node.HasSection, Is.False);
        }

        [Test]
        public void TreeNodeData_WithChildren_HasChildrenIsTrue()
        {
            var node = new TreeNodeData("Parent", "Parent");
            node.Children.Add(new TreeNodeData("Child", "Parent.Child"));

            Assert.That(node.HasChildren, Is.True);
        }

        [Test]
        public void TreeNodeData_NullDisplayName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TreeNodeData(null!, "FullName"));
        }

        [Test]
        public void TreeNodeData_NullFullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TreeNodeData("Display", null!));
        }

        #endregion
    }
}
