package com.example.mapviewapplication.DataProviders;

import java.net.MalformedURLException;
import java.util.ArrayList;
import java.util.List;

import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.os.IBinder;
import android.os.Binder;
import android.os.Handler;
import android.os.Message;
import android.os.Messenger;
import android.os.RemoteException;
import android.util.Log;
import com.example.mapviewapplication.TrackABus.Bus_Route;
import com.example.mapviewapplication.TrackABus.Busses;
import com.google.android.gms.maps.model.LatLng;

public class TrackABusProvider extends Service{
		private final IBinder mBinder = new LocalBinder();
		static Context Context;
		final Messenger mMessenger;
		SoapProvider soapProvider;
		Handler ReplyTo;
		
	/**
	 * Constructor for TrackABusProvider
	 * @param context applicationContext
	 * @param replyTo The handler that messages will be send to, when async methods are done
	 */
	public TrackABusProvider(Context context, Handler replyTo){
		Context = context;	
		ReplyTo = replyTo;
		mMessenger = new Messenger(ReplyTo);
		soapProvider = new SoapProvider();
	}
	
	@Override
	public void onCreate() {
		super.onCreate();
	}

	@Override
	public void onRebind(Intent intent) {
		super.onRebind(intent);	
	}	
	
	@Override
	public boolean onUnbind(Intent intent) {
		return true;
	}

	/**
	 * Sends a message back, with an StringArrayList at key "1"
	 * @param ReplyMessage The what parameter in the message
	 */
	public void GetBusNumber(final int ReplyMessage) {
		
		new Thread(new Runnable() {
	        public void run() {
	    		ArrayList<String> BusName = new ArrayList<String>();
	    		BusName = soapProvider.GetBusList();
	    		
	    		Bundle b = new Bundle();
	    		b.putStringArrayList("1", BusName);
	    		Message bMsg = Message.obtain(null, ReplyMessage, 0, 0);
	    		bMsg.setData(b);
	    		try {
	    			mMessenger.send(bMsg);
	    		} catch (RemoteException e) {
	    			Log.e("MyLog", "Failed to send message");
	    			e.printStackTrace();						
	    		}	        	
	        }
	    }).start();	
	}
	
	/**
	 * Sends a message back, with 2 an float[] at key "Lat" and "Lng"
	 * @param busNumber The Bus number you want the route for
	 * @param ReplyMessage the message that should be send back
	 */
	public void GetBusRoute(final String busNumber, final int ReplyMessage){
		try{
			
			new Thread(new Runnable() {
		        public void run() {
		        	ArrayList<LatLng> arg0 = soapProvider.GetBusRoute(busNumber);
					
					float[] Lat = new float[arg0.size()];
					float[] Lng = new float[arg0.size()];

					for(int i = 0; i < arg0.size(); i++){
						Lat[i] = (float) arg0.get(i).latitude;
						Lng[i] = (float) arg0.get(i).longitude;
					}
					
					Bundle b = new Bundle();
					b.putFloatArray("Lat", Lat);
					b.putFloatArray("Lng", Lng);
					
					Message bMsg = Message.obtain(null, ReplyMessage, 0, 0);
					bMsg.setData(b);
					try {
						mMessenger.send(bMsg);
					} catch (RemoteException e) {
						Log.e("MyLog", "Failed to send message");
						e.printStackTrace();						
					}
		        }}).start();

			
		}catch(Exception e){
			Log.e("MyLog", "You are fucked");
		}		
	}
	
	/**
	 * Binder for the bindService
	 */
	public class LocalBinder extends Binder{
		public TrackABusProvider getService(){
			return TrackABusProvider.this;
		}
	}

	@Override
	public IBinder onBind(Intent intent) {
		return mBinder;
	}
	@Override
	public void onDestroy() {
		super.onDestroy();
		stopSelf();
	}
	
	
}
