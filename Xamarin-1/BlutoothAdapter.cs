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

        public bool StartLeScan()
        {
            if (adapter == null)
                return false;

            if (scancallback == null)
                scancallback = new ScanCallBack();

            if (scancallback != null)
                return adapter.StartLeScan(scancallback);

            return false;
        }

        public void StopLeScan()
        {
            if ((scancallback != null) && (adapter != null))
                adapter.StopLeScan(scancallback);
        }
    }

    public class ScanCallBack : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
    {


        public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
        {
            string deviceName = device.Name;
            if (device.Type == BluetoothDeviceType.Unknown)
            {
                deviceName = "unknown";
            }
        }
    }
}