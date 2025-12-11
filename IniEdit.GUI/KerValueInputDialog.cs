using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniEdit.GUI
{
    public class KeyValueInputDialog : Form
    {
        private TextBox keyTextBox;
        private TextBox valueTextBox;
        private Button okButton;
        private Button cancelButton;

        public string Key => keyTextBox.Text;
        public string Value => valueTextBox.Text;

        public KeyValueInputDialog(string defaultKey = "", string defaultValue = "")
        {
            Text = "Key-Value Input";
            Size = new Size(300, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var keyLabel = new Label
            {
                Text = "Key:",
                Location = new Point(10, 10),
                Size = new Size(260, 20)
            };

            keyTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(260, 20),
                Text = defaultKey
            };

            var valueLabel = new Label
            {
                Text = "Value:",
                Location = new Point(10, 70),
                Size = new Size(260, 20)
            };

            valueTextBox = new TextBox
            {
                Location = new Point(10, 100),
                Size = new Size(260, 20),
                Text = defaultValue
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(100, 130)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(190, 130)
            };

            Controls.AddRange(new Control[] {
            keyLabel, keyTextBox,
            valueLabel, valueTextBox,
            okButton, cancelButton
        });

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}
