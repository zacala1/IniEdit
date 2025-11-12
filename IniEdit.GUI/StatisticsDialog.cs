using System;
using System.Drawing;
using System.Windows.Forms;

namespace IniEdit.GUI
{
    public class StatisticsDialog : Form
    {
        private readonly TextBox statisticsTextBox;
        private readonly Button closeButton;
        private readonly Button copyButton;

        public StatisticsDialog(DocumentStatistics stats)
        {
            Text = "Document Statistics";
            Size = new Size(450, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Statistics display
            statisticsTextBox = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(410, 300),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                Text = stats.ToString()
            };

            // Copy button
            copyButton = new Button
            {
                Text = "Copy to Clipboard",
                Location = new Point(10, 320),
                Size = new Size(130, 30)
            };
            copyButton.Click += (s, e) =>
            {
                Clipboard.SetText(statisticsTextBox.Text);
                MessageBox.Show("Statistics copied to clipboard!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Close button
            closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(290, 320),
                Size = new Size(130, 30)
            };

            Controls.AddRange(new Control[] { statisticsTextBox, copyButton, closeButton });
            AcceptButton = closeButton;
            CancelButton = closeButton;
        }
    }
}
