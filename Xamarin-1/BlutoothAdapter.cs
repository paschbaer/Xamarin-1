﻿using System;
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
        protected GattDevice gattdevice;

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

        public void EnumServices(Context ctx, string identifier)
        {
            BluetoothDevice device = mapDevices[identifier];
            if (device != null)
            {
                if (gattdevice != null)
                {
                    if (identifier.Equals(gattdevice.gattServerName))
                    {
                        gatt.Connect();//to re-connect to device
                        gatt.DiscoverServices();
                    }
                    else
                    {
                        if (gatt != null)
                        {
                            gatt.Disconnect();
                            gatt.Close(); 
                        }

                        gattdevice.Reset(identifier);
                        gatt = device.ConnectGatt(ctx, false, gattdevice);  //-> gattdevice.OnConnectionStateChange() -> gatt.DiscoverServices() ->gattdevice.OnServicesDiscovered() -> gattdevice.StartInit()
                        if (gatt == null)
                        {
                            Log.Error(TAG, string.Format("failed to connect to GATT server '{0}'", identifier));
                        }
                    }
                }
                else
                {
                    gattdevice = new GattDevice(ctx, identifier);
                    gatt = device.ConnectGatt(ctx, false, gattdevice);  //-> gattdevice.OnConnectionStateChange() -> gatt.DiscoverServices() ->gattdevice.OnServicesDiscovered() -> gattdevice.StartInit()
                    if (gatt == null)
                    {
                        Log.Error(TAG, string.Format("failed to connect to GATT server '{0}'", identifier));
                    }
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
        public string gattServerName { get; private set; }

        protected BluetoothGatt gatt { get; private set; }
        protected BluetoothGattService service { get; private set; }

        protected GattDeviceReceiver receiver;

        public static string ACTION_START_ENUM_SERVICES = "START_ENUM_SERVICES";
        public static string ACTION_ENUM_NEXT_SERVICE = "ENUM_NEXT_SERVICE";
        public static string ACTION_READ_NEXT_CHARACTERISTIC = "READ_NEXT_CHARACTERISTIC";
        public static string ACTION_ENUM_SERVICES_FINISHED = "ENUM_SERVICES_FINISHED";

        protected static Java.Util.UUID SERVICE_GENERIC_ACCESS = Java.Util.UUID.FromString("00001800-0000-1000-8000-00805f9b34fb");    //org.bluetooth.service.generic_access

        public GattDevice(Context ctx, string identifier)
        {
            this.ctx = ctx;
            gattServerName = identifier;

            receiver = new GattDeviceReceiver(this);
            Android.Support.V4.Content.LocalBroadcastManager.GetInstance(ctx).RegisterReceiver(receiver, new IntentFilter("com.xamarin.example.BLU.Device"));
        }

        ~GattDevice()
        {
            Android.Support.V4.Content.LocalBroadcastManager.GetInstance(ctx).UnregisterReceiver(receiver);
        }

        public void Reset(string identifier)
        {
            gattServerName = identifier;
        }

        protected virtual void StartInit(BluetoothGatt gatt)
        {
            this.gatt = gatt;
            
            ListServices(gatt);

            UpdateDevice(ACTION_START_ENUM_SERVICES);
        }

        public void ReadService(int index)
        {
            if ((gatt != null) && (index < gatt.Services.Count))
            {
                Log.Debug(TAG, "ReadService({0})", index);
                service = gatt.Services[index];
                ReadService(gatt, service);
            }
            else
            {
                service = null;
                UpdateDevice(ACTION_ENUM_SERVICES_FINISHED);
            }
        }

        protected void ReadService(BluetoothGatt gatt, BluetoothGattService service)
        {
            Log.Debug(TAG, "ReadService(0x{0:X})", GetAssignedNumber(service.Uuid));
            if (service != null)
            {
                if (service.Characteristics.Count > 0)
                    UpdateDevice(ACTION_READ_NEXT_CHARACTERISTIC);
            }
        }

        public void ReadCharacteristic(int index)
        {
            if ((gatt != null) && (service != null))
            {
                if (index < service.Characteristics.Count)
                {
                    Log.Debug(TAG, "ReadCharacteristic({0})", index);
                    gatt.ReadCharacteristic(service.Characteristics[index]);
                }
                else
                {
                    service = null;
                    UpdateDevice(ACTION_ENUM_NEXT_SERVICE);
                }
            }
        }

        protected void ReadCharacteristic(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
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

            UpdateDevice(ACTION_READ_NEXT_CHARACTERISTIC);
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

                StartInit(gatt); //System.Threading.ThreadPool.QueueUserWorkItem(o => StartInit(gatt));
            }
            else if (status == GattStatus.Success)
                Log.Error(TAG, string.Format("failed to retrieve services of '{0}'", gattServerName));

            //base.OnServicesDiscovered(gatt, status);
        }

        protected void UpdateUi(string key, string value)
        {// If desired, pass some values to the broadcast receiver.
            Intent message = new Intent("com.xamarin.example.BLU.Ui");
 
            message.PutExtra(key, value);
            Android.Support.V4.Content.LocalBroadcastManager.GetInstance(ctx).SendBroadcast(message);
        }

        protected void UpdateDevice(string action)
        {// If desired, pass some values to the broadcast receiver.
            Intent message = new Intent("com.xamarin.example.BLU.Device");

            if (!string.IsNullOrEmpty(action))
                message.PutExtra("action", action);

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


    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter(new[] { "com.xamarin.example.BLU.Device" })]
    public class GattDeviceReceiver : BroadcastReceiver
    {
        public readonly static String TAG = typeof(GattDeviceReceiver).Name;

        protected GattDevice device;
        protected int idxSvc;
        protected int idxChar;

        public GattDeviceReceiver()
        {
            this.device = null;
            idxSvc = 0;
            idxChar = 0;
        }

        public GattDeviceReceiver(GattDevice device)
        {
            this.device = device;
            idxSvc = 0;
            idxChar = 0;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.GetStringExtra("action");

            if(action.Equals(GattDevice.ACTION_START_ENUM_SERVICES))
            {
                Log.Debug(TAG, GattDevice.ACTION_START_ENUM_SERVICES);
                idxSvc = 0;
                idxChar = 0;

                if (device != null)
                    device.ReadService(idxSvc++);
            }

            if (action.Equals(GattDevice.ACTION_ENUM_NEXT_SERVICE))
            {
                Log.Debug(TAG, GattDevice.ACTION_ENUM_NEXT_SERVICE);
                idxChar = 0;

                if (device != null)
                    device.ReadService(idxSvc++);
            }

            if (action.Equals(GattDevice.ACTION_READ_NEXT_CHARACTERISTIC))
            {
                Log.Debug(TAG, GattDevice.ACTION_READ_NEXT_CHARACTERISTIC);
                if (device != null)
                    device.ReadCharacteristic(idxChar++);
            }

            if (action.Equals(GattDevice.ACTION_ENUM_SERVICES_FINISHED))
            {
                Log.Debug(TAG, GattDevice.ACTION_ENUM_SERVICES_FINISHED);
            }

        }
    }
}