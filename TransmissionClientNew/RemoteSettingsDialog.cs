﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Jayrock.Json;

namespace TransmissionRemoteDotnet
{
    public partial class RemoteSettingsDialog : Form
    {
        private static RemoteSettingsDialog instance = null;
        private static readonly object padlock = new object();

        public static RemoteSettingsDialog Instance
        {
            get
            {
                lock (padlock)
                {
                    if (!IsActive())
                    {
                        instance = new RemoteSettingsDialog();
                    }
                }
                return instance;
            }
        }

        private static bool IsActive()
        {
            return instance != null && !instance.IsDisposed;
        }

        public static void CloseIfOpen()
        {
            if (IsActive())
            {
                instance.Close();
            }
        }

        private RemoteSettingsDialog()
        {
            InitializeComponent();
        }

        private void CloseFormButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RemoteSettingsDialog_Load(object sender, EventArgs e)
        {
            try
            {
                JsonObject settings = (JsonObject)Program.DaemonDescriptor.SessionData;
                DownloadToField.Text = (string)settings["download-dir"];
                LimitDownloadValue.Enabled = LimitDownloadCheckBox.Checked = ((JsonNumber)settings[ProtocolConstants.FIELD_SPEEDLIMITDOWNENABLED]).ToBoolean();
                SetLimitField(((JsonNumber)settings[ProtocolConstants.FIELD_SPEEDLIMITDOWN]).ToInt32(), LimitDownloadValue);
                LimitUploadValue.Enabled = LimitUploadCheckBox.Checked = ((JsonNumber)settings[ProtocolConstants.FIELD_SPEEDLIMITUPENABLED]).ToBoolean();
                SetLimitField(((JsonNumber)settings[ProtocolConstants.FIELD_SPEEDLIMITUP]).ToInt32(), LimitUploadValue);
                if (settings.Contains("port"))
                {
                    IncomingPortValue.Tag = "port";
                    IncomingPortValue.Value = ((JsonNumber)settings["port"]).ToInt32();
                }
                else if (settings.Contains("peer-port"))
                {
                    IncomingPortValue.Tag = "peer-port";
                    IncomingPortValue.Value = ((JsonNumber)settings["peer-port"]).ToInt32();
                }
                PortForward.Checked = ((JsonNumber)settings["port-forwarding-enabled"]).ToBoolean();
                string enc = settings["encryption"] as string;
                if (enc.Equals("preferred"))
                {
                    EncryptionCombobox.SelectedIndex = 1;
                }
                else if (enc.Equals("required"))
                {
                    EncryptionCombobox.SelectedIndex = 2;
                }
                else
                {
                    EncryptionCombobox.SelectedIndex = 0;
                }
                // peer limit
                if (settings.Contains(ProtocolConstants.FIELD_PEERLIMIT))
                {
                    PeerLimitValue.Value = ((JsonNumber)settings[ProtocolConstants.FIELD_PEERLIMIT]).ToInt32();
                    PeerLimitValue.Tag = ProtocolConstants.FIELD_PEERLIMIT;
                }
                else if (settings.Contains("peer-limit-global"))
                {
                    PeerLimitValue.Value = ((JsonNumber)settings["peer-limit-global"]).ToInt32();
                    PeerLimitValue.Tag = "peer-limit-global";
                }
                // pex
                if (settings.Contains("pex-allowed"))
                {
                    PEXcheckBox.Checked = ((JsonNumber)settings["pex-allowed"]).ToBoolean();
                    PEXcheckBox.Tag = "pex-allowed";
                }
                else if (settings.Contains("pex-enabled"))
                {
                    PEXcheckBox.Checked = ((JsonNumber)settings["pex-enabled"]).ToBoolean();
                    PEXcheckBox.Tag = "pex-enabled";
                }
                // blocklist
                if (blocklistEnabledCheckBox.Enabled = settings.Contains("blocklist-enabled"))
                {
                    blocklistEnabledCheckBox.Checked = ((JsonNumber)settings["blocklist-enabled"]).ToBoolean();
                }
                if (altSpeedLimitEnable.Enabled =
                    altUploadLimitField.Enabled =
                    altDownloadLimitField.Enabled =
                    altTimeConstraintEnabled.Enabled =
                    altTimeConstraintEndField.Enabled =
                    altTimeConstraintStartField.Enabled =
                    settings.Contains("alt-speed-enabled"))
                {
                    altDownloadLimitField.Value = ((JsonNumber)settings["alt-speed-down"]).ToInt32();
                    altUploadLimitField.Value = ((JsonNumber)settings["alt-speed-up"]).ToInt32();
                    altDownloadLimitField.Enabled = altUploadLimitField.Enabled = altSpeedLimitEnable.Checked = ((JsonNumber)settings["alt-speed-enabled"]).ToBoolean();
                    altTimeConstraintStartField.Enabled = altTimeConstraintEndField.Enabled = altTimeConstraintEnabled.Checked = ((JsonNumber)settings["alt-speed-time-enabled"]).ToBoolean();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to load settings data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void SetLimitField(int limit, NumericUpDown field)
        {
            if (Program.DaemonDescriptor.Version < 1.40)
            {
                field.Value = limit >= 1024 && limit <= field.Maximum ? limit / 1024 : 0;
            }
            else
            {
                field.Value = limit >= 0 && limit <= field.Maximum ? limit : 0;
            }
        }

        private void LimitUploadCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            LimitUploadValue.Enabled = LimitUploadCheckBox.Checked;
        }

        private void LimitDownloadCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            LimitDownloadValue.Enabled = LimitDownloadCheckBox.Checked;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            JsonObject request = new JsonObject();
            request.Put(ProtocolConstants.KEY_METHOD, "session-set");
            JsonObject arguments = new JsonObject();
            arguments.Put((string)IncomingPortValue.Tag, IncomingPortValue.Value);
            arguments.Put("port-forwarding-enabled", PortForward.Checked);
            arguments.Put((string)PEXcheckBox.Tag, PEXcheckBox.Checked);
            arguments.Put((string)PeerLimitValue.Tag, PeerLimitValue.Value);
            switch (EncryptionCombobox.SelectedIndex)
            {
                case 1:
                    arguments.Put("encryption", "preferred");
                    break;
                case 2:
                    arguments.Put("encryption", "required");
                    break;
                default:
                    arguments.Put("encryption", "tolerated");
                    break;
            }
            arguments.Put(ProtocolConstants.FIELD_SPEEDLIMITUPENABLED, LimitUploadCheckBox.Checked);
            arguments.Put(ProtocolConstants.FIELD_SPEEDLIMITUP, LimitUploadValue.Value);
            arguments.Put(ProtocolConstants.FIELD_SPEEDLIMITDOWNENABLED, LimitDownloadCheckBox.Checked);
            arguments.Put(ProtocolConstants.FIELD_SPEEDLIMITDOWN, LimitDownloadValue.Value);
            if (altSpeedLimitEnable.Enabled)
            {
                arguments.Put("alt-speed-enabled", altSpeedLimitEnable.Checked);
                arguments.Put("alt-speed-down", altDownloadLimitField.Value);
                arguments.Put("alt-speed-up", altUploadLimitField.Value);
            }
            if (altTimeConstraintEnabled.Enabled)
            {
                arguments.Put("alt-speed-time-enabled", altTimeConstraintEnabled.Checked);
                arguments.Put("alt-speed-time-begin", altTimeConstraintStartField.Value);
                arguments.Put("alt-speed-time-end", altTimeConstraintEndField.Value);
            }
            if (blocklistEnabledCheckBox.Enabled)
            {
                arguments.Put("blocklist-enabled", blocklistEnabledCheckBox.Checked);
            }
            arguments.Put("download-dir", DownloadToField.Text);
            request.Put(ProtocolConstants.KEY_ARGUMENTS, arguments);
            request.Put(ProtocolConstants.KEY_TAG, (int)ResponseTag.DoNothing);
            SettingsWorker.RunWorkerAsync(request);
            this.Close();
        }

        private void SettingsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = CommandFactory.Request((JsonObject)e.Argument);
        }

        private void SettingsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TransmissionCommand command = (TransmissionCommand)e.Result;
            Program.Form.CreateActionWorker().RunWorkerAsync(Requests.SessionGet());
            command.Execute();
        }

        private void altSpeedLimitEnable_CheckedChanged(object sender, EventArgs e)
        {
            altUploadLimitField.Enabled = altDownloadLimitField.Enabled = altSpeedLimitEnable.Checked;
        }

        private void altTimeConstraintEnabled_CheckedChanged(object sender, EventArgs e)
        {
            altTimeConstraintStartField.Enabled = altTimeConstraintEndField.Enabled = altTimeConstraintEnabled.Checked;
        }
    }
}