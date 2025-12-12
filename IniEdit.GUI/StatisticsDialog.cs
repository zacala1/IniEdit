using System;
using System.Drawing;
using System.Windows.Forms;

namespace IniEdit.GUI
{
    public class StatisticsDialog : Form
    {
        private readonly TextBox _statisticsTextBox;
        private readonly Button _closeButton;
        private readonly Button _copyButton;

        public StatisticsDialog(DocumentStatistics stats)
        {
            Text = "Document Statistics";
            Size = new Size(450, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Statistics display
            _statisticsTextBox = new TextBox
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
            _copyButton = new Button
            {
                Text = "Copy to Clipboard",
                Location = new Point(10, 320),
                Size = new Size(130, 30)
            };
            _copyButton.Click += (s, e) =>
            {
                Clipboard.SetText(_statisticsTextBox.Text);
                MessageBox.Show("Statistics copied to clipboard!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Close button
            _closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Location = new Point(290, 320),
                Size = new Size(130, 30)
            };

            Controls.AddRange(new Control[] { _statisticsTextBox, _copyButton, _closeButton });
            AcceptButton = _closeButton;
            CancelButton = _closeButton;
        }
    }
}
