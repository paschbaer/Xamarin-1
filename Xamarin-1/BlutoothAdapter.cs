using System;
using Android.Content;
using Android.Bluetooth;


namespace Core
{
    public class BlutoothAdapter
    {
        protected BluetoothAdapter adapter;
        protected ScanCallBack scancallback;

        public BlutoothAdapter(Android.Content.Context ctxApp)
        {
            if (ctxApp != null)
            {
                BluetoothManager manager = (BluetoothManager)ctxApp.GetSystemService(Context.BluetoothService);
                adapter = manager.Adapter;
            }

        }

        public string GetName()
        {
            if (adapter != null)
            {
                if (adapter.IsEnabled)
                    return adapter.Name;

                return "disabled";
            }

            return "unknown";
        }

        public bool StartLeScan(Android.Widget.TextView textout)
        {
            if (adapter == null)
                return false;

            if (scancallback == null)
                scancallback = new ScanCallBack(adapter, textout);

            if (scancallback != null)
            {
                scancallback.Run();
                return true;
            }

            return false;
        }

        public void StopLeScan()
        {
            if ((scancallback != null) && (adapter != null))
                adapter.StopLeScan(scancallback);
        }
    }

    public class ScanCallBack : Java.Lang.Object, Java.Lang.IRunnable, BluetoothAdapter.ILeScanCallback
    {
        private BluetoothAdapter adapter;
        private Android.Widget.TextView textout;

        public ScanCallBack(BluetoothAdapter adapter, Android.Widget.TextView textout)
        {
            this.adapter = adapter;
            this.textout = textout;
        }

        public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
        {
            string deviceName = device.Name;
            if (string.IsNullOrEmpty(device.Name))
                textout.Text = device.Address;
            else
                textout.Text = device.Name;

            //System.Threading.Thread.Sleep(1000);


            //device.Type == BluetoothDeviceType.Unknown

        }

        public void Run()
        {
            adapter.StartLeScan(this);
        }
    }
}