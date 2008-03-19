namespace BioStream.Micado.User
{
    partial class SettingsUI
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageDrawing = new System.Windows.Forms.TabPage();
            this.groupBoxValve = new System.Windows.Forms.GroupBox();
            this.textBoxValveRelativeHeight = new System.Windows.Forms.TextBox();
            this.labelValveRelativeHeight = new System.Windows.Forms.Label();
            this.textBoxValveRelativeWidth = new System.Windows.Forms.TextBox();
            this.labelValveRelativeWidth = new System.Windows.Forms.Label();
            this.groupBoxPunch = new System.Windows.Forms.GroupBox();
            this.textBoxPunchBarWidth = new System.Windows.Forms.TextBox();
            this.labelPunchBarWidth = new System.Windows.Forms.Label();
            this.textBoxPunchBarNumber = new System.Windows.Forms.TextBox();
            this.labelPunchBarNumber = new System.Windows.Forms.Label();
            this.textBoxPunchRadius = new System.Windows.Forms.TextBox();
            this.labelPunchRadius = new System.Windows.Forms.Label();
            this.tabPageLayers = new System.Windows.Forms.TabPage();
            this.groupBoxControlLayers = new System.Windows.Forms.GroupBox();
            this.buttonAddToControlLayers = new System.Windows.Forms.Button();
            this.textBoxControlLayer = new System.Windows.Forms.TextBox();
            this.checkedListBoxControlLayers = new System.Windows.Forms.CheckedListBox();
            this.groupBoxFlowLayers = new System.Windows.Forms.GroupBox();
            this.buttonAddToFlowLayers = new System.Windows.Forms.Button();
            this.textBoxFlowLayer = new System.Windows.Forms.TextBox();
            this.checkedListBoxFlowLayers = new System.Windows.Forms.CheckedListBox();
            this.tabPageRouting = new System.Windows.Forms.TabPage();
            this.textBoxConnectionWidth = new System.Windows.Forms.TextBox();
            this.labelConnectionWidth = new System.Windows.Forms.Label();
            this.groupBoxExtraWidth = new System.Windows.Forms.GroupBox();
            this.textBoxControlLineExtraWidth = new System.Windows.Forms.TextBox();
            this.labelControlLineExtraWidth = new System.Windows.Forms.Label();
            this.textBoxValveExtraWidth = new System.Windows.Forms.TextBox();
            this.labelValveExtraWidth = new System.Windows.Forms.Label();
            this.textBoxFlowExtraWidth = new System.Windows.Forms.TextBox();
            this.labelFlowExtraWidth = new System.Windows.Forms.Label();
            this.groupBoxMinimumDistance = new System.Windows.Forms.GroupBox();
            this.textBoxPunch2Line = new System.Windows.Forms.TextBox();
            this.labelPunch2Line = new System.Windows.Forms.Label();
            this.textBoxResolution = new System.Windows.Forms.TextBox();
            this.labelResolution = new System.Windows.Forms.Label();
            this.buttonImport = new System.Windows.Forms.Button();
            this.buttonExport = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.tabControl.SuspendLayout();
            this.tabPageDrawing.SuspendLayout();
            this.groupBoxValve.SuspendLayout();
            this.groupBoxPunch.SuspendLayout();
            this.tabPageLayers.SuspendLayout();
            this.groupBoxControlLayers.SuspendLayout();
            this.groupBoxFlowLayers.SuspendLayout();
            this.tabPageRouting.SuspendLayout();
            this.groupBoxExtraWidth.SuspendLayout();
            this.groupBoxMinimumDistance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPageDrawing);
            this.tabControl.Controls.Add(this.tabPageLayers);
            this.tabControl.Controls.Add(this.tabPageRouting);
            this.tabControl.Location = new System.Drawing.Point(1, 3);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(321, 227);
            this.tabControl.TabIndex = 0;
            // 
            // tabPageDrawing
            // 
            this.tabPageDrawing.Controls.Add(this.groupBoxValve);
            this.tabPageDrawing.Controls.Add(this.groupBoxPunch);
            this.tabPageDrawing.Location = new System.Drawing.Point(4, 22);
            this.tabPageDrawing.Name = "tabPageDrawing";
            this.tabPageDrawing.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDrawing.Size = new System.Drawing.Size(313, 201);
            this.tabPageDrawing.TabIndex = 0;
            this.tabPageDrawing.Text = "Drawing";
            this.tabPageDrawing.UseVisualStyleBackColor = true;
            // 
            // groupBoxValve
            // 
            this.groupBoxValve.Controls.Add(this.textBoxValveRelativeHeight);
            this.groupBoxValve.Controls.Add(this.labelValveRelativeHeight);
            this.groupBoxValve.Controls.Add(this.textBoxValveRelativeWidth);
            this.groupBoxValve.Controls.Add(this.labelValveRelativeWidth);
            this.groupBoxValve.Location = new System.Drawing.Point(3, 123);
            this.groupBoxValve.Name = "groupBoxValve";
            this.groupBoxValve.Size = new System.Drawing.Size(307, 72);
            this.groupBoxValve.TabIndex = 1;
            this.groupBoxValve.TabStop = false;
            this.groupBoxValve.Text = "Valve";
            // 
            // textBoxValveRelativeHeight
            // 
            this.textBoxValveRelativeHeight.Location = new System.Drawing.Point(194, 41);
            this.textBoxValveRelativeHeight.Name = "textBoxValveRelativeHeight";
            this.textBoxValveRelativeHeight.Size = new System.Drawing.Size(90, 20);
            this.textBoxValveRelativeHeight.TabIndex = 4;
            this.textBoxValveRelativeHeight.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxValveRelativeHeight_Validating);
            // 
            // labelValveRelativeHeight
            // 
            this.labelValveRelativeHeight.AutoSize = true;
            this.labelValveRelativeHeight.Location = new System.Drawing.Point(2, 44);
            this.labelValveRelativeHeight.Name = "labelValveRelativeHeight";
            this.labelValveRelativeHeight.Size = new System.Drawing.Size(186, 13);
            this.labelValveRelativeHeight.TabIndex = 2;
            this.labelValveRelativeHeight.Text = "Valve Height to Flowline Width Ratio: ";
            // 
            // textBoxValveRelativeWidth
            // 
            this.textBoxValveRelativeWidth.Location = new System.Drawing.Point(194, 13);
            this.textBoxValveRelativeWidth.Name = "textBoxValveRelativeWidth";
            this.textBoxValveRelativeWidth.Size = new System.Drawing.Size(90, 20);
            this.textBoxValveRelativeWidth.TabIndex = 3;
            this.textBoxValveRelativeWidth.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxValveRelativeWidth_Validating);
            // 
            // labelValveRelativeWidth
            // 
            this.labelValveRelativeWidth.AutoSize = true;
            this.labelValveRelativeWidth.Location = new System.Drawing.Point(5, 16);
            this.labelValveRelativeWidth.Name = "labelValveRelativeWidth";
            this.labelValveRelativeWidth.Size = new System.Drawing.Size(183, 13);
            this.labelValveRelativeWidth.TabIndex = 0;
            this.labelValveRelativeWidth.Text = "Valve Width to Flowline Width Ratio: ";
            // 
            // groupBoxPunch
            // 
            this.groupBoxPunch.Controls.Add(this.textBoxPunchBarWidth);
            this.groupBoxPunch.Controls.Add(this.labelPunchBarWidth);
            this.groupBoxPunch.Controls.Add(this.textBoxPunchBarNumber);
            this.groupBoxPunch.Controls.Add(this.labelPunchBarNumber);
            this.groupBoxPunch.Controls.Add(this.textBoxPunchRadius);
            this.groupBoxPunch.Controls.Add(this.labelPunchRadius);
            this.groupBoxPunch.Location = new System.Drawing.Point(3, 6);
            this.groupBoxPunch.Name = "groupBoxPunch";
            this.groupBoxPunch.Size = new System.Drawing.Size(307, 100);
            this.groupBoxPunch.TabIndex = 0;
            this.groupBoxPunch.TabStop = false;
            this.groupBoxPunch.Text = "Punch";
            // 
            // textBoxPunchBarWidth
            // 
            this.textBoxPunchBarWidth.Location = new System.Drawing.Point(194, 74);
            this.textBoxPunchBarWidth.Name = "textBoxPunchBarWidth";
            this.textBoxPunchBarWidth.Size = new System.Drawing.Size(90, 20);
            this.textBoxPunchBarWidth.TabIndex = 2;
            this.textBoxPunchBarWidth.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxPunchBarWidth_Validating);
            // 
            // labelPunchBarWidth
            // 
            this.labelPunchBarWidth.AutoSize = true;
            this.labelPunchBarWidth.Location = new System.Drawing.Point(89, 77);
            this.labelPunchBarWidth.Name = "labelPunchBarWidth";
            this.labelPunchBarWidth.Size = new System.Drawing.Size(99, 13);
            this.labelPunchBarWidth.TabIndex = 4;
            this.labelPunchBarWidth.Text = "Width of each Bar: ";
            // 
            // textBoxPunchBarNumber
            // 
            this.textBoxPunchBarNumber.Location = new System.Drawing.Point(194, 43);
            this.textBoxPunchBarNumber.Name = "textBoxPunchBarNumber";
            this.textBoxPunchBarNumber.Size = new System.Drawing.Size(90, 20);
            this.textBoxPunchBarNumber.TabIndex = 1;
            this.textBoxPunchBarNumber.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxPunchBarNumber_Validating);
            // 
            // labelPunchBarNumber
            // 
            this.labelPunchBarNumber.AutoSize = true;
            this.labelPunchBarNumber.Location = new System.Drawing.Point(102, 46);
            this.labelPunchBarNumber.Name = "labelPunchBarNumber";
            this.labelPunchBarNumber.Size = new System.Drawing.Size(86, 13);
            this.labelPunchBarNumber.TabIndex = 2;
            this.labelPunchBarNumber.Text = "Number of Bars: ";
            // 
            // textBoxPunchRadius
            // 
            this.textBoxPunchRadius.Location = new System.Drawing.Point(194, 13);
            this.textBoxPunchRadius.Name = "textBoxPunchRadius";
            this.textBoxPunchRadius.Size = new System.Drawing.Size(90, 20);
            this.textBoxPunchRadius.TabIndex = 0;
            this.textBoxPunchRadius.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxPunchRadius_Validating);
            // 
            // labelPunchRadius
            // 
            this.labelPunchRadius.AutoSize = true;
            this.labelPunchRadius.Location = new System.Drawing.Point(108, 16);
            this.labelPunchRadius.Name = "labelPunchRadius";
            this.labelPunchRadius.Size = new System.Drawing.Size(80, 13);
            this.labelPunchRadius.TabIndex = 0;
            this.labelPunchRadius.Text = "Punch Radius: ";
            // 
            // tabPageLayers
            // 
            this.tabPageLayers.Controls.Add(this.groupBoxControlLayers);
            this.tabPageLayers.Controls.Add(this.groupBoxFlowLayers);
            this.tabPageLayers.Location = new System.Drawing.Point(4, 22);
            this.tabPageLayers.Name = "tabPageLayers";
            this.tabPageLayers.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLayers.Size = new System.Drawing.Size(313, 201);
            this.tabPageLayers.TabIndex = 1;
            this.tabPageLayers.Text = "Layers";
            this.tabPageLayers.UseVisualStyleBackColor = true;
            // 
            // groupBoxControlLayers
            // 
            this.groupBoxControlLayers.Controls.Add(this.buttonAddToControlLayers);
            this.groupBoxControlLayers.Controls.Add(this.textBoxControlLayer);
            this.groupBoxControlLayers.Controls.Add(this.checkedListBoxControlLayers);
            this.groupBoxControlLayers.Location = new System.Drawing.Point(3, 96);
            this.groupBoxControlLayers.Name = "groupBoxControlLayers";
            this.groupBoxControlLayers.Size = new System.Drawing.Size(307, 103);
            this.groupBoxControlLayers.TabIndex = 1;
            this.groupBoxControlLayers.TabStop = false;
            this.groupBoxControlLayers.Text = "Control Layers";
            // 
            // buttonAddToControlLayers
            // 
            this.buttonAddToControlLayers.Location = new System.Drawing.Point(6, 41);
            this.buttonAddToControlLayers.Name = "buttonAddToControlLayers";
            this.buttonAddToControlLayers.Size = new System.Drawing.Size(144, 23);
            this.buttonAddToControlLayers.TabIndex = 4;
            this.buttonAddToControlLayers.Text = "Add";
            this.buttonAddToControlLayers.UseVisualStyleBackColor = true;
            this.buttonAddToControlLayers.Click += new System.EventHandler(this.buttonAddToControlLayers_Click);
            // 
            // textBoxControlLayer
            // 
            this.textBoxControlLayer.Location = new System.Drawing.Point(6, 15);
            this.textBoxControlLayer.Name = "textBoxControlLayer";
            this.textBoxControlLayer.Size = new System.Drawing.Size(144, 20);
            this.textBoxControlLayer.TabIndex = 3;
            // 
            // checkedListBoxControlLayers
            // 
            this.checkedListBoxControlLayers.FormattingEnabled = true;
            this.checkedListBoxControlLayers.Location = new System.Drawing.Point(156, 15);
            this.checkedListBoxControlLayers.Name = "checkedListBoxControlLayers";
            this.checkedListBoxControlLayers.Size = new System.Drawing.Size(129, 79);
            this.checkedListBoxControlLayers.TabIndex = 5;
            this.checkedListBoxControlLayers.Validating += new System.ComponentModel.CancelEventHandler(this.checkedListBoxControlLayers_Validating);
            // 
            // groupBoxFlowLayers
            // 
            this.groupBoxFlowLayers.Controls.Add(this.buttonAddToFlowLayers);
            this.groupBoxFlowLayers.Controls.Add(this.textBoxFlowLayer);
            this.groupBoxFlowLayers.Controls.Add(this.checkedListBoxFlowLayers);
            this.groupBoxFlowLayers.Location = new System.Drawing.Point(3, 3);
            this.groupBoxFlowLayers.Name = "groupBoxFlowLayers";
            this.groupBoxFlowLayers.Size = new System.Drawing.Size(307, 87);
            this.groupBoxFlowLayers.TabIndex = 0;
            this.groupBoxFlowLayers.TabStop = false;
            this.groupBoxFlowLayers.Text = "Flow Layers";
            // 
            // buttonAddToFlowLayers
            // 
            this.buttonAddToFlowLayers.Location = new System.Drawing.Point(6, 41);
            this.buttonAddToFlowLayers.Name = "buttonAddToFlowLayers";
            this.buttonAddToFlowLayers.Size = new System.Drawing.Size(144, 23);
            this.buttonAddToFlowLayers.TabIndex = 1;
            this.buttonAddToFlowLayers.Text = "Add";
            this.buttonAddToFlowLayers.UseVisualStyleBackColor = true;
            this.buttonAddToFlowLayers.Click += new System.EventHandler(this.buttonAddToFlowLayers_Click);
            // 
            // textBoxFlowLayer
            // 
            this.textBoxFlowLayer.Location = new System.Drawing.Point(6, 15);
            this.textBoxFlowLayer.Name = "textBoxFlowLayer";
            this.textBoxFlowLayer.Size = new System.Drawing.Size(144, 20);
            this.textBoxFlowLayer.TabIndex = 0;
            // 
            // checkedListBoxFlowLayers
            // 
            this.checkedListBoxFlowLayers.FormattingEnabled = true;
            this.checkedListBoxFlowLayers.Location = new System.Drawing.Point(156, 15);
            this.checkedListBoxFlowLayers.Name = "checkedListBoxFlowLayers";
            this.checkedListBoxFlowLayers.Size = new System.Drawing.Size(129, 64);
            this.checkedListBoxFlowLayers.TabIndex = 2;
            this.checkedListBoxFlowLayers.Validating += new System.ComponentModel.CancelEventHandler(this.checkedListBoxFlowLayers_Validating);
            // 
            // tabPageRouting
            // 
            this.tabPageRouting.Controls.Add(this.textBoxConnectionWidth);
            this.tabPageRouting.Controls.Add(this.labelConnectionWidth);
            this.tabPageRouting.Controls.Add(this.groupBoxExtraWidth);
            this.tabPageRouting.Controls.Add(this.groupBoxMinimumDistance);
            this.tabPageRouting.Location = new System.Drawing.Point(4, 22);
            this.tabPageRouting.Name = "tabPageRouting";
            this.tabPageRouting.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageRouting.Size = new System.Drawing.Size(313, 201);
            this.tabPageRouting.TabIndex = 2;
            this.tabPageRouting.Text = "Routing";
            this.tabPageRouting.UseVisualStyleBackColor = true;
            // 
            // textBoxConnectionWidth
            // 
            this.textBoxConnectionWidth.Location = new System.Drawing.Point(194, 175);
            this.textBoxConnectionWidth.Name = "textBoxConnectionWidth";
            this.textBoxConnectionWidth.Size = new System.Drawing.Size(90, 20);
            this.textBoxConnectionWidth.TabIndex = 11;
            this.textBoxConnectionWidth.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxConnectionWidth_Validating);
            // 
            // labelConnectionWidth
            // 
            this.labelConnectionWidth.AutoSize = true;
            this.labelConnectionWidth.Location = new System.Drawing.Point(69, 178);
            this.labelConnectionWidth.Name = "labelConnectionWidth";
            this.labelConnectionWidth.Size = new System.Drawing.Size(121, 13);
            this.labelConnectionWidth.TabIndex = 10;
            this.labelConnectionWidth.Text = "Connection Line Width: ";
            // 
            // groupBoxExtraWidth
            // 
            this.groupBoxExtraWidth.Controls.Add(this.textBoxControlLineExtraWidth);
            this.groupBoxExtraWidth.Controls.Add(this.labelControlLineExtraWidth);
            this.groupBoxExtraWidth.Controls.Add(this.textBoxValveExtraWidth);
            this.groupBoxExtraWidth.Controls.Add(this.labelValveExtraWidth);
            this.groupBoxExtraWidth.Controls.Add(this.textBoxFlowExtraWidth);
            this.groupBoxExtraWidth.Controls.Add(this.labelFlowExtraWidth);
            this.groupBoxExtraWidth.Location = new System.Drawing.Point(6, 77);
            this.groupBoxExtraWidth.Name = "groupBoxExtraWidth";
            this.groupBoxExtraWidth.Size = new System.Drawing.Size(301, 91);
            this.groupBoxExtraWidth.TabIndex = 1;
            this.groupBoxExtraWidth.TabStop = false;
            this.groupBoxExtraWidth.Text = "Extra Space";
            // 
            // textBoxControlLineExtraWidth
            // 
            this.textBoxControlLineExtraWidth.Location = new System.Drawing.Point(188, 65);
            this.textBoxControlLineExtraWidth.Name = "textBoxControlLineExtraWidth";
            this.textBoxControlLineExtraWidth.Size = new System.Drawing.Size(90, 20);
            this.textBoxControlLineExtraWidth.TabIndex = 9;
            this.textBoxControlLineExtraWidth.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxControlLineExtraWidth_Validating);
            // 
            // labelControlLineExtraWidth
            // 
            this.labelControlLineExtraWidth.AutoSize = true;
            this.labelControlLineExtraWidth.Location = new System.Drawing.Point(70, 68);
            this.labelControlLineExtraWidth.Name = "labelControlLineExtraWidth";
            this.labelControlLineExtraWidth.Size = new System.Drawing.Size(114, 13);
            this.labelControlLineExtraWidth.TabIndex = 8;
            this.labelControlLineExtraWidth.Text = "around a Control Line: ";
            // 
            // textBoxValveExtraWidth
            // 
            this.textBoxValveExtraWidth.Location = new System.Drawing.Point(188, 39);
            this.textBoxValveExtraWidth.Name = "textBoxValveExtraWidth";
            this.textBoxValveExtraWidth.Size = new System.Drawing.Size(90, 20);
            this.textBoxValveExtraWidth.TabIndex = 7;
            this.textBoxValveExtraWidth.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxValveExtraWidth_Validating);
            // 
            // labelValveExtraWidth
            // 
            this.labelValveExtraWidth.AutoSize = true;
            this.labelValveExtraWidth.Location = new System.Drawing.Point(99, 42);
            this.labelValveExtraWidth.Name = "labelValveExtraWidth";
            this.labelValveExtraWidth.Size = new System.Drawing.Size(85, 13);
            this.labelValveExtraWidth.TabIndex = 6;
            this.labelValveExtraWidth.Text = "around a Valve: ";
            // 
            // textBoxFlowExtraWidth
            // 
            this.textBoxFlowExtraWidth.Location = new System.Drawing.Point(188, 13);
            this.textBoxFlowExtraWidth.Name = "textBoxFlowExtraWidth";
            this.textBoxFlowExtraWidth.Size = new System.Drawing.Size(90, 20);
            this.textBoxFlowExtraWidth.TabIndex = 5;
            this.textBoxFlowExtraWidth.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxFlowExtraWidth_Validating);
            // 
            // labelFlowExtraWidth
            // 
            this.labelFlowExtraWidth.AutoSize = true;
            this.labelFlowExtraWidth.Location = new System.Drawing.Point(88, 16);
            this.labelFlowExtraWidth.Name = "labelFlowExtraWidth";
            this.labelFlowExtraWidth.Size = new System.Drawing.Size(96, 13);
            this.labelFlowExtraWidth.TabIndex = 4;
            this.labelFlowExtraWidth.Text = "around a Flowline: ";
            // 
            // groupBoxMinimumDistance
            // 
            this.groupBoxMinimumDistance.Controls.Add(this.textBoxPunch2Line);
            this.groupBoxMinimumDistance.Controls.Add(this.labelPunch2Line);
            this.groupBoxMinimumDistance.Controls.Add(this.textBoxResolution);
            this.groupBoxMinimumDistance.Controls.Add(this.labelResolution);
            this.groupBoxMinimumDistance.Location = new System.Drawing.Point(6, 3);
            this.groupBoxMinimumDistance.Name = "groupBoxMinimumDistance";
            this.groupBoxMinimumDistance.Size = new System.Drawing.Size(301, 68);
            this.groupBoxMinimumDistance.TabIndex = 0;
            this.groupBoxMinimumDistance.TabStop = false;
            this.groupBoxMinimumDistance.Text = "Minimum Distance";
            // 
            // textBoxPunch2Line
            // 
            this.textBoxPunch2Line.Location = new System.Drawing.Point(188, 40);
            this.textBoxPunch2Line.Name = "textBoxPunch2Line";
            this.textBoxPunch2Line.Size = new System.Drawing.Size(90, 20);
            this.textBoxPunch2Line.TabIndex = 3;
            this.textBoxPunch2Line.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxPunch2Line_Validating);
            // 
            // labelPunch2Line
            // 
            this.labelPunch2Line.AutoSize = true;
            this.labelPunch2Line.Location = new System.Drawing.Point(1, 43);
            this.labelPunch2Line.Name = "labelPunch2Line";
            this.labelPunch2Line.Size = new System.Drawing.Size(183, 13);
            this.labelPunch2Line.TabIndex = 2;
            this.labelPunch2Line.Text = "from a Punch center to another Line: ";
            // 
            // textBoxResolution
            // 
            this.textBoxResolution.Location = new System.Drawing.Point(188, 13);
            this.textBoxResolution.Name = "textBoxResolution";
            this.textBoxResolution.Size = new System.Drawing.Size(90, 20);
            this.textBoxResolution.TabIndex = 1;
            this.textBoxResolution.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxResolution_Validating);
            // 
            // labelResolution
            // 
            this.labelResolution.AutoSize = true;
            this.labelResolution.Location = new System.Drawing.Point(45, 16);
            this.labelResolution.Name = "labelResolution";
            this.labelResolution.Size = new System.Drawing.Size(139, 13);
            this.labelResolution.TabIndex = 0;
            this.labelResolution.Text = "from a Line to another Line: ";
            // 
            // buttonImport
            // 
            this.buttonImport.Location = new System.Drawing.Point(5, 232);
            this.buttonImport.Name = "buttonImport";
            this.buttonImport.Size = new System.Drawing.Size(75, 23);
            this.buttonImport.TabIndex = 1;
            this.buttonImport.Text = "Import...";
            this.buttonImport.UseVisualStyleBackColor = true;
            this.buttonImport.Click += new System.EventHandler(this.buttonImport_Click);
            // 
            // buttonExport
            // 
            this.buttonExport.Location = new System.Drawing.Point(243, 232);
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.Size = new System.Drawing.Size(75, 23);
            this.buttonExport.TabIndex = 2;
            this.buttonExport.Text = "Export...";
            this.buttonExport.UseVisualStyleBackColor = true;
            this.buttonExport.Click += new System.EventHandler(this.buttonExport_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(86, 248);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(163, 248);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 4;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // SettingsUI
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(323, 278);
            this.ControlBox = false;
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonExport);
            this.Controls.Add(this.buttonImport);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsUI";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Micado Settings";
            this.Load += new System.EventHandler(this.SettingsUI_Load);
            this.tabControl.ResumeLayout(false);
            this.tabPageDrawing.ResumeLayout(false);
            this.groupBoxValve.ResumeLayout(false);
            this.groupBoxValve.PerformLayout();
            this.groupBoxPunch.ResumeLayout(false);
            this.groupBoxPunch.PerformLayout();
            this.tabPageLayers.ResumeLayout(false);
            this.groupBoxControlLayers.ResumeLayout(false);
            this.groupBoxControlLayers.PerformLayout();
            this.groupBoxFlowLayers.ResumeLayout(false);
            this.groupBoxFlowLayers.PerformLayout();
            this.tabPageRouting.ResumeLayout(false);
            this.tabPageRouting.PerformLayout();
            this.groupBoxExtraWidth.ResumeLayout(false);
            this.groupBoxExtraWidth.PerformLayout();
            this.groupBoxMinimumDistance.ResumeLayout(false);
            this.groupBoxMinimumDistance.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageDrawing;
        private System.Windows.Forms.TabPage tabPageLayers;
        private System.Windows.Forms.GroupBox groupBoxValve;
        private System.Windows.Forms.GroupBox groupBoxPunch;
        private System.Windows.Forms.Label labelValveRelativeHeight;
        private System.Windows.Forms.TextBox textBoxValveRelativeWidth;
        private System.Windows.Forms.Label labelValveRelativeWidth;
        private System.Windows.Forms.TextBox textBoxValveRelativeHeight;
        private System.Windows.Forms.TextBox textBoxPunchRadius;
        private System.Windows.Forms.Label labelPunchRadius;
        private System.Windows.Forms.Label labelPunchBarNumber;
        private System.Windows.Forms.TextBox textBoxPunchBarWidth;
        private System.Windows.Forms.Label labelPunchBarWidth;
        private System.Windows.Forms.TextBox textBoxPunchBarNumber;
        private System.Windows.Forms.TabPage tabPageRouting;
        private System.Windows.Forms.GroupBox groupBoxFlowLayers;
        private System.Windows.Forms.CheckedListBox checkedListBoxFlowLayers;
        private System.Windows.Forms.TextBox textBoxFlowLayer;
        private System.Windows.Forms.GroupBox groupBoxControlLayers;
        private System.Windows.Forms.Button buttonAddToControlLayers;
        private System.Windows.Forms.TextBox textBoxControlLayer;
        private System.Windows.Forms.CheckedListBox checkedListBoxControlLayers;
        private System.Windows.Forms.GroupBox groupBoxMinimumDistance;
        private System.Windows.Forms.TextBox textBoxPunch2Line;
        private System.Windows.Forms.Label labelPunch2Line;
        private System.Windows.Forms.TextBox textBoxResolution;
        private System.Windows.Forms.Label labelResolution;
        private System.Windows.Forms.GroupBox groupBoxExtraWidth;
        private System.Windows.Forms.TextBox textBoxFlowExtraWidth;
        private System.Windows.Forms.Label labelFlowExtraWidth;
        private System.Windows.Forms.TextBox textBoxValveExtraWidth;
        private System.Windows.Forms.Label labelValveExtraWidth;
        private System.Windows.Forms.TextBox textBoxControlLineExtraWidth;
        private System.Windows.Forms.Label labelControlLineExtraWidth;
        private System.Windows.Forms.TextBox textBoxConnectionWidth;
        private System.Windows.Forms.Label labelConnectionWidth;
        private System.Windows.Forms.Button buttonImport;
        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonAddToFlowLayers;
        private System.Windows.Forms.ErrorProvider errorProvider;
    }
}