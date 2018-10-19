using System;
using Android.App;
using Android.Content;
using Android.Bluetooth;
using Android.Views;


namespace Core
{
    public class BlutoothAdapter
    {
        protected Context ctxApp;
        protected BluetoothAdapter adapter;
        protected System.Collections.Generic.Dictionary<string, BluetoothDevice> mapDevices = new System.Collections.Generic.Dictionary<string, BluetoothDevice>();
        protected ScanCallBack scancallback;

        public BlutoothAdapter(Context ctxApp)
        {
            if (ctxApp != null)
            {
                this.ctxApp = ctxApp;

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

        public bool StartLeScan(Activity activity, Android.Widget.ListView textout)
        {
            if (adapter == null)
                return false;

            if (scancallback == null)
                scancallback = new ScanCallBack(adapter, activity, mapDevices, textout);
            else
                scancallback.Reset();

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

        public void EnumServices(string deviceName)
        {
            BluetoothDevice device = mapDevices[deviceName];
            if (device != null)
            {
                GattCallBack gattcallback = new GattCallBack();
                BluetoothGatt gatt = device.ConnectGatt(ctxApp, false, gattcallback);
            }
        }
    }

    public class ScanCallBack : Java.Lang.Object, Java.Lang.IRunnable, BluetoothAdapter.ILeScanCallback
    {
        private BluetoothAdapter adapter;
        private System.Collections.Generic.Dictionary<string, BluetoothDevice> mapDevices;
        private Android.Widget.ListView textout;
        private StringListAdapter listAdapter; 

        public ScanCallBack(BluetoothAdapter adapter, Activity activity, System.Collections.Generic.Dictionary<string, BluetoothDevice> mapDevices, Android.Widget.ListView textout)
        {
            this.adapter = adapter;
            this.mapDevices = mapDevices;
            this.textout = textout;

            listAdapter = new StringListAdapter(activity);
            this.textout.Adapter = listAdapter;
        }
            
        public void Reset()
        {
            //textout.Text = string.Empty;
            mapDevices.Clear();
            listAdapter.Reset();
        }

        public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
        {
            string deviceName = device.Name;
            if (string.IsNullOrEmpty(device.Name))
                deviceName = device.Address;

            if (!mapDevices.ContainsKey(deviceName))
            {
                mapDevices.Add(deviceName, device);
                listAdapter.Add(deviceName);

                UpdateTextOut(textout);
            }

            //System.Threading.Thread.Sleep(1000);
            //device.Type == BluetoothDeviceType.Unknown

        }

        public void UpdateTextOut(Android.Widget.ListView textout)
        {
            textout.Adapter = null;
            textout.Adapter = listAdapter;
        }

        public void Run()
        {
            adapter.StartLeScan(this);
        }
    }

    public class GattCallBack : BluetoothGattCallback
    {

    }

    public class StringListAdapter : Android.Widget.BaseAdapter<string>
    {
        private System.Collections.Generic.List<string> listValues = new System.Collections.Generic.List<string>();
        private Activity activity;

        public StringListAdapter(Activity activity) : base()
        {
            this.activity = activity;
        }

        public void Add(string value)
        {
            listValues.Add(value);
        }

        public void Reset()
        {
            listValues.Clear();
        }

        public override string this[int position]
        {
            get { return listValues[position]; }
        }

        public override int Count
        {
            get { return listValues.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null)
                view = activity.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, null);

            view.FindViewById<Android.Widget.TextView>(Android.Resource.Id.Text1).Text = listValues[position];

            return view;
        }
    }
}