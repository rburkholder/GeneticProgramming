using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace UI {
  public partial class frmMain : Form {

    ConsoleRedirect cr;
    Startup startup;

    public frmMain() {
      InitializeComponent();
      cr = new ConsoleRedirect(tbMain);
      Console.SetOut(cr);
      startup = new Startup();
    }

    private void Form1_Load( object sender, EventArgs e ) {

    }

    private void btnStart_Click( object sender, EventArgs e ) {
      startup.Optimize();
    }

    private void btnExit_Click( object sender, EventArgs e ) {
      Application.Exit();
    }

    private void btnStop_Click( object sender, EventArgs e ) {
      startup.Stop();
    }

    private void btnPrintGen_Click( object sender, EventArgs e ) {

    }

    private void btnCopyToClip_Click( object sender, EventArgs e ) {
      tbMain.Copy();
    }

   }

  public class ConsoleRedirect : System.IO.TextWriter {

    private TextBox tb;

    public ConsoleRedirect( TextBox tb ) {
      this.tb = tb;
    }

    public override void Write( string value ) {
      //base.Write(value);
      tb.AppendText(value);
    }

    public override void WriteLine( string value ) {
      //base.WriteLine(value);
      tb.AppendText(value);
      tb.AppendText(Environment.NewLine);

    }

    public override Encoding Encoding {
      get { throw new Exception("The method or operation is not implemented."); }
    }
  }
}