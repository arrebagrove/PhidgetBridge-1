using System;
using System.Text;
using System.Windows.Forms;
using Phidgets;
using Phidgets.Events;
using System.IO;
using TorqueLoggerPhidget.Properties;


namespace TorqueLoggerPhidget
{
	public partial class MainForm : Form
	{
		Bridge br;
		ErrorEventBox errorBox;
		StringBuilder sb = new StringBuilder("Time,Bridge,Gain,CalSet1,CalVal1,CalSet2,CalVal2,Raw Value,Converted Value\n");


		public bool LogOutput { get; set; }


		public MainForm() {
			InitializeComponent();
			errorBox = new ErrorEventBox();
		}

		private void Form1_Load(object sender, EventArgs e) {
			br = new Bridge();

			br.Attach += new AttachEventHandler(br_Attach);
			br.Detach += new DetachEventHandler(br_Detach);
			br.Error += new Phidgets.Events.ErrorEventHandler(br_Error);

			br.BridgeData += new BridgeDataEventHandler(br_Data);

			outFPath.Text = File.Exists(Settings.Default.outFPath) ? Settings.Default.outFPath
							: Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Phidgetlog.csv";
			if (File.Exists(outFPath.Text))
				File.Delete(outFPath.Text);

			openCmdLine(br);
		}

		// Output data to file
		private void btnOutput_Click(object sender, EventArgs e) {
			LogOutput= !LogOutput;
		}

		//Bridge Attach event handler...populate the fields and controls
		void br_Attach(object sender, AttachEventArgs e) {
			Bridge attached = (Bridge)sender;
			attachedTxt.Text = attached.Attached.ToString();
			nameTxt.Text = attached.Name;
			serialTxt.Text = attached.SerialNumber.ToString();
			versiontxt.Text = attached.Version.ToString();
			numBridgesTxt.Text = attached.bridges.Count.ToString();

			dataRateBar.Minimum = attached.DataRateMax / 8;
			dataRateBar.Maximum = attached.DataRateMin / 8;

			dataRateBox.Enabled = true;
			dataRateBar.Enabled = true;
			dataRateBar.Value = 10;
			bridgeCmb.Enabled = true;
			enCheck.Enabled = true;
			enCheck.Checked = false;
			gainCmb.Enabled = true;
			valueTxt.Enabled = true;
			startBut.Enabled = true;
			set1.Enabled = true;
			set2.Enabled = true;

			dataRateBar.Value = br.DataRate / 8;
			dataRateBox.Text = br.DataRate.ToString();

			bridgeCmb.SelectedIndex = 0;
		}

		//Bridge Detach event handler...Clear all the fields and disable all the controls
		void br_Detach(object sender, DetachEventArgs e) {
			Bridge detached = (Bridge)sender;
			attachedTxt.Text = detached.Attached.ToString();
			nameTxt.Clear();
			serialTxt.Clear();
			versiontxt.Clear();
			numBridgesTxt.Clear();

			dataRateBox.Enabled = false;
			dataRateBox.Text = "";
			dataRateBar.Enabled = false;
			bridgeCmb.Enabled = false;
			enCheck.Enabled = false;
			gainCmb.Enabled = false;
			valueTxt.Enabled = false;
			startBut.Enabled = false;
			set1.Enabled = false;
			set2.Enabled = false;

			valueTxt.Clear();
		}

		//error handler...display the error description in a messagebox
		void br_Error(object sender, Phidgets.Events.ErrorEventArgs e) {
			Phidget phid = (Phidget)sender;
			DialogResult result;
			switch (e.Type) {
				case PhidgetException.ErrorType.PHIDGET_ERREVENT_BADPASSWORD:
					phid.close();
					TextInputBox dialog = new TextInputBox("Error Event",
						"Authentication error: This server requires a password.", "Please enter the password, or cancel.");
					result = dialog.ShowDialog();
					if (result == DialogResult.OK)
						openCmdLine(phid, dialog.password);
					else
						Environment.Exit(0);
					break;
				default:
					if (!errorBox.Visible)
						errorBox.Show();
					break;
			}
			errorBox.addMessage(DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + ": " + e.Description);
		}

