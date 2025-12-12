using System;
using System.Drawing;
using System.Windows.Forms;
using IniEdit;

namespace IniEdit.GUI
{
    public class SectionStatisticsDialog : Form
    {
        private readonly TextBox _statisticsTextBox;
        private readonly Button _closeButton;
        private readonly Button _copyButton;

        public SectionStatisticsDialog(Section section, string sectionName)
        {
            Text = $"Statistics - {sectionName}";
            Size = new Size(500, 450);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Calculate statistics
            var stats = CalculateStatistics(section, sectionName);

            // Statistics display
            _statisticsTextBox = new TextBox
            {
                Location = new Point(10, 10),
                Size = new Size(460, 350),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                Text = stats
            };

            // Copy button
            _copyButton = new Button
            {
                Text = "Copy to Clipboard",
                Location = new Point(10, 370),
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
                Location = new Point(340, 370),
                Size = new Size(130, 30)
            };

            Controls.AddRange(new Control[] { _statisticsTextBox, _copyButton, _closeButton });
            AcceptButton = _closeButton;
            CancelButton = _closeButton;
        }

        private string CalculateStatistics(Section section, string sectionName)
        {
            var result = $"Section Statistics: [{sectionName}]\n";
            result += "=".PadRight(50, '=') + "\n\n";

            // Basic counts
            result += $"Total Properties:      {section.PropertyCount}\n\n";

            // Value analysis
            int quotedCount = 0;
            int unquotedCount = 0;
            int emptyValues = 0;
            int numericValues = 0;
            int booleanValues = 0;

            foreach (var prop in section)
            {
                if (prop.IsQuoted)
                    quotedCount++;
                else
                    unquotedCount++;

                if (string.IsNullOrEmpty(prop.Value))
                    emptyValues++;

                if (double.TryParse(prop.Value, out _))
                    numericValues++;

                if (bool.TryParse(prop.Value, out _))
                    booleanValues++;
            }

            result += "Value Types:\n";
            result += $"  Quoted values:       {quotedCount}\n";
            result += $"  Unquoted values:     {unquotedCount}\n";
            result += $"  Empty values:        {emptyValues}\n";
            result += $"  Numeric values:      {numericValues}\n";
            result += $"  Boolean values:      {booleanValues}\n\n";

            // Comment analysis
            int preCommentsCount = section.PreComments.Count;
            int inlineCommentsCount = section.Comment != null ? 1 : 0;
            int propertyComments = 0;
            int propertyPreComments = 0;

            foreach (var prop in section)
            {
                propertyPreComments += prop.PreComments.Count;
                if (prop.Comment != null)
                    propertyComments++;
            }

            int totalComments = preCommentsCount + inlineCommentsCount + propertyComments + propertyPreComments;

            result += "Comments:\n";
            result += $"  Section pre-comments:    {preCommentsCount}\n";
            result += $"  Section inline comment:  {inlineCommentsCount}\n";
            result += $"  Property pre-comments:   {propertyPreComments}\n";
            result += $"  Property inline comments: {propertyComments}\n";
            result += $"  Total comments:          {totalComments}\n\n";

            // Key analysis
            var duplicates = ValidationHelper.GetDuplicateKeys(section);
            result += "Validation:\n";
            result += $"  Duplicate keys:      {duplicates.Count}\n";
            if (duplicates.Count > 0)
            {
                result += "  Duplicates found:\n";
                foreach (var dup in duplicates)
                {
                    result += $"    - {dup}\n";
                }
            }
            result += "\n";

            // Key length statistics
            if (section.PropertyCount > 0)
            {
                int minKeyLength = int.MaxValue;
                int maxKeyLength = 0;
                int totalKeyLength = 0;
                int minValueLength = int.MaxValue;
                int maxValueLength = 0;
                int totalValueLength = 0;

                foreach (var prop in section)
                {
                    int keyLen = prop.Name.Length;
                    int valLen = prop.Value.Length;

                    minKeyLength = Math.Min(minKeyLength, keyLen);
                    maxKeyLength = Math.Max(maxKeyLength, keyLen);
                    totalKeyLength += keyLen;

                    minValueLength = Math.Min(minValueLength, valLen);
                    maxValueLength = Math.Max(maxValueLength, valLen);
                    totalValueLength += valLen;
                }

                double avgKeyLength = (double)totalKeyLength / section.PropertyCount;
                double avgValueLength = (double)totalValueLength / section.PropertyCount;

                result += "Key/Value Lengths:\n";
                result += $"  Shortest key:        {minKeyLength} chars\n";
                result += $"  Longest key:         {maxKeyLength} chars\n";
                result += $"  Average key length:  {avgKeyLength:F1} chars\n";
                result += $"  Shortest value:      {minValueLength} chars\n";
                result += $"  Longest value:       {maxValueLength} chars\n";
                result += $"  Average value length: {avgValueLength:F1} chars\n";
            }

            return result;
        }
    }
}
