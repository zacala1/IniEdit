using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace IniEdit.GUI
{
    public class ValidationDialog : Form
    {
        private readonly ListView errorListView;
        private readonly Button closeButton;
        private readonly Button copyButton;
        private readonly Label summaryLabel;

        public ValidationDialog(List<ValidationHelper.ValidationError> errors)
        {
            Text = "Validation Results";
            Size = new Size(700, 500);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            // Summary label
            summaryLabel = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(660, 30),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };

            if (errors.Count == 0)
            {
                summaryLabel.Text = "✓ No validation errors found!";
                summaryLabel.ForeColor = Color.Green;
            }
            else
            {
                summaryLabel.Text = $"⚠ Found {errors.Count} validation error(s)";
                summaryLabel.ForeColor = Color.Red;
            }

            // Error list view
            errorListView = new ListView
            {
                Location = new Point(10, 45),
                Size = new Size(660, 360),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            errorListView.Columns.Add("Type", 120);
            errorListView.Columns.Add("Section", 150);
            errorListView.Columns.Add("Property", 150);
            errorListView.Columns.Add("Message", 200);

            foreach (var error in errors)
            {
                var item = errorListView.Items.Add(error.Type.ToString());
                item.SubItems.Add(error.SectionName);
                item.SubItems.Add(error.PropertyName ?? "");
                item.SubItems.Add(error.Message);

                // Color code by error type
                switch (error.Type)
                {
                    case ValidationHelper.ValidationErrorType.DuplicateKey:
                        item.BackColor = Color.LightCoral;
                        break;
                    case ValidationHelper.ValidationErrorType.EmptyKey:
                    case ValidationHelper.ValidationErrorType.EmptyValue:
                        item.BackColor = Color.LightYellow;
                        break;
                    case ValidationHelper.ValidationErrorType.InvalidCharacters:
                        item.BackColor = Color.LightSalmon;
                        break;
                }
            }

            // Copy button
            copyButton = new Button
            {
                Text = "Copy All",
                Location = new Point(10, 415),
                Size = new Size(100, 30)
            };
            copyButton.Click += (s, e) => CopyErrorsToClipboard(errors);

            // Close button
            closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(570, 415),
                Size = new Size(100, 30)
            };

            Controls.AddRange(new Control[] {
                summaryLabel,
                errorListView,
                copyButton,
                closeButton
            });

            AcceptButton = closeButton;
            CancelButton = closeButton;
        }

        private void CopyErrorsToClipboard(List<ValidationHelper.ValidationError> errors)
        {
            if (errors.Count == 0)
            {
                Clipboard.SetText("No validation errors found.");
            }
            else
            {
                var text = $"Validation Errors ({errors.Count}):\n\n";
                foreach (var error in errors)
                {
                    text += $"{error.Type}: {error}\n";
                }
                Clipboard.SetText(text);
            }

            MessageBox.Show("Errors copied to clipboard!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
