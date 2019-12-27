using BarRaider.SdTools;
using NAudio.Midi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrumKitSwitcher
{
    [PluginActionId("DrumKitSwitcher.pluginaction")]
    public class PluginAction : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.MidiDeviceName = String.Empty;
                instance.KitNumber = String.Empty;

                return instance;
            }

            [JsonProperty(PropertyName = "midiDeviceName")]
            public string MidiDeviceName { get; set; }

            [JsonProperty(PropertyName = "kitNumber")]
            public string KitNumber { get; set; }
        }

        #region Private Members

        private PluginSettings settings;

        #endregion
        public PluginAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            int kitNumber = 0;

            if (this.settings == null || this.settings.MidiDeviceName == null || this.settings.KitNumber == null || this.settings.KitNumber.Length == 0)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Unable to set kit, no kit number or midi device set.");
                return;
            }

            try
            {
                kitNumber = Int32.Parse(this.settings.KitNumber);
            }
            catch (FormatException)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "kit number not parceable");
                return;
            }

            bool messageSent = false;
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                if (this.settings.MidiDeviceName.Equals(MidiOut.DeviceInfo(device).ProductName))
                {
                    MidiOut midiOut = new MidiOut(device);
                    midiOut.Send(new PatchChangeEvent(0, 10, kitNumber - 1).GetAsShortMessage());
                    midiOut.Dispose();
                    Logger.Instance.LogMessage(TracingLevel.INFO, "Message Sent!");
                    messageSent = true;
                }
            }

            if (!messageSent)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Unable to send message, no device found.");
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion
    }
}