		//BridgeChange event handler...display the data from a bridge change event
		void br_Data(object sender, BridgeDataEventArgs e) {
			if (e.Index == bridgeCmb.SelectedIndex) {
				valueTxt.Text = e.Value.ToString();
				convertTxt.Text = Math.Round((slope*e.Value) + Yint, 2).ToString();

				//Log output		
				sb.AppendLine(DateTime.Now.ToString(Settings.Default.tStampFormat) + "," + bridgeCmb.SelectedIndex + "," + br.bridges[e.Index].Gain
								+ "," + (value1==0 ? "," : value1Calib.ToString() + "," + value1.ToString())
								+ "," + (value2==0 ? "," : value2Calib.ToString() + "," + value2.ToString())
								+ "," + e.Value.ToString() + "," + convertTxt.Text);
			}
		}

		private void enCheck_CheckedChanged(object sender, EventArgs e) {
			br.bridges[bridgeCmb.SelectedIndex].Enabled = enCheck.Checked;
		}

		private void gainCmb_SelectedIndexChanged(object sender, EventArgs e) {
			if (gainCmb.SelectedIndex == 0)
				br.bridges[bridgeCmb.SelectedIndex].Gain = BridgeInput.Gains.GAIN_1;
			else if (gainCmb.SelectedIndex == 1)
				br.bridges[bridgeCmb.SelectedIndex].Gain = BridgeInput.Gains.GAIN_8;
			else if (gainCmb.SelectedIndex == 2)
				br.bridges[bridgeCmb.SelectedIndex].Gain = BridgeInput.Gains.GAIN_16;
			else if (gainCmb.SelectedIndex == 3)
				br.bridges[bridgeCmb.SelectedIndex].Gain = BridgeInput.Gains.GAIN_32;
			else if (gainCmb.SelectedIndex == 4)
				br.bridges[bridgeCmb.SelectedIndex].Gain = BridgeInput.Gains.GAIN_64;
			else if (gainCmb.SelectedIndex == 5)
				br.bridges[bridgeCmb.SelectedIndex].Gain = BridgeInput.Gains.GAIN_128;
		}

		private void bridgeCmb_SelectedIndexChanged(object sender, EventArgs e) {
			enCheck.Checked = br.bridges[bridgeCmb.SelectedIndex].Enabled;
			gainCmb.SelectedIndex = gainLUT(br.bridges[bridgeCmb.SelectedIndex].Gain);
			try {
				valueTxt.Text = br.bridges[bridgeCmb.SelectedIndex].BridgeValue.ToString();
			}
			catch (PhidgetException ex) {
				switch (ex.Type) {
					case PhidgetException.ErrorType.PHIDGET_ERR_UNKNOWNVAL:
						valueTxt.Text = "Unknown";
						break;
				}
			}
		}

		int gainLUT(BridgeInput.Gains val) {
			if (val == BridgeInput.Gains.GAIN_1)
				return 0;
			if (val == BridgeInput.Gains.GAIN_8)
				return 1;
			if (val == BridgeInput.Gains.GAIN_16)
				return 2;
			if (val == BridgeInput.Gains.GAIN_32)
				return 3;
			if (val == BridgeInput.Gains.GAIN_64)
				return 4;
			if (val == BridgeInput.Gains.GAIN_128)
				return 5;

			return 0;
		}

		//When the form is being close, make sure to stop all the motors and close the Phidget.
		private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
			br.Attach -= new AttachEventHandler(br_Attach);
			br.Detach -= new DetachEventHandler(br_Detach);
			br.Error -= new Phidgets.Events.ErrorEventHandler(br_Error);

			br.BridgeData -= new BridgeDataEventHandler(br_Data);

			File.AppendAllText(outFPath.Text, sb.ToString());

			//run any events in the message queue - otherwise close will hang if there are any outstanding `
			Application.DoEvents();

			br.close();
			br = null;
		}


