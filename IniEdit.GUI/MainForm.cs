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
using IniEdit.GUI.Forms;
using IniEdit.GUI.Theme;

namespace IniEdit.GUI
{
    public partial class MainForm : Form
    {
        #region Fields
        private string _currentFilePath = string.Empty;
        private Document? _documentConfig;
        private IniConfigOption _configOptions;
        private bool _isDirty = false;
        private Encoding _currentEncoding = Encoding.UTF8;

        // Inline cell editing
        private TextBox _inlineCellEditBox = new();
        private ListViewItem.ListViewSubItem? _currentEditingCell;

        // Comment editing
        private bool _isUpdatingCommentsFromCode = false;

        // Find/Replace
        private FindReplaceDialog? _findReplaceDialog = null;
        private int _lastSearchSectionIndex = -1;
        private int _lastSearchPropertyIndex = -1;

        // Undo/Redo
        private readonly CommandManager _commandManager = new();
        private ToolStripMenuItem? _undoMenuItem;
        private ToolStripMenuItem? _redoMenuItem;
        private ToolStripMenuItem? _darkModeMenuItem;

        // Recent Files
        private readonly RecentFilesManager _recentFilesManager = new();
        private ToolStripMenuItem? _recentFilesMenuItem;

        // Copy/Paste
        private ToolStripMenuItem? _copyMenuItem;
        private ToolStripMenuItem? _cutMenuItem;
        private ToolStripMenuItem? _pasteMenuItem;

        // Validation and Statistics
        private ToolStripStatusLabel? _encodingStatusLabel;
        private ToolStripStatusLabel? _validationStatusLabel;

        // Performance optimization caches
        private Font? _duplicateKeyFont;
        private DocumentStatistics? _cachedStatistics;
        private bool _statisticsDirty = true;

        // Section filter
        private TextBox? _sectionFilterBox;
        private List<string> _allSectionNames = new();

        // Property filter
        private TextBox? _propertyFilterBox;

        // Auto-backup
        private System.Windows.Forms.Timer? _autoBackupTimer;
        private DateTime _lastAutoBackupTime = DateTime.MinValue;
        private const int AutoBackupCheckIntervalMs = 60000; // 1 minute
        private const int MaxBackupFilesToKeep = 10;

        // TreeView mode
        private TreeView? _sectionTreeView;
        private ToolStripMenuItem? _treeViewModeMenuItem;
        private ToolStripMenuItem? _autoBackupMenuItem;
        private bool _windowStateRestored = false;
        #endregion

        public MainForm()
        {
            InitializeComponent();
            _configOptions = new IniConfigOption { CollectParsingErrors = true };
            SetupForm();
            SetupInlineCellEditor();
            SetupMenuItems();
            SetupCommandManager();
            SetupRecentFiles();
            SetupAccessibility();
            UpdateTitle();

            // Enable key preview for keyboard shortcuts
            this.KeyPreview = true;
            this.KeyDown += OnFormKeyDown;

            // Setup theme
            SetupTheme();

            // Restore window state from settings
            RestoreWindowState();

            // Setup auto-backup
            SetupAutoBackup();

            // Setup TreeView mode if enabled
            if (Properties.Settings.Default.TreeViewMode)
            {
                SetupTreeViewMode();
            }
        }

        /// <summary>
        /// Sets up accessibility properties for screen readers and assistive technologies.
        /// </summary>
        private void SetupAccessibility()
        {
            // Main form
            this.AccessibleName = "INI Editor Main Window";
            this.AccessibleDescription = "Main window for editing INI configuration files";

            // Section view
            sectionView.AccessibleName = "Section List";
            sectionView.AccessibleDescription = "List of sections in the INI file. Select a section to view its properties.";
            sectionView.TabIndex = 0;

            // Property view
            propertyView.AccessibleName = "Property List";
            propertyView.AccessibleDescription = "List of key-value properties in the selected section. Double-click to edit.";
            propertyView.TabIndex = 1;

            // Comment text boxes (logical order: inline first, then pre-comments)
            inlineCommentTextBox.AccessibleName = "Inline Comment";
            inlineCommentTextBox.AccessibleDescription = "Single line comment that appears after the selected item on the same line.";
            inlineCommentTextBox.TabIndex = 2;

            preCommentsTextBox.AccessibleName = "Pre-Comments";
            preCommentsTextBox.AccessibleDescription = "Multi-line comments that appear before the selected item.";
            preCommentsTextBox.TabIndex = 3;

            // Status bar labels
            sectionStatusLabel.AccessibleName = "Section Count";
            keyStatusLabel.AccessibleName = "Key Count";
            filePathStatusLabel.AccessibleName = "Current File Path";
        }

        private void SetupForm()
        {
            newToolStripMenuItem.Click += NewFile;
            openToolStripMenuItem.Click += OpenFile;
            saveToolStripMenuItem.Click += SaveFile;
            saveAsToolStripMenuItem.Click += SaveAsFile;
            sectionView.SelectedIndexChanged += OnSectionSelectionChanged;
            sectionView.DoubleClick += OnSectionDoubleClick;
            propertyView.MouseDoubleClick += OnPropertyDoubleClick;
            propertyView.Click += OnPropertyClick;
            preCommentsTextBox.TextChanged += OnPreCommentsChanged;
            inlineCommentTextBox.TextChanged += OnInlineCommentChanged;

            // Setup status bar labels
            SetupStatusBar();

            // Setup context menus
            SetupSectionContextMenu();
            SetupPropertyContextMenu();

            // Setup drag & drop for reordering
            SetupDragDrop();

            // Setup file drag & drop from Explorer
            SetupFileDragDrop();

            // Setup section filter
            SetupSectionFilter();

            // Setup property filter
            SetupPropertyFilter();

            // Handle resize to adjust columns
            propertyView.Resize += (s, e) => AutoResizeColumns();

            RefreshStatusBar();
        }

        private void SetupSectionFilter()
        {
            _sectionFilterBox = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Filter sections..."
            };

