using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using IniEdit;

namespace IniEdit.GUI
{
    public class SettingsDialog : Form
    {
        private ComboBox duplicateKeyPolicyCombo = null!;
        private ComboBox duplicateSectionPolicyCombo = null!;
        private TextBox commentPrefixCharsTextBox = null!;
        private ComboBox defaultCommentPrefixCombo = null!;
        private CheckBox collectParsingErrorsCheckBox = null!;
        private Button okButton = null!;
        private Button cancelButton = null!;

        public IniConfigOption Options { get; private set; }

        public SettingsDialog(IniConfigOption currentOptions)
        {
            Options = new IniConfigOption
            {
                CommentPrefixChars = currentOptions.CommentPrefixChars.ToArray(),
                DefaultCommentPrefixChar = currentOptions.DefaultCommentPrefixChar,
                DuplicateKeyPolicy = currentOptions.DuplicateKeyPolicy,
                DuplicateSectionPolicy = currentOptions.DuplicateSectionPolicy,
                CollectParsingErrors = currentOptions.CollectParsingErrors
            };

            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            Text = "Settings";
            Size = new System.Drawing.Size(450, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 20;
            int labelWidth = 180;
            int controlX = labelWidth + 20;
            int controlWidth = 200;

            // Duplicate Key Policy
            var duplicateKeyLabel = new Label
            {
                Text = "Duplicate Key Policy:",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(labelWidth, 20)
            };

            duplicateKeyPolicyCombo = new ComboBox
            {
                Location = new System.Drawing.Point(controlX, y),
                Size = new System.Drawing.Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            duplicateKeyPolicyCombo.Items.AddRange(new object[]
            {
                IniConfigOption.DuplicateKeyPolicyType.FirstWin,
                IniConfigOption.DuplicateKeyPolicyType.LastWin,
                IniConfigOption.DuplicateKeyPolicyType.ThrowError
            });

            y += 35;

            // Duplicate Section Policy
            var duplicateSectionLabel = new Label
            {
                Text = "Duplicate Section Policy:",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(labelWidth, 20)
            };

            duplicateSectionPolicyCombo = new ComboBox
            {
                Location = new System.Drawing.Point(controlX, y),
                Size = new System.Drawing.Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            duplicateSectionPolicyCombo.Items.AddRange(new object[]
            {
                IniConfigOption.DuplicateSectionPolicyType.FirstWin,
                IniConfigOption.DuplicateSectionPolicyType.LastWin,
                IniConfigOption.DuplicateSectionPolicyType.Merge,
                IniConfigOption.DuplicateSectionPolicyType.ThrowError
            });

            y += 35;

            // Comment Prefix Chars
            var commentPrefixLabel = new Label
            {
                Text = "Comment Prefix Characters:",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(labelWidth, 20)
            };

            commentPrefixCharsTextBox = new TextBox
            {
                Location = new System.Drawing.Point(controlX, y),
                Size = new System.Drawing.Size(controlWidth, 20)
            };

            var commentPrefixHint = new Label
            {
                Text = "(Comma-separated, e.g., ';,#')",
                Location = new System.Drawing.Point(controlX, y + 25),
                Size = new System.Drawing.Size(controlWidth, 20),
                ForeColor = System.Drawing.Color.Gray,
                Font = new System.Drawing.Font(Font.FontFamily, 7.5f)
            };

            y += 60;

            // Default Comment Prefix Char
            var defaultCommentLabel = new Label
            {
                Text = "Default Comment Prefix:",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(labelWidth, 20)
            };

            defaultCommentPrefixCombo = new ComboBox
            {
                Location = new System.Drawing.Point(controlX, y),
                Size = new System.Drawing.Size(controlWidth, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            commentPrefixCharsTextBox.TextChanged += (s, e) => UpdateDefaultCommentPrefixOptions();

            y += 35;

            // Collect Parsing Errors
            collectParsingErrorsCheckBox = new CheckBox
            {
                Text = "Collect Parsing Errors (instead of stopping on first error)",
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(400, 20)
            };

            y += 40;

            // Description Panel
            var descriptionPanel = new Panel
            {
                Location = new System.Drawing.Point(20, y),
                Size = new System.Drawing.Size(390, 80),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.FromArgb(255, 255, 240)
            };

            var descriptionLabel = new Label
            {
                Text = "Note: These settings will be used when loading INI files.\n" +
                       "• FirstWin: Keep first occurrence\n" +
                       "• LastWin: Keep last occurrence\n" +
                       "• Merge: Combine all sections with same name\n" +
                       "• ThrowError: Show error on duplicates",
                Location = new System.Drawing.Point(5, 5),
                Size = new System.Drawing.Size(380, 70),
                Font = new System.Drawing.Font(Font.FontFamily, 7.5f)
            };

            descriptionPanel.Controls.Add(descriptionLabel);

            y += 90;

            // Buttons
            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(controlX, y),
                Size = new System.Drawing.Size(90, 30)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(controlX + 100, y),
                Size = new System.Drawing.Size(90, 30)
            };

            okButton.Click += OkButton_Click;

            Controls.AddRange(new Control[]
            {
                duplicateKeyLabel, duplicateKeyPolicyCombo,
                duplicateSectionLabel, duplicateSectionPolicyCombo,
                commentPrefixLabel, commentPrefixCharsTextBox, commentPrefixHint,
                defaultCommentLabel, defaultCommentPrefixCombo,
                collectParsingErrorsCheckBox,
                descriptionPanel,
                okButton, cancelButton
            });

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void LoadSettings()
        {
            duplicateKeyPolicyCombo.SelectedItem = Options.DuplicateKeyPolicy;
            duplicateSectionPolicyCombo.SelectedItem = Options.DuplicateSectionPolicy;
            commentPrefixCharsTextBox.Text = string.Join(",", Options.CommentPrefixChars);
            collectParsingErrorsCheckBox.Checked = Options.CollectParsingErrors;

            UpdateDefaultCommentPrefixOptions();
            defaultCommentPrefixCombo.SelectedItem = Options.DefaultCommentPrefixChar;
        }

        private void UpdateDefaultCommentPrefixOptions()
        {
            var currentSelection = defaultCommentPrefixCombo.SelectedItem;
            defaultCommentPrefixCombo.Items.Clear();

            var chars = commentPrefixCharsTextBox.Text
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length == 1)
                .Select(s => s[0])
                .ToArray();

            foreach (var ch in chars)
            {
                defaultCommentPrefixCombo.Items.Add(ch);
            }

            if (currentSelection != null && defaultCommentPrefixCombo.Items.Contains(currentSelection))
            {
                defaultCommentPrefixCombo.SelectedItem = currentSelection;
            }
            else if (defaultCommentPrefixCombo.Items.Count > 0)
            {
                defaultCommentPrefixCombo.SelectedIndex = 0;
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Validate and save settings
                var commentChars = commentPrefixCharsTextBox.Text
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length == 1)
                    .Select(s => s[0])
                    .ToArray();

                if (commentChars.Length == 0)
                {
                    MessageBox.Show("Please specify at least one comment prefix character.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    commentPrefixCharsTextBox.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }

                if (defaultCommentPrefixCombo.SelectedItem == null)
                {
                    MessageBox.Show("Please select a default comment prefix character.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    defaultCommentPrefixCombo.Focus();
                    DialogResult = DialogResult.None;
                    return;
                }

                Options.CommentPrefixChars = commentChars;
                Options.DefaultCommentPrefixChar = (char)defaultCommentPrefixCombo.SelectedItem!;
                Options.DuplicateKeyPolicy = (IniConfigOption.DuplicateKeyPolicyType)duplicateKeyPolicyCombo.SelectedItem!;
                Options.DuplicateSectionPolicy = (IniConfigOption.DuplicateSectionPolicyType)duplicateSectionPolicyCombo.SelectedItem!;
                Options.CollectParsingErrors = collectParsingErrorsCheckBox.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
            }
        }
    }
}
