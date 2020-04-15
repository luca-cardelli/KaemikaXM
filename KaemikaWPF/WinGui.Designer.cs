namespace KaemikaWPF
{
    partial class WinGui
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WinGui));
            this.panel1 = new System.Windows.Forms.Panel();
            this.button_FontSizePlus = new System.Windows.Forms.Button();
            this.button_FontSizeMinus = new System.Windows.Forms.Button();
            this.button_Math = new System.Windows.Forms.Button();
            this.button_Export = new System.Windows.Forms.Button();
            this.button_Parameters = new System.Windows.Forms.Button();
            this.button_Source_Paste = new System.Windows.Forms.Button();
            this.button_Source_Copy = new System.Windows.Forms.Button();
            this.button_Tutorial = new System.Windows.Forms.Button();
            this.txtInput = new System.Windows.Forms.TextBox();
            this.txtOutput = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnStop = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.button_Output = new System.Windows.Forms.Button();
            this.button_FlipMicrofluidics = new System.Windows.Forms.Button();
            this.button_Settings = new System.Windows.Forms.Button();
            this.button_Noise = new System.Windows.Forms.Button();
            this.button_EditChart = new System.Windows.Forms.Button();
            this.button_Device = new System.Windows.Forms.Button();
            this.btnEval = new System.Windows.Forms.Button();
            this.panel_Microfluidics = new System.Windows.Forms.Panel();
            this.splitContainer_Columns = new System.Windows.Forms.SplitContainer();
            this.splitContainer_Rows = new System.Windows.Forms.SplitContainer();
            this.label_Tooltip = new System.Windows.Forms.Label();
            this.panel_KChart = new System.Windows.Forms.Panel();
            this.panel_Splash = new System.Windows.Forms.Panel();
            this.tableLayoutPanel_Parameters = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Legend = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Tutorial = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Export = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Math = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Output = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Settings = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Noise = new System.Windows.Forms.TableLayoutPanel();
            this.panel_KScore = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_Columns)).BeginInit();
            this.splitContainer_Columns.Panel1.SuspendLayout();
            this.splitContainer_Columns.Panel2.SuspendLayout();
            this.splitContainer_Columns.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_Rows)).BeginInit();
            this.splitContainer_Rows.Panel1.SuspendLayout();
            this.splitContainer_Rows.Panel2.SuspendLayout();
            this.splitContainer_Rows.SuspendLayout();
            this.panel_KChart.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.panel1.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.panel1.Controls.Add(this.button_FontSizePlus);
            this.panel1.Controls.Add(this.button_FontSizeMinus);
            this.panel1.Controls.Add(this.button_Math);
            this.panel1.Controls.Add(this.button_Export);
            this.panel1.Controls.Add(this.button_Parameters);
            this.panel1.Controls.Add(this.button_Source_Paste);
            this.panel1.Controls.Add(this.button_Source_Copy);
            this.panel1.Controls.Add(this.button_Tutorial);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(50, 681);
            this.panel1.TabIndex = 1;
            // 
            // button_FontSizePlus
            // 
            this.button_FontSizePlus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_FontSizePlus.FlatAppearance.BorderSize = 0;
            this.button_FontSizePlus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FontSizePlus.Image = global::KaemikaWPF.Properties.Resources.FontSizePlus_W_48x48;
            this.button_FontSizePlus.Location = new System.Drawing.Point(0, 557);
            this.button_FontSizePlus.Name = "button_FontSizePlus";
            this.button_FontSizePlus.Size = new System.Drawing.Size(50, 50);
            this.button_FontSizePlus.TabIndex = 15;
            this.button_FontSizePlus.UseVisualStyleBackColor = true;
            // 
            // button_FontSizeMinus
            // 
            this.button_FontSizeMinus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_FontSizeMinus.FlatAppearance.BorderSize = 0;
            this.button_FontSizeMinus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FontSizeMinus.Image = global::KaemikaWPF.Properties.Resources.FontSizeMinus_W_48x48;
            this.button_FontSizeMinus.Location = new System.Drawing.Point(0, 607);
            this.button_FontSizeMinus.Name = "button_FontSizeMinus";
            this.button_FontSizeMinus.Size = new System.Drawing.Size(50, 50);
            this.button_FontSizeMinus.TabIndex = 14;
            this.button_FontSizeMinus.UseVisualStyleBackColor = true;
            // 
            // button_Math
            // 
            this.button_Math.FlatAppearance.BorderSize = 0;
            this.button_Math.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Math.Image = global::KaemikaWPF.Properties.Resources.icons8_keyboard_96_W_48x48;
            this.button_Math.Location = new System.Drawing.Point(0, 334);
            this.button_Math.Name = "button_Math";
            this.button_Math.Size = new System.Drawing.Size(50, 50);
            this.button_Math.TabIndex = 13;
            this.button_Math.UseVisualStyleBackColor = true;
            // 
            // button_Export
            // 
            this.button_Export.FlatAppearance.BorderSize = 0;
            this.button_Export.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Export.Image = global::KaemikaWPF.Properties.Resources.icons8_share_384_W_48x48;
            this.button_Export.Location = new System.Drawing.Point(0, 262);
            this.button_Export.Name = "button_Export";
            this.button_Export.Size = new System.Drawing.Size(50, 50);
            this.button_Export.TabIndex = 12;
            this.button_Export.UseVisualStyleBackColor = true;
            // 
            // button_Parameters
            // 
            this.button_Parameters.FlatAppearance.BorderSize = 0;
            this.button_Parameters.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Parameters.Image = global::KaemikaWPF.Properties.Resources.Parameters_W_48x48;
            this.button_Parameters.Location = new System.Drawing.Point(0, 406);
            this.button_Parameters.Name = "button_Parameters";
            this.button_Parameters.Size = new System.Drawing.Size(50, 50);
            this.button_Parameters.TabIndex = 14;
            this.button_Parameters.UseVisualStyleBackColor = true;
            // 
            // button_Source_Paste
            // 
            this.button_Source_Paste.FlatAppearance.BorderSize = 0;
            this.button_Source_Paste.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Source_Paste.Image = global::KaemikaWPF.Properties.Resources.FileLoad_48x48;
            this.button_Source_Paste.Location = new System.Drawing.Point(-2, 190);
            this.button_Source_Paste.Name = "button_Source_Paste";
            this.button_Source_Paste.Size = new System.Drawing.Size(50, 50);
            this.button_Source_Paste.TabIndex = 7;
            this.button_Source_Paste.UseVisualStyleBackColor = true;
            // 
            // button_Source_Copy
            // 
            this.button_Source_Copy.FlatAppearance.BorderSize = 0;
            this.button_Source_Copy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Source_Copy.Image = global::KaemikaWPF.Properties.Resources.FileSave_48x48;
            this.button_Source_Copy.Location = new System.Drawing.Point(-2, 118);
            this.button_Source_Copy.Name = "button_Source_Copy";
            this.button_Source_Copy.Size = new System.Drawing.Size(50, 50);
            this.button_Source_Copy.TabIndex = 6;
            this.button_Source_Copy.UseVisualStyleBackColor = true;
            // 
            // button_Tutorial
            // 
            this.button_Tutorial.FlatAppearance.BorderSize = 0;
            this.button_Tutorial.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Tutorial.Image = global::KaemikaWPF.Properties.Resources.icons8text_48x48;
            this.button_Tutorial.Location = new System.Drawing.Point(0, 22);
            this.button_Tutorial.Name = "button_Tutorial";
            this.button_Tutorial.Size = new System.Drawing.Size(50, 50);
            this.button_Tutorial.TabIndex = 5;
            this.button_Tutorial.UseVisualStyleBackColor = true;
            // 
            // txtInput
            // 
            this.txtInput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtInput.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtInput.Location = new System.Drawing.Point(0, 0);
            this.txtInput.Margin = new System.Windows.Forms.Padding(0);
            this.txtInput.Multiline = true;
            this.txtInput.Name = "txtInput";
            this.txtInput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtInput.Size = new System.Drawing.Size(591, 673);
            this.txtInput.TabIndex = 2;
            this.txtInput.WordWrap = false;
            // 
            // txtOutput
            // 
            this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutput.BackColor = System.Drawing.SystemColors.Window;
            this.txtOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtOutput.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutput.Location = new System.Drawing.Point(0, 0);
            this.txtOutput.Margin = new System.Windows.Forms.Padding(10);
            this.txtOutput.Multiline = true;
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtOutput.Size = new System.Drawing.Size(589, 335);
            this.txtOutput.TabIndex = 3;
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Image = global::KaemikaWPF.Properties.Resources.icons8stop40;
            this.btnStop.Location = new System.Drawing.Point(-2, 94);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(50, 50);
            this.btnStop.TabIndex = 3;
            this.toolTip1.SetToolTip(this.btnStop, "Stop program execution");
            this.btnStop.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.panel2.Controls.Add(this.button_Output);
            this.panel2.Controls.Add(this.button_FlipMicrofluidics);
            this.panel2.Controls.Add(this.button_Settings);
            this.panel2.Controls.Add(this.button_Noise);
            this.panel2.Controls.Add(this.button_EditChart);
            this.panel2.Controls.Add(this.button_Device);
            this.panel2.Controls.Add(this.btnStop);
            this.panel2.Controls.Add(this.btnEval);
            this.panel2.Location = new System.Drawing.Point(1234, 0);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(50, 681);
            this.panel2.TabIndex = 5;
            // 
            // button_Output
            // 
            this.button_Output.FlatAppearance.BorderSize = 0;
            this.button_Output.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Output.Image = global::KaemikaWPF.Properties.Resources.Computation_48x48;
            this.button_Output.Location = new System.Drawing.Point(0, 310);
            this.button_Output.Name = "button_Output";
            this.button_Output.Size = new System.Drawing.Size(50, 50);
            this.button_Output.TabIndex = 17;
            this.button_Output.UseVisualStyleBackColor = true;
            // 
            // button_FlipMicrofluidics
            // 
            this.button_FlipMicrofluidics.FlatAppearance.BorderSize = 0;
            this.button_FlipMicrofluidics.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FlipMicrofluidics.Image = global::KaemikaWPF.Properties.Resources.deviceBorder_W_48x48;
            this.button_FlipMicrofluidics.Location = new System.Drawing.Point(-2, 454);
            this.button_FlipMicrofluidics.Name = "button_FlipMicrofluidics";
            this.button_FlipMicrofluidics.Size = new System.Drawing.Size(50, 50);
            this.button_FlipMicrofluidics.TabIndex = 16;
            this.button_FlipMicrofluidics.UseVisualStyleBackColor = true;
            // 
            // button_Settings
            // 
            this.button_Settings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_Settings.FlatAppearance.BorderSize = 0;
            this.button_Settings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Settings.Image = global::KaemikaWPF.Properties.Resources.icons8_settings_384_W_48x48;
            this.button_Settings.Location = new System.Drawing.Point(0, 607);
            this.button_Settings.Name = "button_Settings";
            this.button_Settings.Size = new System.Drawing.Size(50, 50);
            this.button_Settings.TabIndex = 15;
            this.button_Settings.UseVisualStyleBackColor = true;
            // 
            // button_Noise
            // 
            this.button_Noise.FlatAppearance.BorderSize = 0;
            this.button_Noise.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Noise.Image = global::KaemikaWPF.Properties.Resources.Noise_None_W_48x48;
            this.button_Noise.Location = new System.Drawing.Point(0, 166);
            this.button_Noise.Name = "button_Noise";
            this.button_Noise.Size = new System.Drawing.Size(50, 50);
            this.button_Noise.TabIndex = 11;
            this.button_Noise.UseVisualStyleBackColor = false;
            // 
            // button_EditChart
            // 
            this.button_EditChart.FlatAppearance.BorderSize = 0;
            this.button_EditChart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_EditChart.Image = global::KaemikaWPF.Properties.Resources.icons8combochart96_W_48x48;
            this.button_EditChart.Location = new System.Drawing.Point(0, 238);
            this.button_EditChart.Name = "button_EditChart";
            this.button_EditChart.Size = new System.Drawing.Size(50, 50);
            this.button_EditChart.TabIndex = 13;
            this.button_EditChart.UseVisualStyleBackColor = true;
            // 
            // button_Device
            // 
            this.button_Device.FlatAppearance.BorderSize = 0;
            this.button_Device.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Device.Image = global::KaemikaWPF.Properties.Resources.icons8device_OFF_48x48;
            this.button_Device.Location = new System.Drawing.Point(-2, 382);
            this.button_Device.Name = "button_Device";
            this.button_Device.Size = new System.Drawing.Size(50, 50);
            this.button_Device.TabIndex = 4;
            this.button_Device.UseVisualStyleBackColor = true;
            // 
            // btnEval
            // 
            this.btnEval.FlatAppearance.BorderSize = 0;
            this.btnEval.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEval.Image = global::KaemikaWPF.Properties.Resources.icons8play40;
            this.btnEval.Location = new System.Drawing.Point(-2, 22);
            this.btnEval.Name = "btnEval";
            this.btnEval.Size = new System.Drawing.Size(50, 50);
            this.btnEval.TabIndex = 0;
            this.btnEval.UseVisualStyleBackColor = true;
            // 
            // panel_Microfluidics
            // 
            this.panel_Microfluidics.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_Microfluidics.BackColor = System.Drawing.Color.Gold;
            this.panel_Microfluidics.Location = new System.Drawing.Point(0, 0);
            this.panel_Microfluidics.Name = "panel_Microfluidics";
            this.panel_Microfluidics.Size = new System.Drawing.Size(589, 335);
            this.panel_Microfluidics.TabIndex = 15;
            this.panel_Microfluidics.SizeChanged += new System.EventHandler(this.panel_Microfluidics_SizeChanged);
            // 
            // splitContainer_Columns
            // 
            this.splitContainer_Columns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer_Columns.BackColor = System.Drawing.Color.Crimson;
            this.splitContainer_Columns.Location = new System.Drawing.Point(50, 4);
            this.splitContainer_Columns.Name = "splitContainer_Columns";
            // 
            // splitContainer_Columns.Panel1
            // 
            this.splitContainer_Columns.Panel1.Controls.Add(this.txtInput);
            this.splitContainer_Columns.Panel1MinSize = 0;
            // 
            // splitContainer_Columns.Panel2
            // 
            this.splitContainer_Columns.Panel2.Controls.Add(this.splitContainer_Rows);
            this.splitContainer_Columns.Panel2MinSize = 0;
            this.splitContainer_Columns.Size = new System.Drawing.Size(1184, 673);
            this.splitContainer_Columns.SplitterDistance = 591;
            this.splitContainer_Columns.TabIndex = 16;
            this.splitContainer_Columns.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_Columns_SplitterMoved);
            this.splitContainer_Columns.MouseDown += new System.Windows.Forms.MouseEventHandler(this.splitContainer_Columns_MouseDown);
            this.splitContainer_Columns.MouseMove += new System.Windows.Forms.MouseEventHandler(this.splitContainer_Columns_MouseMove);
            this.splitContainer_Columns.MouseUp += new System.Windows.Forms.MouseEventHandler(this.splitContainer_Columns_MouseUp);
            // 
            // splitContainer_Rows
            // 
            this.splitContainer_Rows.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer_Rows.Location = new System.Drawing.Point(0, 0);
            this.splitContainer_Rows.Name = "splitContainer_Rows";
            this.splitContainer_Rows.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer_Rows.Panel1
            // 
            this.splitContainer_Rows.Panel1.Controls.Add(this.label_Tooltip);
            this.splitContainer_Rows.Panel1.Controls.Add(this.panel_KChart);
            this.splitContainer_Rows.Panel1MinSize = 0;
            // 
            // splitContainer_Rows.Panel2
            // 
            this.splitContainer_Rows.Panel2.Controls.Add(this.tableLayoutPanel_Parameters);
            this.splitContainer_Rows.Panel2.Controls.Add(this.tableLayoutPanel_Legend);
            this.splitContainer_Rows.Panel2.Controls.Add(this.panel_KScore);
            this.splitContainer_Rows.Panel2.Controls.Add(this.txtOutput);
            this.splitContainer_Rows.Panel2.Controls.Add(this.panel_Microfluidics);
            this.splitContainer_Rows.Panel2MinSize = 0;
            this.splitContainer_Rows.Size = new System.Drawing.Size(589, 673);
            this.splitContainer_Rows.SplitterDistance = 334;
            this.splitContainer_Rows.TabIndex = 0;
            this.splitContainer_Rows.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_Rows_SplitterMoved);
            this.splitContainer_Rows.MouseDown += new System.Windows.Forms.MouseEventHandler(this.splitContainer_Rows_MouseDown);
            this.splitContainer_Rows.MouseMove += new System.Windows.Forms.MouseEventHandler(this.splitContainer_Rows_MouseMove);
            this.splitContainer_Rows.MouseUp += new System.Windows.Forms.MouseEventHandler(this.splitContainer_Rows_MouseUp);
            // 
            // label_Tooltip
            // 
            this.label_Tooltip.AutoSize = true;
            this.label_Tooltip.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label_Tooltip.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Tooltip.Location = new System.Drawing.Point(466, 18);
            this.label_Tooltip.Name = "label_Tooltip";
            this.label_Tooltip.Size = new System.Drawing.Size(45, 28);
            this.label_Tooltip.TabIndex = 0;
            this.label_Tooltip.Text = "label1\r\nlabel2";
            // 
            // panel_KChart
            // 
            this.panel_KChart.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_KChart.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel_KChart.Controls.Add(this.panel_Splash);
            this.panel_KChart.Location = new System.Drawing.Point(0, 0);
            this.panel_KChart.Margin = new System.Windows.Forms.Padding(0);
            this.panel_KChart.Name = "panel_KChart";
            this.panel_KChart.Size = new System.Drawing.Size(589, 333);
            this.panel_KChart.TabIndex = 0;
            this.panel_KChart.SizeChanged += new System.EventHandler(this.panel_KChart_SizeChanged);
            // 
            // panel_Splash
            // 
            this.panel_Splash.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_Splash.BackColor = System.Drawing.Color.White;
            this.panel_Splash.BackgroundImage = global::KaemikaWPF.Properties.Resources.Splash_589;
            this.panel_Splash.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel_Splash.Location = new System.Drawing.Point(0, 0);
            this.panel_Splash.Name = "panel_Splash";
            this.panel_Splash.Size = new System.Drawing.Size(589, 333);
            this.panel_Splash.TabIndex = 14;
            // 
            // tableLayoutPanel_Parameters
            // 
            this.tableLayoutPanel_Parameters.ColumnCount = 2;
            this.tableLayoutPanel_Parameters.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Parameters.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Parameters.Location = new System.Drawing.Point(7, 5);
            this.tableLayoutPanel_Parameters.Name = "tableLayoutPanel_Parameters";
            this.tableLayoutPanel_Parameters.RowCount = 2;
            this.tableLayoutPanel_Parameters.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Parameters.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Parameters.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel_Parameters.TabIndex = 25;
            // 
            // tableLayoutPanel_Legend
            // 
            this.tableLayoutPanel_Legend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel_Legend.ColumnCount = 2;
            this.tableLayoutPanel_Legend.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Legend.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Legend.Location = new System.Drawing.Point(369, 2);
            this.tableLayoutPanel_Legend.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel_Legend.Name = "tableLayoutPanel_Legend";
            this.tableLayoutPanel_Legend.RowCount = 2;
            this.tableLayoutPanel_Legend.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Legend.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Legend.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel_Legend.TabIndex = 25;
            // 
            // tableLayoutPanel_Tutorial
            // 
            this.tableLayoutPanel_Tutorial.ColumnCount = 2;
            this.tableLayoutPanel_Tutorial.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Tutorial.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Tutorial.Location = new System.Drawing.Point(70, 25);
            this.tableLayoutPanel_Tutorial.Name = "tableLayoutPanel_Tutorial";
            this.tableLayoutPanel_Tutorial.RowCount = 2;
            this.tableLayoutPanel_Tutorial.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Tutorial.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Tutorial.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel_Tutorial.TabIndex = 19;
            // 
            // tableLayoutPanel_Export
            // 
            this.tableLayoutPanel_Export.ColumnCount = 2;
            this.tableLayoutPanel_Export.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Export.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Export.Location = new System.Drawing.Point(66, 180);
            this.tableLayoutPanel_Export.Name = "tableLayoutPanel_Export";
            this.tableLayoutPanel_Export.RowCount = 2;
            this.tableLayoutPanel_Export.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Export.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Export.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel_Export.TabIndex = 20;
            // 
            // tableLayoutPanel_Math
            // 
            this.tableLayoutPanel_Math.ColumnCount = 2;
            this.tableLayoutPanel_Math.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Math.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Math.Location = new System.Drawing.Point(63, 303);
            this.tableLayoutPanel_Math.Name = "tableLayoutPanel_Math";
            this.tableLayoutPanel_Math.RowCount = 2;
            this.tableLayoutPanel_Math.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Math.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Math.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel_Math.TabIndex = 21;
            // 
            // tableLayoutPanel_Output
            // 
            this.tableLayoutPanel_Output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel_Output.ColumnCount = 2;
            this.tableLayoutPanel_Output.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Output.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Output.Location = new System.Drawing.Point(969, 66);
            this.tableLayoutPanel_Output.Name = "tableLayoutPanel_Output";
            this.tableLayoutPanel_Output.RowCount = 2;
            this.tableLayoutPanel_Output.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Output.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Output.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel_Output.TabIndex = 22;
            // 
            // tableLayoutPanel_Settings
            // 
            this.tableLayoutPanel_Settings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel_Settings.ColumnCount = 2;
            this.tableLayoutPanel_Settings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Settings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Settings.Location = new System.Drawing.Point(1015, 562);
            this.tableLayoutPanel_Settings.Name = "tableLayoutPanel_Settings";
            this.tableLayoutPanel_Settings.RowCount = 2;
            this.tableLayoutPanel_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Settings.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Settings.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel_Settings.TabIndex = 23;
            // 
            // tableLayoutPanel_Noise
            // 
            this.tableLayoutPanel_Noise.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel_Noise.ColumnCount = 2;
            this.tableLayoutPanel_Noise.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Noise.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Noise.Location = new System.Drawing.Point(996, 219);
            this.tableLayoutPanel_Noise.Name = "tableLayoutPanel_Noise";
            this.tableLayoutPanel_Noise.RowCount = 2;
            this.tableLayoutPanel_Noise.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Noise.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Noise.Size = new System.Drawing.Size(200, 100);
            this.tableLayoutPanel_Noise.TabIndex = 24;
            // 
            // panel_KScore
            // 
            this.panel_KScore.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_KScore.Location = new System.Drawing.Point(0, 0);
            this.panel_KScore.Name = "panel_KScore";
            this.panel_KScore.Size = new System.Drawing.Size(589, 335);
            this.panel_KScore.TabIndex = 3;
            this.panel_KScore.SizeChanged += new System.EventHandler(this.panel_KScore_SizeChanged);
            // 
            // GuiToWin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightSkyBlue;
            this.ClientSize = new System.Drawing.Size(1284, 681);
            this.Controls.Add(this.tableLayoutPanel_Noise);
            this.Controls.Add(this.tableLayoutPanel_Settings);
            this.Controls.Add(this.tableLayoutPanel_Output);
            this.Controls.Add(this.tableLayoutPanel_Math);
            this.Controls.Add(this.tableLayoutPanel_Export);
            this.Controls.Add(this.tableLayoutPanel_Tutorial);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.splitContainer_Columns);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1024, 640);
            this.Name = "GuiToWin";
            this.Text = "Kaemika";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GuiToWin_FormClosing);
            this.Load += new System.EventHandler(this.GuiToWin_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GuiToWin_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GuiToWin_KeyUp);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.splitContainer_Columns.Panel1.ResumeLayout(false);
            this.splitContainer_Columns.Panel1.PerformLayout();
            this.splitContainer_Columns.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_Columns)).EndInit();
            this.splitContainer_Columns.ResumeLayout(false);
            this.splitContainer_Rows.Panel1.ResumeLayout(false);
            this.splitContainer_Rows.Panel1.PerformLayout();
            this.splitContainer_Rows.Panel2.ResumeLayout(false);
            this.splitContainer_Rows.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer_Rows)).EndInit();
            this.splitContainer_Rows.ResumeLayout(false);
            this.panel_KChart.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Button btnEval;
        public System.Windows.Forms.Panel panel1;
        public System.Windows.Forms.TextBox txtInput;
        public System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.ToolTip toolTip1;
        public System.Windows.Forms.Panel panel2;
        public System.Windows.Forms.Button btnStop;
        public System.Windows.Forms.Button button_Device;
        public System.Windows.Forms.Button button_Tutorial;
        public System.Windows.Forms.Button button_Source_Copy;
        public System.Windows.Forms.Button button_Source_Paste;
        public System.Windows.Forms.Button button_Noise;
        public System.Windows.Forms.Button button_Export;
        public System.Windows.Forms.Button button_EditChart;
        public System.Windows.Forms.Button button_Parameters;
        public System.Windows.Forms.Button button_Settings;
        public System.Windows.Forms.Button button_Math;
        public System.Windows.Forms.Button button_FontSizeMinus;
        public System.Windows.Forms.Button button_FontSizePlus;
        public System.Windows.Forms.Panel panel_Splash;
        public System.Windows.Forms.Panel panel_Microfluidics;
        public System.Windows.Forms.Button button_FlipMicrofluidics;
        public System.Windows.Forms.Button button_Output;
        private System.Windows.Forms.SplitContainer splitContainer_Columns;
        private System.Windows.Forms.SplitContainer splitContainer_Rows;
        public System.Windows.Forms.Panel panel_KChart;
        public System.Windows.Forms.Label label_Tooltip;
        public System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Tutorial;
        public System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Export;
        public System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Math;
        public System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Output;
        public System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Settings;
        public System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Noise;
        public System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Legend;
        public System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Parameters;
        public System.Windows.Forms.Panel panel_KScore;
    }
}