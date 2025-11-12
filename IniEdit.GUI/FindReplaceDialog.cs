using System;
using System.Drawing;
using System.Windows.Forms;

namespace IniEdit.GUI
{
    public class FindReplaceDialog : Form
    {
        private TextBox findTextBox = null!;
        private TextBox replaceTextBox = null!;
        private CheckBox matchCaseCheckBox = null!;
        private CheckBox useRegexCheckBox = null!;
        private CheckBox searchSectionsCheckBox = null!;
        private CheckBox searchKeysCheckBox = null!;
        private CheckBox searchValuesCheckBox = null!;
        private Button findNextButton = null!;
        private Button replaceButton = null!;
        private Button replaceAllButton = null!;
        private Button closeButton = null!;
        private Label statusLabel = null!;

        public string FindText => findTextBox.Text;
        public string ReplaceText => replaceTextBox.Text;
        public bool MatchCase => matchCaseCheckBox.Checked;
        public bool UseRegex => useRegexCheckBox.Checked;
        public bool SearchSections => searchSectionsCheckBox.Checked;
        public bool SearchKeys => searchKeysCheckBox.Checked;
        public bool SearchValues => searchValuesCheckBox.Checked;

        public event EventHandler? FindNextClicked;
        public event EventHandler? ReplaceClicked;
        public event EventHandler? ReplaceAllClicked;

        public FindReplaceDialog()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "Find and Replace";
            Size = new Size(450, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 20;

            // Find
            var findLabel = new Label
            {
                Text = "Find what:",
                Location = new Point(20, y),
                Size = new Size(80, 20)
            };

            findTextBox = new TextBox
            {
                Location = new Point(110, y),
                Size = new Size(300, 20)
            };

            y += 35;

            // Replace
            var replaceLabel = new Label
            {
                Text = "Replace with:",
                Location = new Point(20, y),
                Size = new Size(80, 20)
            };

            replaceTextBox = new TextBox
            {
                Location = new Point(110, y),
                Size = new Size(300, 20)
            };

            y += 35;

            // Options GroupBox
            var optionsGroup = new GroupBox
            {
                Text = "Options",
                Location = new Point(20, y),
                Size = new Size(390, 130)
            };

            matchCaseCheckBox = new CheckBox
            {
                Text = "Match case",
                Location = new Point(10, 20),
                Size = new Size(180, 20)
            };

            useRegexCheckBox = new CheckBox
            {
                Text = "Use regular expressions",
                Location = new Point(10, 45),
                Size = new Size(180, 20)
            };
            useRegexCheckBox.CheckedChanged += OnRegexCheckChanged;

            var searchInLabel = new Label
            {
                Text = "Search in:",
                Location = new Point(10, 75),
                Size = new Size(80, 20),
                Font = new Font(Font, FontStyle.Bold)
            };

            searchSectionsCheckBox = new CheckBox
            {
                Text = "Section names",
                Location = new Point(100, 75),
                Size = new Size(120, 20),
                Checked = true
            };

            searchKeysCheckBox = new CheckBox
            {
                Text = "Key names",
                Location = new Point(220, 75),
                Size = new Size(100, 20),
                Checked = true
            };

            searchValuesCheckBox = new CheckBox
            {
                Text = "Values",
                Location = new Point(320, 75),
                Size = new Size(60, 20),
                Checked = true
            };

            optionsGroup.Controls.AddRange(new Control[]
            {
                matchCaseCheckBox,
                useRegexCheckBox,
                searchInLabel,
                searchSectionsCheckBox,
                searchKeysCheckBox,
                searchValuesCheckBox
            });

            y += 140;

            // Status Label
            statusLabel = new Label
            {
                Text = "",
                Location = new Point(20, y),
                Size = new Size(390, 20),
                ForeColor = Color.Blue
            };

            y += 30;

            // Buttons
            findNextButton = new Button
            {
                Text = "Find Next",
                Location = new Point(20, y),
                Size = new Size(90, 30)
            };
            findNextButton.Click += (s, e) =>
            {
                if (ValidateSearch())
                    FindNextClicked?.Invoke(this, EventArgs.Empty);
            };

            replaceButton = new Button
            {
                Text = "Replace",
                Location = new Point(120, y),
                Size = new Size(90, 30)
            };
            replaceButton.Click += (s, e) =>
            {
                if (ValidateSearch())
                    ReplaceClicked?.Invoke(this, EventArgs.Empty);
            };

            replaceAllButton = new Button
            {
                Text = "Replace All",
                Location = new Point(220, y),
                Size = new Size(90, 30)
            };
            replaceAllButton.Click += (s, e) =>
            {
                if (ValidateSearch())
                    ReplaceAllClicked?.Invoke(this, EventArgs.Empty);
            };

            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(320, y),
                Size = new Size(90, 30)
            };
            closeButton.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            {
                findLabel, findTextBox,
                replaceLabel, replaceTextBox,
                optionsGroup,
                statusLabel,
                findNextButton, replaceButton, replaceAllButton, closeButton
            });

            findTextBox.TextChanged += (s, e) => statusLabel.Text = "";
            CancelButton = closeButton;
        }

        private void OnRegexCheckChanged(object? sender, EventArgs e)
        {
            if (useRegexCheckBox.Checked)
            {
                SetStatus("Regex enabled. Examples: \\d+ (numbers), [a-z]+ (lowercase), .* (anything)", false);
            }
            else
            {
                ClearStatus();
            }
        }

        private bool ValidateSearch()
        {
            if (string.IsNullOrWhiteSpace(findTextBox.Text))
            {
                MessageBox.Show("Please enter text to find.", "Find",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                findTextBox.Focus();
                return false;
            }

            if (!SearchSections && !SearchKeys && !SearchValues)
            {
                MessageBox.Show("Please select at least one search location.", "Find",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Validate regex pattern if regex is enabled
            if (UseRegex)
            {
                try
                {
                    _ = new System.Text.RegularExpressions.Regex(findTextBox.Text);
                }
                catch (System.ArgumentException ex)
                {
                    MessageBox.Show($"Invalid regular expression:\n{ex.Message}", "Regex Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    findTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        public void SetStatus(string message, bool isError = false)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = isError ? Color.Red : Color.Blue;
        }

        public void ClearStatus()
        {
            statusLabel.Text = "";
        }
    }
}
