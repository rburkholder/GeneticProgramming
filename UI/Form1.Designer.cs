namespace UI {
  partial class frmMain {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose( bool disposing ) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      this.panel1 = new System.Windows.Forms.Panel();
      this.tbMain = new System.Windows.Forms.TextBox();
      this.panel2 = new System.Windows.Forms.Panel();
      this.btnExit = new System.Windows.Forms.Button();
      this.btnStart = new System.Windows.Forms.Button();
      this.btnStop = new System.Windows.Forms.Button();
      this.btnPrintGen = new System.Windows.Forms.Button();
      this.btnCopyToClip = new System.Windows.Forms.Button();
      this.panel1.SuspendLayout();
      this.panel2.SuspendLayout();
      this.SuspendLayout();
      // 
      // panel1
      // 
      this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.panel1.Controls.Add(this.tbMain);
      this.panel1.Location = new System.Drawing.Point(12, 12);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(465, 467);
      this.panel1.TabIndex = 5;
      // 
      // tbMain
      // 
      this.tbMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.tbMain.Location = new System.Drawing.Point(3, 3);
      this.tbMain.Multiline = true;
      this.tbMain.Name = "tbMain";
      this.tbMain.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.tbMain.Size = new System.Drawing.Size(459, 461);
      this.tbMain.TabIndex = 5;
      // 
      // panel2
      // 
      this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.panel2.Controls.Add(this.btnCopyToClip);
      this.panel2.Controls.Add(this.btnPrintGen);
      this.panel2.Controls.Add(this.btnStop);
      this.panel2.Controls.Add(this.btnExit);
      this.panel2.Controls.Add(this.btnStart);
      this.panel2.Location = new System.Drawing.Point(15, 485);
      this.panel2.Name = "panel2";
      this.panel2.Size = new System.Drawing.Size(465, 30);
      this.panel2.TabIndex = 6;
      // 
      // btnExit
      // 
      this.btnExit.Anchor = System.Windows.Forms.AnchorStyles.Right;
      this.btnExit.Location = new System.Drawing.Point(387, 3);
      this.btnExit.Name = "btnExit";
      this.btnExit.Size = new System.Drawing.Size(75, 23);
      this.btnExit.TabIndex = 8;
      this.btnExit.Text = "Exit";
      this.btnExit.UseVisualStyleBackColor = true;
      this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
      // 
      // btnStart
      // 
      this.btnStart.Location = new System.Drawing.Point(3, 3);
      this.btnStart.Name = "btnStart";
      this.btnStart.Size = new System.Drawing.Size(75, 23);
      this.btnStart.TabIndex = 7;
      this.btnStart.Text = "Optimize";
      this.btnStart.UseVisualStyleBackColor = true;
      this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
      // 
      // btnStop
      // 
      this.btnStop.Location = new System.Drawing.Point(84, 3);
      this.btnStop.Name = "btnStop";
      this.btnStop.Size = new System.Drawing.Size(75, 23);
      this.btnStop.TabIndex = 9;
      this.btnStop.Text = "Stop";
      this.btnStop.UseVisualStyleBackColor = true;
      this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
      // 
      // btnPrintGen
      // 
      this.btnPrintGen.Location = new System.Drawing.Point(166, 3);
      this.btnPrintGen.Name = "btnPrintGen";
      this.btnPrintGen.Size = new System.Drawing.Size(75, 23);
      this.btnPrintGen.TabIndex = 10;
      this.btnPrintGen.Text = "Print Gen";
      this.btnPrintGen.UseVisualStyleBackColor = true;
      this.btnPrintGen.Click += new System.EventHandler(this.btnPrintGen_Click);
      // 
      // btnCopyToClip
      // 
      this.btnCopyToClip.Location = new System.Drawing.Point(247, 3);
      this.btnCopyToClip.Name = "btnCopyToClip";
      this.btnCopyToClip.Size = new System.Drawing.Size(75, 23);
      this.btnCopyToClip.TabIndex = 11;
      this.btnCopyToClip.Text = "Copy To Clip";
      this.btnCopyToClip.UseVisualStyleBackColor = true;
      this.btnCopyToClip.Click += new System.EventHandler(this.btnCopyToClip_Click);
      // 
      // frmMain
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(489, 527);
      this.Controls.Add(this.panel2);
      this.Controls.Add(this.panel1);
      this.Name = "frmMain";
      this.Text = "GP Test";
      this.Load += new System.EventHandler(this.Form1_Load);
      this.panel1.ResumeLayout(false);
      this.panel1.PerformLayout();
      this.panel2.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.TextBox tbMain;
    private System.Windows.Forms.Panel panel2;
    private System.Windows.Forms.Button btnExit;
    private System.Windows.Forms.Button btnStart;
    private System.Windows.Forms.Button btnCopyToClip;
    private System.Windows.Forms.Button btnPrintGen;
    private System.Windows.Forms.Button btnStop;
  }
}

