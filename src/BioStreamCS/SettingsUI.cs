using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BioStream.Micado.User
{
    internal partial class SettingsUI : Form
    {
        internal SettingsUI()
        {
            InitializeComponent();
        }

        private void SetCheckedListBox(CheckedListBox listBox, string[] items)
        {
            listBox.Items.Clear();
            foreach (string item in items)
            {
                listBox.Items.Add(item, true);
            }
        }

        private string[] GetCheckedItems(CheckedListBox listBox)
        {
            string[] items = new string[listBox.CheckedItems.Count];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = listBox.CheckedItems[i].ToString();
            }
            return items;
        }

        private void UpdateOutEnabled()
        {
            if (errorProvider.GetError(textBoxPunchBarNumber).Equals("") &&
                errorProvider.GetError(textBoxPunchBarWidth).Equals("") &&
                errorProvider.GetError(textBoxPunchRadius).Equals("") &&
                errorProvider.GetError(textBoxValveRelativeWidth).Equals("") &&
                errorProvider.GetError(textBoxValveRelativeHeight).Equals("") &&
                errorProvider.GetError(textBoxResolution).Equals("") &&
                errorProvider.GetError(textBoxConnectionWidth).Equals("") &&
                errorProvider.GetError(textBoxFlowExtraWidth).Equals("") &&
                errorProvider.GetError(textBoxValveExtraWidth).Equals("") &&
                errorProvider.GetError(textBoxControlLineExtraWidth).Equals("") &&
                errorProvider.GetError(textBoxPunch2Line).Equals(""))
            {
                buttonOK.Enabled = true;
                buttonExport.Enabled = true;
            }
            else
            {
                buttonOK.Enabled = false;
                buttonExport.Enabled = false;
            }
        }

        private void CheckInteger(TextBox textBox)
        {
            CheckInteger(textBox, false);
        }

        private void CheckPositiveInteger(TextBox textBox)
        {
            CheckInteger(textBox, true);
        }

        private void CheckNumber(TextBox textBox)
        {
            CheckNumber(textBox, false);
        }

        private void CheckPositiveNumber(TextBox textBox)
        {
            CheckNumber(textBox, true);
        }

        private void CheckInteger(TextBox textBox, bool mustBePositive)
        {
            int res;
            if (int.TryParse(textBox.Text, out res))
            {
                if (mustBePositive && res <= 0)
                {
                    errorProvider.SetError(textBox, "must be strictly positive");
                }
                else
                {
                    errorProvider.SetError(textBox, null);
                }
            }
            else
            {
                errorProvider.SetError(textBox, "not an integer");
            }
            UpdateOutEnabled();
        }

        private void CheckNumber(TextBox textBox, bool mustBePositive)
        {
            double res;
            if (double.TryParse(textBox.Text, out res))
            {
                if (mustBePositive && res <= 0)
                {
                    errorProvider.SetError(textBox, "must be strictly positive");
                }
                else
                {
                    errorProvider.SetError(textBox, null);
                }
            }
            else
            {
                errorProvider.SetError(textBox, "not a number");
            }
            UpdateOutEnabled();
        }

        private void LoadSettings(Settings settings)
        {
            SetCheckedListBox(checkedListBoxFlowLayers, settings.FlowLayers);
            SetCheckedListBox(checkedListBoxControlLayers, settings.ControlLayers);
            
            textBoxPunchBarNumber.Text = settings.PunchBarNumber.ToString();
            textBoxPunchBarWidth.Text = settings.PunchBarWidth.ToString();
            textBoxPunchRadius.Text = settings.PunchRadius.ToString();

            textBoxValveRelativeWidth.Text = settings.ValveRelativeWidth.ToString();
            textBoxValveRelativeHeight.Text = settings.ValveRelativeHeight.ToString();

            textBoxResolution.Text = settings.Resolution.ToString();
            textBoxConnectionWidth.Text = settings.ConnectionWidth.ToString();
            textBoxFlowExtraWidth.Text = settings.FlowExtraWidth.ToString();
            textBoxValveExtraWidth.Text = settings.ValveExtraWidth.ToString();
            textBoxControlLineExtraWidth.Text = settings.ControlLineExtraWidth.ToString();
            textBoxPunch2Line.Text = settings.Punch2Line.ToString();
        }

        private void RecordSettings(Settings settings)
        {
            settings.FlowLayers = GetCheckedItems(checkedListBoxFlowLayers);
            settings.ControlLayers = GetCheckedItems(checkedListBoxControlLayers);

            int outint;
            double outdouble;

            if (int.TryParse(textBoxPunchBarNumber.Text, out outint))
                settings.PunchBarNumber = outint;
            if (double.TryParse(textBoxPunchBarWidth.Text, out outdouble))
                settings.PunchBarWidth = outdouble;
            if (double.TryParse(textBoxPunchRadius.Text, out outdouble))
                settings.PunchRadius = outdouble;

            if (double.TryParse(textBoxValveRelativeWidth.Text, out outdouble))
                settings.ValveRelativeWidth = outdouble;
            if (double.TryParse(textBoxValveRelativeHeight.Text, out outdouble))
                settings.ValveRelativeHeight = outdouble;
            
            if (double.TryParse(textBoxResolution.Text, out outdouble))
                settings.Resolution = outdouble;
            if (double.TryParse(textBoxConnectionWidth.Text, out outdouble))
                settings.ConnectionWidth = outdouble;
            if (double.TryParse(textBoxFlowExtraWidth.Text, out outdouble))
                settings.FlowExtraWidth = outdouble;
            if (double.TryParse(textBoxValveExtraWidth.Text, out outdouble))
                settings.ValveExtraWidth = outdouble;
            if (double.TryParse(textBoxControlLineExtraWidth.Text, out outdouble))
                settings.ControlLineExtraWidth = outdouble;
            if (double.TryParse(textBoxPunch2Line.Text, out outdouble))
                settings.Punch2Line = outdouble;
        }

        private void SettingsUI_Load(object sender, EventArgs e)
        {
            LoadSettings(Settings.Current);
        }

        private void textBoxPunchRadius_Validating(object sender, CancelEventArgs e)
        {
            CheckPositiveNumber(textBoxPunchRadius);
        }

        private void textBoxPunchBarNumber_Validating(object sender, CancelEventArgs e)
        {
            CheckInteger(textBoxPunchBarNumber);
        }

        private void textBoxPunchBarWidth_Validating(object sender, CancelEventArgs e)
        {
            CheckNumber(textBoxPunchBarWidth);
        }

        private void textBoxValveRelativeWidth_Validating(object sender, CancelEventArgs e)
        {
            CheckNumber(textBoxValveRelativeWidth);
        }

        private void textBoxValveRelativeHeight_Validating(object sender, CancelEventArgs e)
        {
            CheckNumber(textBoxValveRelativeHeight);
        }

        private void textBoxResolution_Validating(object sender, CancelEventArgs e)
        {
            CheckPositiveNumber(textBoxResolution);
        }

        private void textBoxPunch2Line_Validating(object sender, CancelEventArgs e)
        {
            CheckNumber(textBoxPunch2Line);
        }

        private void textBoxFlowExtraWidth_Validating(object sender, CancelEventArgs e)
        {
            CheckNumber(textBoxFlowExtraWidth);
        }

        private void textBoxValveExtraWidth_Validating(object sender, CancelEventArgs e)
        {
            CheckNumber(textBoxValveExtraWidth);
        }

        private void textBoxControlLineExtraWidth_Validating(object sender, CancelEventArgs e)
        {
            CheckNumber(textBoxControlLineExtraWidth);
        }

        private void textBoxConnectionWidth_Validating(object sender, CancelEventArgs e)
        {
            CheckNumber(textBoxConnectionWidth);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            RecordSettings(Settings.Current);
            Settings.ExportCurrentSettings(Settings.Current);
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "micado settings (*.xml)|*.xml";
            sfd.Title = "Export Micado Settings";

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string filepath = sfd.FileName;

            Settings settings = new Settings();
            RecordSettings(settings);
            Settings.ExportSettings(settings, filepath);
        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog opf = new OpenFileDialog();
            opf.Filter = "micado settings (*.xml)|*.xml|All files (*.*)|*.*";
            opf.Title = "Import Micado Settings";

            if (opf.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string filepath = opf.FileName;

            Settings settings = Settings.ImportSettings(filepath);
            LoadSettings(settings);
        }
    }
}