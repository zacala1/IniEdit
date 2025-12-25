using System;
using System.Drawing;
using System.Windows.Forms;

namespace IniEdit.GUI.Theme
{
    /// <summary>
    /// Defines the available application themes.
    /// </summary>
    public enum AppTheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Manages application theming and color schemes.
    /// </summary>
    public static class ThemeManager
    {
        private static AppTheme _currentTheme = AppTheme.Light;

        /// <summary>
        /// Gets the current theme.
        /// </summary>
        public static AppTheme CurrentTheme => _currentTheme;

        /// <summary>
        /// Gets whether dark mode is enabled.
        /// </summary>
        public static bool IsDarkMode => _currentTheme == AppTheme.Dark;

        /// <summary>
        /// Event raised when the theme changes.
        /// </summary>
        public static event EventHandler? ThemeChanged;

        #region Color Definitions

        // Light theme colors
        private static readonly Color LightBackground = Color.White;
        private static readonly Color LightControlBackground = Color.FromArgb(248, 248, 248);
        private static readonly Color LightForeground = Color.Black;
        private static readonly Color LightBorder = Color.FromArgb(200, 200, 200);
        private static readonly Color LightSelection = Color.FromArgb(0, 120, 215);
        private static readonly Color LightSelectionText = Color.White;
        private static readonly Color LightMenuBackground = Color.FromArgb(240, 240, 240);
        private static readonly Color LightStatusBar = Color.FromArgb(240, 240, 240);
        private static readonly Color LightGridLine = Color.FromArgb(224, 224, 224);

        // Dark theme colors
        private static readonly Color DarkBackground = Color.FromArgb(30, 30, 30);
        private static readonly Color DarkControlBackground = Color.FromArgb(45, 45, 48);
        private static readonly Color DarkForeground = Color.FromArgb(220, 220, 220);
        private static readonly Color DarkBorder = Color.FromArgb(62, 62, 66);
        private static readonly Color DarkSelection = Color.FromArgb(51, 153, 255);
        private static readonly Color DarkSelectionText = Color.White;
        private static readonly Color DarkMenuBackground = Color.FromArgb(45, 45, 48);
        private static readonly Color DarkStatusBar = Color.FromArgb(0, 122, 204);
        private static readonly Color DarkGridLine = Color.FromArgb(62, 62, 66);

        #endregion

        #region Color Properties

        public static Color Background => IsDarkMode ? DarkBackground : LightBackground;
        public static Color ControlBackground => IsDarkMode ? DarkControlBackground : LightControlBackground;
        public static Color Foreground => IsDarkMode ? DarkForeground : LightForeground;
        public static Color Border => IsDarkMode ? DarkBorder : LightBorder;
        public static Color Selection => IsDarkMode ? DarkSelection : LightSelection;
        public static Color SelectionText => IsDarkMode ? DarkSelectionText : LightSelectionText;
        public static Color MenuBackground => IsDarkMode ? DarkMenuBackground : LightMenuBackground;
        public static Color StatusBarBackground => IsDarkMode ? DarkStatusBar : LightStatusBar;
        public static Color GridLine => IsDarkMode ? DarkGridLine : LightGridLine;

        #endregion

