using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniEdit.GUI
{
    public class InputDialog : Form
    {
        private TextBox textBox;
        private Button okButton;
        private Button cancelButton;
        private Label label;

        public string InputText => textBox.Text;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            Text = title;
            Size = new Size(300, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            label = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(260, 20)
            };

            textBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(260, 20),
                Text = defaultValue
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(100, 70)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(190, 70)
            };

            Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}
