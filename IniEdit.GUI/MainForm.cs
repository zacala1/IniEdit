using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using IniEdit;
using IniEdit.GUI.Commands;

namespace IniEdit.GUI
{
    public partial class MainForm : Form
    {
        #region Fields
        private string currentFilePath = string.Empty;
        private Document? documentConfig;
        private IniConfigOption configOptions;
        private bool isDirty = false;
        private Encoding currentEncoding = Encoding.UTF8;

        // Inline cell editing
        private TextBox inlineCellEditBox = new();
        private ListViewItem.ListViewSubItem? currentEditingCell;

        // Comment editing
        private bool isUpdatingCommentsFromCode = false;

        // Find/Replace
        private FindReplaceDialog? findReplaceDialog = null;
        private int lastSearchSectionIndex = -1;
        private int lastSearchPropertyIndex = -1;

        // Undo/Redo
        private readonly CommandManager commandManager = new();
        private ToolStripMenuItem? undoMenuItem;
        private ToolStripMenuItem? redoMenuItem;

        // Recent Files
        private readonly RecentFilesManager recentFilesManager = new();
        private ToolStripMenuItem? recentFilesMenuItem;

        // Copy/Paste
        private ToolStripMenuItem? copyMenuItem;
        private ToolStripMenuItem? cutMenuItem;
        private ToolStripMenuItem? pasteMenuItem;

        // Validation and Statistics
        private ToolStripStatusLabel? encodingStatusLabel;
        private ToolStripStatusLabel? validationStatusLabel;

        // Performance optimization caches
        private Font? _duplicateKeyFont;
        private DocumentStatistics? _cachedStatistics;
        private bool _statisticsDirty = true;
        #endregion

        public MainForm()
        {
            InitializeComponent();
            configOptions = new IniConfigOption { CollectParsingErrors = true };
            SetupForm();
            SetupInlineCellEditor();
            SetupMenuItems();
            SetupCommandManager();
            SetupRecentFiles();
            UpdateTitle();

            // Enable key preview for keyboard shortcuts
            this.KeyPreview = true;
            this.KeyDown += OnFormKeyDown;
        }

        private void SetupForm()
        {
            newToolStripMenuItem.Click += NewFile;
            openToolStripMenuItem.Click += OpenFile;
            saveToolStripMenuItem.Click += SaveFile;
            saveAsToolStripMenuItem.Click += SaveAsFile;
            sectionView.SelectedIndexChanged += OnSectionSelectionChanged;
            propertyView.MouseDoubleClick += OnPropertyDoubleClick;
            propertyView.Click += OnPropertyClick;
            preCommentsTextBox.TextChanged += OnPreCommentsChanged;
            inlineCommentTextBox.TextChanged += OnInlineCommentChanged;

            // Setup status bar labels
            SetupStatusBar();

            // Setup context menus
            SetupSectionContextMenu();
            SetupPropertyContextMenu();

            RefreshStatusBar();
        }

        private void SetupStatusBar()
        {
            // Add encoding status label
            encodingStatusLabel = new ToolStripStatusLabel
            {
                Text = "Encoding: UTF-8",
                BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched
            };
            encodingStatusLabel.Click += ShowEncodingMenu;
            statusStrip1.Items.Add(encodingStatusLabel);

            // Add validation status label
            validationStatusLabel = new ToolStripStatusLabel
            {
                Text = "✓ No errors",
                ForeColor = Color.Green,
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                IsLink = false
            };
            validationStatusLabel.Click += ShowValidationDialog;
            statusStrip1.Items.Add(validationStatusLabel);
        }

        private void SetupCommandManager()
        {
            commandManager.StateChanged += (s, e) => UpdateUndoRedoMenuItems();
            UpdateUndoRedoMenuItems();
        }

        private void SetupRecentFiles()
        {
            recentFilesManager.RecentFilesChanged += (s, e) => UpdateRecentFilesMenu();
            UpdateRecentFilesMenu();
        }

        private void OnFormKeyDown(object? sender, KeyEventArgs e)
        {
            // Undo: Ctrl+Z
            if (e.Control && e.KeyCode == Keys.Z && !e.Shift)
            {
                Undo(sender, e);
                e.Handled = true;
            }
            // Redo: Ctrl+Y or Ctrl+Shift+Z
            else if ((e.Control && e.KeyCode == Keys.Y) || (e.Control && e.Shift && e.KeyCode == Keys.Z))
            {
                Redo(sender, e);
                e.Handled = true;
            }
            // Copy: Ctrl+C
            else if (e.Control && e.KeyCode == Keys.C)
            {
                Copy(sender, e);
                e.Handled = true;
            }
            // Cut: Ctrl+X
            else if (e.Control && e.KeyCode == Keys.X)
            {
                Cut(sender, e);
                e.Handled = true;
            }
            // Paste: Ctrl+V
            else if (e.Control && e.KeyCode == Keys.V)
            {
                Paste(sender, e);
                e.Handled = true;
            }
        }

        private void SetupMenuItems()
        {
            // Add Recent Files menu item to File menu (after Save As)
            var saveAsIndex = fileToolStripMenuItem.DropDownItems.IndexOf(saveAsToolStripMenuItem);
            if (saveAsIndex >= 0)
            {
                fileToolStripMenuItem.DropDownItems.Insert(saveAsIndex + 1, new ToolStripSeparator());
                recentFilesMenuItem = new ToolStripMenuItem("Recent &Files");
                fileToolStripMenuItem.DropDownItems.Insert(saveAsIndex + 2, recentFilesMenuItem);
            }

            // Add Edit menu
            var editMenu = new ToolStripMenuItem("&Edit");

            // Undo/Redo
            undoMenuItem = new ToolStripMenuItem("&Undo");
            undoMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
            undoMenuItem.Click += Undo;

            redoMenuItem = new ToolStripMenuItem("&Redo");
            redoMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            redoMenuItem.Click += Redo;

            // Copy/Cut/Paste
            copyMenuItem = new ToolStripMenuItem("&Copy");
            copyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            copyMenuItem.Click += Copy;

            cutMenuItem = new ToolStripMenuItem("Cu&t");
            cutMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            cutMenuItem.Click += Cut;

            pasteMenuItem = new ToolStripMenuItem("&Paste");
            pasteMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            pasteMenuItem.Click += Paste;

            // Section menu
            var sectionMenu = new ToolStripMenuItem("Section");
            var addSectionMenu = new ToolStripMenuItem("Add Section");
            var editSectionMenu = new ToolStripMenuItem("Edit Section");
            var deleteSectionMenu = new ToolStripMenuItem("Delete Section");
            var moveSectionUpMenu = new ToolStripMenuItem("Move Section Up");
            var moveSectionDownMenu = new ToolStripMenuItem("Move Section Down");
            var duplicateSectionMenu = new ToolStripMenuItem("Duplicate Section");
            var sortSectionsMenu = new ToolStripMenuItem("Sort Sections");

            addSectionMenu.Click += AddSection;
            editSectionMenu.Click += EditSection;
            deleteSectionMenu.Click += DeleteSection;
            moveSectionUpMenu.Click += MoveSectionUp;
            moveSectionDownMenu.Click += MoveSectionDown;
            duplicateSectionMenu.Click += DuplicateSection;
            sortSectionsMenu.Click += SortSections;

            sectionMenu.DropDownItems.AddRange(new ToolStripItem[] {
                addSectionMenu, editSectionMenu, deleteSectionMenu,
                new ToolStripSeparator(),
                moveSectionUpMenu, moveSectionDownMenu,
                new ToolStripSeparator(),
                duplicateSectionMenu,
                new ToolStripSeparator(),
                sortSectionsMenu
            });

            // Key-Value menu
            var keyValueMenu = new ToolStripMenuItem("Key-Value");
            var addKeyValueMenu = new ToolStripMenuItem("Add Key-Value");
            var editKeyValueMenu = new ToolStripMenuItem("Edit Key-Value");
            var deleteKeyValueMenu = new ToolStripMenuItem("Delete Key-Value");
            var moveKeyUpMenu = new ToolStripMenuItem("Move Key Up");
            var moveKeyDownMenu = new ToolStripMenuItem("Move Key Down");
            var duplicateKeyMenu = new ToolStripMenuItem("Duplicate Key");
            var sortKeysMenu = new ToolStripMenuItem("Sort Keys");

            addKeyValueMenu.Click += AddKeyValue;
            editKeyValueMenu.Click += EditKeyValue;
            deleteKeyValueMenu.Click += DeleteKeyValue;
            moveKeyUpMenu.Click += MoveKeyUp;
            moveKeyDownMenu.Click += MoveKeyDown;
            duplicateKeyMenu.Click += DuplicateKey;
            sortKeysMenu.Click += SortKeys;

            keyValueMenu.DropDownItems.AddRange(new ToolStripItem[] {
                addKeyValueMenu, editKeyValueMenu, deleteKeyValueMenu,
                new ToolStripSeparator(),
                moveKeyUpMenu, moveKeyDownMenu,
                new ToolStripSeparator(),
                duplicateKeyMenu,
                new ToolStripSeparator(),
                sortKeysMenu
            });

            // Find/Replace menu
            var findReplaceMenu = new ToolStripMenuItem("&Find && Replace");
            findReplaceMenu.ShortcutKeys = Keys.Control | Keys.F;
            findReplaceMenu.Click += OpenFindReplace;

            editMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                undoMenuItem,
                redoMenuItem,
                new ToolStripSeparator(),
                copyMenuItem,
                cutMenuItem,
                pasteMenuItem,
                new ToolStripSeparator(),
                findReplaceMenu,
                new ToolStripSeparator(),
                sectionMenu,
                keyValueMenu
            });
            menuStrip1.Items.Add(editMenu);