        /// <summary>
        /// Sets the application theme.
        /// </summary>
        public static void SetTheme(AppTheme theme)
        {
            if (_currentTheme == theme)
                return;

            _currentTheme = theme;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Toggles between light and dark theme.
        /// </summary>
        public static void ToggleTheme()
        {
            SetTheme(IsDarkMode ? AppTheme.Light : AppTheme.Dark);
        }

        /// <summary>
        /// Applies the current theme to a form and all its controls.
        /// </summary>
        public static void ApplyTheme(Form form)
        {
            ApplyThemeToControl(form);

            // Apply to all child controls recursively
            foreach (Control control in form.Controls)
            {
                ApplyThemeRecursive(control);
            }
        }

        private static void ApplyThemeRecursive(Control control)
        {
            ApplyThemeToControl(control);

            foreach (Control child in control.Controls)
            {
                ApplyThemeRecursive(child);
            }
        }

        private static void ApplyThemeToControl(Control control)
        {
            switch (control)
            {
                case Form form:
                    form.BackColor = Background;
                    form.ForeColor = Foreground;
                    break;

                case MenuStrip menuStrip:
                    ApplyThemeToMenuStrip(menuStrip);
                    break;

                case StatusStrip statusStrip:
                    ApplyThemeToStatusStrip(statusStrip);
                    break;

                case ToolStrip toolStrip:
                    ApplyThemeToToolStrip(toolStrip);
                    break;

                case ListView listView:
                    ApplyThemeToListView(listView);
                    break;

                case ListBox listBox:
                    listBox.BackColor = ControlBackground;
                    listBox.ForeColor = Foreground;
                    break;

                case TextBox textBox:
                    textBox.BackColor = ControlBackground;
                    textBox.ForeColor = Foreground;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case RichTextBox richTextBox:
                    richTextBox.BackColor = ControlBackground;
                    richTextBox.ForeColor = Foreground;
                    break;

                case Button button:
                    button.BackColor = ControlBackground;
                    button.ForeColor = Foreground;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Border;
                    break;

                case ComboBox comboBox:
                    comboBox.BackColor = ControlBackground;
                    comboBox.ForeColor = Foreground;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    break;

                case CheckBox checkBox:
                    checkBox.BackColor = Color.Transparent;
                    checkBox.ForeColor = Foreground;
                    break;

                case RadioButton radioButton:
                    radioButton.BackColor = Color.Transparent;
                    radioButton.ForeColor = Foreground;
                    break;

                case Label label:
                    label.BackColor = Color.Transparent;
                    label.ForeColor = Foreground;
                    break;

                case TabPage tabPage:
                    tabPage.BackColor = Background;
                    tabPage.ForeColor = Foreground;
                    break;

                case SplitContainer splitContainer:
                    splitContainer.BackColor = Background;
                    splitContainer.ForeColor = Foreground;
                    break;

                case GroupBox groupBox:
                    groupBox.BackColor = Background;
                    groupBox.ForeColor = Foreground;
                    break;

                case TabControl tabControl:
                    tabControl.BackColor = Background;
                    tabControl.ForeColor = Foreground;
                    break;

                case Panel panel:
                    panel.BackColor = Background;
                    panel.ForeColor = Foreground;
                    break;

                case TreeView treeView:
                    treeView.BackColor = ControlBackground;
                    treeView.ForeColor = Foreground;
                    treeView.LineColor = Border;
                    break;

                case DataGridView dataGridView:
                    ApplyThemeToDataGridView(dataGridView);
                    break;

                default:
                    control.BackColor = Background;
                    control.ForeColor = Foreground;
                    break;
            }
        }

        private static void ApplyThemeToMenuStrip(MenuStrip menuStrip)
        {
            menuStrip.BackColor = MenuBackground;
            menuStrip.ForeColor = Foreground;
            menuStrip.Renderer = new ThemeMenuRenderer();

            foreach (ToolStripItem item in menuStrip.Items)
            {
                ApplyThemeToToolStripItem(item);
            }
        }

        private static void ApplyThemeToToolStrip(ToolStrip toolStrip)
        {
            toolStrip.BackColor = MenuBackground;
            toolStrip.ForeColor = Foreground;
            toolStrip.Renderer = new ThemeMenuRenderer();

            foreach (ToolStripItem item in toolStrip.Items)
            {
                ApplyThemeToToolStripItem(item);
            }
        }

        private static void ApplyThemeToStatusStrip(StatusStrip statusStrip)
        {
            statusStrip.BackColor = StatusBarBackground;
            statusStrip.ForeColor = IsDarkMode ? Color.White : Foreground;
            statusStrip.Renderer = new ThemeMenuRenderer();

            foreach (ToolStripItem item in statusStrip.Items)
            {
                item.BackColor = StatusBarBackground;
                item.ForeColor = IsDarkMode ? Color.White : Foreground;
            }
        }

        private static void ApplyThemeToToolStripItem(ToolStripItem item)
        {
            item.BackColor = MenuBackground;
            item.ForeColor = Foreground;

            if (item is ToolStripDropDownItem dropDownItem)
            {
                foreach (ToolStripItem subItem in dropDownItem.DropDownItems)
                {
                    ApplyThemeToToolStripItem(subItem);
                }
            }
        }

        private static void ApplyThemeToListView(ListView listView)
        {
            listView.BackColor = ControlBackground;
            listView.ForeColor = Foreground;

            if (listView.OwnerDraw)
                return;

            // Configure owner-draw for better theming
            listView.OwnerDraw = true;
            listView.DrawColumnHeader += ListView_DrawColumnHeader;
            listView.DrawItem += ListView_DrawItem;
            listView.DrawSubItem += ListView_DrawSubItem;
        }

        private static void ListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using var brush = new SolidBrush(MenuBackground);
            e.Graphics.FillRectangle(brush, e.Bounds);

            using var textBrush = new SolidBrush(Foreground);
            using var format = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near
            };

