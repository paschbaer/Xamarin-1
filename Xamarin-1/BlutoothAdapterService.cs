using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Blu
{
    [Service]
    public class BlutoothAdapterService : Service
    {
        public readonly static String TAG = typeof(BlutoothAdapterService).Name;

        public IBinder binder { get; private set; }
        protected BlutoothAdapter blu;

        public BlutoothAdapterService()
        {}

        public override IBinder OnBind(Intent intent)
        {
            this.binder = new BlutoothAdapterServiceBinder(this);
            return this.binder;
        }

        public override bool OnUnbind(Intent intent)
        {
            return base.OnUnbind(intent);
        }

        public override void OnCreate()
        {
            base.OnCreate();

            binder = null;
            blu = new Blu.BlutoothAdapter();
        }

        public override void OnDestroy()
        {
            binder = null;
            blu = null;

            base.OnDestroy();
        }

        public bool StartLeScan(Activity activity, Android.Widget.ListView textout)
        {
            if (blu != null)
                return blu.StartLeScan(activity, textout);

            return false;
        }

        public string GetName()
        {
            if (blu != null)
                return blu.GetName();

            return null;
        }

        public void StopLeScan()
        {
            if (blu != null)
                blu.StopLeScan();
        }

        public void EnumServices(Context ctx, string identifier)
        {
            if(blu != null)
                blu.EnumServices(ctx, identifier);
        }
    }

    public class BlutoothAdapterServiceBinder : Binder
    {
        public BlutoothAdapterService service { get; private set; }

        public BlutoothAdapterServiceBinder(BlutoothAdapterService service)
        {
            this.service = service;
        }
    }

    public class BlutoothAdapterServiceConnection : Java.Lang.Object, IServiceConnection
    {
        static readonly string TAG = typeof(BlutoothAdapterServiceConnection).FullName;

        public bool isConnected { get; private set; }
        public BlutoothAdapterServiceBinder binder { get; private set; }

        MainActivity mainActivity;
        public BlutoothAdapterServiceConnection(MainActivity activity)
        {
            isConnected = false;
            binder = null;
            mainActivity = activity;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            binder = service as BlutoothAdapterServiceBinder;
            isConnected = this.binder != null;

            if (isConnected)
            {
                Log.Debug(TAG, "bound to service {0}", name.ClassName);
                //mainActivity.UpdateUiForBoundService();
            }
            else
            {
                  //mainActivity.UpdateUiForBoundService();
            }

            //mainActivity.timestampMessageTextView.Text = message;

        }

        public void OnServiceDisconnected(ComponentName name)
        {
            isConnected = false;
            binder = null;
        }

        public bool StartLeScan(Activity activity, Android.Widget.ListView textout)
        {
            if (isConnected)
                return binder.service.StartLeScan(activity, textout);   //return binder?.StartLeScan(activity, textout);

            return false;
        }

        public string GetName()
        {
            if (isConnected)
                return binder.service.GetName();

            return null;
        }

        public void StopLeScan()
        {
            if (isConnected)
                binder.service.StopLeScan();
        }

        public void EnumServices(string identifier)
        {
            if (isConnected)
                binder.service.EnumServices(mainActivity.ApplicationContext, identifier);
        }
    }
}