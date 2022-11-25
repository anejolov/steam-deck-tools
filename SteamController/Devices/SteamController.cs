using hidapi;
using PowerControl.External;
using static CommonHelpers.Log;

namespace SteamController.Devices
{
    public partial class SteamController : IDisposable
    {
        public const ushort SteamVendorID = 0x28DE;
        public const ushort SteamProductID = 0x1205;
        public const int ReadTimeout = 50;

        private hidapi.HidDevice neptuneDevice;

        public SteamController()
        {
            InitializeButtons();
            InitializeActions();

            neptuneDevice = new hidapi.HidDevice(SteamVendorID, SteamProductID, 64);
            neptuneDevice.OpenDevice();
        }

        public void Dispose()
        {
        }

        public bool Updated { get; private set; }

        internal void Reset()
        {
            foreach (var action in AllActions)
                action.Reset();
        }

        private void BeforeUpdate(byte[] buffer)
        {
            foreach (var action in AllActions)
                action.BeforeUpdate(buffer, this);
        }

        internal void BeforeUpdate()
        {
            byte[] data = neptuneDevice.Read(ReadTimeout);
            if (data == null)
            {
                Reset();
                Updated = false;
                return;
            }

            BeforeUpdate(data);
            Updated = true;
        }

        internal void Update()
        {
            foreach (var action in AllActions)
                action.Update();

            UpdateLizardButtons();
            UpdateLizardMouse();
        }
    }
}