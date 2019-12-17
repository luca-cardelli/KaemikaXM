namespace KaemikaWPF
{
    partial class GUI2
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GUI2));
            this.panel1 = new System.Windows.Forms.Panel();
            this.button_FontSizePlus = new System.Windows.Forms.Button();
            this.button_FontSizeMinus = new System.Windows.Forms.Button();
            this.button_Math = new System.Windows.Forms.Button();
            this.button_Source_Paste = new System.Windows.Forms.Button();
            this.button_Source_Copy = new System.Windows.Forms.Button();
            this.button_Device = new System.Windows.Forms.Button();
            this.button_Tutorial = new System.Windows.Forms.Button();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.txtTarget = new System.Windows.Forms.TextBox();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btnStop = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.button_FlipMicrofluidics = new System.Windows.Forms.Button();
            this.button_Settings = new System.Windows.Forms.Button();
            this.button_Parameters = new System.Windows.Forms.Button();
            this.button_EditChart = new System.Windows.Forms.Button();
            this.button_Export = new System.Windows.Forms.Button();
            this.button_Noise = new System.Windows.Forms.Button();
            this.btnEval = new System.Windows.Forms.Button();
            this.checkedListBox_Series = new System.Windows.Forms.CheckedListBox();
            this.flowLayoutPanel_Parameters = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel_Tutorial = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel_Noise = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel_Export = new System.Windows.Forms.FlowLayoutPanel();
            this.panel_Settings = new System.Windows.Forms.Panel();
            this.textBox_OutputDirectory = new System.Windows.Forms.TextBox();
            this.label_OutputDirectory = new System.Windows.Forms.Label();
            this.button_TraceComputational = new System.Windows.Forms.Button();
            this.button_TraceChemical = new System.Windows.Forms.Button();
            this.label_Trace = new System.Windows.Forms.Label();
            this.button_PrecomputeLNA = new System.Windows.Forms.Button();
            this.label_LNA = new System.Windows.Forms.Label();
            this.button_GearBDF = new System.Windows.Forms.Button();
            this.button_RK547M = new System.Windows.Forms.Button();
            this.label_Solvers = new System.Windows.Forms.Label();
            this.panel_ModalPopUp = new System.Windows.Forms.Panel();
            this.label_ModalPopUpText2 = new System.Windows.Forms.Label();
            this.button_ModalPopUp_Cancel = new System.Windows.Forms.Button();
            this.button_ModalPopUp_OK = new System.Windows.Forms.Button();
            this.label_ModalPopUpText = new System.Windows.Forms.Label();
            this.flowLayoutPanel_Math = new System.Windows.Forms.FlowLayoutPanel();
            this.panel_Microfluidics = new System.Windows.Forms.Panel();
            this.panel_Splash = new System.Windows.Forms.Panel();
            this.label_Version = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel_Settings.SuspendLayout();
            this.panel_ModalPopUp.SuspendLayout();
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
            this.panel1.Controls.Add(this.button_Source_Paste);
            this.panel1.Controls.Add(this.button_Source_Copy);
            this.panel1.Controls.Add(this.button_Device);
            this.panel1.Controls.Add(this.button_Tutorial);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(48, 681);
            this.panel1.TabIndex = 1;
            // 
            // button_FontSizePlus
            // 
            this.button_FontSizePlus.FlatAppearance.BorderSize = 0;
            this.button_FontSizePlus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FontSizePlus.Image = global::KaemikaWPF.Properties.Resources.FontSizePlus_W_48x48;
            this.button_FontSizePlus.Location = new System.Drawing.Point(-2, 557);
            this.button_FontSizePlus.Name = "button_FontSizePlus";
            this.button_FontSizePlus.Size = new System.Drawing.Size(50, 50);
            this.button_FontSizePlus.TabIndex = 15;
            this.button_FontSizePlus.UseVisualStyleBackColor = true;
            this.button_FontSizePlus.Click += new System.EventHandler(this.button_FontSizePlus_Click);
            // 
            // button_FontSizeMinus
            // 
            this.button_FontSizeMinus.FlatAppearance.BorderSize = 0;
            this.button_FontSizeMinus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FontSizeMinus.Image = global::KaemikaWPF.Properties.Resources.FontSizeMinus_W_48x48;
            this.button_FontSizeMinus.Location = new System.Drawing.Point(-2, 607);
            this.button_FontSizeMinus.Name = "button_FontSizeMinus";
            this.button_FontSizeMinus.Size = new System.Drawing.Size(50, 50);
            this.button_FontSizeMinus.TabIndex = 14;
            this.button_FontSizeMinus.UseVisualStyleBackColor = true;
            this.button_FontSizeMinus.Click += new System.EventHandler(this.button_FontSizeMinus_Click);
            // 
            // button_Math
            // 
            this.button_Math.FlatAppearance.BorderSize = 0;
            this.button_Math.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Math.Image = global::KaemikaWPF.Properties.Resources.icons8_keyboard_96_W_48x48;
            this.button_Math.Location = new System.Drawing.Point(-2, 262);
            this.button_Math.Name = "button_Math";
            this.button_Math.Size = new System.Drawing.Size(50, 50);
            this.button_Math.TabIndex = 13;
            this.button_Math.UseVisualStyleBackColor = true;
            // 
            // button_Source_Paste
            // 
            this.button_Source_Paste.FlatAppearance.BorderSize = 0;
            this.button_Source_Paste.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Source_Paste.Image = global::KaemikaWPF.Properties.Resources.icons8export96rht_48x48;
            this.button_Source_Paste.Location = new System.Drawing.Point(-2, 190);
            this.button_Source_Paste.Name = "button_Source_Paste";
            this.button_Source_Paste.Size = new System.Drawing.Size(50, 50);
            this.button_Source_Paste.TabIndex = 7;
            this.button_Source_Paste.UseVisualStyleBackColor = true;
            this.button_Source_Paste.Click += new System.EventHandler(this.button_Source_Paste_Click);
            // 
            // button_Source_Copy
            // 
            this.button_Source_Copy.FlatAppearance.BorderSize = 0;
            this.button_Source_Copy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Source_Copy.Image = global::KaemikaWPF.Properties.Resources.icons8import96rht_48x48;
            this.button_Source_Copy.Location = new System.Drawing.Point(-2, 118);
            this.button_Source_Copy.Name = "button_Source_Copy";
            this.button_Source_Copy.Size = new System.Drawing.Size(50, 50);
            this.button_Source_Copy.TabIndex = 6;
            this.button_Source_Copy.UseVisualStyleBackColor = true;
            this.button_Source_Copy.Click += new System.EventHandler(this.button_Source_Copy_Click);
            // 
            // button_Device
            // 
            this.button_Device.FlatAppearance.BorderSize = 0;
            this.button_Device.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Device.Image = global::KaemikaWPF.Properties.Resources.icons8device_OFF_48x48;
            this.button_Device.Location = new System.Drawing.Point(-2, 406);
            this.button_Device.Name = "button_Device";
            this.button_Device.Size = new System.Drawing.Size(50, 50);
            this.button_Device.TabIndex = 4;
            this.button_Device.UseVisualStyleBackColor = true;
            this.button_Device.Click += new System.EventHandler(this.button_Device_Click);
            // 
            // button_Tutorial
            // 
            this.button_Tutorial.FlatAppearance.BorderSize = 0;
            this.button_Tutorial.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Tutorial.Image = global::KaemikaWPF.Properties.Resources.icons8text_48x48;
            this.button_Tutorial.Location = new System.Drawing.Point(-2, 22);
            this.button_Tutorial.Margin = new System.Windows.Forms.Padding(0);
            this.button_Tutorial.Name = "button_Tutorial";
            this.button_Tutorial.Size = new System.Drawing.Size(50, 50);
            this.button_Tutorial.TabIndex = 5;
            this.button_Tutorial.UseVisualStyleBackColor = true;
            // 
            // richTextBox
            // 
            this.richTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.richTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox.Font = new System.Drawing.Font("Lucida Sans Typewriter", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox.Location = new System.Drawing.Point(51, 3);
            this.richTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.Size = new System.Drawing.Size(590, 675);
            this.richTextBox.TabIndex = 2;
            this.richTextBox.Text = "";
            this.richTextBox.WordWrap = false;
            // 
            // txtTarget
            // 
            this.txtTarget.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTarget.BackColor = System.Drawing.SystemColors.Window;
            this.txtTarget.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtTarget.Font = new System.Drawing.Font("Lucida Sans Typewriter", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTarget.Location = new System.Drawing.Point(644, 342);
            this.txtTarget.Margin = new System.Windows.Forms.Padding(10);
            this.txtTarget.Multiline = true;
            this.txtTarget.Name = "txtTarget";
            this.txtTarget.ReadOnly = true;
            this.txtTarget.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtTarget.Size = new System.Drawing.Size(589, 336);
            this.txtTarget.TabIndex = 3;
            // 
            // chart1
            // 
            this.chart1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chart1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.chart1.BorderlineColor = System.Drawing.Color.Black;
            this.chart1.BorderSkin.BackImageAlignment = System.Windows.Forms.DataVisualization.Charting.ChartImageAlignmentStyle.Right;
            this.chart1.BorderSkin.PageColor = System.Drawing.SystemColors.Control;
            chartArea1.AxisX.MajorGrid.LineColor = System.Drawing.Color.Gray;
            chartArea1.AxisX.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea1.AxisX.ScrollBar.BackColor = System.Drawing.Color.White;
            chartArea1.AxisX.ScrollBar.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            chartArea1.AxisX.Title = "Time (s)";
            chartArea1.AxisY.MajorGrid.LineColor = System.Drawing.Color.Gray;
            chartArea1.AxisY.MajorGrid.LineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Dot;
            chartArea1.AxisY.ScrollBar.BackColor = System.Drawing.Color.White;
            chartArea1.AxisY.ScrollBar.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            chartArea1.AxisY.Title = "Molarity (M)";
            chartArea1.BorderWidth = 0;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            legend1.IsTextAutoFit = false;
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(644, 3);
            this.chart1.Margin = new System.Windows.Forms.Padding(0);
            this.chart1.Name = "chart1";
            this.chart1.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Bright;
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Legend = "Legend1";
            series1.Name = "-";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(589, 336);
            this.chart1.SuppressExceptions = true;
            this.chart1.TabIndex = 4;
            this.chart1.Text = "chart1";
            this.toolTip1.SetToolTip(this.chart1, "pinch or mouse-wheel to zoom");
            this.chart1.SizeChanged += new System.EventHandler(this.chart1_SizeChanged);
            this.chart1.Click += new System.EventHandler(this.chart1_Click);
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Image = global::KaemikaWPF.Properties.Resources.icons8stop40;
            this.btnStop.Location = new System.Drawing.Point(-2, 94);
            this.btnStop.Margin = new System.Windows.Forms.Padding(0);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(50, 50);
            this.btnStop.TabIndex = 3;
            this.toolTip1.SetToolTip(this.btnStop, "Stop program execution");
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.panel2.Controls.Add(this.button_FlipMicrofluidics);
            this.panel2.Controls.Add(this.button_Settings);
            this.panel2.Controls.Add(this.button_Parameters);
            this.panel2.Controls.Add(this.button_EditChart);
            this.panel2.Controls.Add(this.button_Export);
            this.panel2.Controls.Add(this.button_Noise);
            this.panel2.Controls.Add(this.btnStop);
            this.panel2.Controls.Add(this.btnEval);
            this.panel2.Location = new System.Drawing.Point(1236, 0);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(48, 681);
            this.panel2.TabIndex = 5;
            // 
            // button_FlipMicrofluidics
            // 
            this.button_FlipMicrofluidics.FlatAppearance.BorderSize = 0;
            this.button_FlipMicrofluidics.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_FlipMicrofluidics.Image = global::KaemikaWPF.Properties.Resources.deviceBorder_W_48x48;
            this.button_FlipMicrofluidics.Location = new System.Drawing.Point(-2, 478);
            this.button_FlipMicrofluidics.Margin = new System.Windows.Forms.Padding(2);
            this.button_FlipMicrofluidics.Name = "button_FlipMicrofluidics";
            this.button_FlipMicrofluidics.Size = new System.Drawing.Size(50, 50);
            this.button_FlipMicrofluidics.TabIndex = 16;
            this.button_FlipMicrofluidics.UseVisualStyleBackColor = true;
            this.button_FlipMicrofluidics.Click += new System.EventHandler(this.button_FlipMicrofluidics_Click);
            // 
            // button_Settings
            // 
            this.button_Settings.FlatAppearance.BorderSize = 0;
            this.button_Settings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Settings.Image = global::KaemikaWPF.Properties.Resources.icons8_settings_384_W_48x48;
            this.button_Settings.Location = new System.Drawing.Point(-2, 607);
            this.button_Settings.Name = "button_Settings";
            this.button_Settings.Size = new System.Drawing.Size(50, 50);
            this.button_Settings.TabIndex = 15;
            this.button_Settings.UseVisualStyleBackColor = true;
            this.button_Settings.Click += new System.EventHandler(this.button_Settings_Click);
            // 
            // button_Parameters
            // 
            this.button_Parameters.FlatAppearance.BorderSize = 0;
            this.button_Parameters.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Parameters.Image = global::KaemikaWPF.Properties.Resources.Parameters_W_48x48;
            this.button_Parameters.Location = new System.Drawing.Point(-2, 406);
            this.button_Parameters.Margin = new System.Windows.Forms.Padding(2);
            this.button_Parameters.Name = "button_Parameters";
            this.button_Parameters.Size = new System.Drawing.Size(50, 50);
            this.button_Parameters.TabIndex = 14;
            this.button_Parameters.UseVisualStyleBackColor = true;
            this.button_Parameters.Click += new System.EventHandler(this.button_Parameters_Click);
            // 
            // button_EditChart
            // 
            this.button_EditChart.FlatAppearance.BorderSize = 0;
            this.button_EditChart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_EditChart.Image = global::KaemikaWPF.Properties.Resources.icons8combochart96_W_48x48;
            this.button_EditChart.Location = new System.Drawing.Point(-2, 334);
            this.button_EditChart.Name = "button_EditChart";
            this.button_EditChart.Size = new System.Drawing.Size(50, 50);
            this.button_EditChart.TabIndex = 13;
            this.button_EditChart.UseVisualStyleBackColor = true;
            this.button_EditChart.Click += new System.EventHandler(this.button_EditChart_Click);
            // 
            // button_Export
            // 
            this.button_Export.FlatAppearance.BorderSize = 0;
            this.button_Export.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Export.Image = global::KaemikaWPF.Properties.Resources.icons8_share_384_W_48x48;
            this.button_Export.Location = new System.Drawing.Point(-2, 262);
            this.button_Export.Name = "button_Export";
            this.button_Export.Size = new System.Drawing.Size(50, 50);
            this.button_Export.TabIndex = 12;
            this.button_Export.UseVisualStyleBackColor = true;
            // 
            // button_Noise
            // 
            this.button_Noise.FlatAppearance.BorderSize = 0;
            this.button_Noise.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_Noise.Image = global::KaemikaWPF.Properties.Resources.Noise_None_W_48x48;
            this.button_Noise.Location = new System.Drawing.Point(-2, 190);
            this.button_Noise.Name = "button_Noise";
            this.button_Noise.Size = new System.Drawing.Size(50, 50);
            this.button_Noise.TabIndex = 11;
            this.button_Noise.UseVisualStyleBackColor = false;
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
            this.btnEval.Click += new System.EventHandler(this.btnEval_Click);
            // 
            // checkedListBox_Series
            // 
            this.checkedListBox_Series.BackColor = System.Drawing.Color.Thistle;
            this.checkedListBox_Series.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.checkedListBox_Series.CheckOnClick = true;
            this.checkedListBox_Series.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkedListBox_Series.FormattingEnabled = true;
            this.checkedListBox_Series.Location = new System.Drawing.Point(904, 338);
            this.checkedListBox_Series.MultiColumn = true;
            this.checkedListBox_Series.Name = "checkedListBox_Series";
            this.checkedListBox_Series.Size = new System.Drawing.Size(330, 173);
            this.checkedListBox_Series.TabIndex = 6;
            this.checkedListBox_Series.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox_Series_ItemCheck);
            this.checkedListBox_Series.SelectedIndexChanged += new System.EventHandler(this.checkedListBox_Series_SelectedIndexChanged);
            // 
            // flowLayoutPanel_Parameters
            // 
            this.flowLayoutPanel_Parameters.AutoScroll = true;
            this.flowLayoutPanel_Parameters.BackColor = System.Drawing.Color.Thistle;
            this.flowLayoutPanel_Parameters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanel_Parameters.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel_Parameters.Location = new System.Drawing.Point(903, 412);
            this.flowLayoutPanel_Parameters.Name = "flowLayoutPanel_Parameters";
            this.flowLayoutPanel_Parameters.Size = new System.Drawing.Size(330, 257);
            this.flowLayoutPanel_Parameters.TabIndex = 7;
            // 
            // flowLayoutPanel_Tutorial
            // 
            this.flowLayoutPanel_Tutorial.AutoScroll = true;
            this.flowLayoutPanel_Tutorial.BackColor = System.Drawing.SystemColors.ControlDark;
            this.flowLayoutPanel_Tutorial.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel_Tutorial.Location = new System.Drawing.Point(69, 0);
            this.flowLayoutPanel_Tutorial.Name = "flowLayoutPanel_Tutorial";
            this.flowLayoutPanel_Tutorial.Size = new System.Drawing.Size(340, 669);
            this.flowLayoutPanel_Tutorial.TabIndex = 8;
            // 
            // flowLayoutPanel_Noise
            // 
            this.flowLayoutPanel_Noise.BackColor = System.Drawing.SystemColors.ControlDark;
            this.flowLayoutPanel_Noise.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel_Noise.Location = new System.Drawing.Point(1163, 160);
            this.flowLayoutPanel_Noise.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel_Noise.Name = "flowLayoutPanel_Noise";
            this.flowLayoutPanel_Noise.Size = new System.Drawing.Size(48, 196);
            this.flowLayoutPanel_Noise.TabIndex = 9;
            // 
            // flowLayoutPanel_Export
            // 
            this.flowLayoutPanel_Export.Location = new System.Drawing.Point(1062, 160);
            this.flowLayoutPanel_Export.Name = "flowLayoutPanel_Export";
            this.flowLayoutPanel_Export.Size = new System.Drawing.Size(85, 243);
            this.flowLayoutPanel_Export.TabIndex = 10;
            // 
            // panel_Settings
            // 
            this.panel_Settings.BackColor = System.Drawing.Color.Purple;
            this.panel_Settings.Controls.Add(this.label_Version);
            this.panel_Settings.Controls.Add(this.textBox_OutputDirectory);
            this.panel_Settings.Controls.Add(this.label_OutputDirectory);
            this.panel_Settings.Controls.Add(this.button_TraceComputational);
            this.panel_Settings.Controls.Add(this.button_TraceChemical);
            this.panel_Settings.Controls.Add(this.label_Trace);
            this.panel_Settings.Controls.Add(this.button_PrecomputeLNA);
            this.panel_Settings.Controls.Add(this.label_LNA);
            this.panel_Settings.Controls.Add(this.button_GearBDF);
            this.panel_Settings.Controls.Add(this.button_RK547M);
            this.panel_Settings.Controls.Add(this.label_Solvers);
            this.panel_Settings.Location = new System.Drawing.Point(669, 356);
            this.panel_Settings.Margin = new System.Windows.Forms.Padding(0);
            this.panel_Settings.Name = "panel_Settings";
            this.panel_Settings.Size = new System.Drawing.Size(200, 286);
            this.panel_Settings.TabIndex = 11;
            // 
            // textBox_OutputDirectory
            // 
            this.textBox_OutputDirectory.Location = new System.Drawing.Point(25, 225);
            this.textBox_OutputDirectory.Margin = new System.Windows.Forms.Padding(2);
            this.textBox_OutputDirectory.Name = "textBox_OutputDirectory";
            this.textBox_OutputDirectory.Size = new System.Drawing.Size(152, 20);
            this.textBox_OutputDirectory.TabIndex = 9;
            // 
            // label_OutputDirectory
            // 
            this.label_OutputDirectory.AutoSize = true;
            this.label_OutputDirectory.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_OutputDirectory.ForeColor = System.Drawing.Color.White;
            this.label_OutputDirectory.Location = new System.Drawing.Point(8, 201);
            this.label_OutputDirectory.Name = "label_OutputDirectory";
            this.label_OutputDirectory.Size = new System.Drawing.Size(165, 16);
            this.label_OutputDirectory.TabIndex = 8;
            this.label_OutputDirectory.Text = "Directory for output files";
            // 
            // button_TraceComputational
            // 
            this.button_TraceComputational.BackColor = System.Drawing.Color.Silver;
            this.button_TraceComputational.FlatAppearance.BorderSize = 0;
            this.button_TraceComputational.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_TraceComputational.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_TraceComputational.ForeColor = System.Drawing.Color.White;
            this.button_TraceComputational.Location = new System.Drawing.Point(115, 160);
            this.button_TraceComputational.Name = "button_TraceComputational";
            this.button_TraceComputational.Size = new System.Drawing.Size(60, 23);
            this.button_TraceComputational.TabIndex = 7;
            this.button_TraceComputational.Text = "Full";
            this.button_TraceComputational.UseVisualStyleBackColor = false;
            this.button_TraceComputational.Click += new System.EventHandler(this.button_TraceComputational_Click);
            // 
            // button_TraceChemical
            // 
            this.button_TraceChemical.BackColor = System.Drawing.Color.RoyalBlue;
            this.button_TraceChemical.FlatAppearance.BorderSize = 0;
            this.button_TraceChemical.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_TraceChemical.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_TraceChemical.ForeColor = System.Drawing.Color.White;
            this.button_TraceChemical.Location = new System.Drawing.Point(25, 160);
            this.button_TraceChemical.Name = "button_TraceChemical";
            this.button_TraceChemical.Size = new System.Drawing.Size(90, 23);
            this.button_TraceChemical.TabIndex = 6;
            this.button_TraceChemical.Text = "Chemical";
            this.button_TraceChemical.UseVisualStyleBackColor = false;
            this.button_TraceChemical.Click += new System.EventHandler(this.button_TraceChemical_Click);
            // 
            // label_Trace
            // 
            this.label_Trace.AutoSize = true;
            this.label_Trace.Font = new System.Drawing.Font("Lucida Sans Unicode", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Trace.ForeColor = System.Drawing.Color.White;
            this.label_Trace.Location = new System.Drawing.Point(8, 136);
            this.label_Trace.Name = "label_Trace";
            this.label_Trace.Size = new System.Drawing.Size(112, 20);
            this.label_Trace.TabIndex = 5;
            this.label_Trace.Text = "Output Trace";
            // 
            // button_PrecomputeLNA
            // 
            this.button_PrecomputeLNA.BackColor = System.Drawing.Color.Silver;
            this.button_PrecomputeLNA.FlatAppearance.BorderSize = 0;
            this.button_PrecomputeLNA.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_PrecomputeLNA.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_PrecomputeLNA.ForeColor = System.Drawing.Color.White;
            this.button_PrecomputeLNA.Location = new System.Drawing.Point(25, 100);
            this.button_PrecomputeLNA.Name = "button_PrecomputeLNA";
            this.button_PrecomputeLNA.Size = new System.Drawing.Size(150, 23);
            this.button_PrecomputeLNA.TabIndex = 4;
            this.button_PrecomputeLNA.Text = "Precompute drift";
            this.button_PrecomputeLNA.UseVisualStyleBackColor = false;
            this.button_PrecomputeLNA.Click += new System.EventHandler(this.button_PrecomputeLNA_Click);
            // 
            // label_LNA
            // 
            this.label_LNA.AutoSize = true;
            this.label_LNA.Font = new System.Drawing.Font("Lucida Sans Unicode", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_LNA.ForeColor = System.Drawing.Color.White;
            this.label_LNA.Location = new System.Drawing.Point(8, 76);
            this.label_LNA.Name = "label_LNA";
            this.label_LNA.Size = new System.Drawing.Size(41, 20);
            this.label_LNA.TabIndex = 3;
            this.label_LNA.Text = "LNA";
            // 
            // button_GearBDF
            // 
            this.button_GearBDF.BackColor = System.Drawing.Color.Silver;
            this.button_GearBDF.FlatAppearance.BorderSize = 0;
            this.button_GearBDF.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_GearBDF.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_GearBDF.ForeColor = System.Drawing.Color.White;
            this.button_GearBDF.Location = new System.Drawing.Point(100, 40);
            this.button_GearBDF.Name = "button_GearBDF";
            this.button_GearBDF.Size = new System.Drawing.Size(75, 23);
            this.button_GearBDF.TabIndex = 2;
            this.button_GearBDF.Text = "GearBDF";
            this.button_GearBDF.UseVisualStyleBackColor = false;
            this.button_GearBDF.Click += new System.EventHandler(this.button_GearBDF_Click);
            // 
            // button_RK547M
            // 
            this.button_RK547M.BackColor = System.Drawing.Color.RoyalBlue;
            this.button_RK547M.FlatAppearance.BorderSize = 0;
            this.button_RK547M.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_RK547M.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_RK547M.ForeColor = System.Drawing.Color.White;
            this.button_RK547M.Location = new System.Drawing.Point(25, 40);
            this.button_RK547M.Name = "button_RK547M";
            this.button_RK547M.Size = new System.Drawing.Size(75, 23);
            this.button_RK547M.TabIndex = 1;
            this.button_RK547M.Text = "RK547M";
            this.button_RK547M.UseVisualStyleBackColor = false;
            this.button_RK547M.Click += new System.EventHandler(this.button_RK547M_Click);
            // 
            // label_Solvers
            // 
            this.label_Solvers.AutoSize = true;
            this.label_Solvers.Font = new System.Drawing.Font("Lucida Sans Unicode", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Solvers.ForeColor = System.Drawing.Color.White;
            this.label_Solvers.Location = new System.Drawing.Point(8, 16);
            this.label_Solvers.Name = "label_Solvers";
            this.label_Solvers.Size = new System.Drawing.Size(104, 20);
            this.label_Solvers.TabIndex = 0;
            this.label_Solvers.Text = "ODE Solvers";
            // 
            // panel_ModalPopUp
            // 
            this.panel_ModalPopUp.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.panel_ModalPopUp.Controls.Add(this.label_ModalPopUpText2);
            this.panel_ModalPopUp.Controls.Add(this.button_ModalPopUp_Cancel);
            this.panel_ModalPopUp.Controls.Add(this.button_ModalPopUp_OK);
            this.panel_ModalPopUp.Controls.Add(this.label_ModalPopUpText);
            this.panel_ModalPopUp.Location = new System.Drawing.Point(443, 81);
            this.panel_ModalPopUp.Name = "panel_ModalPopUp";
            this.panel_ModalPopUp.Size = new System.Drawing.Size(500, 179);
            this.panel_ModalPopUp.TabIndex = 12;
            // 
            // label_ModalPopUpText2
            // 
            this.label_ModalPopUpText2.AutoSize = true;
            this.label_ModalPopUpText2.Font = new System.Drawing.Font("Lucida Sans Unicode", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_ModalPopUpText2.ForeColor = System.Drawing.Color.White;
            this.label_ModalPopUpText2.Location = new System.Drawing.Point(66, 57);
            this.label_ModalPopUpText2.Name = "label_ModalPopUpText2";
            this.label_ModalPopUpText2.Size = new System.Drawing.Size(53, 20);
            this.label_ModalPopUpText2.TabIndex = 4;
            this.label_ModalPopUpText2.Text = "Line2";
            // 
            // button_ModalPopUp_Cancel
            // 
            this.button_ModalPopUp_Cancel.BackColor = System.Drawing.Color.RoyalBlue;
            this.button_ModalPopUp_Cancel.FlatAppearance.BorderSize = 0;
            this.button_ModalPopUp_Cancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_ModalPopUp_Cancel.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_ModalPopUp_Cancel.ForeColor = System.Drawing.Color.White;
            this.button_ModalPopUp_Cancel.Location = new System.Drawing.Point(266, 110);
            this.button_ModalPopUp_Cancel.Name = "button_ModalPopUp_Cancel";
            this.button_ModalPopUp_Cancel.Size = new System.Drawing.Size(75, 28);
            this.button_ModalPopUp_Cancel.TabIndex = 3;
            this.button_ModalPopUp_Cancel.Text = "Cancel";
            this.button_ModalPopUp_Cancel.UseVisualStyleBackColor = false;
            // 
            // button_ModalPopUp_OK
            // 
            this.button_ModalPopUp_OK.BackColor = System.Drawing.Color.Purple;
            this.button_ModalPopUp_OK.FlatAppearance.BorderSize = 0;
            this.button_ModalPopUp_OK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_ModalPopUp_OK.Font = new System.Drawing.Font("Lucida Sans Unicode", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_ModalPopUp_OK.ForeColor = System.Drawing.Color.White;
            this.button_ModalPopUp_OK.Location = new System.Drawing.Point(149, 110);
            this.button_ModalPopUp_OK.Name = "button_ModalPopUp_OK";
            this.button_ModalPopUp_OK.Size = new System.Drawing.Size(75, 28);
            this.button_ModalPopUp_OK.TabIndex = 2;
            this.button_ModalPopUp_OK.Text = "OK";
            this.button_ModalPopUp_OK.UseVisualStyleBackColor = false;
            // 
            // label_ModalPopUpText
            // 
            this.label_ModalPopUpText.AutoSize = true;
            this.label_ModalPopUpText.Font = new System.Drawing.Font("Lucida Sans Unicode", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_ModalPopUpText.ForeColor = System.Drawing.Color.White;
            this.label_ModalPopUpText.Location = new System.Drawing.Point(66, 31);
            this.label_ModalPopUpText.Name = "label_ModalPopUpText";
            this.label_ModalPopUpText.Size = new System.Drawing.Size(53, 20);
            this.label_ModalPopUpText.TabIndex = 0;
            this.label_ModalPopUpText.Text = "Line1";
            // 
            // flowLayoutPanel_Math
            // 
            this.flowLayoutPanel_Math.Location = new System.Drawing.Point(54, 260);
            this.flowLayoutPanel_Math.Name = "flowLayoutPanel_Math";
            this.flowLayoutPanel_Math.Size = new System.Drawing.Size(132, 221);
            this.flowLayoutPanel_Math.TabIndex = 13;
            // 
            // panel_Microfluidics
            // 
            this.panel_Microfluidics.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_Microfluidics.BackColor = System.Drawing.Color.Gold;
            this.panel_Microfluidics.Location = new System.Drawing.Point(644, 342);
            this.panel_Microfluidics.Name = "panel_Microfluidics";
            this.panel_Microfluidics.Size = new System.Drawing.Size(589, 336);
            this.panel_Microfluidics.TabIndex = 15;
            this.panel_Microfluidics.SizeChanged += new System.EventHandler(this.panel_Microfluidics_SizeChanged);
            // 
            // panel_Splash
            // 
            this.panel_Splash.BackColor = System.Drawing.Color.White;
            this.panel_Splash.BackgroundImage = global::KaemikaWPF.Properties.Resources.Splash_589;
            this.panel_Splash.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel_Splash.Location = new System.Drawing.Point(443, 318);
            this.panel_Splash.Name = "panel_Splash";
            this.panel_Splash.Size = new System.Drawing.Size(181, 163);
            this.panel_Splash.TabIndex = 14;
            // 
            // label_Version
            // 
            this.label_Version.AutoSize = true;
            this.label_Version.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.label_Version.Location = new System.Drawing.Point(10, 260);
            this.label_Version.Name = "label_Version";
            this.label_Version.Size = new System.Drawing.Size(90, 13);
            this.label_Version.TabIndex = 10;
            this.label_Version.Text = "Version 6.022e23";
            // 
            // GUI2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightSkyBlue;
            this.ClientSize = new System.Drawing.Size(1284, 681);
            this.Controls.Add(this.panel_Splash);
            this.Controls.Add(this.flowLayoutPanel_Math);
            this.Controls.Add(this.panel_ModalPopUp);
            this.Controls.Add(this.panel_Settings);
            this.Controls.Add(this.flowLayoutPanel_Export);
            this.Controls.Add(this.flowLayoutPanel_Noise);
            this.Controls.Add(this.flowLayoutPanel_Tutorial);
            this.Controls.Add(this.flowLayoutPanel_Parameters);
            this.Controls.Add(this.checkedListBox_Series);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.txtTarget);
            this.Controls.Add(this.richTextBox);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel_Microfluidics);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GUI2";
            this.Text = "Keamika";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GUI2_FormClosing);
            this.Load += new System.EventHandler(this.GUI2_Load);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel_Settings.ResumeLayout(false);
            this.panel_Settings.PerformLayout();
            this.panel_ModalPopUp.ResumeLayout(false);
            this.panel_ModalPopUp.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnEval;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RichTextBox richTextBox;
        public System.Windows.Forms.TextBox txtTarget;
        public System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Panel panel2;
        public System.Windows.Forms.CheckedListBox checkedListBox_Series;
        public System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button button_Device;
        public System.Windows.Forms.FlowLayoutPanel flowLayoutPanel_Parameters;
        private System.Windows.Forms.Button button_Tutorial;
        public System.Windows.Forms.FlowLayoutPanel flowLayoutPanel_Tutorial;
        private System.Windows.Forms.Button button_Source_Copy;
        private System.Windows.Forms.Button button_Source_Paste;
        public System.Windows.Forms.Button button_Noise;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel_Noise;
        private System.Windows.Forms.Button button_Export;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel_Export;
        private System.Windows.Forms.Button button_EditChart;
        private System.Windows.Forms.Button button_Parameters;
        private System.Windows.Forms.Button button_Settings;
        public System.Windows.Forms.Panel panel_Settings;
        private System.Windows.Forms.Button button_RK547M;
        private System.Windows.Forms.Label label_Solvers;
        private System.Windows.Forms.Button button_GearBDF;
        private System.Windows.Forms.Button button_PrecomputeLNA;
        private System.Windows.Forms.Label label_LNA;
        private System.Windows.Forms.Button button_TraceComputational;
        private System.Windows.Forms.Button button_TraceChemical;
        private System.Windows.Forms.Label label_Trace;
        private System.Windows.Forms.Panel panel_ModalPopUp;
        private System.Windows.Forms.Button button_ModalPopUp_Cancel;
        private System.Windows.Forms.Button button_ModalPopUp_OK;
        private System.Windows.Forms.Label label_ModalPopUpText;
        private System.Windows.Forms.Button button_Math;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel_Math;
        private System.Windows.Forms.Button button_FontSizeMinus;
        private System.Windows.Forms.Button button_FontSizePlus;
        private System.Windows.Forms.Panel panel_Splash;
        private System.Windows.Forms.Label label_OutputDirectory;
        private System.Windows.Forms.TextBox textBox_OutputDirectory;
        private System.Windows.Forms.Label label_ModalPopUpText2;
        public System.Windows.Forms.Panel panel_Microfluidics;
        private System.Windows.Forms.Button button_FlipMicrofluidics;
        private System.Windows.Forms.Label label_Version;
    }
}