            // Add Tools menu
            var toolsMenu = new ToolStripMenuItem("&Tools");

            var validateMenu = new ToolStripMenuItem("&Validate Document");
            validateMenu.ShortcutKeys = Keys.F8;
            validateMenu.Click += ShowValidationDialog;

            var statisticsMenu = new ToolStripMenuItem("Show &Statistics");
            statisticsMenu.ShortcutKeys = Keys.F9;
            statisticsMenu.Click += ShowStatisticsDialog;

            var encodingMenu = new ToolStripMenuItem("Change &Encoding...");
            encodingMenu.Click += ShowEncodingMenu;

            var settingsMenu = new ToolStripMenuItem("&Settings");
            settingsMenu.Click += OpenSettings;

            toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                validateMenu,
                statisticsMenu,
                new ToolStripSeparator(),
                encodingMenu,
                new ToolStripSeparator(),
                settingsMenu
            });
            menuStrip1.Items.Add(toolsMenu);

            // Add Help menu
            var helpMenu = new ToolStripMenuItem("&Help");
            var aboutMenu = new ToolStripMenuItem("&About");
            aboutMenu.ShortcutKeys = Keys.F1;
            aboutMenu.Click += ShowAboutDialog;
            helpMenu.DropDownItems.Add(aboutMenu);
            menuStrip1.Items.Add(helpMenu);
        }

        private void RefreshSectionList()
        {
            if (documentConfig == null)
                return;

            sectionView.Items.Clear();
            sectionView.Items.Add(GetGlobalSectionName());
            foreach (var section in documentConfig)
            {
                sectionView.Items.Add(section.Name);
            }
        }

        private void RefreshKeyValueList(string sectionName)
        {
            propertyView.Items.Clear();
            var section = GetSection(sectionName);

            var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in section)
            {
                if (!seenKeys.Add(property.Name))
                    duplicateKeys.Add(property.Name);
            }

            if (_duplicateKeyFont == null && duplicateKeys.Count > 0)
                _duplicateKeyFont = new Font(propertyView.Font, FontStyle.Bold);

            foreach (var property in section)
            {
                var item = propertyView.Items.Add(property.Name);
                item.SubItems.Add(property.Value);

                if (duplicateKeys.Contains(property.Name))
                {
                    item.BackColor = Color.LightCoral;
                    item.ForeColor = Color.DarkRed;
                    item.Font = _duplicateKeyFont;
                    item.ToolTipText = "⚠ Duplicate key detected!";
                }
            }
        }

        private void RefreshStatusBar()
        {
            int totalSections = sectionView.Items.Count;
            int currentSection = sectionView.SelectedIndex + 1;
            sectionStatusLabel.Text = $"Sections: {currentSection}/{totalSections}";

            int totalKeys = propertyView.Items.Count;
            int currentKey = propertyView.SelectedItems.Count > 0 ? propertyView.SelectedIndices[0] + 1 : 0;
            string selectedKey = propertyView.SelectedItems.Count > 0 ? propertyView.SelectedItems[0].Text : "-";
            keyStatusLabel.Text = $"Keys: {currentKey}/{totalKeys} [{selectedKey}]";

            string filePath = string.IsNullOrEmpty(currentFilePath) ? "-" : currentFilePath;
            filePathStatusLabel.Text = $"File: {filePath}";

            if (encodingStatusLabel != null)
            {
                encodingStatusLabel.Text = $"Encoding: {EncodingHelper.GetEncodingName(currentEncoding)}";
            }

            if (validationStatusLabel != null && documentConfig != null)
            {
                if (_statisticsDirty)
                {
                    _cachedStatistics = ValidationHelper.GetStatistics(documentConfig);
                    _statisticsDirty = false;
                }

                if (_cachedStatistics!.ValidationErrors > 0)
                {
                    validationStatusLabel.Text = $"⚠ {_cachedStatistics.ValidationErrors} validation error(s)";
                    validationStatusLabel.ForeColor = Color.Red;
                    validationStatusLabel.IsLink = true;
                }
                else
                {
                    validationStatusLabel.Text = "✓ No errors";
                    validationStatusLabel.ForeColor = Color.Green;
                    validationStatusLabel.IsLink = false;
                }
            }
        }

        private void SetupInlineCellEditor()
        {
            inlineCellEditBox.Visible = false;
            inlineCellEditBox.BorderStyle = BorderStyle.FixedSingle;
            inlineCellEditBox.KeyPress += OnInlineCellEditorKeyPress;
            inlineCellEditBox.LostFocus += OnInlineCellEditorLostFocus;
            propertyView.Controls.Add(inlineCellEditBox);
        }

        private void BeginInlineCellEdit(Point clientPoint)
        {
            ListViewHitTestInfo hitTest = propertyView.HitTest(clientPoint);
            if (hitTest.SubItem != null)
            {
                Rectangle subItemRect = hitTest.SubItem.Bounds;
                currentEditingCell = hitTest.SubItem;

                inlineCellEditBox.Location = new Point(subItemRect.Left, subItemRect.Top);
                inlineCellEditBox.Size = new Size(subItemRect.Width, subItemRect.Height);
                inlineCellEditBox.Text = currentEditingCell.Text;
                inlineCellEditBox.Visible = true;
                inlineCellEditBox.Focus();
                inlineCellEditBox.SelectAll();
            }
        }

        private bool IsKeyDuplicateInSection(string sectionName, string newKey, string oldKey)
        {
            var section = GetSection(sectionName);
            return section.HasProperty(newKey) && newKey != oldKey;
        }

        private void CommitInlineCellEdit()
        {
            if (currentEditingCell != null && propertyView.SelectedItems.Count > 0)
            {
                ListViewItem currentItem = propertyView.SelectedItems[0];
                bool isKeyColumn = currentEditingCell == currentItem.SubItems[0];
                string oldValue = currentEditingCell.Text;
                string newValue = inlineCellEditBox.Text;

                if (isKeyColumn)
                {
                    if (IsKeyDuplicateInSection(GetSelectedSectionName(), newValue, oldValue))
                    {
                        MessageBox.Show($"Key '{newValue}' already exists in this section!",
                            "Duplicate Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        inlineCellEditBox.Focus();
                        return;
                    }
                    UpdateKey(oldValue, newValue);
                }
                else
                {
                    string key = currentItem.SubItems[0].Text;
                    UpdateValue(key, newValue);
                }

                currentEditingCell.Text = newValue;
                inlineCellEditBox.Visible = false;
                currentEditingCell = null;
                SetDirty();
            }
        }

        private void AddSection(object? sender, EventArgs e)
        {
            if (documentConfig == null)
                return;

            using (var dialog = new InputDialog("Add Section", "Enter section name:"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string sectionName = dialog.InputText;
                    if (string.IsNullOrWhiteSpace(sectionName))
                    {
                        MessageBox.Show("Section name cannot be empty.");
                        return;
                    }

                    if (documentConfig.HasSection(sectionName) ||
                        sectionName == GetGlobalSectionName())
                    {
                        MessageBox.Show("Section already exists.");
                        return;
                    }

                    documentConfig.AddSection(new IniEdit.Section(sectionName));
                    RefreshSectionList();
                    SetDirty();
                }
            }
        }

        private void EditSection(object? sender, EventArgs e)
        {
            if (documentConfig == null)
                return;
            if (sectionView.SelectedItem == null)
                return;

            var selectedSection = GetSelectedSectionName();
            if (selectedSection == null)
            {
                MessageBox.Show("Please select a section first.");
                return;
            }
            if (selectedSection == GetGlobalSectionName())
            {
                MessageBox.Show("Default Section name cannot be edited.");
                return;
            }

            using (var dialog = new InputDialog("Edit Section", "Enter new section name:", selectedSection))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string newName = dialog.InputText;
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        MessageBox.Show("Section name cannot be empty.");
                        return;
                    }

                    if (documentConfig.HasSection(newName) && newName != selectedSection)
                    {
                        MessageBox.Show("Section already exists.");
                        return;
                    }

                    var section = GetSelectedSection();
                    documentConfig.RemoveSection(selectedSection);
                    var newSection = new IniEdit.Section(newName);
                    newSection.AddPropertyRange(section.GetProperties());
                    newSection.PreComments.AddRange(section.PreComments);
                    newSection.Comment = section.Comment;
                    documentConfig.AddSection(newSection);
                    RefreshSectionList();
                    SetDirty();
                }
            }
        }

        private void DeleteSection(object? sender, EventArgs e)
        {
            if (documentConfig == null)
                return;
            if (sectionView.SelectedItem == null)
                return;

            var selectedSection = GetSelectedSectionName();
            if (selectedSection == null)
            {
                MessageBox.Show("Please select a section first.");
                return;
            }

            if (MessageBox.Show($"Are you sure you want to delete section '{selectedSection}'?",
                "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (selectedSection == GetGlobalSectionName())
                {
                    documentConfig.DefaultSection.Clear();
                    RefreshKeyValueList(selectedSection);
                }
                else
                {
                    documentConfig.RemoveSection(selectedSection);
                    RefreshSectionList();
                    SetDirty();
                    // Select first section after deletion
                    if (sectionView.Items.Count > 0)
                    {
                        sectionView.SelectedIndex = 0;
                    }
                }
                RefreshStatusBar();
            }
        }

        private void MoveSectionUp(object? sender, EventArgs e)
        {
            if (documentConfig == null)
                return;
            if (sectionView.SelectedItem == null)
                return;
            if (sectionView.SelectedIndex <= 1)
                return;

            int currentIndex = sectionView.SelectedIndex - 1;
            // ���� ��ġ ����
            var section = documentConfig[currentIndex];
            documentConfig.RemoveSection(currentIndex);
            documentConfig.InsertSection(currentIndex - 1, section);

            RefreshSectionList();
            sectionView.SelectedIndex = currentIndex;
            RefreshStatusBar();
        }

        private void MoveSectionDown(object? sender, EventArgs e)
        {
            if (documentConfig == null)
                return;
            if (sectionView.SelectedItem == null)
                return;
            if (sectionView.SelectedIndex == 0 ||
                sectionView.SelectedIndex == sectionView.Items.Count - 1)
                return;

            int currentIndex = sectionView.SelectedIndex - 1;
            // ���� ��ġ ����
            var section = documentConfig[currentIndex];
            documentConfig.RemoveSection(currentIndex);
            documentConfig.InsertSection(currentIndex + 1, section);

            RefreshSectionList();
            sectionView.SelectedIndex = currentIndex;
            RefreshStatusBar();
        }

        private void DuplicateSection(object? sender, EventArgs e)
        {
            if (documentConfig == null)
                return;
            if (sectionView.SelectedItem == null)
                return;

            string originalName = sectionView.SelectedItem.ToString() ?? string.Empty;
            using (var dialog = new InputDialog("Duplicate Section",
                "Enter new section name:", originalName + "_copy"))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string newName = dialog.InputText;
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        MessageBox.Show("Section name cannot be empty.");
                        return;
                    }

                    if (documentConfig.HasSection(newName) ||
                        newName == GetGlobalSectionName())
                    {
                        MessageBox.Show("Section already exists.");
                        return;
                    }

                    // ���� ����
                    var originalSection = GetSection(originalName);
                    var newSection = new Section(newName);

                    // �Ӽ� ����
                    foreach (var property in originalSection)
                    {
                        newSection.AddProperty(property.Clone());
                    }

                    // �ּ� ����
                    newSection.PreComments.AddRange(originalSection.PreComments);
                    newSection.Comment = originalSection.Comment?.Clone();

                    documentConfig.AddSection(newSection);
                    RefreshSectionList();
                    SetDirty();
                    sectionView.SelectedItem = newName;
                    RefreshStatusBar();
                }
            }
        }

        private void SortSections(object? sender, EventArgs e)
        {
            if (documentConfig == null)
                return;
            string? selectedSection = sectionView.SelectedItem?.ToString();

            documentConfig.SortSectionsByName();

            RefreshSectionList();
            SetDirty();
            if (selectedSection != null)
            {
                sectionView.SelectedItem = selectedSection;
            }
            RefreshStatusBar();
        }

        private void AddKeyValue(object? sender, EventArgs e)
        {
            if (sectionView.SelectedItem == null)
            {
                MessageBox.Show("Please select a section first.");
                return;
            }

            using (var dialog = new KeyValueInputDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string key = dialog.Key;
                    string value = dialog.Value;
                    var selectedSection = GetSelectedSection();

                    if (selectedSection.TryGetProperty(key, out _))
                    {
                        MessageBox.Show("Key already exists in this section.");
                        return;
                    }

                    selectedSection.AddProperty(new Property(key, value));
                    RefreshKeyValueList(selectedSection.Name);
                    SetDirty();
                }
            }
        }

        private void EditKeyValue(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count == 0)
                return;

            var selectedItem = propertyView.SelectedItems[0];
            string oldKey = selectedItem.SubItems[0].Text;
            string oldValue = selectedItem.SubItems[1].Text;

            using (var dialog = new KeyValueInputDialog(oldKey, oldValue))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string newKey = dialog.Key;
                    string newValue = dialog.Value;
                    var selectedSection = GetSelectedSection();

                    if (newKey != oldKey && selectedSection.TryGetProperty(newKey, out _))
                    {
                        MessageBox.Show("Key already exists in this section.");
                        return;
                    }

                    // Preserve order and comments
                    var oldProperty = selectedSection[oldKey];
                    int index = -1;
                    for (int i = 0; i < selectedSection.PropertyCount; i++)
                    {
                        if (selectedSection[i] == oldProperty)
                        {
                            index = i;
                            break;
                        }
                    }

                    var newProperty = new Property(newKey, newValue);
                    newProperty.PreComments.AddRange(oldProperty.PreComments);
                    newProperty.Comment = oldProperty.Comment;
                    newProperty.IsQuoted = oldProperty.IsQuoted;

                    selectedSection.RemoveProperty(oldKey);
                    if (index >= 0)
                    {
                        selectedSection.InsertProperty(index, newProperty);
                    }
                    else
                    {
                        selectedSection.AddProperty(newProperty);
                    }
                    RefreshKeyValueList(selectedSection.Name);
                    SetDirty();
                }
            }
        }

        private void DeleteKeyValue(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count == 0)
                return;

            var selectedItem = propertyView.SelectedItems[0];
            string key = selectedItem.SubItems[0].Text;
            var selectedSection = GetSelectedSection();

            if (MessageBox.Show($"Are you sure you want to delete key '{key}'?",
                "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                selectedSection.RemoveProperty(key);
                RefreshKeyValueList(selectedSection.Name);
                SetDirty();
            }
        }

        private void MoveKeyUp(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count == 0)
                return;
            if (propertyView.SelectedIndices[0] == 0)
                return;

            int currentIndex = propertyView.SelectedIndices[0];
            var selectedSection = GetSelectedSection();

            // �Ӽ� ��ġ ����
            var property = selectedSection[currentIndex];
            selectedSection.RemoveProperty(currentIndex);
            selectedSection.InsertProperty(currentIndex - 1, property);

            RefreshKeyValueList(selectedSection.Name);
            propertyView.Items[currentIndex - 1].Selected = true;
            RefreshStatusBar();
        }

        private void MoveKeyDown(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count == 0)
                return;
            if (propertyView.SelectedIndices[0] == propertyView.Items.Count - 1)
                return;

            int currentIndex = propertyView.SelectedIndices[0];
            var selectedSection = GetSelectedSection();

            // �Ӽ� ��ġ ����
            var property = selectedSection[currentIndex];
            selectedSection.RemoveProperty(currentIndex);
            selectedSection.InsertProperty(currentIndex + 1, property);

            RefreshKeyValueList(selectedSection.Name);
            propertyView.Items[currentIndex + 1].Selected = true;
            RefreshStatusBar();
        }

        private void DuplicateKey(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count == 0)
                return;

            var selectedItem = propertyView.SelectedItems[0];
            string originalKey = selectedItem.SubItems[0].Text;
            string originalValue = selectedItem.SubItems[1].Text;
            var selectedSection = GetSelectedSection();

            using (var dialog = new KeyValueInputDialog(originalKey + "_copy", originalValue))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string newKey = dialog.Key;
                    string newValue = dialog.Value;

                    if (selectedSection.TryGetProperty(newKey, out _))
                    {
                        MessageBox.Show("Key already exists in this section.");
                        return;
                    }

                    // �Ӽ� ����
                    var originalProperty = selectedSection[originalKey].Clone();
                    var newProperty = new Property(newKey, newValue);
                    newProperty.PreComments.AddRange(originalProperty.PreComments);
                    newProperty.Comment = originalProperty.Comment;

                    selectedSection.AddProperty(newProperty);
                    RefreshKeyValueList(selectedSection.Name);

                    // ���� �߰��� �׸� ����
                    foreach (ListViewItem item in propertyView.Items)
                    {
                        if (item.Text == newKey)
                        {
                            item.Selected = true;
                            break;
                        }
                    }
                    RefreshStatusBar();
                }
            }
        }

        private void SortKeys(object? sender, EventArgs e)
        {
            if (sectionView.SelectedItem == null)
                return;

            var selectedSection = GetSelectedSection();
            string? selectedKey = propertyView.SelectedItems.Count > 0 ?
                propertyView.SelectedItems[0].Text : null;
            selectedSection.SortPropertiesByName();

            RefreshKeyValueList(selectedSection.Name);
            if (selectedKey != null)
            {
                foreach (ListViewItem item in propertyView.Items)
                {
                    if (item.Text == selectedKey)
                    {
                        item.Selected = true;
                        break;
                    }
                }
            }
            RefreshStatusBar();
        }

        private void OnInlineCellEditorKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                CommitInlineCellEdit();
                e.Handled = true;
            }
            else if (e.KeyChar == (char)Keys.Escape)
            {
                inlineCellEditBox.Visible = false;
                currentEditingCell = null;
                e.Handled = true;
            }
        }

        private void OnInlineCellEditorLostFocus(object? sender, EventArgs e)
        {
            if (inlineCellEditBox.Visible)
            {
                CommitInlineCellEdit();
            }
        }

        private void OnSectionSelectionChanged(object? sender, EventArgs e)
        {
            if (sectionView.SelectedItem == null)
                return;

            try
            {
                isUpdatingCommentsFromCode = true;

                var selectedSection = GetSelectedSection();

                if (selectedSection.Name == GetGlobalSectionName())
                {
                    preCommentsTextBox.Enabled = false;
                    inlineCommentTextBox.Enabled = false;
                }
                else
                {
                    preCommentsTextBox.Enabled = true;
                    inlineCommentTextBox.Enabled = true;
                }

                preCommentsTextBox.Text = selectedSection.PreComments.ToMultiLineText();
                inlineCommentTextBox.Text = selectedSection.Comment?.Value ?? "";

                RefreshKeyValueList(selectedSection.Name);
                RefreshStatusBar();
            }
            finally
            {
                isUpdatingCommentsFromCode = false;
            }
        }

        private void OnPropertyDoubleClick(object? sender, MouseEventArgs e)
        {
            BeginInlineCellEdit(e.Location);
        }

        private void OnPropertyClick(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count > 0)
            {
                ListViewHitTestInfo hitTest = propertyView.HitTest(propertyView.PointToClient(Cursor.Position));
                try
                {
                    isUpdatingCommentsFromCode = true;

                    if (hitTest.Item != null)
                    {
                        string key = hitTest.Item.SubItems[0].Text;
                        var selectedSection = GetSelectedSection();
                        var property = selectedSection[key];
                        preCommentsTextBox.Text = property.PreComments.ToMultiLineText();
                        inlineCommentTextBox.Text = property.Comment?.Value ?? "";
                        RefreshStatusBar();
                    }
                }
                finally
                {
                    isUpdatingCommentsFromCode = false;
                }
            }
        }

        private void UpdateKey(string oldKey, string newKey)
        {
            if (oldKey == newKey)
                return;
            if (sectionView.SelectedItem == null)
                return;

            var selectedSection = GetSelectedSection();
            var property = selectedSection[oldKey];

            // �� Property ���� �� ��/�ּ� ����
            var newProperty = new Property(newKey, property.Value);
            newProperty.PreComments.AddRange(property.PreComments);
            newProperty.Comment = property.Comment;

            // ���� Property ���� �� �� Property �߰�
            selectedSection.RemoveProperty(oldKey);
            selectedSection.AddProperty(newProperty);
        }

        private void UpdateValue(string key, string newValue)
        {
            if (sectionView.SelectedItem == null)
                return;

            var selectedSection = GetSelectedSection();
            selectedSection.SetProperty(key, newValue);

            // UI ������Ʈ
            if (propertyView.SelectedItems.Count > 0)
            {
                ListViewItem currentItem = propertyView.SelectedItems[0];
                if (currentItem.SubItems[0].Text == key)
                {
                    currentItem.SubItems[1].Text = newValue;
                }
            }
        }

        private void ValidateValueComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return;

            if (comment.Contains("\n"))
            {
                throw new InvalidOperationException("Value comments cannot contain multiple lines!");
            }
        }

        private void OnPreCommentsChanged(object? sender, EventArgs e)
        {
            if (isUpdatingCommentsFromCode)
                return;

            if (propertyView.SelectedItems.Count > 0)
            {
                ListViewHitTestInfo hitTest = propertyView.HitTest(propertyView.PointToClient(Cursor.Position));
                if (hitTest.Item != null)
                {
                    string key = hitTest.Item.SubItems[0].Text;
                    var selectedSection = GetSelectedSection();
                    var property = selectedSection[key];

                    try
                    {
                        property.PreComments.TrySetMultiLineText(preCommentsTextBox.Text);
                        SetDirty();
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(ex.Message, "Invalid Comment",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        preCommentsTextBox.Focus();
                        return;
                    }
                }
            }
            else if (sectionView.SelectedItem != null)
            {
                var selectedSection = GetSelectedSection();
                selectedSection.PreComments.TrySetMultiLineText(preCommentsTextBox.Text);
            }
        }

        private void OnInlineCommentChanged(object? sender, EventArgs e)
        {
            if (isUpdatingCommentsFromCode)
                return;

            if (propertyView.SelectedItems.Count > 0)
            {
                ListViewHitTestInfo hitTest = propertyView.HitTest(propertyView.PointToClient(Cursor.Position));
                if (hitTest.Item != null)
                {
                    string key = hitTest.Item.SubItems[0].Text;
                    var selectedSection = GetSelectedSection();
                    var property = selectedSection[key];

                    try
                    {
                        ValidateValueComment(inlineCommentTextBox.Text);
                        property.Comment = string.IsNullOrEmpty(inlineCommentTextBox.Text)
                            ? null
                            : new Comment(inlineCommentTextBox.Text);
                        SetDirty();
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(ex.Message, "Invalid Comment",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        inlineCommentTextBox.Focus();
                        return;
                    }
                }
            }
            else if (sectionView.SelectedItem != null)
            {
                var selectedSection = GetSelectedSection();
                try
                {
                    ValidateValueComment(inlineCommentTextBox.Text);
                    selectedSection.Comment = string.IsNullOrEmpty(inlineCommentTextBox.Text)
                        ? null
                        : new Comment(inlineCommentTextBox.Text);
                    SetDirty();
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Invalid Comment",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    inlineCommentTextBox.Focus();
                }
            }
        }

        private void NewFile(object? sender, EventArgs e)
        {
            if (!PromptSaveChanges())
                return;

            currentFilePath = "";
            documentConfig = new();
            commandManager.Clear(); // Clear undo/redo history

            RefreshSectionList();
            if (sectionView.Items.Count > 0)
            {
                sectionView.SelectedIndex = 0;
                RefreshKeyValueList(GetSelectedSectionName());
            }
            RefreshStatusBar();
            SetDirty(false);
        }

        private async void OpenFile(object? sender, EventArgs e)
        {
            if (!PromptSaveChanges())
                return;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    await LoadIniFileAsync(ofd.FileName);
                }
            }
        }

        private async Task LoadIniFileAsync(string filename)
        {
            currentFilePath = filename;
            sectionView.Items.Clear();

            try
            {
                // Detect file encoding
                currentEncoding = EncodingHelper.DetectEncoding(filename);

                // Use async I/O with current options and detected encoding
                documentConfig = await IniConfigManager.LoadAsync(filename, currentEncoding, configOptions);

                RefreshSectionList();

                if (sectionView.Items.Count > 0)
                {
                    sectionView.SelectedIndex = 0;
                    RefreshKeyValueList(GetSelectedSectionName());
                }

                RefreshStatusBar();
                SetDirty(false);
                commandManager.Clear(); // Clear undo/redo history
                recentFilesManager.AddRecentFile(filename); // Add to recent files

                // Show parsing errors if any
                if (documentConfig.ParsingErrors.Count > 0)
                {
                    var errorMsg = $"File loaded with {documentConfig.ParsingErrors.Count} parsing error(s):\n\n";
                    foreach (var error in documentConfig.ParsingErrors.Take(5))
                    {
                        errorMsg += $"Line {error.LineNumber}: {error.Reason}\n";
                    }
                    if (documentConfig.ParsingErrors.Count > 5)
                    {
                        errorMsg += $"\n... and {documentConfig.ParsingErrors.Count - 5} more error(s)";
                    }
                    MessageBox.Show(errorMsg, "Parsing Warnings",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method for opening recent files
        private async void LoadFile(string filePath)
        {
            await LoadIniFileAsync(filePath);
            UpdateTitle();
        }

        private async void SaveFile(object? sender, EventArgs e)
        {
            if (documentConfig == null)
            {
                MessageBox.Show($"Error saving file: documentConfig invalid", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveAsFile(sender, e);
                return;
            }

            try
            {
                await IniConfigManager.SaveAsync(currentFilePath, documentConfig);
                SetDirty(false);
                MessageBox.Show("File saved successfully!", "Save",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SaveAsFile(object? sender, EventArgs e)
        {
            if (documentConfig == null)
            {
                MessageBox.Show($"Error saving file: documentConfig invalid", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using SaveFileDialog saveFileDialog = new()
            {
                Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = "ini"
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                currentFilePath = saveFileDialog.FileName;
                await IniConfigManager.SaveAsync(currentFilePath, documentConfig);
                SetDirty(false);
                RefreshStatusBar();
                MessageBox.Show("File saved successfully!", "Save",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAboutDialog(object? sender, EventArgs e)
        {
            AboutBox box = new AboutBox();
            box.Show();
        }

        #region Get Section
        private string GetSelectedSectionName()
        {
            return sectionView.SelectedItem?.ToString() ?? string.Empty;
        }

        private string GetGlobalSectionName()
        {
            return documentConfig?.DefaultSection.Name ?? string.Empty;
        }

        private Section GetSelectedSection()
        {
            var sectionName = GetSelectedSectionName();
            return GetSection(sectionName);
        }

        private Section GetSection(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException(nameof(sectionName));
            if (documentConfig == null)
                throw new InvalidOperationException("documentConfig is null");

            if (sectionName == GetGlobalSectionName())
            {
                return documentConfig.DefaultSection;
            }
            return documentConfig[sectionName];
        }
        #endregion

        #region Dirty Flag Management
        private void SetDirty(bool dirty = true)
        {
            isDirty = dirty;
            _statisticsDirty = true; // Mark statistics as needing recalculation
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            string fileName = string.IsNullOrEmpty(currentFilePath) ? "Untitled" : Path.GetFileName(currentFilePath);
            string dirtyMarker = isDirty ? "*" : "";
            Text = $"{fileName}{dirtyMarker} - IniEdit Editor";
        }

        private bool PromptSaveChanges()
        {
            if (!isDirty)
                return true;

            var result = MessageBox.Show(
                "Do you want to save changes?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SaveFile(null, EventArgs.Empty);
                return !isDirty; // Return false if save failed
            }
            else if (result == DialogResult.No)
            {
                return true;
            }
            else // Cancel
            {
                return false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!PromptSaveChanges())
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }
        #endregion

        #region Settings
        private void OpenSettings(object? sender, EventArgs e)
        {
            using (var dialog = new SettingsDialog(configOptions))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    configOptions = dialog.Options;
                    MessageBox.Show("Settings saved. New settings will be applied when loading files.",
                        "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        #endregion

        #region Find and Replace
        private void OpenFindReplace(object? sender, EventArgs e)
        {
            if (findReplaceDialog == null || findReplaceDialog.IsDisposed)
            {
                findReplaceDialog = new FindReplaceDialog();
                findReplaceDialog.FindNextClicked += FindNext;
                findReplaceDialog.ReplaceClicked += Replace;
                findReplaceDialog.ReplaceAllClicked += ReplaceAll;
            }

            findReplaceDialog.Show();
            findReplaceDialog.BringToFront();
        }

        private bool IsMatch(string text, string pattern, bool matchCase, bool useRegex)
        {
            if (useRegex)
            {
                try
                {
                    var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                    return Regex.IsMatch(text, pattern, options);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                return text.IndexOf(pattern, comparison) >= 0;
            }
        }

        private void FindNext(object? sender, EventArgs e)
        {
            if (findReplaceDialog == null || documentConfig == null)
                return;

            string findText = findReplaceDialog.FindText;
            bool matchCase = findReplaceDialog.MatchCase;
            bool useRegex = findReplaceDialog.UseRegex;

            // Start from current position
            int startSectionIndex = lastSearchSectionIndex >= 0 ? lastSearchSectionIndex : 0;
            int startPropertyIndex = lastSearchPropertyIndex + 1;

            // Search sections
            if (findReplaceDialog.SearchSections)
            {
                for (int i = startSectionIndex; i < sectionView.Items.Count; i++)
                {
                    string sectionName = sectionView.Items[i].ToString() ?? "";
                    if (IsMatch(sectionName, findText, matchCase, useRegex))
                    {
                        sectionView.SelectedIndex = i;
                        lastSearchSectionIndex = i;
                        lastSearchPropertyIndex = -1;
                        findReplaceDialog.SetStatus($"Found in section: {sectionName}");
                        return;
                    }
                }
            }

            // Search properties
            for (int i = startSectionIndex; i < sectionView.Items.Count; i++)
            {
                string sectionName = sectionView.Items[i].ToString() ?? "";
                var section = GetSection(sectionName);

                int propStart = (i == startSectionIndex) ? startPropertyIndex : 0;

                for (int j = propStart; j < section.PropertyCount; j++)
                {
                    var prop = section[j];
                    bool found = false;
                    string location = "";

                    if (findReplaceDialog.SearchKeys && IsMatch(prop.Name, findText, matchCase, useRegex))
                    {
                        found = true;
                        location = "key";
                    }
                    else if (findReplaceDialog.SearchValues && IsMatch(prop.Value, findText, matchCase, useRegex))
                    {
                        found = true;
                        location = "value";
                    }

                    if (found)
                    {
                        sectionView.SelectedIndex = i;
                        propertyView.Items[j].Selected = true;
                        propertyView.Items[j].EnsureVisible();
                        lastSearchSectionIndex = i;
                        lastSearchPropertyIndex = j;
                        findReplaceDialog.SetStatus($"Found in {location}: {prop.Name}");
                        return;
                    }
                }
            }

            // Not found, reset and notify
            lastSearchSectionIndex = -1;
            lastSearchPropertyIndex = -1;
            findReplaceDialog.SetStatus("No more matches found.", true);
        }

        private void Replace(object? sender, EventArgs e)
        {
            if (findReplaceDialog == null || documentConfig == null)
                return;

            if (propertyView.SelectedItems.Count == 0)
            {
                findReplaceDialog.SetStatus("Please select a property first.", true);
                return;
            }

            var selectedItem = propertyView.SelectedItems[0];
            string key = selectedItem.SubItems[0].Text;
            string value = selectedItem.SubItems[1].Text;
            var selectedSection = GetSelectedSection();

            string findText = findReplaceDialog.FindText;
            string replaceText = findReplaceDialog.ReplaceText;
            bool matchCase = findReplaceDialog.MatchCase;
            bool useRegex = findReplaceDialog.UseRegex;

            bool replaced = false;

            if (findReplaceDialog.SearchKeys && IsMatch(key, findText, matchCase, useRegex))
            {
                string newKey = ReplaceString(key, findText, replaceText, matchCase, useRegex);
                if (newKey != key && !selectedSection.HasProperty(newKey))
                {
                    UpdateKey(key, newKey);
                    selectedItem.SubItems[0].Text = newKey;
                    replaced = true;
                    SetDirty();
                }
            }

            if (findReplaceDialog.SearchValues && IsMatch(value, findText, matchCase, useRegex))
            {
                string newValue = ReplaceString(value, findText, replaceText, matchCase, useRegex);
                UpdateValue(key, newValue);
                selectedItem.SubItems[1].Text = newValue;
                replaced = true;
                SetDirty();
            }

            if (replaced)
            {
                findReplaceDialog.SetStatus("Replaced.");
                FindNext(sender, e); // Move to next match
            }
            else
            {
                findReplaceDialog.SetStatus("No match in selection.", true);
            }
        }

        private void ReplaceAll(object? sender, EventArgs e)
        {
            if (findReplaceDialog == null || documentConfig == null)
                return;

            string findText = findReplaceDialog.FindText;
            string replaceText = findReplaceDialog.ReplaceText;
            bool matchCase = findReplaceDialog.MatchCase;
            bool useRegex = findReplaceDialog.UseRegex;
            int replaceCount = 0;
            bool anyModified = false; // Track if any modification occurred

            // Replace in all sections and properties
            foreach (ListViewItem sectionItem in sectionView.Items)
            {
                string sectionName = sectionItem.ToString() ?? "";
                var section = GetSection(sectionName);

                for (int i = section.PropertyCount - 1; i >= 0; i--)
                {
                    var prop = section[i];
                    bool modified = false;

                    if (findReplaceDialog.SearchKeys)
                    {
                        string newKey = ReplaceString(prop.Name, findText, replaceText, matchCase, useRegex);
                        if (newKey != prop.Name && !section.HasProperty(newKey))
                        {
                            var newProp = new Property(newKey, prop.Value);
                            newProp.PreComments.AddRange(prop.PreComments);
                            newProp.Comment = prop.Comment;
                            newProp.IsQuoted = prop.IsQuoted;
                            section.RemoveProperty(i);
                            section.InsertProperty(i, newProp);
                            modified = true;
                            replaceCount++;
                        }
                    }

                    if (findReplaceDialog.SearchValues)
                    {
                        string newValue = ReplaceString(prop.Value, findText, replaceText, matchCase, useRegex);
                        if (newValue != prop.Value)
                        {
                            prop.Value = newValue;
                            modified = true;
                            replaceCount++;
                        }
                    }

                    if (modified)
                    {
                        anyModified = true; // Mark that we had modifications
                    }
                }
            }

            // Batch UI updates: only update once after all replacements
            if (anyModified)
            {
                SetDirty();
                if (sectionView.SelectedItem != null)
                {
                    RefreshKeyValueList(GetSelectedSectionName());
                }
            }

            findReplaceDialog.SetStatus($"Replaced {replaceCount} occurrence(s).");
            lastSearchSectionIndex = -1;
            lastSearchPropertyIndex = -1;
        }

        private string ReplaceString(string input, string find, string replace, bool matchCase, bool useRegex)
        {
            if (useRegex)
            {
                try
                {
                    var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                    return Regex.Replace(input, find, replace, options);
                }
                catch
                {
                    return input; // If regex fails, return original
                }
            }
            else
            {
                if (matchCase)
                {
                    return input.Replace(find, replace);
                }
                else
                {
                    // Case-insensitive replace
                    int index = input.IndexOf(find, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        return input.Substring(0, index) + replace + input.Substring(index + find.Length);
                    }
                    return input;
                }
            }
        }
        #endregion

        #region Undo/Redo

        private void Undo(object? sender, EventArgs e)
        {
            if (commandManager.CanUndo)
            {
                commandManager.Undo();
                SetDirty();
            }
        }

        private void Redo(object? sender, EventArgs e)
        {
            if (commandManager.CanRedo)
            {
                commandManager.Redo();
                SetDirty();
            }
        }

        private void UpdateUndoRedoMenuItems()
        {
            if (undoMenuItem != null)
            {
                undoMenuItem.Enabled = commandManager.CanUndo;
                undoMenuItem.Text = commandManager.CanUndo
                    ? $"&Undo {commandManager.UndoDescription}"
                    : "&Undo";
            }

            if (redoMenuItem != null)
            {
                redoMenuItem.Enabled = commandManager.CanRedo;
                redoMenuItem.Text = commandManager.CanRedo
                    ? $"&Redo {commandManager.RedoDescription}"
                    : "&Redo";
            }
        }

        #endregion

        #region Copy/Paste/Cut

        private void Copy(object? sender, EventArgs e)
        {
            // Check if a section is selected
            if (sectionView.SelectedIndex >= 0)
            {
                string sectionName = sectionView.SelectedItem?.ToString() ?? "";
                var section = GetSection(sectionName);
                ClipboardHelper.CopySection(section);
                return;
            }

            // Check if a property is selected
            if (propertyView.SelectedItems.Count > 0)
            {
                var selectedItem = propertyView.SelectedItems[0];
                string key = selectedItem.Text;

                if (sectionView.SelectedIndex >= 0)
                {
                    string sectionName = sectionView.SelectedItem?.ToString() ?? "";
                    var section = GetSection(sectionName);
                    var property = section.GetProperty(key);

                    if (property != null)
                    {
                        ClipboardHelper.CopyProperty(property);
                    }
                }
            }
        }

        private void Cut(object? sender, EventArgs e)
        {
            // Copy first
            Copy(sender, e);

            // Then delete
            if (propertyView.SelectedItems.Count > 0)
            {
                DeleteKeyValue(sender, e);
            }
            else if (sectionView.SelectedIndex > 0) // Don't allow cutting global section
            {
                DeleteSection(sender, e);
            }
        }

        private void Paste(object? sender, EventArgs e)
        {
            if (documentConfig == null)
                return;

            // Try to paste section
            if (ClipboardHelper.HasSection())
            {
                var section = ClipboardHelper.GetSection();
                if (section != null)
                {
                    // Generate unique name if needed
                    string newName = section.Name;
                    int counter = 1;
                    while (documentConfig.HasSection(newName))
                    {
                        newName = $"{section.Name}_{counter++}";
                    }

                    // Create new section with unique name
                    var newSection = new Section(newName);
                    newSection.AddPropertyRange(section.GetProperties());
                    newSection.PreComments.AddRange(section.PreComments);
                    newSection.Comment = section.Comment;

                    int index = sectionView.SelectedIndex >= 0 ? sectionView.SelectedIndex : documentConfig.SectionCount;
                    var command = new AddSectionCommand(documentConfig, newSection, index, () =>
                    {
                        RefreshSectionList();
                        RefreshStatusBar();
                    });
                    commandManager.ExecuteCommand(command);
                    SetDirty();
                }
                return;
            }

            // Try to paste property
            if (ClipboardHelper.HasProperty())
            {
                var property = ClipboardHelper.GetProperty();
                if (property != null && sectionView.SelectedIndex >= 0)
                {
                    string sectionName = sectionView.SelectedItem?.ToString() ?? "";
                    var section = GetSection(sectionName);

                    // Generate unique key if needed
                    string newKey = property.Name;
                    int counter = 1;
                    while (section.HasProperty(newKey))
                    {
                        newKey = $"{property.Name}_{counter++}";
                    }

                    var newProperty = new Property(newKey, property.Value)
                    {
                        Comment = property.Comment,
                        IsQuoted = property.IsQuoted
                    };
                    foreach (var comment in property.PreComments)
                    {
                        newProperty.PreComments.Add(comment);
                    }

                    int index = propertyView.SelectedItems.Count > 0
                        ? propertyView.SelectedIndices[0]
                        : section.PropertyCount;

                    var command = new AddPropertyCommand(section, newProperty, index, () =>
                    {
                        RefreshKeyValueList(sectionName);
                        RefreshStatusBar();
                    });
                    commandManager.ExecuteCommand(command);
                    SetDirty();
                }
            }
        }

        #endregion

        #region Recent Files

        private void UpdateRecentFilesMenu()
        {
            if (recentFilesMenuItem == null)
                return;

            recentFilesMenuItem.DropDownItems.Clear();

            var recentFiles = recentFilesManager.RecentFiles;

            if (recentFiles.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(No recent files)");
                emptyItem.Enabled = false;
                recentFilesMenuItem.DropDownItems.Add(emptyItem);
                return;
            }

            for (int i = 0; i < recentFiles.Count; i++)
            {
                string filePath = recentFiles[i];
                var menuItem = new ToolStripMenuItem($"&{i + 1}  {filePath}");
                menuItem.Tag = filePath;
                menuItem.Click += (s, e) =>
                {
                    if (s is ToolStripMenuItem item && item.Tag is string path)
                    {
                        if (File.Exists(path))
                        {
                            if (PromptSaveChanges())
                            {
                                LoadFile(path);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"File not found: {path}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            recentFilesManager.RemoveRecentFile(path);
                        }
                    }
                };
                recentFilesMenuItem.DropDownItems.Add(menuItem);
            }

            recentFilesMenuItem.DropDownItems.Add(new ToolStripSeparator());

            var clearItem = new ToolStripMenuItem("&Clear Recent Files");
            clearItem.Click += (s, e) =>
            {
                recentFilesManager.ClearRecentFiles();
            };
            recentFilesMenuItem.DropDownItems.Add(clearItem);
        }

        #endregion

        #region Context Menus

        private void SetupSectionContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            // Add Section
            var addSectionItem = new ToolStripMenuItem("Add Section...");
            addSectionItem.Click += AddSection;
            contextMenu.Items.Add(addSectionItem);

            // Edit Section
            var editSectionItem = new ToolStripMenuItem("Edit Section...");
            editSectionItem.Click += EditSection;
            contextMenu.Items.Add(editSectionItem);

            // Duplicate Section
            var duplicateSectionItem = new ToolStripMenuItem("Duplicate Section");
            duplicateSectionItem.Click += DuplicateSection;
            contextMenu.Items.Add(duplicateSectionItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Move Up
            var moveUpItem = new ToolStripMenuItem("Move Section Up");
            moveUpItem.Click += MoveSectionUp;
            contextMenu.Items.Add(moveUpItem);

            // Move Down
            var moveDownItem = new ToolStripMenuItem("Move Section Down");
            moveDownItem.Click += MoveSectionDown;
            contextMenu.Items.Add(moveDownItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Sort Sections
            var sortItem = new ToolStripMenuItem("Sort Sections");
            sortItem.Click += SortSections;
            contextMenu.Items.Add(sortItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Section Statistics
            var sectionStatsItem = new ToolStripMenuItem("Section Statistics...");
            sectionStatsItem.Click += ShowSectionStatistics;
            contextMenu.Items.Add(sectionStatsItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Copy
            var copyItem = new ToolStripMenuItem("Copy");
            copyItem.ShortcutKeyDisplayString = "Ctrl+C";
            copyItem.Click += Copy;
            contextMenu.Items.Add(copyItem);

            // Paste
            var pasteItem = new ToolStripMenuItem("Paste");
            pasteItem.ShortcutKeyDisplayString = "Ctrl+V";
            pasteItem.Click += Paste;
            contextMenu.Items.Add(pasteItem);

            // Cut
            var cutItem = new ToolStripMenuItem("Cut");
            cutItem.ShortcutKeyDisplayString = "Ctrl+X";
            cutItem.Click += Cut;
            contextMenu.Items.Add(cutItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Delete Section
            var deleteSectionItem = new ToolStripMenuItem("Delete Section");
            deleteSectionItem.ShortcutKeyDisplayString = "Del";
            deleteSectionItem.Click += DeleteSection;
            contextMenu.Items.Add(deleteSectionItem);

            // Set context menu opening event to enable/disable items
            contextMenu.Opening += (s, e) =>
            {
                bool hasSelection = sectionView.SelectedIndex >= 0;
                bool isGlobalSection = hasSelection && sectionView.SelectedIndex == 0;

                editSectionItem.Enabled = hasSelection && !isGlobalSection;
                duplicateSectionItem.Enabled = hasSelection;
                moveUpItem.Enabled = hasSelection && sectionView.SelectedIndex > 1; // Can't move global or first section up
                moveDownItem.Enabled = hasSelection && sectionView.SelectedIndex < sectionView.Items.Count - 1;
                sortItem.Enabled = sectionView.Items.Count > 1;
                sectionStatsItem.Enabled = hasSelection;
                copyItem.Enabled = hasSelection;
                pasteItem.Enabled = ClipboardHelper.HasSection();
                cutItem.Enabled = hasSelection && !isGlobalSection;
                deleteSectionItem.Enabled = hasSelection && !isGlobalSection;
            };

            sectionView.ContextMenuStrip = contextMenu;
        }

        private void SetupPropertyContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            // Add Key-Value
            var addKeyValueItem = new ToolStripMenuItem("Add Key-Value...");
            addKeyValueItem.Click += AddKeyValue;
            contextMenu.Items.Add(addKeyValueItem);

            // Edit Key-Value
            var editKeyValueItem = new ToolStripMenuItem("Edit Key-Value...");
            editKeyValueItem.Click += EditKeyValue;
            contextMenu.Items.Add(editKeyValueItem);

            // Duplicate Key
            var duplicateKeyItem = new ToolStripMenuItem("Duplicate Key");
            duplicateKeyItem.Click += DuplicateKey;
            contextMenu.Items.Add(duplicateKeyItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Move Up
            var moveUpItem = new ToolStripMenuItem("Move Key Up");
            moveUpItem.Click += MoveKeyUp;
            contextMenu.Items.Add(moveUpItem);

            // Move Down
            var moveDownItem = new ToolStripMenuItem("Move Key Down");
            moveDownItem.Click += MoveKeyDown;
            contextMenu.Items.Add(moveDownItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Sort Keys
            var sortItem = new ToolStripMenuItem("Sort Keys");
            sortItem.Click += SortKeys;
            contextMenu.Items.Add(sortItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Copy
            var copyItem = new ToolStripMenuItem("Copy");
            copyItem.ShortcutKeyDisplayString = "Ctrl+C";
            copyItem.Click += Copy;
            contextMenu.Items.Add(copyItem);

            // Paste
            var pasteItem = new ToolStripMenuItem("Paste");
            pasteItem.ShortcutKeyDisplayString = "Ctrl+V";
            pasteItem.Click += Paste;
            contextMenu.Items.Add(pasteItem);

            // Cut
            var cutItem = new ToolStripMenuItem("Cut");
            cutItem.ShortcutKeyDisplayString = "Ctrl+X";
            cutItem.Click += Cut;
            contextMenu.Items.Add(cutItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // Delete Key-Value
            var deleteKeyValueItem = new ToolStripMenuItem("Delete Key-Value");
            deleteKeyValueItem.ShortcutKeyDisplayString = "Del";
            deleteKeyValueItem.Click += DeleteKeyValue;
            contextMenu.Items.Add(deleteKeyValueItem);

            // Set context menu opening event to enable/disable items
            contextMenu.Opening += (s, e) =>
            {
                bool hasSectionSelected = sectionView.SelectedIndex >= 0;
                bool hasPropertySelected = propertyView.SelectedItems.Count > 0;
                int propertyCount = propertyView.Items.Count;

                addKeyValueItem.Enabled = hasSectionSelected;
                editKeyValueItem.Enabled = hasPropertySelected;
                duplicateKeyItem.Enabled = hasPropertySelected;
                moveUpItem.Enabled = hasPropertySelected && propertyView.SelectedIndices[0] > 0;
                moveDownItem.Enabled = hasPropertySelected && propertyView.SelectedIndices[0] < propertyCount - 1;
                sortItem.Enabled = propertyCount > 1;
                copyItem.Enabled = hasPropertySelected;
                pasteItem.Enabled = hasSectionSelected && ClipboardHelper.HasProperty();
                cutItem.Enabled = hasPropertySelected;
                deleteKeyValueItem.Enabled = hasPropertySelected;
            };

            propertyView.ContextMenuStrip = contextMenu;
        }

        #endregion

        #region Validation, Statistics, and Encoding

        private void ShowValidationDialog(object? sender, EventArgs e)
        {
            if (documentConfig == null)
            {
                MessageBox.Show("No document loaded.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var errors = ValidationHelper.ValidateDocument(documentConfig);
            using (var dialog = new ValidationDialog(errors))
            {
                dialog.ShowDialog(this);
            }
        }

        private void ShowStatisticsDialog(object? sender, EventArgs e)
        {
            if (documentConfig == null)
            {
                MessageBox.Show("No document loaded.", "Statistics",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var stats = ValidationHelper.GetStatistics(documentConfig);
            using (var dialog = new StatisticsDialog(stats))
            {
                dialog.ShowDialog(this);
            }
        }

        private void ShowSectionStatistics(object? sender, EventArgs e)
        {
            if (documentConfig == null || sectionView.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a section first.", "Section Statistics",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string sectionName = GetSelectedSectionName();
            var section = GetSelectedSection();

            using (var dialog = new SectionStatisticsDialog(section, sectionName))
            {
                dialog.ShowDialog(this);
            }
        }

        private void ShowEncodingMenu(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                MessageBox.Show("No file is currently open.\n\nPlease open or save a file first.",
                    "Change Encoding", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Create encoding selection dialog
            var encodingDialog = new Form
            {
                Text = "Change File Encoding",
                Size = new Size(450, 250),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "Select encoding:",
                Location = new Point(10, 10),
                Size = new Size(400, 20)
            };

            var currentLabel = new Label
            {
                Text = $"Current encoding: {EncodingHelper.GetEncodingName(currentEncoding)}",
                Location = new Point(10, 35),
                Size = new Size(400, 20),
                ForeColor = Color.Blue
            };

            var encodingComboBox = new ComboBox
            {
                Location = new Point(10, 65),
                Size = new Size(410, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Add common encodings
            var commonEncodings = new[]
            {
                Encoding.UTF8,
                Encoding.Unicode,
                Encoding.BigEndianUnicode,
                Encoding.UTF32,
                Encoding.ASCII,
                Encoding.Default
            };

            foreach (var enc in commonEncodings)
            {
                encodingComboBox.Items.Add(EncodingHelper.GetEncodingName(enc));
            }

            encodingComboBox.SelectedIndex = 0; // Default to UTF-8

            var warningLabel = new Label
            {
                Text = "⚠ Warning: Changing encoding will reload the file and may lose unsaved changes.",
                Location = new Point(10, 100),
                Size = new Size(410, 40),
                ForeColor = Color.Red,
                Font = new Font(Font.FontFamily, 8)
            };

            var okButton = new Button
            {
                Text = "Change",
                DialogResult = DialogResult.OK,
                Location = new Point(230, 150),
                Size = new Size(90, 30)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(330, 150),
                Size = new Size(90, 30)
            };

            encodingDialog.Controls.AddRange(new Control[]
            {
                label,
                currentLabel,
                encodingComboBox,
                warningLabel,
                okButton,
                cancelButton
            });

            encodingDialog.AcceptButton = okButton;
            encodingDialog.CancelButton = cancelButton;

            if (encodingDialog.ShowDialog(this) == DialogResult.OK)
            {
                if (!PromptSaveChanges())
                    return;

                // Get selected encoding
                Encoding newEncoding = encodingComboBox.SelectedIndex switch
                {
                    0 => Encoding.UTF8,
                    1 => Encoding.Unicode,
                    2 => Encoding.BigEndianUnicode,
                    3 => Encoding.UTF32,
                    4 => Encoding.ASCII,
                    5 => Encoding.Default,
                    _ => Encoding.UTF8
                };

                try
                {
                    // Convert file encoding
                    EncodingHelper.ConvertFileEncoding(currentFilePath, currentEncoding, newEncoding);
                    currentEncoding = newEncoding;

                    // Reload file
                    LoadFile(currentFilePath);

                    MessageBox.Show($"File encoding changed to {EncodingHelper.GetEncodingName(newEncoding)}",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error changing encoding: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion
    }
}