            var textRect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            e.Graphics.DrawString(e.Header?.Text ?? "", e.Font ?? SystemFonts.DefaultFont, textBrush, textRect, format);

            using var borderPen = new Pen(Border);
            e.Graphics.DrawLine(borderPen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
            e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private static void ListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private static void ListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private static void ApplyThemeToDataGridView(DataGridView dataGridView)
        {
            dataGridView.BackgroundColor = ControlBackground;
            dataGridView.ForeColor = Foreground;
            dataGridView.GridColor = GridLine;
            dataGridView.DefaultCellStyle.BackColor = ControlBackground;
            dataGridView.DefaultCellStyle.ForeColor = Foreground;
            dataGridView.DefaultCellStyle.SelectionBackColor = Selection;
            dataGridView.DefaultCellStyle.SelectionForeColor = SelectionText;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = MenuBackground;
            dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Foreground;
            dataGridView.EnableHeadersVisualStyles = false;
        }
    }

    /// <summary>
    /// Custom renderer for themed menus and toolstrips.
    /// </summary>
    internal sealed class ThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemeMenuRenderer() : base(new ThemeColorTable())
        {
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = ThemeManager.Foreground;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = ThemeManager.Foreground;
            base.OnRenderArrow(e);
        }
    }

    /// <summary>
    /// Custom color table for themed menus.
    /// </summary>
    internal sealed class ThemeColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => ThemeManager.Border;
        public override Color MenuItemBorder => ThemeManager.Border;
        public override Color MenuItemSelected => ThemeManager.IsDarkMode
            ? Color.FromArgb(62, 62, 64)
            : Color.FromArgb(200, 210, 230);
        public override Color MenuItemSelectedGradientBegin => MenuItemSelected;
        public override Color MenuItemSelectedGradientEnd => MenuItemSelected;
        public override Color MenuItemPressedGradientBegin => ThemeManager.Selection;
        public override Color MenuItemPressedGradientEnd => ThemeManager.Selection;
        public override Color MenuStripGradientBegin => ThemeManager.MenuBackground;
        public override Color MenuStripGradientEnd => ThemeManager.MenuBackground;
        public override Color ToolStripDropDownBackground => ThemeManager.MenuBackground;
        public override Color ImageMarginGradientBegin => ThemeManager.MenuBackground;
        public override Color ImageMarginGradientMiddle => ThemeManager.MenuBackground;
        public override Color ImageMarginGradientEnd => ThemeManager.MenuBackground;
        public override Color SeparatorDark => ThemeManager.Border;
        public override Color SeparatorLight => ThemeManager.Border;
        public override Color StatusStripGradientBegin => ThemeManager.StatusBarBackground;
        public override Color StatusStripGradientEnd => ThemeManager.StatusBarBackground;
        public override Color ToolStripGradientBegin => ThemeManager.MenuBackground;
        public override Color ToolStripGradientMiddle => ThemeManager.MenuBackground;
        public override Color ToolStripGradientEnd => ThemeManager.MenuBackground;
        public override Color ToolStripBorder => ThemeManager.Border;
        public override Color ToolStripContentPanelGradientBegin => ThemeManager.Background;
        public override Color ToolStripContentPanelGradientEnd => ThemeManager.Background;
    }
}
