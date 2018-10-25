using System;
using Android.App;
using Android.Content;
using Android.Bluetooth;
using Android.Views;
using Android.Util;
using Android.Runtime;

namespace Blu
{
    public class BlutoothAdapter
    {
        public readonly static String TAG = typeof(BlutoothAdapter).Name;

        protected BluetoothAdapter adapter;
        protected BluetoothGatt gatt;
        protected System.Collections.Generic.Dictionary<string, BluetoothDevice> mapDevices = new System.Collections.Generic.Dictionary<string, BluetoothDevice>();
        protected ScanCallBack scancallback;

        public BlutoothAdapter()
        {}

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
            {
                BluetoothManager manager = (BluetoothManager)activity.ApplicationContext.GetSystemService(Context.BluetoothService);
                adapter = manager.Adapter;
            }

            if (scancallback == null)
                scancallback = new ScanCallBack(adapter, activity, mapDevices, textout);
            else
                scancallback.Reset();

            if (scancallback != null)
            {
                System.Threading.ThreadPool.QueueUserWorkItem(o => scancallback.Run());
                return true;
            }

            return false;
        }

        public void StopLeScan()
        {
            if ((scancallback != null) && (adapter != null))
                adapter.StopLeScan(scancallback);
        }

        public void EnumServices(Context ctx, string identifer)
        {
            BluetoothDevice device = mapDevices[identifer];
            if (device != null)
            {
                GattDevice gattdevice = new GattDevice(ctx, identifer);
                if (gatt != null)
                {
                    //gatt.Disconnect();
                    gatt.Close();   //or gatt.Connect() to re-connect to device
                    gatt.Dispose();
                }

                gatt = device.ConnectGatt(ctx, false, gattdevice);  //-> OnConnectionStateChange() -> DiscoverServices() -> OnServicesDiscovered() -> GattDevice.Initialize()
                if (gatt == null)
                {
                    Log.Error(TAG, string.Format("failed to connect to GATT server '{0}'", identifer));
                }
            }
        }
    }

    public class ScanCallBack : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
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

    public class GattDevice : BluetoothGattCallback
    {
        public readonly static String TAG = typeof(GattDevice).Name;

        protected Context ctx;
        protected string gattServerName;

        protected int idxService = 0; 
        protected int idxCharacteristic = 0;

        protected static Java.Util.UUID SERVICE_GENERIC_ACCESS = Java.Util.UUID.FromString("00001800-0000-1000-8000-00805f9b34fb");    //org.bluetooth.service.generic_access

        public GattDevice(Context ctx, string identifier)
        {
            this.ctx = ctx;
            gattServerName = identifier;
            idxService = 0;
            idxCharacteristic = 0;
        }

        protected virtual void ReadFirstService(BluetoothGatt gatt)
        {
            ListServices(gatt);

            idxCharacteristic = 0;
            BluetoothGattService service = gatt.Services[idxService];
            ReadService(gatt, service);
        }

        public virtual void ReadNextService(BluetoothGatt gatt)
        {
            idxService++;
            Log.Debug(TAG, "ReadNextService({0})", idxService);
            if (idxService < gatt.Services.Count)
            {
                BluetoothGattService service = gatt.Services[idxService];
                ReadService(gatt, service);
            }
            else
            {
                Log.Debug(TAG, "ReadNextService finished");
            }

        }

        protected void ReadService(BluetoothGatt gatt, BluetoothGattService service)
        {
            idxCharacteristic = 0;
            Log.Debug(TAG, "ReadService(0x{0:X})", GetAssignedNumber(service.Uuid));
            if (service != null)
            {
                if (service.Characteristics.Count > 0)
                {
                    BluetoothGattCharacteristic characteristic = service.Characteristics[0];
                    Log.Debug(TAG, "ReadCharacteristic(0x{0:X})", GetAssignedNumber(characteristic.Uuid));
                    gatt.ReadCharacteristic(characteristic);
                }
            }
        }

        public void ReadCharacteristic(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            BluetoothGattService service = characteristic.Service;

            byte[] value = characteristic.GetValue();
            if (value != null)
            {   
                Log.Debug(TAG, 
                    "service '0x{0:X}' - characteristic '0x{1:X}' - '{2}'",
                    GetAssignedNumber(service.Uuid),
                    GetAssignedNumber(characteristic.Uuid), 
                    BitConverter.ToString(value));
            }
            else
            {
                Log.Debug(TAG, 
                    "service '0x{0:X}' - characteristic '0x{1:X}' - 'empty'",
                    GetAssignedNumber(service.Uuid),
                    GetAssignedNumber(characteristic.Uuid));
            }

            idxCharacteristic++;
            if (idxCharacteristic < characteristic.Service.Characteristics.Count)
            {
                BluetoothGattCharacteristic nextCharacteristic = service.Characteristics[idxCharacteristic];
                Log.Debug(TAG, "ReadNextCharacteristic(0x{0:X})", GetAssignedNumber(nextCharacteristic.Uuid));
                gatt.ReadCharacteristic(characteristic);
            }
            else
                ReadNextService(gatt);
        }

        protected static void ListServices(BluetoothGatt gatt)
        {
            foreach (BluetoothGattService service in gatt.Services)
            {
                Log.Debug(TAG, string.Format("UUID: '0x{0:X}'", GetAssignedNumber(service.Uuid)));
            }
        }

        public static int GetAssignedNumber(Java.Util.UUID uuid)
        {
            // Keep only the significant bits of the UUID
            return (int)((uuid.MostSignificantBits & 0x0000FFFF00000000L) >> 32);
        }

        public static void DumpService(BluetoothGattService service, string serviceName)
        {
            if (service != null)
            {
                foreach (BluetoothGattCharacteristic characteristic in service.Characteristics)
                {
                    Log.Debug(serviceName, string.Format("'0x{0:X}: characteristic's UUID : '0x{1:X}'", GetAssignedNumber(service.Uuid), GetAssignedNumber(characteristic.Uuid)));
                }
            }
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            base.OnCharacteristicChanged(gatt, characteristic);
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            Log.Debug(TAG, "OnCharacteristicRead(0x{0:X})", GetAssignedNumber(characteristic.Uuid));
            ReadCharacteristic(gatt, characteristic);

            //if (BlutoothService.GetAssignedNumber(characteristic.Service.Uuid) == 0x1800)   //org.bluetooth.service.generic_access
        }

        public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            base.OnCharacteristicWrite(gatt, characteristic, status);
        }
        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState state)
        {
            if (state == ProfileState.Connected)
            {
                Log.Debug(TAG, string.Format("connected to GATT server '{0}'", gattServerName));
                if (gatt != null)
                {
                    Log.Debug(TAG, string.Format("discover services of '{0}'", gattServerName));
                    gatt.DiscoverServices();
                }
            }
            else if (state == ProfileState.Disconnected)
                Log.Debug(TAG, string.Format("disconnected from GATT server '{0}'", gattServerName));

            //base.OnConnectionStateChange(gatt, status, state);
        }

        public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            base.OnDescriptorRead(gatt, descriptor, status);
        }

        public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
        {
            base.OnDescriptorWrite(gatt, descriptor, status);
        }

        public override void OnMtuChanged(BluetoothGatt gatt, Int32 mtu, GattStatus status)
        {
            base.OnMtuChanged(gatt, mtu, status);
        }

        public override void OnReadRemoteRssi(BluetoothGatt gatt, Int32 rssi, GattStatus status)
        {
            base.OnReadRemoteRssi(gatt, rssi, status);
        }

        public override void OnReliableWriteCompleted(BluetoothGatt gatt, GattStatus status)
        {
            base.OnReliableWriteCompleted(gatt, status);
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            if (status == GattStatus.Success)
            {
                Log.Debug(TAG, string.Format("services of '{0}' successfully retrieved", gattServerName));
                
                System.Threading.ThreadPool.QueueUserWorkItem(o => ReadFirstService(gatt));
            }
            else if (status == GattStatus.Success)
                Log.Error(TAG, string.Format("failed to retrieve services of '{0}'", gattServerName));

            //base.OnServicesDiscovered(gatt, status);
        }

        protected void UpdateUi(string key, string value)
        {// If desired, pass some values to the broadcast receiver.
            Intent message = new Intent("com.xamarin.example.BLU");
 
            message.PutExtra(key, value);
            Android.Support.V4.Content.LocalBroadcastManager.GetInstance(ctx).SendBroadcast(message);
        }
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
            NotifyDataSetChanged();

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