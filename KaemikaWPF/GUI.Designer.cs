namespace KaemikaWPF
{
    partial class GUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GUI));
            this.btnConstruct = new System.Windows.Forms.Button();
            this.txtTarget = new System.Windows.Forms.TextBox();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.btnEval = new System.Windows.Forms.Button();
            this.btnScope = new System.Windows.Forms.Button();
            this.checkBox_ScopeVariants = new System.Windows.Forms.CheckBox();
            this.label_Kaemika = new System.Windows.Forms.Label();
            this.label_Version = new System.Windows.Forms.Label();
            this.button_Source_Copy = new System.Windows.Forms.Button();
            this.button_Source_Paste = new System.Windows.Forms.Button();
            this.checkBox_RemapVariants = new System.Windows.Forms.CheckBox();
            this.btnParse = new System.Windows.Forms.Button();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.comboBox_Examples = new System.Windows.Forms.ComboBox();
            this.comboBox_Solvers = new System.Windows.Forms.ComboBox();
            this.btnStop = new System.Windows.Forms.Button();
            this.checkBox_LNA = new System.Windows.Forms.CheckBox();
            this.groupBox_LNA = new System.Windows.Forms.GroupBox();
            this.radioButton_LNA_CV = new System.Windows.Forms.RadioButton();
            this.radioButton_LNA_Fano = new System.Windows.Forms.RadioButton();
            this.radioButton_LNA_VarRange = new System.Windows.Forms.RadioButton();
            this.radioButton_LNA_SDRange = new System.Windows.Forms.RadioButton();
            this.radioButton_LNA_Var = new System.Windows.Forms.RadioButton();
            this.radioButton_LNA_SD = new System.Windows.Forms.RadioButton();
            this.checkedListBox_Series = new System.Windows.Forms.CheckedListBox();
            this.button_ChartSnap = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.radioButton_TraceComputational = new System.Windows.Forms.RadioButton();
            this.radioButton_TraceChemical = new System.Windows.Forms.RadioButton();
            this.numericUpDown_Transparency = new System.Windows.Forms.NumericUpDown();
            this.button_Target_Copy = new System.Windows.Forms.Button();
            this.checkBoxButton_EditChart = new System.Windows.Forms.CheckBox();
            this.comboBox_Export = new System.Windows.Forms.ComboBox();
            this.checkBox_ChartGrid = new System.Windows.Forms.CheckBox();
            this.checkBox_CharAxes = new System.Windows.Forms.CheckBox();
            this.checkBox_precomputeLNA = new System.Windows.Forms.CheckBox();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.button_Continue = new System.Windows.Forms.Button();
            this.groupBox_Trace = new System.Windows.Forms.GroupBox();
            this.comboBox_Sub = new System.Windows.Forms.ComboBox();
            this.label_Sub = new System.Windows.Forms.Label();
            this.comboBox_Sup = new System.Windows.Forms.ComboBox();
            this.label_Sup = new System.Windows.Forms.Label();
            this.panel_Header = new System.Windows.Forms.Panel();
            this.deviceButton = new System.Windows.Forms.Button();
            this.label_Math = new System.Windows.Forms.Label();
            this.comboBox_Math = new System.Windows.Forms.ComboBox();
            this.panel_Simulate = new System.Windows.Forms.Panel();
            this.label_Solvers = new System.Windows.Forms.Label();
            this.panel_Controls = new System.Windows.Forms.Panel();
            this.label_Parameters = new System.Windows.Forms.Label();
            this.checkBoxButton_Parameters = new System.Windows.Forms.CheckBox();
            this.label_Transparency = new System.Windows.Forms.Label();
            this.label_Legend = new System.Windows.Forms.Label();
            this.tableLayoutPanel_LeftColumn = new System.Windows.Forms.TableLayoutPanel();
            this.panel_richTextBoxPadding = new System.Windows.Forms.Panel();
            this.flowLayoutPanel_Parameters = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel_RightColumn = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel_Columns = new System.Windows.Forms.TableLayoutPanel();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.groupBox_LNA.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Transparency)).BeginInit();
            this.groupBox_Trace.SuspendLayout();
            this.panel_Header.SuspendLayout();
            this.panel_Simulate.SuspendLayout();
            this.panel_Controls.SuspendLayout();
            this.tableLayoutPanel_LeftColumn.SuspendLayout();
            this.panel_richTextBoxPadding.SuspendLayout();
            this.flowLayoutPanel_Parameters.SuspendLayout();
            this.tableLayoutPanel_RightColumn.SuspendLayout();
            this.tableLayoutPanel_Columns.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnConstruct
            // 
            this.btnConstruct.Location = new System.Drawing.Point(64, 1175);
            this.btnConstruct.Name = "btnConstruct";
            this.btnConstruct.Size = new System.Drawing.Size(47, 31);
            this.btnConstruct.TabIndex = 7;
            this.btnConstruct.Text = "Build";
            this.toolTip1.SetToolTip(this.btnConstruct, "Build the abstract syntax tree (debug)");
            this.btnConstruct.UseVisualStyleBackColor = true;
            this.btnConstruct.Visible = false;
            this.btnConstruct.Click += new System.EventHandler(this.btnConstruct_Click);
            // 
            // txtTarget
            // 
            this.txtTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTarget.BackColor = System.Drawing.SystemColors.Window;
            this.txtTarget.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTarget.Location = new System.Drawing.Point(3, 52);
            this.txtTarget.Multiline = true;
            this.txtTarget.Name = "txtTarget";
            this.txtTarget.ReadOnly = true;
            this.txtTarget.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtTarget.Size = new System.Drawing.Size(630, 288);
            this.txtTarget.TabIndex = 6;
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // btnEval
            // 
            this.btnEval.BackColor = System.Drawing.Color.LightSalmon;
            this.btnEval.Location = new System.Drawing.Point(12, 11);
            this.btnEval.Name = "btnEval";
            this.btnEval.Size = new System.Drawing.Size(96, 32);
            this.btnEval.TabIndex = 8;
            this.btnEval.Text = "Play";
            this.toolTip1.SetToolTip(this.btnEval, "Execute the program\r\n(Parse + Build + Scope + Eval)");
            this.btnEval.UseVisualStyleBackColor = false;
            this.btnEval.Click += new System.EventHandler(this.btnEval_Click);
            // 
            // btnScope
            // 
            this.btnScope.Location = new System.Drawing.Point(117, 1175);
            this.btnScope.Name = "btnScope";
            this.btnScope.Size = new System.Drawing.Size(47, 31);
            this.btnScope.TabIndex = 9;
            this.btnScope.Text = "Scope";
            this.toolTip1.SetToolTip(this.btnScope, "Validate the use of program variables");
            this.btnScope.UseVisualStyleBackColor = true;
            this.btnScope.Visible = false;
            this.btnScope.Click += new System.EventHandler(this.btnScope_Click);
            // 
            // checkBox_ScopeVariants
            // 
            this.checkBox_ScopeVariants.AutoSize = true;
            this.checkBox_ScopeVariants.Checked = true;
            this.checkBox_ScopeVariants.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox_ScopeVariants.Location = new System.Drawing.Point(170, 1170);
            this.checkBox_ScopeVariants.Name = "checkBox_ScopeVariants";
            this.checkBox_ScopeVariants.Size = new System.Drawing.Size(91, 17);
            this.checkBox_ScopeVariants.TabIndex = 10;
            this.checkBox_ScopeVariants.Text = "show variants";
            this.toolTip1.SetToolTip(this.checkBox_ScopeVariants, "If distinct variables have the same name\r\ndistinguish them by a unique number");
            this.checkBox_ScopeVariants.UseVisualStyleBackColor = true;
            this.checkBox_ScopeVariants.Visible = false;
            this.checkBox_ScopeVariants.CheckedChanged += new System.EventHandler(this.checkBox_ScopeVariants_CheckedChanged);
            // 
            // label_Kaemika
            // 
            this.label_Kaemika.AutoSize = true;
            this.label_Kaemika.Font = new System.Drawing.Font("Matura MT Script Capitals", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Kaemika.Location = new System.Drawing.Point(4, 0);
            this.label_Kaemika.Name = "label_Kaemika";
            this.label_Kaemika.Size = new System.Drawing.Size(150, 42);
            this.label_Kaemika.TabIndex = 11;
            this.label_Kaemika.Text = "Kaemika";
            // 
            // label_Version
            // 
            this.label_Version.AutoSize = true;
            this.label_Version.ForeColor = System.Drawing.Color.DarkGreen;
            this.label_Version.Location = new System.Drawing.Point(55, 35);
            this.label_Version.Name = "label_Version";
            this.label_Version.Size = new System.Drawing.Size(90, 13);
            this.label_Version.TabIndex = 12;
            this.label_Version.Text = "Version 6.022e23";
            // 
            // button_Source_Copy
            // 
            this.button_Source_Copy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Source_Copy.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.button_Source_Copy.Location = new System.Drawing.Point(381, 11);
            this.button_Source_Copy.Name = "button_Source_Copy";
            this.button_Source_Copy.Size = new System.Drawing.Size(75, 32);
            this.button_Source_Copy.TabIndex = 14;
            this.button_Source_Copy.Text = "Copy";
            this.toolTip1.SetToolTip(this.button_Source_Copy, "Copy Kaemika program to clipboard");
            this.button_Source_Copy.UseVisualStyleBackColor = false;
            this.button_Source_Copy.Click += new System.EventHandler(this.button_Source_Copy_Click);
            // 
            // button_Source_Paste
            // 
            this.button_Source_Paste.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Source_Paste.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.button_Source_Paste.Location = new System.Drawing.Point(462, 11);
            this.button_Source_Paste.Name = "button_Source_Paste";
            this.button_Source_Paste.Size = new System.Drawing.Size(75, 32);
            this.button_Source_Paste.TabIndex = 15;
            this.button_Source_Paste.Text = "Paste";
            this.toolTip1.SetToolTip(this.button_Source_Paste, "Paste Kaemika program from clipboard");
            this.button_Source_Paste.UseVisualStyleBackColor = false;
            this.button_Source_Paste.Click += new System.EventHandler(this.button_Source_Paste_Click);
            // 
            // checkBox_RemapVariants
            // 
            this.checkBox_RemapVariants.AutoSize = true;
            this.checkBox_RemapVariants.Checked = true;
            this.checkBox_RemapVariants.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox_RemapVariants.Location = new System.Drawing.Point(170, 1186);
            this.checkBox_RemapVariants.Name = "checkBox_RemapVariants";
            this.checkBox_RemapVariants.Size = new System.Drawing.Size(95, 17);
            this.checkBox_RemapVariants.TabIndex = 17;
            this.checkBox_RemapVariants.Text = "remap variants";
            this.toolTip1.SetToolTip(this.checkBox_RemapVariants, "Remap the unique varible numbers from 0");
            this.checkBox_RemapVariants.UseVisualStyleBackColor = true;
            this.checkBox_RemapVariants.Visible = false;
            this.checkBox_RemapVariants.CheckedChanged += new System.EventHandler(this.checkBox_RemapVariants_CheckedChanged);
            // 
            // btnParse
            // 
            this.btnParse.Location = new System.Drawing.Point(11, 1175);
            this.btnParse.Name = "btnParse";
            this.btnParse.Size = new System.Drawing.Size(47, 31);
            this.btnParse.TabIndex = 20;
            this.btnParse.Text = "Parse";
            this.toolTip1.SetToolTip(this.btnParse, "Validate the program syntax");
            this.btnParse.UseVisualStyleBackColor = true;
            this.btnParse.Visible = false;
            this.btnParse.Click += new System.EventHandler(this.btnParse_Click);
            // 
            // chart1
            // 
            this.chart1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chart1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
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
            chartArea1.BorderWidth = 10;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            legend1.IsTextAutoFit = false;
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(6, 386);
            this.chart1.Margin = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.chart1.Name = "chart1";
            this.chart1.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.Bright;
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Legend = "Legend1";
            series1.Name = "-";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(624, 286);
            this.chart1.SuppressExceptions = true;
            this.chart1.TabIndex = 24;
            this.chart1.Text = "chart1";
            this.toolTip1.SetToolTip(this.chart1, "pinch or mouse-wheel to zoom\r\ndrag to scroll when zoomed\r\ndouble-click to cancel " +
        "zoom");
            this.chart1.Click += new System.EventHandler(this.chart1_Click);
            // 
            // comboBox_Examples
            // 
            this.comboBox_Examples.BackColor = System.Drawing.SystemColors.Window;
            this.comboBox_Examples.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Examples.FormattingEnabled = true;
            this.comboBox_Examples.Location = new System.Drawing.Point(160, 18);
            this.comboBox_Examples.MaxDropDownItems = 100;
            this.comboBox_Examples.Name = "comboBox_Examples";
            this.comboBox_Examples.Size = new System.Drawing.Size(121, 21);
            this.comboBox_Examples.TabIndex = 25;
            this.comboBox_Examples.SelectedIndexChanged += new System.EventHandler(this.comboBox_Examples_SelectedIndexChanged);
            // 
            // comboBox_Solvers
            // 
            this.comboBox_Solvers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Solvers.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox_Solvers.FormattingEnabled = true;
            this.comboBox_Solvers.Items.AddRange(new object[] {
            "RK547M",
            "GearBDF"});
            this.comboBox_Solvers.Location = new System.Drawing.Point(313, 14);
            this.comboBox_Solvers.Name = "comboBox_Solvers";
            this.comboBox_Solvers.Size = new System.Drawing.Size(66, 20);
            this.comboBox_Solvers.TabIndex = 27;
            this.comboBox_Solvers.SelectedIndexChanged += new System.EventHandler(this.comboBox_Solvers_SelectedIndexChanged);
            // 
            // btnStop
            // 
            this.btnStop.BackColor = System.Drawing.Color.Gainsboro;
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(183, 11);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(96, 32);
            this.btnStop.TabIndex = 29;
            this.btnStop.Text = "Stop";
            this.toolTip1.SetToolTip(this.btnStop, "Stop program execution");
            this.btnStop.UseVisualStyleBackColor = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // checkBox_LNA
            // 
            this.checkBox_LNA.AutoSize = true;
            this.checkBox_LNA.ForeColor = System.Drawing.Color.DarkGreen;
            this.checkBox_LNA.Location = new System.Drawing.Point(5, 11);
            this.checkBox_LNA.Name = "checkBox_LNA";
            this.checkBox_LNA.Size = new System.Drawing.Size(47, 17);
            this.checkBox_LNA.TabIndex = 30;
            this.checkBox_LNA.Text = "LNA";
            this.toolTip1.SetToolTip(this.checkBox_LNA, "Compute and plot the Linear Noise Approximation");
            this.checkBox_LNA.UseVisualStyleBackColor = true;
            this.checkBox_LNA.CheckedChanged += new System.EventHandler(this.checkBox_LNA_CheckedChanged);
            // 
            // groupBox_LNA
            // 
            this.groupBox_LNA.Controls.Add(this.radioButton_LNA_CV);
            this.groupBox_LNA.Controls.Add(this.radioButton_LNA_Fano);
            this.groupBox_LNA.Controls.Add(this.radioButton_LNA_VarRange);
            this.groupBox_LNA.Controls.Add(this.radioButton_LNA_SDRange);
            this.groupBox_LNA.Controls.Add(this.radioButton_LNA_Var);
            this.groupBox_LNA.Controls.Add(this.radioButton_LNA_SD);
            this.groupBox_LNA.Controls.Add(this.checkBox_LNA);
            this.groupBox_LNA.Location = new System.Drawing.Point(286, 1);
            this.groupBox_LNA.Margin = new System.Windows.Forms.Padding(0);
            this.groupBox_LNA.Name = "groupBox_LNA";
            this.groupBox_LNA.Size = new System.Drawing.Size(194, 45);
            this.groupBox_LNA.TabIndex = 32;
            this.groupBox_LNA.TabStop = false;
            // 
            // radioButton_LNA_CV
            // 
            this.radioButton_LNA_CV.AutoSize = true;
            this.radioButton_LNA_CV.Enabled = false;
            this.radioButton_LNA_CV.Location = new System.Drawing.Point(146, 10);
            this.radioButton_LNA_CV.Name = "radioButton_LNA_CV";
            this.radioButton_LNA_CV.Size = new System.Drawing.Size(43, 17);
            this.radioButton_LNA_CV.TabIndex = 36;
            this.radioButton_LNA_CV.TabStop = true;
            this.radioButton_LNA_CV.Text = "σ/μ";
            this.toolTip1.SetToolTip(this.radioButton_LNA_CV, "Plot the coefficient of variation");
            this.radioButton_LNA_CV.UseVisualStyleBackColor = true;
            // 
            // radioButton_LNA_Fano
            // 
            this.radioButton_LNA_Fano.AutoSize = true;
            this.radioButton_LNA_Fano.Enabled = false;
            this.radioButton_LNA_Fano.Location = new System.Drawing.Point(146, 27);
            this.radioButton_LNA_Fano.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.radioButton_LNA_Fano.Name = "radioButton_LNA_Fano";
            this.radioButton_LNA_Fano.Size = new System.Drawing.Size(46, 17);
            this.radioButton_LNA_Fano.TabIndex = 35;
            this.radioButton_LNA_Fano.Text = "σ²/μ";
            this.toolTip1.SetToolTip(this.radioButton_LNA_Fano, "Plot the Fano factor");
            this.radioButton_LNA_Fano.UseVisualStyleBackColor = true;
            // 
            // radioButton_LNA_VarRange
            // 
            this.radioButton_LNA_VarRange.AutoSize = true;
            this.radioButton_LNA_VarRange.Enabled = false;
            this.radioButton_LNA_VarRange.Location = new System.Drawing.Point(57, 27);
            this.radioButton_LNA_VarRange.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.radioButton_LNA_VarRange.Name = "radioButton_LNA_VarRange";
            this.radioButton_LNA_VarRange.Size = new System.Drawing.Size(44, 17);
            this.radioButton_LNA_VarRange.TabIndex = 34;
            this.radioButton_LNA_VarRange.Text = "± σ²";
            this.toolTip1.SetToolTip(this.radioButton_LNA_VarRange, "Plot variance as a band");
            this.radioButton_LNA_VarRange.UseVisualStyleBackColor = true;
            // 
            // radioButton_LNA_SDRange
            // 
            this.radioButton_LNA_SDRange.AutoSize = true;
            this.radioButton_LNA_SDRange.Checked = true;
            this.radioButton_LNA_SDRange.Enabled = false;
            this.radioButton_LNA_SDRange.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton_LNA_SDRange.ForeColor = System.Drawing.SystemColors.ControlText;
            this.radioButton_LNA_SDRange.Location = new System.Drawing.Point(57, 10);
            this.radioButton_LNA_SDRange.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.radioButton_LNA_SDRange.Name = "radioButton_LNA_SDRange";
            this.radioButton_LNA_SDRange.Size = new System.Drawing.Size(41, 17);
            this.radioButton_LNA_SDRange.TabIndex = 33;
            this.radioButton_LNA_SDRange.TabStop = true;
            this.radioButton_LNA_SDRange.Text = "± σ";
            this.toolTip1.SetToolTip(this.radioButton_LNA_SDRange, "Plot standard deviation as a band");
            this.radioButton_LNA_SDRange.UseVisualStyleBackColor = true;
            // 
            // radioButton_LNA_Var
            // 
            this.radioButton_LNA_Var.AutoSize = true;
            this.radioButton_LNA_Var.Enabled = false;
            this.radioButton_LNA_Var.Location = new System.Drawing.Point(106, 27);
            this.radioButton_LNA_Var.Name = "radioButton_LNA_Var";
            this.radioButton_LNA_Var.Size = new System.Drawing.Size(35, 17);
            this.radioButton_LNA_Var.TabIndex = 32;
            this.radioButton_LNA_Var.Text = "σ²";
            this.toolTip1.SetToolTip(this.radioButton_LNA_Var, "Plot variance");
            this.radioButton_LNA_Var.UseVisualStyleBackColor = true;
            this.radioButton_LNA_Var.CheckedChanged += new System.EventHandler(this.radioButton_LNA_Var_CheckedChanged);
            // 
            // radioButton_LNA_SD
            // 
            this.radioButton_LNA_SD.AutoSize = true;
            this.radioButton_LNA_SD.Enabled = false;
            this.radioButton_LNA_SD.Location = new System.Drawing.Point(106, 10);
            this.radioButton_LNA_SD.Name = "radioButton_LNA_SD";
            this.radioButton_LNA_SD.Size = new System.Drawing.Size(32, 17);
            this.radioButton_LNA_SD.TabIndex = 31;
            this.radioButton_LNA_SD.Text = "σ";
            this.toolTip1.SetToolTip(this.radioButton_LNA_SD, "Plot standard deviation");
            this.radioButton_LNA_SD.UseVisualStyleBackColor = true;
            this.radioButton_LNA_SD.CheckedChanged += new System.EventHandler(this.radioButton_LNA_SD_CheckedChanged);
            // 
            // checkedListBox_Series
            // 
            this.checkedListBox_Series.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBox_Series.BackColor = System.Drawing.Color.Linen;
            this.checkedListBox_Series.CheckOnClick = true;
            this.checkedListBox_Series.Font = new System.Drawing.Font("Lucida Sans Unicode", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkedListBox_Series.FormattingEnabled = true;
            this.checkedListBox_Series.Location = new System.Drawing.Point(2, 2);
            this.checkedListBox_Series.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.checkedListBox_Series.MultiColumn = true;
            this.checkedListBox_Series.Name = "checkedListBox_Series";
            this.checkedListBox_Series.Size = new System.Drawing.Size(329, 99);
            this.checkedListBox_Series.TabIndex = 33;
            this.checkedListBox_Series.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox_Series_ItemCheck);
            this.checkedListBox_Series.SelectedIndexChanged += new System.EventHandler(this.checkedListBox_Series_SelectedIndexChanged);
            // 
            // button_ChartSnap
            // 
            this.button_ChartSnap.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.button_ChartSnap.Location = new System.Drawing.Point(483, 5);
            this.button_ChartSnap.Name = "button_ChartSnap";
            this.button_ChartSnap.Size = new System.Drawing.Size(75, 32);
            this.button_ChartSnap.TabIndex = 35;
            this.button_ChartSnap.Text = "Snap";
            this.toolTip1.SetToolTip(this.button_ChartSnap, resources.GetString("button_ChartSnap.ToolTip"));
            this.button_ChartSnap.UseVisualStyleBackColor = false;
            this.button_ChartSnap.Click += new System.EventHandler(this.button_ChartSnap_Click);
            // 
            // radioButton_TraceComputational
            // 
            this.radioButton_TraceComputational.AutoSize = true;
            this.radioButton_TraceComputational.ForeColor = System.Drawing.Color.DarkGreen;
            this.radioButton_TraceComputational.Location = new System.Drawing.Point(5, 27);
            this.radioButton_TraceComputational.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.radioButton_TraceComputational.Name = "radioButton_TraceComputational";
            this.radioButton_TraceComputational.Size = new System.Drawing.Size(123, 17);
            this.radioButton_TraceComputational.TabIndex = 1;
            this.radioButton_TraceComputational.Text = "Computational Trace";
            this.toolTip1.SetToolTip(this.radioButton_TraceComputational, "Output all defined entities");
            this.radioButton_TraceComputational.UseVisualStyleBackColor = true;
            this.radioButton_TraceComputational.CheckedChanged += new System.EventHandler(this.radioButton_TraceComputational_CheckedChanged);
            // 
            // radioButton_TraceChemical
            // 
            this.radioButton_TraceChemical.AutoSize = true;
            this.radioButton_TraceChemical.Checked = true;
            this.radioButton_TraceChemical.ForeColor = System.Drawing.Color.DarkGreen;
            this.radioButton_TraceChemical.Location = new System.Drawing.Point(5, 8);
            this.radioButton_TraceChemical.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.radioButton_TraceChemical.Name = "radioButton_TraceChemical";
            this.radioButton_TraceChemical.Size = new System.Drawing.Size(99, 17);
            this.radioButton_TraceChemical.TabIndex = 0;
            this.radioButton_TraceChemical.TabStop = true;
            this.radioButton_TraceChemical.Text = "Chemical Trace";
            this.toolTip1.SetToolTip(this.radioButton_TraceChemical, "Output only samples, species, and reactions");
            this.radioButton_TraceChemical.UseVisualStyleBackColor = true;
            this.radioButton_TraceChemical.CheckedChanged += new System.EventHandler(this.radioButton_TraceChemical_CheckedChanged);
            // 
            // numericUpDown_Transparency
            // 
            this.numericUpDown_Transparency.Location = new System.Drawing.Point(437, 17);
            this.numericUpDown_Transparency.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numericUpDown_Transparency.Name = "numericUpDown_Transparency";
            this.numericUpDown_Transparency.Size = new System.Drawing.Size(39, 20);
            this.numericUpDown_Transparency.TabIndex = 40;
            this.toolTip1.SetToolTip(this.numericUpDown_Transparency, "Transparency of bands");
            this.numericUpDown_Transparency.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.numericUpDown_Transparency.ValueChanged += new System.EventHandler(this.numericUpDown_Transparency_ValueChanged);
            // 
            // button_Target_Copy
            // 
            this.button_Target_Copy.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.button_Target_Copy.Location = new System.Drawing.Point(135, 5);
            this.button_Target_Copy.Name = "button_Target_Copy";
            this.button_Target_Copy.Size = new System.Drawing.Size(75, 32);
            this.button_Target_Copy.TabIndex = 16;
            this.button_Target_Copy.Text = "Copy";
            this.toolTip1.SetToolTip(this.button_Target_Copy, "Copy text output to clipboard");
            this.button_Target_Copy.UseVisualStyleBackColor = false;
            this.button_Target_Copy.Click += new System.EventHandler(this.button_Target_Copy_Click);
            // 
            // checkBoxButton_EditChart
            // 
            this.checkBoxButton_EditChart.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxButton_EditChart.AutoSize = true;
            this.checkBoxButton_EditChart.BackColor = System.Drawing.Color.Linen;
            this.checkBoxButton_EditChart.Location = new System.Drawing.Point(564, 14);
            this.checkBoxButton_EditChart.Name = "checkBoxButton_EditChart";
            this.checkBoxButton_EditChart.Size = new System.Drawing.Size(53, 23);
            this.checkBoxButton_EditChart.TabIndex = 34;
            this.checkBoxButton_EditChart.Text = "Legend";
            this.toolTip1.SetToolTip(this.checkBoxButton_EditChart, "Select data series to plot\r\n\r\nRemembered across \r\nsimulations, if left open");
            this.checkBoxButton_EditChart.UseVisualStyleBackColor = false;
            this.checkBoxButton_EditChart.CheckedChanged += new System.EventHandler(this.checkBoxButton_EditChart_CheckedChanged);
            // 
            // comboBox_Export
            // 
            this.comboBox_Export.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Export.FormattingEnabled = true;
            this.comboBox_Export.Items.AddRange(new object[] {
            "   Export",
            "Chemical Trace",
            "Computational Trace",
            "Reaction Graph",
            "Reaction Complex Graph",
            "Protocol Step Graph",
            "Protocol State Graph",
            "System Reactions",
            "System Equations",
            "System Stoichiometry",
            " ",
            "Protocol",
            "ODE (Oscill8)",
            "Equilibrium (Wolfram)",
            "CRN (LBS html5)",
            "CRN (LBS silverlight)",
            "Last simulation state"});
            this.comboBox_Export.Location = new System.Drawing.Point(6, 12);
            this.comboBox_Export.Name = "comboBox_Export";
            this.comboBox_Export.Size = new System.Drawing.Size(123, 21);
            this.comboBox_Export.TabIndex = 37;
            this.toolTip1.SetToolTip(this.comboBox_Export, "Export and copy result to clipboard");
            this.comboBox_Export.SelectedIndexChanged += new System.EventHandler(this.comboBox_Export_SelectedIndexChanged);
            // 
            // checkBox_ChartGrid
            // 
            this.checkBox_ChartGrid.AutoSize = true;
            this.checkBox_ChartGrid.Location = new System.Drawing.Point(385, 21);
            this.checkBox_ChartGrid.Name = "checkBox_ChartGrid";
            this.checkBox_ChartGrid.Size = new System.Drawing.Size(43, 17);
            this.checkBox_ChartGrid.TabIndex = 41;
            this.checkBox_ChartGrid.Text = "grid";
            this.toolTip1.SetToolTip(this.checkBox_ChartGrid, "Show/hide grid");
            this.checkBox_ChartGrid.UseVisualStyleBackColor = true;
            this.checkBox_ChartGrid.CheckedChanged += new System.EventHandler(this.checkBox_ChartGrid_CheckedChanged);
            // 
            // checkBox_CharAxes
            // 
            this.checkBox_CharAxes.AutoSize = true;
            this.checkBox_CharAxes.Checked = true;
            this.checkBox_CharAxes.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox_CharAxes.Location = new System.Drawing.Point(385, 4);
            this.checkBox_CharAxes.Name = "checkBox_CharAxes";
            this.checkBox_CharAxes.Size = new System.Drawing.Size(48, 17);
            this.checkBox_CharAxes.TabIndex = 42;
            this.checkBox_CharAxes.Text = "axes";
            this.toolTip1.SetToolTip(this.checkBox_CharAxes, "Show/hide axes");
            this.checkBox_CharAxes.UseVisualStyleBackColor = true;
            this.checkBox_CharAxes.CheckedChanged += new System.EventHandler(this.checkBox_CharAxes_CheckedChanged);
            // 
            // checkBox_precomputeLNA
            // 
            this.checkBox_precomputeLNA.AutoSize = true;
            this.checkBox_precomputeLNA.Location = new System.Drawing.Point(292, 19);
            this.checkBox_precomputeLNA.Name = "checkBox_precomputeLNA";
            this.checkBox_precomputeLNA.Size = new System.Drawing.Size(15, 14);
            this.checkBox_precomputeLNA.TabIndex = 45;
            this.toolTip1.SetToolTip(this.checkBox_precomputeLNA, "Precompute LNA drift matrix (big)");
            this.checkBox_precomputeLNA.UseVisualStyleBackColor = false;
            this.checkBox_precomputeLNA.CheckedChanged += new System.EventHandler(this.CheckBox_precomputeLNA_CheckedChanged);
            // 
            // richTextBox
            // 
            this.richTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox.Font = new System.Drawing.Font("Lucida Sans Typewriter", 7.85F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox.Location = new System.Drawing.Point(4, 0);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.Size = new System.Drawing.Size(620, 615);
            this.richTextBox.TabIndex = 47;
            this.richTextBox.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10\n11\n12\n13\n14\n15\n16\n17\n18\n19\n20\n21\n22\n23\n24\n25\n26\n27\n28\n29\n30\n" +
    "31\n32\n33\n34";
            this.richTextBox.WordWrap = false;
            // 
            // button_Continue
            // 
            this.button_Continue.BackColor = System.Drawing.Color.Gainsboro;
            this.button_Continue.Enabled = false;
            this.button_Continue.Location = new System.Drawing.Point(113, 11);
            this.button_Continue.Name = "button_Continue";
            this.button_Continue.Size = new System.Drawing.Size(64, 32);
            this.button_Continue.TabIndex = 38;
            this.button_Continue.Text = "Continue";
            this.button_Continue.UseVisualStyleBackColor = false;
            this.button_Continue.Click += new System.EventHandler(this.button_Continue_Click);
            // 
            // groupBox_Trace
            // 
            this.groupBox_Trace.Controls.Add(this.radioButton_TraceComputational);
            this.groupBox_Trace.Controls.Add(this.radioButton_TraceChemical);
            this.groupBox_Trace.Location = new System.Drawing.Point(487, 1);
            this.groupBox_Trace.Margin = new System.Windows.Forms.Padding(0);
            this.groupBox_Trace.Name = "groupBox_Trace";
            this.groupBox_Trace.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.groupBox_Trace.Size = new System.Drawing.Size(133, 45);
            this.groupBox_Trace.TabIndex = 39;
            this.groupBox_Trace.TabStop = false;
            // 
            // comboBox_Sub
            // 
            this.comboBox_Sub.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_Sub.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Sub.FormattingEnabled = true;
            this.comboBox_Sub.Items.AddRange(new object[] {
            "_",
            "₊",
            "₋",
            "₌",
            "₀",
            "₁",
            "₂",
            "₃",
            "₄",
            "₅",
            "₆",
            "₇",
            "₈",
            "₉",
            "₍",
            "₎"});
            this.comboBox_Sub.Location = new System.Drawing.Point(568, 27);
            this.comboBox_Sub.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.comboBox_Sub.Name = "comboBox_Sub";
            this.comboBox_Sub.Size = new System.Drawing.Size(21, 21);
            this.comboBox_Sub.TabIndex = 43;
            this.comboBox_Sub.SelectedIndexChanged += new System.EventHandler(this.comboBox_Sub_SelectedIndexChanged);
            // 
            // label_Sub
            // 
            this.label_Sub.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label_Sub.AutoSize = true;
            this.label_Sub.ForeColor = System.Drawing.Color.DarkGreen;
            this.label_Sub.Location = new System.Drawing.Point(567, 13);
            this.label_Sub.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_Sub.Name = "label_Sub";
            this.label_Sub.Size = new System.Drawing.Size(26, 13);
            this.label_Sub.TabIndex = 44;
            this.label_Sub.Text = "Sub";
            // 
            // comboBox_Sup
            // 
            this.comboBox_Sup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_Sup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Sup.FormattingEnabled = true;
            this.comboBox_Sup.Items.AddRange(new object[] {
            "\'",
            "⁺",
            "⁻",
            "⁼",
            "⁰",
            "¹",
            "²",
            "³",
            "⁴",
            "⁵",
            "⁶",
            "⁷",
            "⁸",
            "⁹",
            "⁽",
            "⁾"});
            this.comboBox_Sup.Location = new System.Drawing.Point(594, 16);
            this.comboBox_Sup.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.comboBox_Sup.Name = "comboBox_Sup";
            this.comboBox_Sup.Size = new System.Drawing.Size(21, 21);
            this.comboBox_Sup.TabIndex = 45;
            this.comboBox_Sup.SelectedIndexChanged += new System.EventHandler(this.comboBox_Sup_SelectedIndexChanged);
            // 
            // label_Sup
            // 
            this.label_Sup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label_Sup.AutoSize = true;
            this.label_Sup.ForeColor = System.Drawing.Color.DarkGreen;
            this.label_Sup.Location = new System.Drawing.Point(591, 0);
            this.label_Sup.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_Sup.Name = "label_Sup";
            this.label_Sup.Size = new System.Drawing.Size(26, 13);
            this.label_Sup.TabIndex = 46;
            this.label_Sup.Text = "Sup";
            // 
            // panel_Header
            // 
            this.panel_Header.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_Header.Controls.Add(this.deviceButton);
            this.panel_Header.Controls.Add(this.label_Math);
            this.panel_Header.Controls.Add(this.comboBox_Math);
            this.panel_Header.Controls.Add(this.label_Version);
            this.panel_Header.Controls.Add(this.label_Kaemika);
            this.panel_Header.Controls.Add(this.comboBox_Examples);
            this.panel_Header.Controls.Add(this.label_Sup);
            this.panel_Header.Controls.Add(this.comboBox_Sup);
            this.panel_Header.Controls.Add(this.label_Sub);
            this.panel_Header.Controls.Add(this.comboBox_Sub);
            this.panel_Header.Controls.Add(this.button_Source_Paste);
            this.panel_Header.Controls.Add(this.button_Source_Copy);
            this.panel_Header.Location = new System.Drawing.Point(0, 0);
            this.panel_Header.Margin = new System.Windows.Forms.Padding(0);
            this.panel_Header.Name = "panel_Header";
            this.panel_Header.Size = new System.Drawing.Size(636, 49);
            this.panel_Header.TabIndex = 36;
            // 
            // deviceButton
            // 
            this.deviceButton.BackColor = System.Drawing.Color.Gold;
            this.deviceButton.Location = new System.Drawing.Point(308, 16);
            this.deviceButton.Name = "deviceButton";
            this.deviceButton.Size = new System.Drawing.Size(52, 23);
            this.deviceButton.TabIndex = 49;
            this.deviceButton.Text = "Device";
            this.deviceButton.UseVisualStyleBackColor = false;
            this.deviceButton.Click += new System.EventHandler(this.deviceButton_Click);
            // 
            // label_Math
            // 
            this.label_Math.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label_Math.AutoSize = true;
            this.label_Math.ForeColor = System.Drawing.Color.DarkGreen;
            this.label_Math.Location = new System.Drawing.Point(546, 5);
            this.label_Math.Name = "label_Math";
            this.label_Math.Size = new System.Drawing.Size(15, 13);
            this.label_Math.TabIndex = 48;
            this.label_Math.Text = "∑";
            // 
            // comboBox_Math
            // 
            this.comboBox_Math.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_Math.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Math.DropDownWidth = 21;
            this.comboBox_Math.FormattingEnabled = true;
            this.comboBox_Math.Items.AddRange(new object[] {
            "∂",
            "μ",
            "σ",
            "±",
            "ʃ",
            "√",
            "∑",
            "∏"});
            this.comboBox_Math.Location = new System.Drawing.Point(543, 21);
            this.comboBox_Math.Name = "comboBox_Math";
            this.comboBox_Math.Size = new System.Drawing.Size(22, 21);
            this.comboBox_Math.TabIndex = 47;
            this.comboBox_Math.SelectedIndexChanged += new System.EventHandler(this.comboBox_Math_SelectedIndexChanged);
            // 
            // panel_Simulate
            // 
            this.panel_Simulate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_Simulate.Controls.Add(this.groupBox_LNA);
            this.panel_Simulate.Controls.Add(this.groupBox_Trace);
            this.panel_Simulate.Controls.Add(this.button_Continue);
            this.panel_Simulate.Controls.Add(this.btnStop);
            this.panel_Simulate.Controls.Add(this.btnEval);
            this.panel_Simulate.Location = new System.Drawing.Point(0, 0);
            this.panel_Simulate.Margin = new System.Windows.Forms.Padding(0);
            this.panel_Simulate.Name = "panel_Simulate";
            this.panel_Simulate.Size = new System.Drawing.Size(636, 49);
            this.panel_Simulate.TabIndex = 48;
            // 
            // label_Solvers
            // 
            this.label_Solvers.AutoSize = true;
            this.label_Solvers.ForeColor = System.Drawing.Color.DarkGreen;
            this.label_Solvers.Location = new System.Drawing.Point(313, 0);
            this.label_Solvers.Name = "label_Solvers";
            this.label_Solvers.Size = new System.Drawing.Size(42, 13);
            this.label_Solvers.TabIndex = 28;
            this.label_Solvers.Text = "Solvers";
            // 
            // panel_Controls
            // 
            this.panel_Controls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_Controls.Controls.Add(this.label_Parameters);
            this.panel_Controls.Controls.Add(this.checkBoxButton_Parameters);
            this.panel_Controls.Controls.Add(this.checkBox_precomputeLNA);
            this.panel_Controls.Controls.Add(this.checkBoxButton_EditChart);
            this.panel_Controls.Controls.Add(this.label_Transparency);
            this.panel_Controls.Controls.Add(this.label_Legend);
            this.panel_Controls.Controls.Add(this.checkBox_CharAxes);
            this.panel_Controls.Controls.Add(this.comboBox_Solvers);
            this.panel_Controls.Controls.Add(this.checkBox_ChartGrid);
            this.panel_Controls.Controls.Add(this.comboBox_Export);
            this.panel_Controls.Controls.Add(this.label_Solvers);
            this.panel_Controls.Controls.Add(this.button_ChartSnap);
            this.panel_Controls.Controls.Add(this.numericUpDown_Transparency);
            this.panel_Controls.Controls.Add(this.button_Target_Copy);
            this.panel_Controls.Location = new System.Drawing.Point(0, 343);
            this.panel_Controls.Margin = new System.Windows.Forms.Padding(0);
            this.panel_Controls.Name = "panel_Controls";
            this.panel_Controls.Size = new System.Drawing.Size(636, 39);
            this.panel_Controls.TabIndex = 49;
            // 
            // label_Parameters
            // 
            this.label_Parameters.AutoSize = true;
            this.label_Parameters.Location = new System.Drawing.Point(233, 1);
            this.label_Parameters.Name = "label_Parameters";
            this.label_Parameters.Size = new System.Drawing.Size(25, 13);
            this.label_Parameters.TabIndex = 47;
            this.label_Parameters.Text = "↑↑↑";
            // 
            // checkBoxButton_Parameters
            // 
            this.checkBoxButton_Parameters.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxButton_Parameters.AutoSize = true;
            this.checkBoxButton_Parameters.BackColor = System.Drawing.Color.Linen;
            this.checkBoxButton_Parameters.Location = new System.Drawing.Point(216, 14);
            this.checkBoxButton_Parameters.Name = "checkBoxButton_Parameters";
            this.checkBoxButton_Parameters.Size = new System.Drawing.Size(70, 23);
            this.checkBoxButton_Parameters.TabIndex = 46;
            this.checkBoxButton_Parameters.Text = "Parameters";
            this.checkBoxButton_Parameters.UseVisualStyleBackColor = false;
            this.checkBoxButton_Parameters.CheckedChanged += new System.EventHandler(this.CheckBoxButton_Parameters_CheckedChanged);
            // 
            // label_Transparency
            // 
            this.label_Transparency.AutoSize = true;
            this.label_Transparency.Location = new System.Drawing.Point(433, 4);
            this.label_Transparency.Name = "label_Transparency";
            this.label_Transparency.Size = new System.Drawing.Size(33, 13);
            this.label_Transparency.TabIndex = 44;
            this.label_Transparency.Text = "alpha";
            // 
            // label_Legend
            // 
            this.label_Legend.AutoSize = true;
            this.label_Legend.Location = new System.Drawing.Point(571, 1);
            this.label_Legend.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_Legend.Name = "label_Legend";
            this.label_Legend.Size = new System.Drawing.Size(25, 13);
            this.label_Legend.TabIndex = 43;
            this.label_Legend.Text = "↑↑↑";
            // 
            // tableLayoutPanel_LeftColumn
            // 
            this.tableLayoutPanel_LeftColumn.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel_LeftColumn.ColumnCount = 1;
            this.tableLayoutPanel_LeftColumn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_LeftColumn.Controls.Add(this.panel_richTextBoxPadding, 0, 1);
            this.tableLayoutPanel_LeftColumn.Controls.Add(this.panel_Header, 0, 0);
            this.tableLayoutPanel_LeftColumn.Location = new System.Drawing.Point(2, 2);
            this.tableLayoutPanel_LeftColumn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tableLayoutPanel_LeftColumn.Name = "tableLayoutPanel_LeftColumn";
            this.tableLayoutPanel_LeftColumn.RowCount = 2;
            this.tableLayoutPanel_LeftColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.tableLayoutPanel_LeftColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_LeftColumn.Size = new System.Drawing.Size(636, 676);
            this.tableLayoutPanel_LeftColumn.TabIndex = 0;
            // 
            // panel_richTextBoxPadding
            // 
            this.panel_richTextBoxPadding.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_richTextBoxPadding.BackColor = System.Drawing.Color.White;
            this.panel_richTextBoxPadding.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel_richTextBoxPadding.Controls.Add(this.richTextBox);
            this.panel_richTextBoxPadding.Location = new System.Drawing.Point(3, 52);
            this.panel_richTextBoxPadding.Name = "panel_richTextBoxPadding";
            this.panel_richTextBoxPadding.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.panel_richTextBoxPadding.Size = new System.Drawing.Size(630, 621);
            this.panel_richTextBoxPadding.TabIndex = 35;
            // 
            // flowLayoutPanel_Parameters
            // 
            this.flowLayoutPanel_Parameters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel_Parameters.AutoScroll = true;
            this.flowLayoutPanel_Parameters.BackColor = System.Drawing.Color.Linen;
            this.flowLayoutPanel_Parameters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanel_Parameters.Controls.Add(this.checkedListBox_Series);
            this.flowLayoutPanel_Parameters.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel_Parameters.Location = new System.Drawing.Point(1020, 480);
            this.flowLayoutPanel_Parameters.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.flowLayoutPanel_Parameters.Name = "flowLayoutPanel_Parameters";
            this.flowLayoutPanel_Parameters.Size = new System.Drawing.Size(290, 213);
            this.flowLayoutPanel_Parameters.TabIndex = 49;
            this.flowLayoutPanel_Parameters.WrapContents = false;
            // 
            // tableLayoutPanel_RightColumn
            // 
            this.tableLayoutPanel_RightColumn.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel_RightColumn.ColumnCount = 1;
            this.tableLayoutPanel_RightColumn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_RightColumn.Controls.Add(this.panel_Controls, 0, 2);
            this.tableLayoutPanel_RightColumn.Controls.Add(this.panel_Simulate, 0, 0);
            this.tableLayoutPanel_RightColumn.Controls.Add(this.chart1, 0, 3);
            this.tableLayoutPanel_RightColumn.Controls.Add(this.txtTarget, 0, 1);
            this.tableLayoutPanel_RightColumn.Location = new System.Drawing.Point(642, 2);
            this.tableLayoutPanel_RightColumn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tableLayoutPanel_RightColumn.Name = "tableLayoutPanel_RightColumn";
            this.tableLayoutPanel_RightColumn.RowCount = 4;
            this.tableLayoutPanel_RightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.tableLayoutPanel_RightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_RightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 39F));
            this.tableLayoutPanel_RightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_RightColumn.Size = new System.Drawing.Size(636, 676);
            this.tableLayoutPanel_RightColumn.TabIndex = 1;
            // 
            // tableLayoutPanel_Columns
            // 
            this.tableLayoutPanel_Columns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel_Columns.ColumnCount = 2;
            this.tableLayoutPanel_Columns.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Columns.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel_Columns.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel_Columns.Controls.Add(this.tableLayoutPanel_RightColumn, 1, 0);
            this.tableLayoutPanel_Columns.Controls.Add(this.tableLayoutPanel_LeftColumn, 0, 0);
            this.tableLayoutPanel_Columns.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel_Columns.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tableLayoutPanel_Columns.Name = "tableLayoutPanel_Columns";
            this.tableLayoutPanel_Columns.RowCount = 1;
            this.tableLayoutPanel_Columns.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_Columns.Size = new System.Drawing.Size(1280, 680);
            this.tableLayoutPanel_Columns.TabIndex = 34;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Text = "notifyIcon1";
            this.notifyIcon1.Visible = true;
            // 
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1284, 681);
            this.Controls.Add(this.flowLayoutPanel_Parameters);
            this.Controls.Add(this.btnParse);
            this.Controls.Add(this.checkBox_RemapVariants);
            this.Controls.Add(this.checkBox_ScopeVariants);
            this.Controls.Add(this.btnScope);
            this.Controls.Add(this.btnConstruct);
            this.Controls.Add(this.tableLayoutPanel_Columns);
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(694, 58);
            this.Name = "GUI";
            this.Text = "Kaemika";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.groupBox_LNA.ResumeLayout(false);
            this.groupBox_LNA.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Transparency)).EndInit();
            this.groupBox_Trace.ResumeLayout(false);
            this.groupBox_Trace.PerformLayout();
            this.panel_Header.ResumeLayout(false);
            this.panel_Header.PerformLayout();
            this.panel_Simulate.ResumeLayout(false);
            this.panel_Controls.ResumeLayout(false);
            this.panel_Controls.PerformLayout();
            this.tableLayoutPanel_LeftColumn.ResumeLayout(false);
            this.panel_richTextBoxPadding.ResumeLayout(false);
            this.flowLayoutPanel_Parameters.ResumeLayout(false);
            this.tableLayoutPanel_RightColumn.ResumeLayout(false);
            this.tableLayoutPanel_RightColumn.PerformLayout();
            this.tableLayoutPanel_Columns.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        internal System.Windows.Forms.Button btnConstruct;
        internal System.Windows.Forms.TextBox txtTarget;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Button btnEval;
        private System.Windows.Forms.Button btnScope;
        public System.Windows.Forms.CheckBox checkBox_ScopeVariants;
        private System.Windows.Forms.Label label_Kaemika;
        private System.Windows.Forms.Label label_Version;
        private System.Windows.Forms.Button button_Source_Copy;
        private System.Windows.Forms.Button button_Source_Paste;
        public System.Windows.Forms.CheckBox checkBox_RemapVariants;
        private System.Windows.Forms.Button btnParse;
        public System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.ComboBox comboBox_Examples;
        public System.Windows.Forms.ComboBox comboBox_Solvers;
        public System.Windows.Forms.Button btnStop;
        public System.Windows.Forms.CheckBox checkBox_LNA;
        private System.Windows.Forms.GroupBox groupBox_LNA;
        private System.Windows.Forms.RadioButton radioButton_LNA_Var;
        private System.Windows.Forms.RadioButton radioButton_LNA_SD;
        public System.Windows.Forms.CheckedListBox checkedListBox_Series;
        private System.Windows.Forms.Button button_ChartSnap;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.RichTextBox richTextBox;
        private System.Windows.Forms.RadioButton radioButton_LNA_Fano;
        private System.Windows.Forms.RadioButton radioButton_LNA_VarRange;
        private System.Windows.Forms.RadioButton radioButton_LNA_SDRange;
        private System.Windows.Forms.RadioButton radioButton_LNA_CV;
        public System.Windows.Forms.Button button_Continue;
        private System.Windows.Forms.GroupBox groupBox_Trace;
        public System.Windows.Forms.RadioButton radioButton_TraceComputational;
        public System.Windows.Forms.RadioButton radioButton_TraceChemical;
        private System.Windows.Forms.NumericUpDown numericUpDown_Transparency;
        private System.Windows.Forms.ComboBox comboBox_Sub;
        private System.Windows.Forms.Label label_Sub;
        private System.Windows.Forms.ComboBox comboBox_Sup;
        private System.Windows.Forms.Label label_Sup;
        private System.Windows.Forms.Panel panel_Header;
        private System.Windows.Forms.Label label_Math;
        private System.Windows.Forms.ComboBox comboBox_Math;
        private System.Windows.Forms.Panel panel_Simulate;
        private System.Windows.Forms.Button button_Target_Copy;
        private System.Windows.Forms.Label label_Solvers;
        private System.Windows.Forms.CheckBox checkBoxButton_EditChart;
        private System.Windows.Forms.ComboBox comboBox_Export;
        private System.Windows.Forms.CheckBox checkBox_ChartGrid;
        private System.Windows.Forms.CheckBox checkBox_CharAxes;
        private System.Windows.Forms.Panel panel_Controls;
        private System.Windows.Forms.Label label_Legend;
        private System.Windows.Forms.Label label_Transparency;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_LeftColumn;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_RightColumn;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_Columns;
        private System.Windows.Forms.Panel panel_richTextBoxPadding;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        public System.Windows.Forms.CheckBox checkBox_precomputeLNA;
        public System.Windows.Forms.FlowLayoutPanel flowLayoutPanel_Parameters;
        private System.Windows.Forms.CheckBox checkBoxButton_Parameters;
        private System.Windows.Forms.Label label_Parameters;
        private System.Windows.Forms.Button deviceButton;
    }
}

