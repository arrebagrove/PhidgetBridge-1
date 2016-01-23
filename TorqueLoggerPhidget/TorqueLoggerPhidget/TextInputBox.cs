using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TorqueLoggerPhidget
{
	public partial class TextInputBox : Form
	{
		public TextInputBox() {
			InitializeComponent();
		}

		public TextInputBox(string title, string string1, string string2) {
			InitializeComponent();
			this.Text = title;
			message1.Text = string1;
			message2.Text = string2;

			okButton.DialogResult = DialogResult.OK;
			cancelButton.DialogResult = DialogResult.Cancel;
		}

		public string password {
			get {
				return passwordBox.Text;
			}
		}

		private void okButton_Click(object sender, EventArgs e) {

		}
	}
}
