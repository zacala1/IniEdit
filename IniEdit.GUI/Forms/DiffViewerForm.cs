using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IniEdit;
using IniEdit.GUI.Theme;

namespace IniEdit.GUI.Forms
{
    /// <summary>
    /// A form for viewing and merging differences between two INI documents.
    /// </summary>
    public sealed class DiffViewerForm : Form
    {
        private readonly Document _leftDocument;
        private readonly Document _rightDocument;
        private readonly DocumentDiff _diff;
        private readonly string _leftTitle;
        private readonly string _rightTitle;

        private TreeView _diffTree = null!;
        private RichTextBox _leftPreview = null!;
        private RichTextBox _rightPreview = null!;
        private Button _mergeAllButton = null!;
        private Button _mergeSelectedButton = null!;
        private Button _closeButton = null!;
        private Label _summaryLabel = null!;
        private SplitContainer _mainSplit = null!;
        private SplitContainer _previewSplit = null!;

        /// <summary>
        /// Gets the merge result after the dialog is closed.
        /// </summary>
        public MergeResult? MergeResult { get; private set; }

        /// <summary>
        /// Initializes a new instance of the DiffViewerForm.
        /// </summary>
        public DiffViewerForm(Document left, Document right, string leftTitle = "Left", string rightTitle = "Right")
        {
            _leftDocument = left ?? throw new ArgumentNullException(nameof(left));
            _rightDocument = right ?? throw new ArgumentNullException(nameof(right));
            _leftTitle = leftTitle;
            _rightTitle = rightTitle;
            _diff = left.Compare(right);

            InitializeComponent();
            LoadDiffTree();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "Document Comparison";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);

            // Main layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(8)
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Summary
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Buttons

            // Summary label
            _summaryLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(this.Font, FontStyle.Bold)
            };
            mainPanel.Controls.Add(_summaryLabel, 0, 0);