            _sectionFilterBox.TextChanged += OnSectionFilterChanged;
            _sectionFilterBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    _sectionFilterBox.Text = "";
                    sectionView.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down && sectionView.Items.Count > 0)
                {
                    sectionView.Focus();
                    if (sectionView.SelectedIndex < 0)
                        sectionView.SelectedIndex = 0;
                    e.Handled = true;
                }
            };

            // Insert after label but before sectionView
            splitContainer1.Panel1.Controls.Add(_sectionFilterBox);
            _sectionFilterBox.BringToFront();
            sectionView.BringToFront();
        }

        private void OnSectionFilterChanged(object? sender, EventArgs e)
        {
            if (_documentConfig == null || _sectionFilterBox == null)
                return;

            string filter = _sectionFilterBox.Text.Trim();

            if (string.IsNullOrEmpty(filter))
            {
                // Restore all sections
                RefreshSectionList();
                return;
            }

            // Filter sections
            sectionView.BeginUpdate();
            sectionView.Items.Clear();

            foreach (var name in _allSectionNames)
            {
                if (name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    sectionView.Items.Add(name);
                }
            }

            sectionView.EndUpdate();

            if (sectionView.Items.Count > 0)
                sectionView.SelectedIndex = 0;
        }

        private void SetupPropertyFilter()
        {
            _propertyFilterBox = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9F),
                PlaceholderText = "Filter properties..."
            };

            _propertyFilterBox.TextChanged += OnPropertyFilterChanged;
            _propertyFilterBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    _propertyFilterBox.Text = "";
                    propertyView.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down && propertyView.Items.Count > 0)
                {
                    propertyView.Focus();
                    if (propertyView.SelectedItems.Count == 0)
                        propertyView.Items[0].Selected = true;
                    e.Handled = true;
                }
            };

            // Insert after label but before propertyView
            splitContainer2.Panel1.Controls.Add(_propertyFilterBox);
            _propertyFilterBox.BringToFront();
            propertyView.BringToFront();
        }

        private void OnPropertyFilterChanged(object? sender, EventArgs e)
        {
            if (_documentConfig == null || _propertyFilterBox == null)
                return;

            string filter = _propertyFilterBox.Text.Trim();
            var section = GetSelectedSection();

            if (string.IsNullOrEmpty(filter))
            {
                // Restore all properties
                RefreshKeyValueList(section?.Name ?? "");
                return;
            }

            // Filter properties
            propertyView.BeginUpdate();
            propertyView.Items.Clear();

            foreach (var property in section)
            {
                if (property.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    property.Value.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    var item = propertyView.Items.Add(property.Name);
                    item.SubItems.Add(property.Value);
                }
            }

            propertyView.EndUpdate();
            AutoResizeColumns();
        }

        private void SetupStatusBar()
        {
            // Add encoding status label
            _encodingStatusLabel = new ToolStripStatusLabel
            {
                Text = "Encoding: UTF-8",
                BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched
            };
            _encodingStatusLabel.Click += ShowEncodingMenu;
            statusStrip1.Items.Add(_encodingStatusLabel);

            // Add validation status label
            // Using DarkGreen for better contrast (WCAG AA compliance)
            _validationStatusLabel = new ToolStripStatusLabel
            {
                Text = "✓ No errors",
                ForeColor = Color.FromArgb(0, 128, 0), // Dark green for better contrast
                BorderSides = ToolStripStatusLabelBorderSides.Right,
                BorderStyle = Border3DStyle.Etched,
                IsLink = false
            };
            _validationStatusLabel.Click += ShowValidationDialog;
            statusStrip1.Items.Add(_validationStatusLabel);
        }

        private void SetupCommandManager()
        {
            _commandManager.StateChanged += OnCommandManagerStateChanged;
            UpdateUndoRedoMenuItems();
        }

        private void OnCommandManagerStateChanged(object? sender, EventArgs e)
        {
            UpdateUndoRedoMenuItems();
            // Sync dirty flag with command manager save point
            SyncDirtyWithCommandManager();
        }

        /// <summary>
        /// Synchronizes the dirty flag with the command manager's save point tracking.
        /// </summary>
        private void SyncDirtyWithCommandManager()
        {
            bool shouldBeDirty = _commandManager.IsDirtyFromSavePoint;
            if (_isDirty != shouldBeDirty)
            {
                _isDirty = shouldBeDirty;
                _statisticsDirty = true;
                UpdateTitle();
            }
        }

        private void SetupRecentFiles()
        {
            _recentFilesManager.RecentFilesChanged += OnRecentFilesChanged;
            UpdateRecentFilesMenu();
        }

        private void OnRecentFilesChanged(object? sender, EventArgs e)
        {
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
            // Delete: Delete key
            else if (e.KeyCode == Keys.Delete && !e.Control && !e.Alt)
            {
                // Only handle if not in a text editing mode
                if (ActiveControl != preCommentsTextBox && ActiveControl != inlineCommentTextBox)
                {
                    if (propertyView.Focused && propertyView.SelectedItems.Count > 0)
                    {
                        DeleteKeyValue(sender, e);
                        e.Handled = true;
                    }
                    else if (sectionView.Focused && sectionView.SelectedIndex > 0)
                    {
                        DeleteSection(sender, e);
                        e.Handled = true;
                    }
                }
            }
            // F2: Edit selected item
            else if (e.KeyCode == Keys.F2 && !e.Control && !e.Alt && !e.Shift)
            {
                if (propertyView.Focused && propertyView.SelectedItems.Count == 1)
                {
                    EditKeyValue(sender, e);
                    e.Handled = true;
                }
                else if (sectionView.Focused && sectionView.SelectedIndex > 0)
                {
                    EditSection(sender, e);
                    e.Handled = true;
                }
            }
            // Select All: Ctrl+A
            else if (e.Control && e.KeyCode == Keys.A && !e.Shift && !e.Alt)
            {
                if (propertyView.Focused)
                {
                    foreach (ListViewItem item in propertyView.Items)
                    {
                        item.Selected = true;
                    }
                    e.Handled = true;
                }
            }
            // Go to Section: Ctrl+G
            else if (e.Control && e.KeyCode == Keys.G && !e.Shift && !e.Alt)
            {
                GoToSection();
                e.Handled = true;
            }
        }

        private void SetupMenuItems()
        {
            // Add Export submenu to File menu (after Save As)
            var saveAsIndex = fileToolStripMenuItem.DropDownItems.IndexOf(saveAsToolStripMenuItem);
            if (saveAsIndex >= 0)
            {
                var exportMenu = new ToolStripMenuItem("&Export");
                var exportJsonMenu = new ToolStripMenuItem("Export as &JSON...");
                var exportXmlMenu = new ToolStripMenuItem("Export as &XML...");
                var exportCsvMenu = new ToolStripMenuItem("Export as &CSV...");

                exportJsonMenu.Click += ExportAsJson;
                exportXmlMenu.Click += ExportAsXml;
                exportCsvMenu.Click += ExportAsCsv;

                exportMenu.DropDownItems.AddRange(new ToolStripItem[]
                {
                    exportJsonMenu,
                    exportXmlMenu,
                    exportCsvMenu
                });

                var reloadMenu = new ToolStripMenuItem("&Reload from Disk");
                reloadMenu.ShortcutKeys = Keys.F5;
                reloadMenu.Click += ReloadFromDisk;

                fileToolStripMenuItem.DropDownItems.Insert(saveAsIndex + 1, new ToolStripSeparator());
                fileToolStripMenuItem.DropDownItems.Insert(saveAsIndex + 2, exportMenu);
                fileToolStripMenuItem.DropDownItems.Insert(saveAsIndex + 3, reloadMenu);
                fileToolStripMenuItem.DropDownItems.Insert(saveAsIndex + 4, new ToolStripSeparator());
                _recentFilesMenuItem = new ToolStripMenuItem("Recent &Files");
                fileToolStripMenuItem.DropDownItems.Insert(saveAsIndex + 5, _recentFilesMenuItem);
            }
            else
            {
                _recentFilesMenuItem = new ToolStripMenuItem("Recent &Files");
            }

            // Add Edit menu
            var editMenu = new ToolStripMenuItem("&Edit");

            // Undo/Redo
            _undoMenuItem = new ToolStripMenuItem("&Undo");
            _undoMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
            _undoMenuItem.Click += Undo;

            _redoMenuItem = new ToolStripMenuItem("&Redo");
            _redoMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            _redoMenuItem.Click += Redo;

            // Copy/Cut/Paste
            _copyMenuItem = new ToolStripMenuItem("&Copy");
            _copyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            _copyMenuItem.Click += Copy;

            _cutMenuItem = new ToolStripMenuItem("Cu&t");
            _cutMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            _cutMenuItem.Click += Cut;

            _pasteMenuItem = new ToolStripMenuItem("&Paste");
            _pasteMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            _pasteMenuItem.Click += Paste;

            // Section menu
            var sectionMenu = new ToolStripMenuItem("Section");
            var addSectionMenu = new ToolStripMenuItem("Add Section");
            var editSectionMenu = new ToolStripMenuItem("Edit Section");
            var deleteSectionMenu = new ToolStripMenuItem("Delete Section");
            var moveSectionUpMenu = new ToolStripMenuItem("Move Section Up");
            var moveSectionDownMenu = new ToolStripMenuItem("Move Section Down");
            var duplicateSectionMenu = new ToolStripMenuItem("Duplicate Section");
            var sortSectionsMenu = new ToolStripMenuItem("Sort Sections");

            // Show shortcut hints (handled by OnFormKeyDown)
            editSectionMenu.ShortcutKeyDisplayString = "F2";
            deleteSectionMenu.ShortcutKeyDisplayString = "Del";

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

            // Show shortcut hints (handled by OnFormKeyDown)
            editKeyValueMenu.ShortcutKeyDisplayString = "F2";
            deleteKeyValueMenu.ShortcutKeyDisplayString = "Del";

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

            // Go to Section menu
            var goToSectionMenu = new ToolStripMenuItem("&Go to Section...");
            goToSectionMenu.ShortcutKeys = Keys.Control | Keys.G;
            goToSectionMenu.Click += (s, e) => GoToSection();

            editMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                _undoMenuItem,
                _redoMenuItem,
                new ToolStripSeparator(),
                _copyMenuItem,
                _cutMenuItem,
                _pasteMenuItem,
                new ToolStripSeparator(),
                findReplaceMenu,
                goToSectionMenu,
                new ToolStripSeparator(),
                sectionMenu,
                keyValueMenu
            });
            menuStrip1.Items.Add(editMenu);

            // Add View menu
            var viewMenu = new ToolStripMenuItem("&View");

            _darkModeMenuItem = new ToolStripMenuItem("&Dark Mode");
            _darkModeMenuItem.CheckOnClick = true;
            _darkModeMenuItem.Checked = ThemeManager.IsDarkMode;
            _darkModeMenuItem.Click += ToggleDarkMode;

            _treeViewModeMenuItem = new ToolStripMenuItem("&Tree View Mode");
            _treeViewModeMenuItem.CheckOnClick = true;
            _treeViewModeMenuItem.Checked = Properties.Settings.Default.TreeViewMode;
            _treeViewModeMenuItem.Click += ToggleTreeViewMode;

            _autoBackupMenuItem = new ToolStripMenuItem("&Auto-Backup");
            _autoBackupMenuItem.CheckOnClick = true;
            _autoBackupMenuItem.Checked = Properties.Settings.Default.AutoBackupEnabled;
            _autoBackupMenuItem.Click += ToggleAutoBackup;

            viewMenu.DropDownItems.Add(_darkModeMenuItem);
            viewMenu.DropDownItems.Add(_treeViewModeMenuItem);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add(_autoBackupMenuItem);
            menuStrip1.Items.Add(viewMenu);

            // Add Tools menu
            var toolsMenu = new ToolStripMenuItem("&Tools");

            var validateMenu = new ToolStripMenuItem("&Validate Document");
            validateMenu.ShortcutKeys = Keys.F8;
            validateMenu.Click += ShowValidationDialog;

            var statisticsMenu = new ToolStripMenuItem("Show &Statistics");
            statisticsMenu.ShortcutKeys = Keys.F9;
            statisticsMenu.Click += ShowStatisticsDialog;

            var compareMenu = new ToolStripMenuItem("&Compare Documents...");
            compareMenu.ShortcutKeys = Keys.F7;
            compareMenu.Click += ShowCompareDialog;

            var encodingMenu = new ToolStripMenuItem("Change &Encoding...");
            encodingMenu.Click += ShowEncodingMenu;

            var settingsMenu = new ToolStripMenuItem("&Settings");
            settingsMenu.Click += OpenSettings;

            toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                validateMenu,
                statisticsMenu,
                new ToolStripSeparator(),
                compareMenu,
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
            if (_documentConfig == null)
                return;

            // Build the complete list of section names for filtering
            _allSectionNames.Clear();
            _allSectionNames.Add(GetGlobalSectionName());
            foreach (var section in _documentConfig)
            {
                _allSectionNames.Add(section.Name);
            }

            // Clear filter when refreshing
            if (_sectionFilterBox != null && !string.IsNullOrEmpty(_sectionFilterBox.Text))
            {
                _sectionFilterBox.Text = "";
            }

            sectionView.Items.Clear();
            foreach (var name in _allSectionNames)
            {
                sectionView.Items.Add(name);
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

            // Auto-resize columns to fit content
            AutoResizeColumns();
        }

        private void AutoResizeColumns()
        {
            if (propertyView.Items.Count == 0)
            {
                // Use fixed widths for empty list
                keyHeader.Width = 200;
                valueHeader.Width = Math.Max(350, propertyView.ClientSize.Width - 210);
                return;
            }

            // Auto-resize based on content
            keyHeader.Width = -2; // Auto-size to header and content
            valueHeader.Width = -2;

            // Ensure minimum widths
            if (keyHeader.Width < 100) keyHeader.Width = 100;
            if (valueHeader.Width < 150) valueHeader.Width = 150;

            // If total width is less than available space, expand value column
            int availableWidth = propertyView.ClientSize.Width - keyHeader.Width - 25; // 25 for scrollbar
            if (valueHeader.Width < availableWidth)
                valueHeader.Width = availableWidth;
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

            string filePath = string.IsNullOrEmpty(_currentFilePath) ? "-" : _currentFilePath;
            filePathStatusLabel.Text = $"File: {filePath}";

            if (_encodingStatusLabel != null)
            {
                _encodingStatusLabel.Text = $"Encoding: {EncodingHelper.GetEncodingName(_currentEncoding)}";
            }

            if (_validationStatusLabel != null && _documentConfig != null)
            {
                if (_statisticsDirty)
                {
                    _cachedStatistics = ValidationHelper.GetStatistics(_documentConfig);
                    _statisticsDirty = false;
                }

                if (_cachedStatistics!.ValidationErrors > 0)
                {
                    _validationStatusLabel.Text = $"⚠ {_cachedStatistics.ValidationErrors} validation error(s)";
                    _validationStatusLabel.ForeColor = Color.FromArgb(180, 0, 0); // Dark red for better contrast
                    _validationStatusLabel.IsLink = true;
                }
                else
                {
                    _validationStatusLabel.Text = "✓ No errors";
                    _validationStatusLabel.ForeColor = Color.FromArgb(0, 128, 0); // Dark green for better contrast
                    _validationStatusLabel.IsLink = false;
                }
            }
        }

        private void SetupInlineCellEditor()
        {
            _inlineCellEditBox.Visible = false;
            _inlineCellEditBox.BorderStyle = BorderStyle.FixedSingle;
            _inlineCellEditBox.KeyPress += OnInlineCellEditorKeyPress;
            _inlineCellEditBox.LostFocus += OnInlineCellEditorLostFocus;
            propertyView.Controls.Add(_inlineCellEditBox);
        }

        private void BeginInlineCellEdit(Point clientPoint)
        {
            ListViewHitTestInfo hitTest = propertyView.HitTest(clientPoint);
            if (hitTest.SubItem != null)
            {
                Rectangle subItemRect = hitTest.SubItem.Bounds;
                _currentEditingCell = hitTest.SubItem;

                _inlineCellEditBox.Location = new Point(subItemRect.Left, subItemRect.Top);
                _inlineCellEditBox.Size = new Size(subItemRect.Width, subItemRect.Height);
                _inlineCellEditBox.Text = _currentEditingCell.Text;
                _inlineCellEditBox.Visible = true;
                _inlineCellEditBox.Focus();
                _inlineCellEditBox.SelectAll();
            }
        }

        private bool IsKeyDuplicateInSection(string sectionName, string newKey, string oldKey)
        {
            var section = GetSection(sectionName);
            return section.HasProperty(newKey) && newKey != oldKey;
        }

        private void CommitInlineCellEdit()
        {
            if (_currentEditingCell != null && propertyView.SelectedItems.Count > 0)
            {
                ListViewItem currentItem = propertyView.SelectedItems[0];
                bool isKeyColumn = _currentEditingCell == currentItem.SubItems[0];
                string oldValue = _currentEditingCell.Text;
                string newValue = _inlineCellEditBox.Text;

                if (isKeyColumn)
                {
                    if (IsKeyDuplicateInSection(GetSelectedSectionName(), newValue, oldValue))
                    {
                        MessageBox.Show($"Key '{newValue}' already exists in this section!",
                            "Duplicate Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        _inlineCellEditBox.Focus();
                        return;
                    }
                    UpdateKeyWithCommand(oldValue, newValue, currentItem);
                }
                else
                {
                    string key = currentItem.SubItems[0].Text;
                    UpdateValueWithCommand(key, oldValue, newValue, currentItem);
                }

                _inlineCellEditBox.Visible = false;
                _currentEditingCell = null;
            }
        }

        private void AddSection(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
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

                    if (_documentConfig.HasSection(sectionName) ||
                        sectionName == GetGlobalSectionName())
                    {
                        MessageBox.Show("Section already exists.");
                        return;
                    }

                    var command = new AddSectionCommand(
                        _documentConfig,
                        new Section(sectionName),
                        -1,
                        () => { RefreshSectionList(); RefreshStatusBar(); });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
            }
        }

        private void EditSection(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
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

                    if (_documentConfig.HasSection(newName) && newName != selectedSection)
                    {
                        MessageBox.Show("Section already exists.");
                        return;
                    }

                    var command = new EditSectionCommand(
                        _documentConfig,
                        selectedSection,
                        newName,
                        () => { RefreshSectionList(); RefreshStatusBar(); });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
            }
        }

        private void DeleteSection(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
                return;
            if (sectionView.SelectedItem == null)
                return;

            var selectedSectionName = GetSelectedSectionName();
            if (selectedSectionName == null)
            {
                MessageBox.Show("Please select a section first.");
                return;
            }

            if (MessageBox.Show($"Are you sure you want to delete section '{selectedSectionName}'?",
                "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (selectedSectionName == GetGlobalSectionName())
                {
                    // Save properties for undo
                    var savedProperties = _documentConfig.DefaultSection.Select(p => p.Clone()).ToList();
                    var savedComments = _documentConfig.DefaultSection.PreComments.Select(c => c.Clone()).ToList();
                    var savedInlineComment = _documentConfig.DefaultSection.Comment?.Clone();

                    var command = new GenericCommand(
                        "Clear Default Section",
                        () =>
                        {
                            _documentConfig.DefaultSection.Clear();
                            RefreshKeyValueList(selectedSectionName);
                            RefreshStatusBar();
                        },
                        () =>
                        {
                            foreach (var prop in savedProperties)
                                _documentConfig.DefaultSection.AddProperty(prop);
                            _documentConfig.DefaultSection.PreComments.AddRange(savedComments);
                            _documentConfig.DefaultSection.Comment = savedInlineComment;
                            RefreshKeyValueList(selectedSectionName);
                            RefreshStatusBar();
                        });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
                else
                {
                    var section = _documentConfig.GetSection(selectedSectionName);
                    if (section == null) return;

                    int sectionIndex = GetSectionIndex(selectedSectionName);
                    var command = new DeleteSectionCommand(
                        _documentConfig,
                        section,
                        sectionIndex,
                        () =>
                        {
                            RefreshSectionList();
                            RefreshStatusBar();
                            if (sectionView.Items.Count > 0)
                            {
                                sectionView.SelectedIndex = 0;
                            }
                        });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
            }
        }

        private int GetSectionIndex(string sectionName)
        {
            if (_documentConfig == null) return -1;
            for (int i = 0; i < _documentConfig.SectionCount; i++)
            {
                var section = _documentConfig.GetSectionByIndex(i);
                if (section?.Name == sectionName)
                    return i;
            }
            return -1;
        }

        private void MoveSectionUp(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
                return;
            if (sectionView.SelectedItem == null)
                return;
            if (sectionView.SelectedIndex <= 1)
                return;

            int currentIndex = sectionView.SelectedIndex - 1;
            var section = _documentConfig[currentIndex];
            int targetIndex = currentIndex - 1;

            var command = new MoveSectionCommand(
                _documentConfig,
                section.Name,
                currentIndex,
                targetIndex,
                () =>
                {
                    RefreshSectionList();
                    sectionView.SelectedIndex = targetIndex + 1; // +1 for global section
                    RefreshStatusBar();
                });
            _commandManager.ExecuteCommand(command);
            SetDirty();
        }

        private void MoveSectionDown(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
                return;
            if (sectionView.SelectedItem == null)
                return;
            if (sectionView.SelectedIndex == 0 ||
                sectionView.SelectedIndex == sectionView.Items.Count - 1)
                return;

            int currentIndex = sectionView.SelectedIndex - 1;
            var section = _documentConfig[currentIndex];
            int targetIndex = currentIndex + 1;

            var command = new MoveSectionCommand(
                _documentConfig,
                section.Name,
                currentIndex,
                targetIndex,
                () =>
                {
                    RefreshSectionList();
                    sectionView.SelectedIndex = targetIndex + 1; // +1 for global section
                    RefreshStatusBar();
                });
            _commandManager.ExecuteCommand(command);
            SetDirty();
        }

        private void DuplicateSection(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
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

                    if (_documentConfig.HasSection(newName) ||
                        newName == GetGlobalSectionName())
                    {
                        MessageBox.Show("Section already exists.");
                        return;
                    }

                    var originalSection = GetSection(originalName);
                    var newSection = new Section(newName);

                    foreach (var property in originalSection)
                    {
                        newSection.AddProperty(property.Clone());
                    }

                    newSection.PreComments.AddRange(originalSection.PreComments);
                    newSection.Comment = originalSection.Comment?.Clone();

                    var command = new AddSectionCommand(
                        _documentConfig,
                        newSection,
                        -1,
                        () =>
                        {
                            RefreshSectionList();
                            sectionView.SelectedItem = newName;
                            RefreshStatusBar();
                        });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
            }
        }

        private void SortSections(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
                return;
            string? selectedSection = sectionView.SelectedItem?.ToString();

            var command = new SortSectionsCommand(
                _documentConfig,
                () =>
                {
                    RefreshSectionList();
                    if (selectedSection != null)
                    {
                        sectionView.SelectedItem = selectedSection;
                    }
                    RefreshStatusBar();
                });
            _commandManager.ExecuteCommand(command);
            SetDirty();
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

                    var command = new AddPropertyCommand(
                        selectedSection,
                        new Property(key, value),
                        -1,
                        () => { RefreshKeyValueList(selectedSection.Name); RefreshStatusBar(); });
                    _commandManager.ExecuteCommand(command);
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

                    var oldProperty = selectedSection[oldKey];
                    int index = GetPropertyIndex(selectedSection, oldKey);

                    var command = new EditPropertyCommand(
                        selectedSection,
                        oldKey,
                        oldValue,
                        newKey,
                        newValue,
                        oldProperty.Comment,
                        oldProperty.Comment,
                        oldProperty.IsQuoted,
                        oldProperty.IsQuoted,
                        oldProperty.PreComments,
                        index,
                        () => { RefreshKeyValueList(selectedSection.Name); RefreshStatusBar(); });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
            }
        }

        private int GetPropertyIndex(Section section, string propertyName)
        {
            for (int i = 0; i < section.PropertyCount; i++)
            {
                if (section[i].Name == propertyName)
                    return i;
            }
            return -1;
        }

        private void DeleteKeyValue(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count == 0)
                return;

            var selectedSection = GetSelectedSection();
            int count = propertyView.SelectedItems.Count;

            string confirmMessage = count == 1
                ? $"Are you sure you want to delete key '{propertyView.SelectedItems[0].SubItems[0].Text}'?"
                : $"Are you sure you want to delete {count} selected keys?";

            if (MessageBox.Show(confirmMessage, "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                // Collect all properties to delete (in reverse order to preserve indices)
                var itemsToDelete = new List<(Property property, int index)>();
                foreach (ListViewItem item in propertyView.SelectedItems)
                {
                    string key = item.SubItems[0].Text;
                    var property = selectedSection[key];
                    int index = GetPropertyIndex(selectedSection, key);
                    itemsToDelete.Add((property, index));
                }

                // Sort by index descending so we delete from bottom to top
                itemsToDelete = itemsToDelete.OrderByDescending(x => x.index).ToList();

                // Create a batch delete command
                var savedProperties = itemsToDelete.Select(x => (x.property.Clone(), x.index)).ToList();
                var command = new GenericCommand(
                    $"Delete {count} Properties",
                    () =>
                    {
                        foreach (var (property, _) in itemsToDelete)
                        {
                            selectedSection.RemoveProperty(property.Name);
                        }
                        RefreshKeyValueList(selectedSection.Name);
                        RefreshStatusBar();
                    },
                    () =>
                    {
                        // Restore in original order (ascending index)
                        foreach (var (property, index) in savedProperties.OrderBy(x => x.index))
                        {
                            if (index >= 0 && index <= selectedSection.PropertyCount)
                                selectedSection.InsertProperty(index, property);
                            else
                                selectedSection.AddProperty(property);
                        }
                        RefreshKeyValueList(selectedSection.Name);
                        RefreshStatusBar();
                    });
                _commandManager.ExecuteCommand(command);
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
            var property = selectedSection[currentIndex];
            int targetIndex = currentIndex - 1;

            var command = new MovePropertyCommand(
                selectedSection,
                property.Name,
                currentIndex,
                targetIndex,
                () =>
                {
                    RefreshKeyValueList(selectedSection.Name);
                    if (propertyView.Items.Count > targetIndex)
                        propertyView.Items[targetIndex].Selected = true;
                    RefreshStatusBar();
                });
            _commandManager.ExecuteCommand(command);
            SetDirty();
        }

        private void MoveKeyDown(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count == 0)
                return;
            if (propertyView.SelectedIndices[0] == propertyView.Items.Count - 1)
                return;

            int currentIndex = propertyView.SelectedIndices[0];
            var selectedSection = GetSelectedSection();
            var property = selectedSection[currentIndex];
            int targetIndex = currentIndex + 1;

            var command = new MovePropertyCommand(
                selectedSection,
                property.Name,
                currentIndex,
                targetIndex,
                () =>
                {
                    RefreshKeyValueList(selectedSection.Name);
                    if (propertyView.Items.Count > targetIndex)
                        propertyView.Items[targetIndex].Selected = true;
                    RefreshStatusBar();
                });
            _commandManager.ExecuteCommand(command);
            SetDirty();
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

                    var originalProperty = selectedSection[originalKey].Clone();
                    var newProperty = new Property(newKey, newValue);
                    newProperty.PreComments.AddRange(originalProperty.PreComments);
                    newProperty.Comment = originalProperty.Comment;

                    var command = new AddPropertyCommand(
                        selectedSection,
                        newProperty,
                        -1,
                        () =>
                        {
                            RefreshKeyValueList(selectedSection.Name);
                            foreach (ListViewItem item in propertyView.Items)
                            {
                                if (item.Text == newKey)
                                {
                                    item.Selected = true;
                                    break;
                                }
                            }
                            RefreshStatusBar();
                        });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
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

            var command = new SortPropertiesCommand(
                selectedSection,
                () =>
                {
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
                });
            _commandManager.ExecuteCommand(command);
            SetDirty();
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
                _inlineCellEditBox.Visible = false;
                _currentEditingCell = null;
                e.Handled = true;
            }
        }

        private void OnInlineCellEditorLostFocus(object? sender, EventArgs e)
        {
            if (_inlineCellEditBox.Visible)
            {
                CommitInlineCellEdit();
            }
        }

        private void OnSectionSelectionChanged(object? sender, EventArgs e)
        {
            if (sectionView.SelectedItem == null || _documentConfig == null)
                return;

            // Clear property filter when changing sections
            if (_propertyFilterBox != null && !string.IsNullOrEmpty(_propertyFilterBox.Text))
            {
                _propertyFilterBox.Text = "";
            }

            try
            {
                _isUpdatingCommentsFromCode = true;

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
                _isUpdatingCommentsFromCode = false;
            }
        }

        private void OnSectionDoubleClick(object? sender, EventArgs e)
        {
            // Allow editing non-default sections (index > 0)
            if (sectionView.SelectedIndex > 0)
            {
                EditSection(sender, e);
            }
        }

        private void OnPropertyDoubleClick(object? sender, MouseEventArgs e)
        {
            BeginInlineCellEdit(e.Location);
        }

        private void OnPropertyClick(object? sender, EventArgs e)
        {
            if (propertyView.SelectedItems.Count > 0 && sectionView.SelectedItem != null && _documentConfig != null)
            {
                ListViewHitTestInfo hitTest = propertyView.HitTest(propertyView.PointToClient(Cursor.Position));
                try
                {
                    _isUpdatingCommentsFromCode = true;

                    if (hitTest.Item != null)
                    {
                        string key = hitTest.Item.SubItems[0].Text;
                        var selectedSection = GetSelectedSection();
                        if (selectedSection.TryGetProperty(key, out var property) && property != null)
                        {
                            preCommentsTextBox.Text = property.PreComments.ToMultiLineText();
                            inlineCommentTextBox.Text = property.Comment?.Value ?? "";
                            RefreshStatusBar();
                        }
                    }
                }
                finally
                {
                    _isUpdatingCommentsFromCode = false;
                }
            }
        }

        private void UpdateKeyWithCommand(string oldKey, string newKey, ListViewItem listItem)
        {
            if (oldKey == newKey)
            {
                listItem.SubItems[0].Text = newKey;
                return;
            }
            if (sectionView.SelectedItem == null)
                return;

            var selectedSection = GetSelectedSection();
            var property = selectedSection[oldKey];
            int index = GetPropertyIndex(selectedSection, oldKey);

            var command = new EditPropertyCommand(
                selectedSection,
                oldKey,
                property.Value,
                newKey,
                property.Value,
                property.Comment,
                property.Comment,
                property.IsQuoted,
                property.IsQuoted,
                property.PreComments,
                index,
                () =>
                {
                    listItem.SubItems[0].Text = newKey;
                    RefreshStatusBar();
                });
            _commandManager.ExecuteCommand(command);
            SetDirty();
        }

        private void UpdateValueWithCommand(string key, string oldValue, string newValue, ListViewItem listItem)
        {
            if (oldValue == newValue)
            {
                listItem.SubItems[1].Text = newValue;
                return;
            }
            if (sectionView.SelectedItem == null)
                return;

            var selectedSection = GetSelectedSection();
            var property = selectedSection[key];
            int index = GetPropertyIndex(selectedSection, key);

            var command = new EditPropertyCommand(
                selectedSection,
                key,
                oldValue,
                key,
                newValue,
                property.Comment,
                property.Comment,
                property.IsQuoted,
                property.IsQuoted,
                property.PreComments,
                index,
                () =>
                {
                    listItem.SubItems[1].Text = newValue;
                    RefreshStatusBar();
                });
            _commandManager.ExecuteCommand(command);
            SetDirty();
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
            if (_isUpdatingCommentsFromCode)
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
            if (_isUpdatingCommentsFromCode)
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

            _currentFilePath = "";
            _documentConfig = new();
            _commandManager.Clear(); // Clear undo/redo history

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
            _currentFilePath = filename;
            sectionView.Items.Clear();

            try
            {
                // Detect file encoding
                _currentEncoding = EncodingHelper.DetectEncoding(filename);

                // Use async I/O with current options and detected encoding
                _documentConfig = await IniConfigManager.LoadAsync(filename, _currentEncoding, _configOptions);

                RefreshSectionList();

                if (sectionView.Items.Count > 0)
                {
                    sectionView.SelectedIndex = 0;
                    RefreshKeyValueList(GetSelectedSectionName());
                }

                RefreshStatusBar();
                SetDirty(false);
                _commandManager.Clear(); // Clear undo/redo history
                _recentFilesManager.AddRecentFile(filename); // Add to recent files

                // Show parsing errors if any
                if (_documentConfig.ParsingErrors.Count > 0)
                {
                    var errorMsg = $"File loaded with {_documentConfig.ParsingErrors.Count} parsing error(s):\n\n";
                    foreach (var error in _documentConfig.ParsingErrors.Take(5))
                    {
                        errorMsg += $"Line {error.LineNumber}: {error.Reason}\n";
                    }
                    if (_documentConfig.ParsingErrors.Count > 5)
                    {
                        errorMsg += $"\n... and {_documentConfig.ParsingErrors.Count - 5} more error(s)";
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

        private void ReloadFromDisk(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                MessageBox.Show("No file is currently open.", "Reload", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(_currentFilePath))
            {
                MessageBox.Show($"File not found: {_currentFilePath}", "Reload", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_commandManager.IsDirtyFromSavePoint)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Reloading will discard all changes.\n\nDo you want to continue?",
                    "Reload from Disk",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            LoadFile(_currentFilePath);
        }

        private async void SaveFile(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
            {
                MessageBox.Show($"Error saving file: _documentConfig invalid", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsFile(sender, e);
                return;
            }

            try
            {
                await IniConfigManager.SaveAsync(_currentFilePath, _documentConfig);
                _commandManager.MarkSavePoint();
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
            if (_documentConfig == null)
            {
                MessageBox.Show($"Error saving file: _documentConfig invalid", "Error",
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
                _currentFilePath = saveFileDialog.FileName;
                await IniConfigManager.SaveAsync(_currentFilePath, _documentConfig);
                _commandManager.MarkSavePoint();
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

        #region Export Methods

        private void ExportAsJson(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
            {
                MessageBox.Show("No document to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using SaveFileDialog saveFileDialog = new()
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = "json",
                FileName = Path.GetFileNameWithoutExtension(_currentFilePath)
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var options = new JsonExportOptions
                {
                    Indented = true,
                    AutoConvertTypes = true
                };
                DocumentExporter.ToJsonFile(_documentConfig, saveFileDialog.FileName, options);
                MessageBox.Show("File exported successfully!", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportAsXml(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
            {
                MessageBox.Show("No document to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using SaveFileDialog saveFileDialog = new()
            {
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = "xml",
                FileName = Path.GetFileNameWithoutExtension(_currentFilePath)
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var options = new XmlExportOptions
                {
                    Indented = true,
                    IncludeXmlDeclaration = true
                };
                DocumentExporter.ToXmlFile(_documentConfig, saveFileDialog.FileName, options);
                MessageBox.Show("File exported successfully!", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportAsCsv(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
            {
                MessageBox.Show("No document to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using SaveFileDialog saveFileDialog = new()
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = "csv",
                FileName = Path.GetFileNameWithoutExtension(_currentFilePath)
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var options = new CsvExportOptions
                {
                    IncludeHeader = true,
                    IncludeComments = true
                };
                DocumentExporter.ToCsvFile(_documentConfig, saveFileDialog.FileName, options);
                MessageBox.Show("File exported successfully!", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Theme

        private void SetupTheme()
        {
            // Subscribe to theme changes
            ThemeManager.ThemeChanged += OnThemeChanged;

            // Load saved theme preference
            var savedTheme = Properties.Settings.Default.DarkMode;
            if (savedTheme)
            {
                ThemeManager.SetTheme(AppTheme.Dark);
            }

            // Apply initial theme
            ApplyTheme();
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            ApplyTheme();

            // Update menu check state
            if (_darkModeMenuItem != null)
            {
                _darkModeMenuItem.Checked = ThemeManager.IsDarkMode;
            }

            // Save preference
            Properties.Settings.Default.DarkMode = ThemeManager.IsDarkMode;
            Properties.Settings.Default.Save();
        }

        private void ToggleDarkMode(object? sender, EventArgs e)
        {
            ThemeManager.ToggleTheme();
        }

        private void ApplyTheme()
        {
            ThemeManager.ApplyTheme(this);

            // Custom adjustments for specific controls
            ApplyThemeToPropertyView();
            ApplyThemeToSectionView();
            ApplyThemeToCommentBox();

            // Refresh lists to apply colors
            Refresh();
        }

        private void ApplyThemeToPropertyView()
        {
            propertyView.BackColor = ThemeManager.ControlBackground;
            propertyView.ForeColor = ThemeManager.Foreground;

            // Update column header style (handled by ThemeManager's OwnerDraw)
        }

        private void ApplyThemeToSectionView()
        {
            sectionView.BackColor = ThemeManager.ControlBackground;
            sectionView.ForeColor = ThemeManager.Foreground;
        }

        private void ApplyThemeToCommentBox()
        {
            preCommentsTextBox.BackColor = ThemeManager.ControlBackground;
            preCommentsTextBox.ForeColor = ThemeManager.Foreground;
            inlineCommentTextBox.BackColor = ThemeManager.ControlBackground;
            inlineCommentTextBox.ForeColor = ThemeManager.Foreground;
        }

        #endregion

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
            return _documentConfig?.DefaultSection.Name ?? string.Empty;
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
            if (_documentConfig == null)
                throw new InvalidOperationException("_documentConfig is null");

            if (sectionName == GetGlobalSectionName())
            {
                return _documentConfig.DefaultSection;
            }
            return _documentConfig[sectionName];
        }
        #endregion

        #region Dirty Flag Management
        private void SetDirty(bool dirty = true)
        {
            _isDirty = dirty;
            _statisticsDirty = true; // Mark statistics as needing recalculation
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath) ? "Untitled" : Path.GetFileName(_currentFilePath);
            string dirtyMarker = _isDirty ? "*" : "";
            Text = $"{fileName}{dirtyMarker} - IniEdit Editor";
        }

        private bool PromptSaveChanges()
        {
            if (!_isDirty)
                return true;

            var result = MessageBox.Show(
                "Do you want to save changes?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SaveFile(null, EventArgs.Empty);
                return !_isDirty; // Return false if save failed
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
                return;
            }

            // Save window state before closing
            SaveWindowState();

            // Unsubscribe event handlers to prevent memory leaks
            CleanupEventHandlers();

            base.OnFormClosing(e);
        }

        private void CleanupEventHandlers()
        {
            // Unsubscribe form events
            this.KeyDown -= OnFormKeyDown;

            // Unsubscribe CommandManager events
            _commandManager.StateChanged -= OnCommandManagerStateChanged;

            // Unsubscribe RecentFilesManager events
            _recentFilesManager.RecentFilesChanged -= OnRecentFilesChanged;

            // Unsubscribe FindReplaceDialog events
            if (_findReplaceDialog != null && !_findReplaceDialog.IsDisposed)
            {
                _findReplaceDialog.FindNextClicked -= FindNext;
                _findReplaceDialog.ReplaceClicked -= Replace;
                _findReplaceDialog.ReplaceAllClicked -= ReplaceAll;
                _findReplaceDialog.Dispose();
                _findReplaceDialog = null;
            }

            // Dispose inline cell editor
            _inlineCellEditBox.Dispose();

            // Dispose auto-backup timer
            if (_autoBackupTimer != null)
            {
                _autoBackupTimer.Stop();
                _autoBackupTimer.Tick -= OnAutoBackupTick;
                _autoBackupTimer.Dispose();
                _autoBackupTimer = null;
            }

            // Cleanup TreeView if in tree mode
            if (_sectionTreeView != null)
            {
                _sectionTreeView.AfterSelect -= OnTreeViewSectionSelected;
                _sectionTreeView.NodeMouseDoubleClick -= OnTreeViewNodeDoubleClick;
                var contextMenu = _sectionTreeView.ContextMenuStrip;
                _sectionTreeView.ContextMenuStrip = null;
                contextMenu?.Dispose();
                _sectionTreeView.Dispose();
                _sectionTreeView = null;
            }
        }
        #endregion

        #region Settings
        private void OpenSettings(object? sender, EventArgs e)
        {
            using (var dialog = new SettingsDialog(_configOptions))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _configOptions = dialog.Options;
                    MessageBox.Show("Settings saved. New settings will be applied when loading files.",
                        "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        #endregion

        #region Window State Persistence
        private void RestoreWindowState()
        {
            try
            {
                var settings = Properties.Settings.Default;

                // Restore window size first (before location to ensure proper bounds checking)
                if (settings.WindowSize.Width >= MinimumSize.Width &&
                    settings.WindowSize.Height >= MinimumSize.Height)
                {
                    this.Size = settings.WindowSize;
                }

                // Restore window location (ensure it's visible on screen)
                var location = settings.WindowLocation;
                var screenBounds = Screen.FromPoint(location).WorkingArea;
                if (location.X >= screenBounds.Left && location.X < screenBounds.Right &&
                    location.Y >= screenBounds.Top && location.Y < screenBounds.Bottom)
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = location;
                }

                // Restore window state (normal, maximized, etc.)
                if (settings.WindowState != FormWindowState.Minimized)
                {
                    this.WindowState = settings.WindowState;
                }

                // Restore splitter distances after form is shown (only once)
                if (!_windowStateRestored)
                {
                    _windowStateRestored = true;
                    this.Shown += OnFormShownRestoreSplitters;
                }
            }
            catch
            {
                // If settings fail to load, use defaults
            }
        }

        private void OnFormShownRestoreSplitters(object? sender, EventArgs e)
        {
            // Unsubscribe immediately to prevent duplicate calls
            this.Shown -= OnFormShownRestoreSplitters;

            try
            {
                var settings = Properties.Settings.Default;

                if (settings.SplitterDistance1 > 0 && settings.SplitterDistance1 < splitContainer1.Width)
                    splitContainer1.SplitterDistance = settings.SplitterDistance1;

                if (settings.SplitterDistance2 > 0 && settings.SplitterDistance2 < splitContainer2.Height)
                    splitContainer2.SplitterDistance = settings.SplitterDistance2;
            }
            catch
            {
                // Ignore splitter distance errors
            }
        }

        private void SaveWindowState()
        {
            try
            {
                var settings = Properties.Settings.Default;

                // Save window state
                settings.WindowState = this.WindowState;

                // Save size and location only if not minimized/maximized
                if (this.WindowState == FormWindowState.Normal)
                {
                    settings.WindowLocation = this.Location;
                    settings.WindowSize = this.Size;
                }
                else
                {
                    // Save the restore bounds when maximized/minimized
                    settings.WindowLocation = this.RestoreBounds.Location;
                    settings.WindowSize = this.RestoreBounds.Size;
                }

                // Save splitter distances
                settings.SplitterDistance1 = splitContainer1.SplitterDistance;
                settings.SplitterDistance2 = splitContainer2.SplitterDistance;

                settings.Save();
            }
            catch
            {
                // Ignore save errors
            }
        }
        #endregion

        #region Auto-Backup
        private void SetupAutoBackup()
        {
            _autoBackupTimer = new System.Windows.Forms.Timer
            {
                Interval = AutoBackupCheckIntervalMs
            };
            _autoBackupTimer.Tick += OnAutoBackupTick;

            // Start timer if auto-backup is enabled
            if (Properties.Settings.Default.AutoBackupEnabled)
            {
                _autoBackupTimer.Start();
            }
        }

        private void OnAutoBackupTick(object? sender, EventArgs e)
        {
            if (!Properties.Settings.Default.AutoBackupEnabled)
                return;

            // Only backup if there's a file open, it's dirty, and enough time has passed
            if (_documentConfig == null || !_isDirty || string.IsNullOrEmpty(_currentFilePath))
                return;

            var interval = TimeSpan.FromMinutes(Properties.Settings.Default.AutoBackupIntervalMinutes);
            if (DateTime.Now - _lastAutoBackupTime < interval)
                return;

            PerformAutoBackup();
        }

        private void PerformAutoBackup()
        {
            if (_documentConfig == null || string.IsNullOrEmpty(_currentFilePath))
                return;

            try
            {
                // Create backup file path
                var backupDir = Path.Combine(Path.GetDirectoryName(_currentFilePath) ?? ".", ".ini_backup");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                    // Hide the backup directory
                    File.SetAttributes(backupDir, File.GetAttributes(backupDir) | FileAttributes.Hidden);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = Path.GetFileNameWithoutExtension(_currentFilePath);
                var ext = Path.GetExtension(_currentFilePath);
                var backupPath = Path.Combine(backupDir, $"{fileName}_{timestamp}{ext}.bak");

                // Write backup
                IniConfigManager.Save(backupPath, _currentEncoding, _documentConfig);

                _lastAutoBackupTime = DateTime.Now;

                // Clean up old backups (keep only last 10)
                CleanupOldBackups(backupDir, fileName, ext);
            }
            catch
            {
                // Silently ignore backup errors
            }
        }

        private void CleanupOldBackups(string backupDir, string fileName, string ext)
        {
            try
            {
                var pattern = $"{fileName}_*{ext}.bak";
                var filesToDelete = Directory.GetFiles(backupDir, pattern)
                    .OrderByDescending(f => f)
                    .Skip(MaxBackupFilesToKeep);

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore individual file deletion errors
                    }
                }
            }
            catch
            {
                // Silently ignore cleanup errors
            }
        }

        private void ToggleAutoBackup(object? sender, EventArgs e)
        {
            var settings = Properties.Settings.Default;
            settings.AutoBackupEnabled = !settings.AutoBackupEnabled;
            settings.Save();

            if (_autoBackupMenuItem != null)
            {
                _autoBackupMenuItem.Checked = settings.AutoBackupEnabled;
            }

            if (settings.AutoBackupEnabled)
            {
                _autoBackupTimer?.Start();
            }
            else
            {
                _autoBackupTimer?.Stop();
            }
        }

        #endregion

        #region TreeView Mode
        private void SetupTreeViewMode()
        {
            if (_sectionTreeView != null)
                return;

            _sectionTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true
            };

            _sectionTreeView.AfterSelect += OnTreeViewSectionSelected;
            _sectionTreeView.NodeMouseDoubleClick += OnTreeViewNodeDoubleClick;

            // Apply theme
            _sectionTreeView.BackColor = ThemeManager.ControlBackground;
            _sectionTreeView.ForeColor = ThemeManager.Foreground;
            _sectionTreeView.LineColor = ThemeManager.Border;

            // Hide ListBox, show TreeView
            sectionView.Visible = false;
            splitContainer1.Panel1.Controls.Add(_sectionTreeView);
            _sectionTreeView.BringToFront();

            // Setup context menu for TreeView
            SetupTreeViewContextMenu();

            // Refresh to populate TreeView
            if (_documentConfig != null)
            {
                RefreshTreeView();
            }
        }

        private void TearDownTreeViewMode()
        {
            if (_sectionTreeView == null)
                return;

            // Unsubscribe event handlers
            _sectionTreeView.AfterSelect -= OnTreeViewSectionSelected;
            _sectionTreeView.NodeMouseDoubleClick -= OnTreeViewNodeDoubleClick;

            // Dispose context menu
            var contextMenu = _sectionTreeView.ContextMenuStrip;
            _sectionTreeView.ContextMenuStrip = null;
            contextMenu?.Dispose();

            // Remove and dispose TreeView
            splitContainer1.Panel1.Controls.Remove(_sectionTreeView);
            _sectionTreeView.Dispose();
            _sectionTreeView = null;

            sectionView.Visible = true;

            // Refresh ListBox
            if (_documentConfig != null)
            {
                RefreshSectionList();
            }
        }

        private void ToggleTreeViewMode(object? sender, EventArgs e)
        {
            var settings = Properties.Settings.Default;
            settings.TreeViewMode = !settings.TreeViewMode;
            settings.Save();

            if (_treeViewModeMenuItem != null)
            {
                _treeViewModeMenuItem.Checked = settings.TreeViewMode;
            }

            if (settings.TreeViewMode)
            {
                SetupTreeViewMode();
            }
            else
            {
                TearDownTreeViewMode();
            }
        }

        private void RefreshTreeView()
        {
            if (_sectionTreeView == null || _documentConfig == null)
                return;

            _sectionTreeView.BeginUpdate();
            _sectionTreeView.Nodes.Clear();

            var separator = Properties.Settings.Default.TreeViewSeparator;
            if (string.IsNullOrEmpty(separator))
                separator = ".";

            // Add global section
            var globalNode = new TreeNode(GetGlobalSectionName())
            {
                Tag = (Section?)null  // Global section
            };
            _sectionTreeView.Nodes.Add(globalNode);

            // Build tree from section names
            foreach (var section in _documentConfig)
            {
                var parts = section.Name.Split(new[] { separator }, StringSplitOptions.None);
                var currentNodes = _sectionTreeView.Nodes;
                TreeNode? parentNode = null;

                // Build partial name incrementally to avoid O(n²) string.Join
                var partialNameBuilder = new StringBuilder();

                for (int i = 0; i < parts.Length; i++)
                {
                    var displayName = parts[i];

                    // Build partial name incrementally
                    if (i > 0)
                        partialNameBuilder.Append(separator);
                    partialNameBuilder.Append(displayName);

                    // Look for existing node at this level
                    TreeNode? existingNode = null;
                    foreach (TreeNode node in currentNodes)
                    {
                        if (node.Text == displayName)
                        {
                            existingNode = node;
                            break;
                        }
                    }

                    if (existingNode != null)
                    {
                        parentNode = existingNode;
                        currentNodes = existingNode.Nodes;
                    }
                    else
                    {
                        // Create new node
                        var newNode = new TreeNode(displayName);

                        // If this is the last part, it's the actual section
                        if (i == parts.Length - 1)
                        {
                            newNode.Tag = section;
                        }
                        else
                        {
                            // Intermediate node - might represent a section or just a path
                            var partialName = partialNameBuilder.ToString();
                            var matchingSection = _documentConfig.GetSection(partialName);
                            newNode.Tag = matchingSection;
                        }

                        currentNodes.Add(newNode);
                        parentNode = newNode;
                        currentNodes = newNode.Nodes;
                    }
                }
            }

            _sectionTreeView.EndUpdate();

            // Expand first level
            foreach (TreeNode node in _sectionTreeView.Nodes)
            {
                node.Expand();
            }
        }

        private void OnTreeViewSectionSelected(object? sender, TreeViewEventArgs e)
        {
            if (e.Node == null)
                return;

            try
            {
                _isUpdatingCommentsFromCode = true;

                // If node has a Tag, it's a section; otherwise it's just a folder
                if (e.Node.Tag is Section section)
                {
                    RefreshKeyValueList(section.Name);
                    preCommentsTextBox.Text = section.PreComments.ToMultiLineText();
                    inlineCommentTextBox.Text = section.Comment?.Value ?? "";
                    preCommentsTextBox.Enabled = true;
                    inlineCommentTextBox.Enabled = true;
                }
                else if (e.Node.Tag == null && e.Node.Text == GetGlobalSectionName())
                {
                    // Global section
                    RefreshKeyValueList(GetGlobalSectionName());
                    preCommentsTextBox.Text = "";
                    inlineCommentTextBox.Text = "";
                    preCommentsTextBox.Enabled = false;
                    inlineCommentTextBox.Enabled = false;
                }
                else
                {
                    // Just a folder node, clear properties
                    propertyView.Items.Clear();
                    preCommentsTextBox.Text = "";
                    inlineCommentTextBox.Text = "";
                    preCommentsTextBox.Enabled = false;
                    inlineCommentTextBox.Enabled = false;
                }

                RefreshStatusBar();
            }
            finally
            {
                _isUpdatingCommentsFromCode = false;
            }
        }

        private void OnTreeViewNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is Section section)
            {
                // Edit section name
                EditSectionByName(section.Name);
            }
        }

        private void SetupTreeViewContextMenu()
        {
            if (_sectionTreeView == null)
                return;

            var contextMenu = new ContextMenuStrip();

            var addMenu = new ToolStripMenuItem("Add Section");
            addMenu.Click += AddSection;

            var editMenu = new ToolStripMenuItem("Edit Section");
            editMenu.Click += (s, e) =>
            {
                if (_sectionTreeView.SelectedNode?.Tag is Section section)
                {
                    EditSectionByName(section.Name);
                }
            };

            var deleteMenu = new ToolStripMenuItem("Delete Section");
            deleteMenu.Click += (s, e) =>
            {
                if (_sectionTreeView.SelectedNode?.Tag is Section section)
                {
                    DeleteSectionByName(section.Name);
                }
            };

            var expandAllMenu = new ToolStripMenuItem("Expand All");
            expandAllMenu.Click += (s, e) => _sectionTreeView.ExpandAll();

            var collapseAllMenu = new ToolStripMenuItem("Collapse All");
            collapseAllMenu.Click += (s, e) => _sectionTreeView.CollapseAll();

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                addMenu,
                editMenu,
                deleteMenu,
                new ToolStripSeparator(),
                expandAllMenu,
                collapseAllMenu
            });

            _sectionTreeView.ContextMenuStrip = contextMenu;
        }

        private void EditSectionByName(string sectionName)
        {
            if (_documentConfig == null)
                return;

            var section = _documentConfig.GetSection(sectionName);
            if (section == null)
                return;

            using var dialog = new InputDialog("Edit Section", "Section name:", sectionName);
            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                var newName = dialog.InputText.Trim();
                if (newName != sectionName)
                {
                    if (_documentConfig.HasSection(newName))
                    {
                        MessageBox.Show($"Section '{newName}' already exists.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var command = new EditSectionCommand(
                        _documentConfig,
                        sectionName,
                        newName,
                        () => { RefreshTreeView(); RefreshStatusBar(); });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
            }
        }

        private void DeleteSectionByName(string sectionName)
        {
            if (_documentConfig == null)
                return;

            var section = _documentConfig.GetSection(sectionName);
            if (section == null)
                return;

            var result = MessageBox.Show($"Are you sure you want to delete section '{sectionName}'?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                int sectionIndex = GetSectionIndex(sectionName);
                var command = new DeleteSectionCommand(
                    _documentConfig,
                    section,
                    sectionIndex,
                    () =>
                    {
                        RefreshTreeView();
                        RefreshStatusBar();
                    });
                _commandManager.ExecuteCommand(command);
                SetDirty();
            }
        }

        #endregion

        #region Go to Section
        private void GoToSection()
        {
            if (_documentConfig == null || sectionView.Items.Count == 0)
                return;

            using var dialog = new Form
            {
                Text = "Go to Section",
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = "Select section:",
                Location = new Point(12, 15),
                AutoSize = true
            };

            var comboBox = new ComboBox
            {
                Location = new Point(12, 35),
                Size = new Size(360, 23),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            foreach (var item in sectionView.Items)
            {
                comboBox.Items.Add(item?.ToString() ?? "");
            }

            if (sectionView.SelectedIndex >= 0)
                comboBox.SelectedIndex = sectionView.SelectedIndex;

            var okButton = new Button
            {
                Text = "Go",
                DialogResult = DialogResult.OK,
                Location = new Point(216, 70),
                Size = new Size(75, 23)
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(297, 70),
                Size = new Size(75, 23)
            };

            dialog.Controls.AddRange(new Control[] { label, comboBox, okButton, cancelButton });
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) == DialogResult.OK && comboBox.SelectedIndex >= 0)
            {
                sectionView.SelectedIndex = comboBox.SelectedIndex;
                sectionView.Focus();
            }
            else if (dialog.DialogResult == DialogResult.OK && !string.IsNullOrEmpty(comboBox.Text))
            {
                // Try to find section by name
                for (int i = 0; i < sectionView.Items.Count; i++)
                {
                    if (sectionView.Items[i].ToString()?.Equals(comboBox.Text, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        sectionView.SelectedIndex = i;
                        sectionView.Focus();
                        return;
                    }
                }
                MessageBox.Show($"Section '{comboBox.Text}' not found.", "Go to Section", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region Find and Replace
        private void OpenFindReplace(object? sender, EventArgs e)
        {
            if (_findReplaceDialog == null || _findReplaceDialog.IsDisposed)
            {
                _findReplaceDialog = new FindReplaceDialog();
                _findReplaceDialog.FindNextClicked += FindNext;
                _findReplaceDialog.ReplaceClicked += Replace;
                _findReplaceDialog.ReplaceAllClicked += ReplaceAll;
            }

            _findReplaceDialog.Show();
            _findReplaceDialog.BringToFront();
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
            if (_findReplaceDialog == null || _documentConfig == null)
                return;

            string findText = _findReplaceDialog.FindText;
            bool matchCase = _findReplaceDialog.MatchCase;
            bool useRegex = _findReplaceDialog.UseRegex;

            // Start from current position
            int startSectionIndex = _lastSearchSectionIndex >= 0 ? _lastSearchSectionIndex : 0;
            int startPropertyIndex = _lastSearchPropertyIndex + 1;

            // Search sections
            if (_findReplaceDialog.SearchSections)
            {
                for (int i = startSectionIndex; i < sectionView.Items.Count; i++)
                {
                    string sectionName = sectionView.Items[i].ToString() ?? "";
                    if (IsMatch(sectionName, findText, matchCase, useRegex))
                    {
                        sectionView.SelectedIndex = i;
                        _lastSearchSectionIndex = i;
                        _lastSearchPropertyIndex = -1;
                        _findReplaceDialog.SetStatus($"Found in section: {sectionName}");
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

                    if (_findReplaceDialog.SearchKeys && IsMatch(prop.Name, findText, matchCase, useRegex))
                    {
                        found = true;
                        location = "key";
                    }
                    else if (_findReplaceDialog.SearchValues && IsMatch(prop.Value, findText, matchCase, useRegex))
                    {
                        found = true;
                        location = "value";
                    }

                    if (found)
                    {
                        sectionView.SelectedIndex = i;
                        propertyView.Items[j].Selected = true;
                        propertyView.Items[j].EnsureVisible();
                        _lastSearchSectionIndex = i;
                        _lastSearchPropertyIndex = j;
                        _findReplaceDialog.SetStatus($"Found in {location}: {prop.Name}");
                        return;
                    }
                }
            }

            // Not found, reset and notify
            _lastSearchSectionIndex = -1;
            _lastSearchPropertyIndex = -1;
            _findReplaceDialog.SetStatus("No more matches found.", true);
        }

        private void Replace(object? sender, EventArgs e)
        {
            if (_findReplaceDialog == null || _documentConfig == null)
                return;

            if (propertyView.SelectedItems.Count == 0)
            {
                _findReplaceDialog.SetStatus("Please select a property first.", true);
                return;
            }

            var selectedItem = propertyView.SelectedItems[0];
            string key = selectedItem.SubItems[0].Text;
            string value = selectedItem.SubItems[1].Text;
            var selectedSection = GetSelectedSection();

            string findText = _findReplaceDialog.FindText;
            string replaceText = _findReplaceDialog.ReplaceText;
            bool matchCase = _findReplaceDialog.MatchCase;
            bool useRegex = _findReplaceDialog.UseRegex;

            bool replaced = false;

            if (_findReplaceDialog.SearchKeys && IsMatch(key, findText, matchCase, useRegex))
            {
                string newKey = ReplaceString(key, findText, replaceText, matchCase, useRegex);
                if (newKey != key && !selectedSection.HasProperty(newKey))
                {
                    UpdateKeyWithCommand(key, newKey, selectedItem);
                    replaced = true;
                }
            }

            // If key was replaced, update key variable for value replacement
            key = selectedItem.SubItems[0].Text;

            if (_findReplaceDialog.SearchValues && IsMatch(value, findText, matchCase, useRegex))
            {
                string newValue = ReplaceString(value, findText, replaceText, matchCase, useRegex);
                UpdateValueWithCommand(key, value, newValue, selectedItem);
                replaced = true;
            }

            if (replaced)
            {
                _findReplaceDialog.SetStatus("Replaced.");
                FindNext(sender, e); // Move to next match
            }
            else
            {
                _findReplaceDialog.SetStatus("No match in selection.", true);
            }
        }

        private void ReplaceAll(object? sender, EventArgs e)
        {
            if (_findReplaceDialog == null || _documentConfig == null)
                return;

            string findText = _findReplaceDialog.FindText;
            string replaceText = _findReplaceDialog.ReplaceText;
            bool matchCase = _findReplaceDialog.MatchCase;
            bool useRegex = _findReplaceDialog.UseRegex;
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

                    if (_findReplaceDialog.SearchKeys)
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

                    if (_findReplaceDialog.SearchValues)
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

            _findReplaceDialog.SetStatus($"Replaced {replaceCount} occurrence(s).");
            _lastSearchSectionIndex = -1;
            _lastSearchPropertyIndex = -1;
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
            if (_commandManager.CanUndo)
            {
                _commandManager.Undo();
                SetDirty();
            }
        }

        private void Redo(object? sender, EventArgs e)
        {
            if (_commandManager.CanRedo)
            {
                _commandManager.Redo();
                SetDirty();
            }
        }

        private void UpdateUndoRedoMenuItems()
        {
            if (_undoMenuItem != null)
            {
                _undoMenuItem.Enabled = _commandManager.CanUndo;
                _undoMenuItem.Text = _commandManager.CanUndo
                    ? $"&Undo {_commandManager.UndoDescription}"
                    : "&Undo";
            }

            if (_redoMenuItem != null)
            {
                _redoMenuItem.Enabled = _commandManager.CanRedo;
                _redoMenuItem.Text = _commandManager.CanRedo
                    ? $"&Redo {_commandManager.RedoDescription}"
                    : "&Redo";
            }
        }

        #endregion

        #region Copy/Paste/Cut

        private void Copy(object? sender, EventArgs e)
        {
            // Check if properties are selected (prioritize property selection)
            if (propertyView.SelectedItems.Count > 0 && sectionView.SelectedIndex >= 0)
            {
                string sectionName = sectionView.SelectedItem?.ToString() ?? "";
                var section = GetSection(sectionName);

                if (propertyView.SelectedItems.Count == 1)
                {
                    // Single property
                    string key = propertyView.SelectedItems[0].Text;
                    var property = section.GetProperty(key);
                    if (property != null)
                    {
                        ClipboardHelper.CopyProperty(property);
                    }
                }
                else
                {
                    // Multiple properties
                    var properties = new List<Property>();
                    foreach (ListViewItem item in propertyView.SelectedItems)
                    {
                        var property = section.GetProperty(item.Text);
                        if (property != null)
                        {
                            properties.Add(property);
                        }
                    }
                    ClipboardHelper.CopyProperties(properties);
                }
                return;
            }

            // Check if a section is selected (no properties selected)
            if (sectionView.SelectedIndex >= 0)
            {
                string sectionName = sectionView.SelectedItem?.ToString() ?? "";
                var section = GetSection(sectionName);
                ClipboardHelper.CopySection(section);
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
            if (_documentConfig == null)
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
                    while (_documentConfig.HasSection(newName))
                    {
                        newName = $"{section.Name}_{counter++}";
                    }

                    // Create new section with unique name
                    var newSection = new Section(newName);
                    newSection.AddPropertyRange(section.GetProperties());
                    newSection.PreComments.AddRange(section.PreComments);
                    newSection.Comment = section.Comment;

                    int index = sectionView.SelectedIndex >= 0 ? sectionView.SelectedIndex : _documentConfig.SectionCount;
                    var command = new AddSectionCommand(_documentConfig, newSection, index, () =>
                    {
                        RefreshSectionList();
                        RefreshStatusBar();
                    });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
                return;
            }

            // Try to paste multiple properties
            if (ClipboardHelper.HasProperties())
            {
                var properties = ClipboardHelper.GetProperties();
                if (properties != null && properties.Count > 0 && sectionView.SelectedIndex >= 0)
                {
                    string sectionName = sectionView.SelectedItem?.ToString() ?? "";
                    var section = GetSection(sectionName);

                    var newProperties = new List<Property>();
                    foreach (var property in properties)
                    {
                        // Generate unique key if needed
                        string newKey = property.Name;
                        int counter = 1;
                        while (section.HasProperty(newKey) || newProperties.Any(p => p.Name == newKey))
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
                        newProperties.Add(newProperty);
                    }

                    int startIndex = propertyView.SelectedItems.Count > 0
                        ? propertyView.SelectedIndices[0]
                        : section.PropertyCount;

                    var command = new GenericCommand(
                        $"Paste {newProperties.Count} Properties",
                        () =>
                        {
                            for (int i = 0; i < newProperties.Count; i++)
                            {
                                section.InsertProperty(startIndex + i, newProperties[i]);
                            }
                            RefreshKeyValueList(sectionName);
                            RefreshStatusBar();
                        },
                        () =>
                        {
                            foreach (var prop in newProperties)
                            {
                                section.RemoveProperty(prop.Name);
                            }
                            RefreshKeyValueList(sectionName);
                            RefreshStatusBar();
                        });
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
                return;
            }

            // Try to paste single property
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
                    _commandManager.ExecuteCommand(command);
                    SetDirty();
                }
            }
        }

        #endregion

        #region Recent Files

        private void UpdateRecentFilesMenu()
        {
            if (_recentFilesMenuItem == null)
                return;

            _recentFilesMenuItem.DropDownItems.Clear();

            var recentFiles = _recentFilesManager.RecentFiles;

            if (recentFiles.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(No recent files)");
                emptyItem.Enabled = false;
                _recentFilesMenuItem.DropDownItems.Add(emptyItem);
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
                            _recentFilesManager.RemoveRecentFile(path);
                        }
                    }
                };
                _recentFilesMenuItem.DropDownItems.Add(menuItem);
            }

            _recentFilesMenuItem.DropDownItems.Add(new ToolStripSeparator());

            var clearItem = new ToolStripMenuItem("&Clear Recent Files");
            clearItem.Click += (s, e) =>
            {
                _recentFilesManager.ClearRecentFiles();
            };
            _recentFilesMenuItem.DropDownItems.Add(clearItem);
        }

        #endregion

        #region Drag and Drop

        private int _dragSectionIndex = -1;
        private int _dragPropertyIndex = -1;

        private void SetupDragDrop()
        {
            // Section view drag & drop
            sectionView.AllowDrop = true;
            sectionView.MouseDown += SectionView_MouseDown;
            sectionView.MouseMove += SectionView_MouseMove;
            sectionView.DragOver += SectionView_DragOver;
            sectionView.DragDrop += SectionView_DragDrop;

            // Property view drag & drop
            propertyView.AllowDrop = true;
            propertyView.ItemDrag += PropertyView_ItemDrag;
            propertyView.DragOver += PropertyView_DragOver;
            propertyView.DragDrop += PropertyView_DragDrop;
        }

        private void SectionView_MouseDown(object? sender, MouseEventArgs e)
        {
            _dragSectionIndex = sectionView.IndexFromPoint(e.Location);
        }

        private void SectionView_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragSectionIndex < 0)
                return;

            if (sectionView.Items.Count <= 1)
                return;

            var item = sectionView.Items[_dragSectionIndex];
            sectionView.DoDragDrop(item, DragDropEffects.Move);
        }

        private void SectionView_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void SectionView_DragDrop(object? sender, DragEventArgs e)
        {
            if (_documentConfig == null || _dragSectionIndex < 0)
                return;

            var point = sectionView.PointToClient(new Point(e.X, e.Y));
            int targetIndex = sectionView.IndexFromPoint(point);

            if (targetIndex < 0)
                targetIndex = sectionView.Items.Count - 1;

            if (targetIndex == _dragSectionIndex)
                return;

            // Get the section being moved
            var sectionName = sectionView.Items[_dragSectionIndex]?.ToString();
            if (string.IsNullOrEmpty(sectionName))
                return;

            var section = _documentConfig.GetSection(sectionName);
            if (section == null)
                return;

            // Create command for undo/redo
            int oldIndex = _dragSectionIndex;
            int newIndex = targetIndex;
            var command = new GenericCommand(
                $"Move section '{sectionName}'",
                () => MoveSectionToIndex(sectionName, newIndex),
                () => MoveSectionToIndex(sectionName, oldIndex)
            );
            _commandManager.ExecuteCommand(command);

            _dragSectionIndex = -1;
        }

        private void MoveSectionToIndex(string sectionName, int targetIndex)
        {
            if (_documentConfig == null)
                return;

            var section = _documentConfig.GetSection(sectionName);
            if (section == null)
                return;

            // Remove and re-add at new position
            _documentConfig.RemoveSection(sectionName);

            // Insert at the correct position
            var allSections = _documentConfig.ToList();
            if (targetIndex >= allSections.Count)
            {
                _documentConfig.AddSection(section);
            }
            else
            {
                // We need to reorder - remove all and add back in order
                foreach (var s in allSections)
                    _documentConfig.RemoveSection(s.Name);

                allSections.Insert(targetIndex, section);
                foreach (var s in allSections)
                    _documentConfig.AddSection(s);
            }

            RefreshSectionList();
            // Select the moved section
            for (int i = 0; i < sectionView.Items.Count; i++)
            {
                if (sectionView.Items[i]?.ToString() == sectionName)
                {
                    sectionView.SelectedIndex = i;
                    break;
                }
            }
        }

        private void PropertyView_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is ListViewItem item)
            {
                _dragPropertyIndex = item.Index;
                propertyView.DoDragDrop(item, DragDropEffects.Move);
            }
        }

        private void PropertyView_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void PropertyView_DragDrop(object? sender, DragEventArgs e)
        {
            if (_documentConfig == null || _dragPropertyIndex < 0)
                return;

            var point = propertyView.PointToClient(new Point(e.X, e.Y));
            var targetItem = propertyView.GetItemAt(point.X, point.Y);
            int targetIndex = targetItem?.Index ?? propertyView.Items.Count - 1;

            if (targetIndex == _dragPropertyIndex)
                return;

            var section = GetSelectedSection();
            if (section == null)
                return;

            var properties = section.GetProperties().ToList();
            if (_dragPropertyIndex >= properties.Count)
                return;

            var property = properties[_dragPropertyIndex];

            // Create command for undo/redo
            int oldIndex = _dragPropertyIndex;
            int newIndex = targetIndex;
            var command = new GenericCommand(
                $"Move property '{property.Name}'",
                () => MovePropertyToIndex(section.Name, property.Name, newIndex),
                () => MovePropertyToIndex(section.Name, property.Name, oldIndex)
            );
            _commandManager.ExecuteCommand(command);

            _dragPropertyIndex = -1;
        }

        private void MovePropertyToIndex(string sectionName, string propertyName, int targetIndex)
        {
            if (_documentConfig == null)
                return;

            var section = sectionName == ""
                ? _documentConfig.DefaultSection
                : _documentConfig.GetSection(sectionName);

            if (section == null)
                return;

            var property = section.GetProperty(propertyName);
            if (property == null)
                return;

            // Remove and re-add at new position
            var properties = section.GetProperties().ToList();
            var currentIndex = properties.FindIndex(p => p.Name == propertyName);

            if (currentIndex < 0 || currentIndex == targetIndex)
                return;

            // Remove all properties and re-add in new order
            foreach (var p in properties)
                section.RemoveProperty(p.Name);

            properties.RemoveAt(currentIndex);
            if (targetIndex > currentIndex)
                targetIndex--;
            properties.Insert(Math.Min(targetIndex, properties.Count), property);

            foreach (var p in properties)
                section.AddProperty(p);

            RefreshKeyValueList(sectionName);
        }

        private void SetupFileDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
        }

        private void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files?.Length > 0 && files[0].EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files?.Length > 0 && files[0].EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
            {
                if (!PromptSaveChanges())
                    return;

                LoadFile(files[0]);
            }
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
            if (_documentConfig == null)
            {
                MessageBox.Show("No document loaded.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var errors = ValidationHelper.ValidateDocument(_documentConfig);
            using (var dialog = new ValidationDialog(errors))
            {
                dialog.ShowDialog(this);
            }
        }

        private void ShowStatisticsDialog(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
            {
                MessageBox.Show("No document loaded.", "Statistics",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var stats = ValidationHelper.GetStatistics(_documentConfig);
            using (var dialog = new StatisticsDialog(stats))
            {
                dialog.ShowDialog(this);
            }
        }

        private async void ShowCompareDialog(object? sender, EventArgs e)
        {
            if (_documentConfig == null)
            {
                MessageBox.Show("Please open a document first.", "Compare Documents",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using OpenFileDialog openDialog = new()
            {
                Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
                FilterIndex = 1,
                Title = "Select document to compare with"
            };

            if (openDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var otherEncoding = EncodingHelper.DetectEncoding(openDialog.FileName);
                var otherDoc = await IniConfigManager.LoadAsync(openDialog.FileName, otherEncoding, _configOptions);
                var leftTitle = string.IsNullOrEmpty(_currentFilePath) ? "Current Document" : Path.GetFileName(_currentFilePath);
                var rightTitle = Path.GetFileName(openDialog.FileName);

                using var diffViewer = new DiffViewerForm(_documentConfig, otherDoc, leftTitle, rightTitle);
                if (diffViewer.ShowDialog(this) == DialogResult.OK && diffViewer.MergeResult != null)
                {
                    // Changes were merged
                    if (diffViewer.MergeResult.TotalChanges > 0)
                    {
                        _isDirty = true;
                        UpdateTitle();
                        RefreshSectionList();
                        if (sectionView.Items.Count > 0)
                        {
                            sectionView.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading document: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSectionStatistics(object? sender, EventArgs e)
        {
            if (_documentConfig == null || sectionView.SelectedIndex < 0)
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
            if (string.IsNullOrEmpty(_currentFilePath))
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
                Text = $"Current encoding: {EncodingHelper.GetEncodingName(_currentEncoding)}",
                Location = new Point(10, 35),
                Size = new Size(400, 20),
                ForeColor = Color.FromArgb(0, 0, 139) // Dark blue for better contrast
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
                ForeColor = Color.FromArgb(180, 0, 0), // Dark red for better contrast
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
                    EncodingHelper.ConvertFileEncoding(_currentFilePath, _currentEncoding, newEncoding);
                    _currentEncoding = newEncoding;

                    // Reload file
                    LoadFile(_currentFilePath);

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