		double value1, value2, value1Calib, value2Calib, slope, Yint;
		private void startBut_Click(object sender, EventArgs e) {
			value1txt.ReadOnly = false;
			set1.Enabled = true;
			set2.Enabled = false;
			value2txt.ReadOnly = true;
		}

		private void set1_Click(object sender, EventArgs e) {
			try {
				value1 = double.Parse(valueTxt.Text);
				value1Calib = double.Parse(value1txt.Text);
				value2txt.ReadOnly = false;
				value1txt.ReadOnly = true;
				set1.Enabled = false;
				set2.Enabled = true;
			}
			catch { }
		}

		private void set2_Click(object sender, EventArgs e) {
			try {
				value2 = double.Parse(valueTxt.Text);
				value2Calib = double.Parse(value2txt.Text);
				//Calculate formula
				slope = (value2Calib - value1Calib) / (value2 - value1);
				Yint = value1Calib - (value1 * slope);
				//formulaTxt.Text = "y=" + slope.ToString("f4") + "x+" + Yint.ToString("f4");
				formulaTxt.Text = string.Format("y = {0}x + {1}", slope.ToString("f4"), Yint.ToString("f4"));


				value2txt.ReadOnly = true;
				set2.Enabled = false;
			}
			catch { }
		}

		private void dataRateBar_Scroll(object sender, EventArgs e) {
			dataRateBox.Text = (dataRateBar.Value * 8).ToString();
			br.DataRate = dataRateBar.Value * 8;
		}

		//Parses command line arguments and calls the appropriate open
		#region Command line open functions
		private void openCmdLine(Phidget p) {
			openCmdLine(p, null);
		}
		private void openCmdLine(Phidget p, string pass) {
			int serial = -1;
			string logFile = null;
			int port = 5001;
			string host = null;
			bool remote = false, remoteIP = false;
			string[] args = Environment.GetCommandLineArgs();
			string appName = args[0];

			try { //Parse the flags
				for (int i = 1; i < args.Length; i++) {
					if (args[i].StartsWith("-"))
						switch (args[i].Remove(0, 1).ToLower()) {
							case "l":
								logFile = (args[++i]);
								break;
							case "n":
								serial = int.Parse(args[++i]);
								break;
							case "r":
								remote = true;
								break;
							case "s":
								remote = true;
								host = args[++i];
								break;
							case "p":
								pass = args[++i];
								break;
							case "i":
								remoteIP = true;
								host = args[++i];
								if (host.Contains(":")) {
									port = int.Parse(host.Split(':')[1]);
									host = host.Split(':')[0];
								}
								break;
							default:
								goto usage;
						}
					else
						goto usage;
				}
				if (logFile != null)
					Phidget.enableLogging(Phidget.LogLevel.PHIDGET_LOG_INFO, logFile);
				if (remoteIP)
					p.open(serial, host, port, pass);
				else if (remote)
					p.open(serial, host, pass);
				else
					p.open(serial);
				return; //success
			}
			catch { }
			usage:
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Invalid Command line arguments." + Environment.NewLine);
			sb.AppendLine("Usage: " + appName + " [Flags...]");
			sb.AppendLine("Flags:\t-n   serialNumber\tSerial Number, omit for any serial");
			sb.AppendLine("\t-l   logFile\tEnable phidget21 logging to logFile.");
			sb.AppendLine("\t-r\t\tOpen remotely");
			sb.AppendLine("\t-s   serverID\tServer ID, omit for any server");
			sb.AppendLine("\t-i   ipAddress:port\tIp Address and Port. Port is optional, defaults to 5001");
			sb.AppendLine("\t-p   password\tPassword, omit for no password" + Environment.NewLine);
			sb.AppendLine("Examples: ");
			sb.AppendLine(appName + " -n 50098");
			sb.AppendLine(appName + " -r");
			sb.AppendLine(appName + " -s myphidgetserver");
			sb.AppendLine(appName + " -n 45670 -i 127.0.0.1:5001 -p paswrd");
			MessageBox.Show(sb.ToString(), "Argument Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

			Application.Exit();
		}
		#endregion
	}
}