            // Main split container
            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 300
            };

            // Diff tree
            _diffTree = new TreeView
            {
                Dock = DockStyle.Fill,
                ImageList = CreateImageList(),
                HideSelection = false,
                ShowNodeToolTips = true
            };
            _diffTree.AfterSelect += DiffTree_AfterSelect;
            _mainSplit.Panel1.Controls.Add(_diffTree);

            // Preview split
            _previewSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };

            // Left preview
            var leftPanel = new Panel { Dock = DockStyle.Fill };
            var leftLabel = new Label
            {
                Text = _leftTitle,
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font(this.Font, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 230, 200)
            };
            _leftPreview = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                WordWrap = false
            };
            leftPanel.Controls.Add(_leftPreview);
            leftPanel.Controls.Add(leftLabel);
            _previewSplit.Panel1.Controls.Add(leftPanel);

            // Right preview
            var rightPanel = new Panel { Dock = DockStyle.Fill };
            var rightLabel = new Label
            {
                Text = _rightTitle,
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font(this.Font, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 200, 230)
            };
            _rightPreview = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                WordWrap = false
            };
            rightPanel.Controls.Add(_rightPreview);
            rightPanel.Controls.Add(rightLabel);
            _previewSplit.Panel2.Controls.Add(rightPanel);

            _mainSplit.Panel2.Controls.Add(_previewSplit);
            mainPanel.Controls.Add(_mainSplit, 0, 1);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 5, 0, 0)
            };

            _closeButton = new Button
            {
                Text = "Close",
                Width = 100,
                Height = 30
            };
            _closeButton.Click += (s, e) => this.Close();

            _mergeSelectedButton = new Button
            {
                Text = "Merge Selected",
                Width = 120,
                Height = 30,
                Enabled = false
            };
            _mergeSelectedButton.Click += MergeSelected_Click;

            _mergeAllButton = new Button
            {
                Text = "Merge All",
                Width = 100,
                Height = 30,
                Enabled = _diff.HasChanges
            };
            _mergeAllButton.Click += MergeAll_Click;

            buttonPanel.Controls.Add(_closeButton);
            buttonPanel.Controls.Add(_mergeSelectedButton);
            buttonPanel.Controls.Add(_mergeAllButton);

            mainPanel.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainPanel);

            UpdateSummary();
        }

        private ImageList CreateImageList()
        {
            var imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);

            // Create simple colored icons
            imageList.Images.Add("section", CreateColoredIcon(Color.Blue));
            imageList.Images.Add("property", CreateColoredIcon(Color.Gray));
            imageList.Images.Add("added", CreateColoredIcon(Color.Green));
            imageList.Images.Add("removed", CreateColoredIcon(Color.Red));
            imageList.Images.Add("modified", CreateColoredIcon(Color.Orange));
            imageList.Images.Add("unchanged", CreateColoredIcon(Color.LightGray));

            return imageList;
        }

        private Bitmap CreateColoredIcon(Color color)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                }
            }
            return bmp;
        }

        private void LoadDiffTree()
        {
            _diffTree.Nodes.Clear();

            // Added sections
            if (_diff.AddedSections.Count > 0)
            {
                var addedNode = new TreeNode($"Added Sections ({_diff.AddedSections.Count})")
                {
                    ImageKey = "added",
                    SelectedImageKey = "added"
                };

                foreach (var section in _diff.AddedSections)
                {
                    var sectionNode = new TreeNode($"[{section.Name}]")
                    {
                        ImageKey = "added",
                        SelectedImageKey = "added",
                        Tag = new DiffItem(DiffItemType.AddedSection, section.Name, null, section),
                        ToolTipText = $"New section with {section.PropertyCount} properties"
                    };

                    foreach (var prop in section.GetProperties())
                    {
                        var propNode = new TreeNode($"{prop.Name} = {TruncateValue(prop.Value)}")
                        {
                            ImageKey = "added",
                            SelectedImageKey = "added",
                            Tag = new DiffItem(DiffItemType.AddedProperty, section.Name, prop.Name, prop)
                        };
                        sectionNode.Nodes.Add(propNode);
                    }

                    addedNode.Nodes.Add(sectionNode);
                }

                _diffTree.Nodes.Add(addedNode);
            }

            // Removed sections
            if (_diff.RemovedSections.Count > 0)
            {
                var removedNode = new TreeNode($"Removed Sections ({_diff.RemovedSections.Count})")
                {
                    ImageKey = "removed",
                    SelectedImageKey = "removed"
                };

                foreach (var section in _diff.RemovedSections)
                {
                    var sectionNode = new TreeNode($"[{section.Name}]")
                    {
                        ImageKey = "removed",
                        SelectedImageKey = "removed",
                        Tag = new DiffItem(DiffItemType.RemovedSection, section.Name, null, section),
                        ToolTipText = $"Removed section with {section.PropertyCount} properties"
                    };

                    foreach (var prop in section.GetProperties())
                    {
                        var propNode = new TreeNode($"{prop.Name} = {TruncateValue(prop.Value)}")
                        {
                            ImageKey = "removed",
                            SelectedImageKey = "removed",
                            Tag = new DiffItem(DiffItemType.RemovedProperty, section.Name, prop.Name, prop)
                        };
                        sectionNode.Nodes.Add(propNode);
                    }

                    removedNode.Nodes.Add(sectionNode);
                }

                _diffTree.Nodes.Add(removedNode);
            }

            // Modified sections
            if (_diff.ModifiedSections.Count > 0)
            {
                var modifiedNode = new TreeNode($"Modified Sections ({_diff.ModifiedSections.Count})")
                {
                    ImageKey = "modified",
                    SelectedImageKey = "modified"
                };

                foreach (var sectionDiff in _diff.ModifiedSections)
                {
                    var sectionName = string.IsNullOrEmpty(sectionDiff.SectionName) ? "(Default)" : $"[{sectionDiff.SectionName}]";
                    var sectionNode = new TreeNode(sectionName)
                    {
                        ImageKey = "modified",
                        SelectedImageKey = "modified",
                        Tag = new DiffItem(DiffItemType.ModifiedSection, sectionDiff.SectionName, null, sectionDiff)
                    };

                    // Added properties
                    foreach (var prop in sectionDiff.AddedProperties)
                    {
                        var propNode = new TreeNode($"+ {prop.Name} = {TruncateValue(prop.Value)}")
                        {
                            ImageKey = "added",
                            SelectedImageKey = "added",
                            Tag = new DiffItem(DiffItemType.AddedProperty, sectionDiff.SectionName, prop.Name, prop),
                            ForeColor = Color.Green
                        };
                        sectionNode.Nodes.Add(propNode);
                    }

                    // Removed properties
                    foreach (var prop in sectionDiff.RemovedProperties)
                    {
                        var propNode = new TreeNode($"- {prop.Name} = {TruncateValue(prop.Value)}")
                        {
                            ImageKey = "removed",
                            SelectedImageKey = "removed",
                            Tag = new DiffItem(DiffItemType.RemovedProperty, sectionDiff.SectionName, prop.Name, prop),
                            ForeColor = Color.Red
                        };
                        sectionNode.Nodes.Add(propNode);
                    }

                    // Modified properties
                    foreach (var propDiff in sectionDiff.ModifiedProperties)
                    {
                        var propNode = new TreeNode($"~ {propDiff.PropertyName}: {TruncateValue(propDiff.OldValue)} → {TruncateValue(propDiff.NewValue)}")
                        {
                            ImageKey = "modified",
                            SelectedImageKey = "modified",
                            Tag = new DiffItem(DiffItemType.ModifiedProperty, sectionDiff.SectionName, propDiff.PropertyName, propDiff),
                            ForeColor = Color.Orange
                        };
                        sectionNode.Nodes.Add(propNode);
                    }

                    modifiedNode.Nodes.Add(sectionNode);
                }

                _diffTree.Nodes.Add(modifiedNode);
            }

            if (!_diff.HasChanges)
            {
                _diffTree.Nodes.Add(new TreeNode("Documents are identical")
                {
                    ImageKey = "unchanged",
                    SelectedImageKey = "unchanged"
                });
            }

            _diffTree.ExpandAll();
        }

        private string TruncateValue(string value, int maxLength = 50)
        {
            if (string.IsNullOrEmpty(value))
                return "(empty)";
            if (value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength) + "...";
        }

        private void DiffTree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is DiffItem item)
            {
                _mergeSelectedButton.Enabled = true;
                UpdatePreview(item);
            }
            else
            {
                _mergeSelectedButton.Enabled = false;
                _leftPreview.Clear();
                _rightPreview.Clear();
            }
        }

        private void UpdatePreview(DiffItem item)
        {
            _leftPreview.Clear();
            _rightPreview.Clear();

            switch (item.Type)
            {
                case DiffItemType.AddedSection:
                    _leftPreview.Text = "(Section does not exist)";
                    if (item.Data is Section addedSection)
                    {
                        _rightPreview.Text = FormatSection(addedSection);
                    }
                    break;

                case DiffItemType.RemovedSection:
                    if (item.Data is Section removedSection)
                    {
                        _leftPreview.Text = FormatSection(removedSection);
                    }
                    _rightPreview.Text = "(Section does not exist)";
                    break;

                case DiffItemType.ModifiedSection:
                    if (item.Data is SectionDiff sectionDiff)
                    {
                        var leftSection = string.IsNullOrEmpty(sectionDiff.SectionName)
                            ? _leftDocument.DefaultSection
                            : _leftDocument.GetSection(sectionDiff.SectionName);
                        var rightSection = string.IsNullOrEmpty(sectionDiff.SectionName)
                            ? _rightDocument.DefaultSection
                            : _rightDocument.GetSection(sectionDiff.SectionName);

                        _leftPreview.Text = leftSection != null ? FormatSection(leftSection) : "(Section does not exist)";
                        _rightPreview.Text = rightSection != null ? FormatSection(rightSection) : "(Section does not exist)";
                    }
                    break;

                case DiffItemType.AddedProperty:
                    _leftPreview.Text = "(Property does not exist)";
                    if (item.Data is Property addedProp)
                    {
                        _rightPreview.Text = $"{addedProp.Name} = {addedProp.Value}";
                    }
                    break;

                case DiffItemType.RemovedProperty:
                    if (item.Data is Property removedProp)
                    {
                        _leftPreview.Text = $"{removedProp.Name} = {removedProp.Value}";
                    }
                    _rightPreview.Text = "(Property does not exist)";
                    break;

                case DiffItemType.ModifiedProperty:
                    if (item.Data is PropertyDiff propDiff)
                    {
                        _leftPreview.Text = $"{propDiff.PropertyName} = {propDiff.OldValue}";
                        _rightPreview.Text = $"{propDiff.PropertyName} = {propDiff.NewValue}";
                    }
                    break;
            }
        }

        private string FormatSection(Section section)
        {
            var lines = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(section.Name))
            {
                lines.AppendLine($"[{section.Name}]");
            }
            foreach (var prop in section.GetProperties())
            {
                lines.AppendLine($"{prop.Name} = {prop.Value}");
            }
            return lines.ToString();
        }

        private void UpdateSummary()
        {
            var addedSections = _diff.AddedSections.Count;
            var removedSections = _diff.RemovedSections.Count;
            var modifiedSections = _diff.ModifiedSections.Count;

            var addedProps = _diff.ModifiedSections.Sum(s => s.AddedProperties.Count);
            var removedProps = _diff.ModifiedSections.Sum(s => s.RemovedProperties.Count);
            var modifiedProps = _diff.ModifiedSections.Sum(s => s.ModifiedProperties.Count);

            _summaryLabel.Text = _diff.HasChanges
                ? $"Differences: {addedSections} added, {removedSections} removed, {modifiedSections} modified sections | " +
                  $"{addedProps} added, {removedProps} removed, {modifiedProps} modified properties"
                : "No differences found - documents are identical";
        }

        private void MergeAll_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "This will apply all changes from the right document to the left document. Continue?",
                "Merge All Changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var options = new MergeOptions
                {
                    ApplyAddedSections = true,
                    ApplyRemovedSections = true,
                    ApplyAddedProperties = true,
                    ApplyRemovedProperties = true,
                    ApplyModifiedProperties = true
                };

                MergeResult = _leftDocument.Merge(_diff, options);

                MessageBox.Show(
                    $"Merge completed:\n" +
                    $"- {MergeResult.SectionsAdded} sections added\n" +
                    $"- {MergeResult.SectionsRemoved} sections removed\n" +
                    $"- {MergeResult.PropertiesAdded} properties added\n" +
                    $"- {MergeResult.PropertiesRemoved} properties removed\n" +
                    $"- {MergeResult.PropertiesModified} properties modified",
                    "Merge Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void MergeSelected_Click(object? sender, EventArgs e)
        {
            if (_diffTree.SelectedNode?.Tag is not DiffItem item)
                return;

            var options = new MergeOptions
            {
                ApplyAddedSections = false,
                ApplyRemovedSections = false,
                ApplyAddedProperties = false,
                ApplyRemovedProperties = false,
                ApplyModifiedProperties = false
            };

            switch (item.Type)
            {
                case DiffItemType.AddedSection:
                    options.ApplyAddedSections = true;
                    break;
                case DiffItemType.RemovedSection:
                    options.ApplyRemovedSections = true;
                    break;
                case DiffItemType.AddedProperty:
                    options.ApplyAddedProperties = true;
                    break;
                case DiffItemType.RemovedProperty:
                    options.ApplyRemovedProperties = true;
                    break;
                case DiffItemType.ModifiedProperty:
                    options.ApplyModifiedProperties = true;
                    break;
            }

            // Create a partial diff for the selected item
            var partialDiff = CreatePartialDiff(item);
            var result = _leftDocument.Merge(partialDiff, options);

            if (result.TotalChanges > 0)
            {
                MessageBox.Show($"Applied {result.TotalChanges} change(s).", "Merge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Recalculate diff and refresh tree
                var newDiff = _leftDocument.Compare(_rightDocument);
                UpdateDiffFromNewComparison(newDiff);
            }
        }

        private DocumentDiff CreatePartialDiff(DiffItem item)
        {
            var partialDiff = new DocumentDiff();

            switch (item.Type)
            {
                case DiffItemType.AddedSection:
                    if (item.Data is Section addedSection)
                    {
                        partialDiff.AddedSections.Add(addedSection);
                    }
                    break;

                case DiffItemType.RemovedSection:
                    if (item.Data is Section removedSection)
                    {
                        partialDiff.RemovedSections.Add(removedSection);
                    }
                    break;

                case DiffItemType.ModifiedSection:
                    if (item.Data is SectionDiff sectionDiff)
                    {
                        partialDiff.ModifiedSections.Add(sectionDiff);
                    }
                    break;

                case DiffItemType.AddedProperty:
                case DiffItemType.RemovedProperty:
                case DiffItemType.ModifiedProperty:
                    var newSectionDiff = new SectionDiff(item.SectionName);
                    if (item.Type == DiffItemType.AddedProperty && item.Data is Property addedProp)
                    {
                        newSectionDiff.AddedProperties.Add(addedProp);
                    }
                    else if (item.Type == DiffItemType.RemovedProperty && item.Data is Property removedProp)
                    {
                        newSectionDiff.RemovedProperties.Add(removedProp);
                    }
                    else if (item.Type == DiffItemType.ModifiedProperty && item.Data is PropertyDiff propDiff)
                    {
                        newSectionDiff.ModifiedProperties.Add(propDiff);
                    }
                    partialDiff.ModifiedSections.Add(newSectionDiff);
                    break;
            }

            return partialDiff;
        }

        private void UpdateDiffFromNewComparison(DocumentDiff newDiff)
        {
            _diff.AddedSections.Clear();
            _diff.AddedSections.AddRange(newDiff.AddedSections);

            _diff.RemovedSections.Clear();
            _diff.RemovedSections.AddRange(newDiff.RemovedSections);

            _diff.ModifiedSections.Clear();
            _diff.ModifiedSections.AddRange(newDiff.ModifiedSections);

            LoadDiffTree();
            UpdateSummary();
            _mergeAllButton.Enabled = _diff.HasChanges;
        }

        private void ApplyTheme()
        {
            ThemeManager.ApplyTheme(this);
        }

        private enum DiffItemType
        {
            AddedSection,
            RemovedSection,
            ModifiedSection,
            AddedProperty,
            RemovedProperty,
            ModifiedProperty
        }

        private sealed class DiffItem
        {
            public DiffItemType Type { get; }
            public string SectionName { get; }
            public string? PropertyName { get; }
            public object? Data { get; }

            public DiffItem(DiffItemType type, string sectionName, string? propertyName, object? data)
            {
                Type = type;
                SectionName = sectionName;
                PropertyName = propertyName;
                Data = data;
            }
        }
    }
}
