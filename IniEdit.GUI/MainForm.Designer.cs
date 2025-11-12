using System.Windows.Forms;

namespace IniEdit.GUI
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel sectionStatusLabel;
        private ToolStripStatusLabel keyStatusLabel;
        private ToolStripStatusLabel filePathStatusLabel;
        private ListBox sectionView;
        private ListView propertyView;
        private TextBox preCommentsTextBox;
        private TextBox inlineCommentTextBox;
        private Label commentTypeLabel1;
        private Label commentTypeLabel2;
        private Label sectionViewLabel;
        private Label propertyViewLabel;
        private Panel mainPanel;
        private ColumnHeader keyHeader;
        private ColumnHeader valueHeader;
        private Panel commentPanel;

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            mainPanel = new Panel();
            splitContainer1 = new SplitContainer();
            sectionView = new ListBox();
            sectionViewLabel = new Label();
            splitContainer2 = new SplitContainer();
            propertyView = new ListView();
            keyHeader = new ColumnHeader();
            valueHeader = new ColumnHeader();
            propertyViewLabel = new Label();
            commentPanel = new Panel();
            preCommentsTextBox = new TextBox();
            commentTypeLabel1 = new Label();
            inlineCommentTextBox = new TextBox();
            commentTypeLabel2 = new Label();
            statusStrip1 = new StatusStrip();
            sectionStatusLabel = new ToolStripStatusLabel();
            keyStatusLabel = new ToolStripStatusLabel();
            filePathStatusLabel = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            commentPanel.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(784, 24);
            menuStrip1.TabIndex = 1;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newToolStripMenuItem, openToolStripMenuItem, saveToolStripMenuItem, saveAsToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;
            newToolStripMenuItem.Size = new Size(186, 22);
            newToolStripMenuItem.Text = "&New";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(186, 22);
            openToolStripMenuItem.Text = "&Open";
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveToolStripMenuItem.Size = new Size(186, 22);
            saveToolStripMenuItem.Text = "&Save";
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            saveAsToolStripMenuItem.Size = new Size(186, 22);
            saveAsToolStripMenuItem.Text = "Save &As...";
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(splitContainer1);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 24);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(784, 515);
            mainPanel.TabIndex = 0;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(sectionView);
            splitContainer1.Panel1.Controls.Add(sectionViewLabel);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Size = new Size(784, 515);
            splitContainer1.SplitterDistance = 121;
            splitContainer1.TabIndex = 0;
            // 
            // sectionView
            // 
            sectionView.Dock = DockStyle.Fill;
            sectionView.Font = new Font("Segoe UI", 9F);
            sectionView.HorizontalScrollbar = true;
            sectionView.IntegralHeight = false;
            sectionView.ItemHeight = 15;
            sectionView.Location = new Point(0, 21);
            sectionView.Name = "sectionView";
            sectionView.Size = new Size(121, 494);
            sectionView.TabIndex = 0;
            // 
            // sectionViewLabel
            // 
            sectionViewLabel.AutoSize = true;
            sectionViewLabel.Dock = DockStyle.Top;
            sectionViewLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            sectionViewLabel.Location = new Point(0, 0);
            sectionViewLabel.Name = "sectionViewLabel";
            sectionViewLabel.Padding = new Padding(3);
            sectionViewLabel.Size = new Size(86, 21);
            sectionViewLabel.TabIndex = 1;
            sectionViewLabel.Text = "Section View";
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(propertyView);
            splitContainer2.Panel1.Controls.Add(propertyViewLabel);
            splitContainer2.Panel1MinSize = 200;
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(commentPanel);
            splitContainer2.Panel2MinSize = 100;
            splitContainer2.Size = new Size(659, 515);
            splitContainer2.SplitterDistance = 364;
            splitContainer2.TabIndex = 0;
            // 
            // propertyView
            // 
            propertyView.Columns.AddRange(new ColumnHeader[] { keyHeader, valueHeader });
            propertyView.Dock = DockStyle.Fill;
            propertyView.FullRowSelect = true;
            propertyView.GridLines = true;
            propertyView.Location = new Point(0, 21);
            propertyView.Name = "propertyView";
            propertyView.Size = new Size(659, 343);
            propertyView.TabIndex = 0;
            propertyView.UseCompatibleStateImageBehavior = false;
            propertyView.View = View.Details;
            // 
            // keyHeader
            // 
            keyHeader.Text = "Key";
            keyHeader.Width = 200;
            // 
            // valueHeader
            // 
            valueHeader.Text = "Value";
            valueHeader.Width = 350;
            // 
            // propertyViewLabel
            // 
            propertyViewLabel.AutoSize = true;
            propertyViewLabel.Dock = DockStyle.Top;
            propertyViewLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            propertyViewLabel.Location = new Point(0, 0);
            propertyViewLabel.Name = "propertyViewLabel";
            propertyViewLabel.Padding = new Padding(3);
            propertyViewLabel.Size = new Size(93, 21);
            propertyViewLabel.TabIndex = 1;
            propertyViewLabel.Text = "Property View";
            //
            // commentPanel
            //
            commentPanel.Controls.Add(preCommentsTextBox);
            commentPanel.Controls.Add(commentTypeLabel1);
            commentPanel.Controls.Add(inlineCommentTextBox);
            commentPanel.Controls.Add(commentTypeLabel2);
            commentPanel.Dock = DockStyle.Fill;
            commentPanel.Location = new Point(0, 0);
            commentPanel.Name = "commentPanel";
            commentPanel.Size = new Size(659, 147);
            commentPanel.TabIndex = 0;
            //
            // preCommentsTextBox
            //
            preCommentsTextBox.Dock = DockStyle.Fill;
            preCommentsTextBox.Font = new Font("Consolas", 9F);
            preCommentsTextBox.Location = new Point(0, 64);
            preCommentsTextBox.Multiline = true;
            preCommentsTextBox.Name = "preCommentsTextBox";
            preCommentsTextBox.ScrollBars = ScrollBars.Vertical;
            preCommentsTextBox.Size = new Size(659, 83);
            preCommentsTextBox.TabIndex = 0;
            // 
            // commentTypeLabel1
            // 
            commentTypeLabel1.AutoSize = true;
            commentTypeLabel1.Dock = DockStyle.Top;
            commentTypeLabel1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            commentTypeLabel1.Location = new Point(0, 43);
            commentTypeLabel1.Name = "commentTypeLabel1";
            commentTypeLabel1.Padding = new Padding(3);
            commentTypeLabel1.Size = new Size(163, 21);
            commentTypeLabel1.TabIndex = 1;
            commentTypeLabel1.Text = "Pre Comments (multi lines)";
            //
            // inlineCommentTextBox
            //
            inlineCommentTextBox.Dock = DockStyle.Top;
            inlineCommentTextBox.Font = new Font("Consolas", 9F);
            inlineCommentTextBox.Location = new Point(0, 21);
            inlineCommentTextBox.Name = "inlineCommentTextBox";
            inlineCommentTextBox.Size = new Size(659, 22);
            inlineCommentTextBox.TabIndex = 0;
            // 
            // commentTypeLabel2
            // 
            commentTypeLabel2.AutoSize = true;
            commentTypeLabel2.Dock = DockStyle.Top;
            commentTypeLabel2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            commentTypeLabel2.Location = new Point(0, 0);
            commentTypeLabel2.Name = "commentTypeLabel2";
            commentTypeLabel2.Padding = new Padding(3);
            commentTypeLabel2.Size = new Size(168, 21);
            commentTypeLabel2.TabIndex = 0;
            commentTypeLabel2.Text = "Inline Comment (single line)";
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { sectionStatusLabel, keyStatusLabel, filePathStatusLabel });
            statusStrip1.Location = new Point(0, 539);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(784, 22);
            statusStrip1.TabIndex = 2;
            // 
            // sectionStatusLabel
            // 
            sectionStatusLabel.Name = "sectionStatusLabel";
            sectionStatusLabel.Size = new Size(78, 17);
            sectionStatusLabel.Text = "Sections: 0/0";
            // 
            // keyStatusLabel
            // 
            keyStatusLabel.Name = "keyStatusLabel";
            keyStatusLabel.Size = new Size(57, 17);
            keyStatusLabel.Text = "Keys: 0/0";
            // 
            // filePathStatusLabel
            // 
            filePathStatusLabel.Name = "filePathStatusLabel";
            filePathStatusLabel.Size = new Size(634, 17);
            filePathStatusLabel.Spring = true;
            filePathStatusLabel.Text = "File: -";
            filePathStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            ClientSize = new Size(784, 561);
            Controls.Add(mainPanel);
            Controls.Add(menuStrip1);
            Controls.Add(statusStrip1);
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(800, 600);
            Name = "MainForm";
            Text = "INI Editor";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            mainPanel.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel1.PerformLayout();
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            commentPanel.ResumeLayout(false);
            commentPanel.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
