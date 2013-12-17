package dk.TrackABus.DataProviders;


import java.util.ArrayList;

import com.google.android.gms.maps.model.LatLng;

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
import dk.TrackABus.Models.BusRoute;
import dk.TrackABus.Models.BusStop;

public class TrackABusProvider extends Service{
		private final IBinder mBinder = new LocalBinder();
		static Context Context;
		Messenger mMessenger;
		SoapProvider soapProvider;
		Handler ReplyTo;
		public boolean handlingBusPos = true;
		public boolean handlingBusTime = true;
		public Thread timeThread;
		
	/**
	 * Constructor for TrackABusProvider
	 * @param context applicationContext
	 * @param replyTo The handler that messages will be send to, when async methods are done
	 */		
	public TrackABusProvider(){
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
		StopWork();
		super.onUnbind(intent);
		return true;
	}

	/**
	 * Sends a message back, with an StringArrayList at key "1"
	 * @param ReplyMessage The what parameter in the message
	 */
	public void GetBusNumber(final int ReplyMessage, final Handler replyTo) {		
		new Thread(new Runnable() {
	        public void run() {
	        	mMessenger = new Messenger(replyTo);
	    		ArrayList<String> BusName = new ArrayList<String>();
	    		BusName = soapProvider.GetBusList();
	    		Bundle b = new Bundle();
	    		if(BusName != null)
	    			b.putStringArrayList("1", BusName);
	    		else
	    			b.putString("Error", "Error");
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
	public void GetBusRoute(final String busNumber, final int ReplyMessage, final Handler replyTo){
		try{
			new Thread(new Runnable() {
		        public void run() {
		        	mMessenger = new Messenger(replyTo);
		        	Bundle b = new Bundle();
		        	ArrayList<BusRoute> arg0 = soapProvider.GetBusRoute(busNumber);
		        	ArrayList<BusStop> arg1 = soapProvider.GetBusStops(busNumber);
		        	b.putParcelableArrayList("BusRoute", arg0);
		        	b.putParcelableArrayList("BusStop", arg1);

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
			Log.e("MyLog", e.getMessage());
		}		
	}
	
	public void GetBusPos(final String busNumber, final int ReplyMessage, final Handler replyTo){
		try{			
			new Thread(new Runnable() {
				public void run() {
					mMessenger = new Messenger(replyTo);
					handlingBusPos = true;
					while(handlingBusPos){				
					Bundle b = new Bundle();
		        	ArrayList<LatLng> arg0 = soapProvider.GetBusPos(busNumber);
			        	b.putParcelableArrayList("BusPos", arg0);

						Message bMsg = Message.obtain(null, ReplyMessage, 0, 0);
						bMsg.setData(b);
						try {
							mMessenger.send(bMsg);							
						}catch (RemoteException e) {
							Log.e("MyLog", "Failed to send message");
							e.printStackTrace();						
						}
						try {
							Thread.sleep(1000);
						} catch (InterruptedException e) {
							Log.e("MyLog", "Failed to sleep");
							e.printStackTrace();
						}
					}
				}
			}).start();
			}catch(Exception e){
				Log.e("MyLog", e.getMessage());
			}
	}
	
	public void GetBusTime(final String busNumber, final String BusStop, final int ReplyMessage, final Handler replyTo)
	{
		try{			
			timeThread = new Thread(new Runnable() {
				public void run() {
					mMessenger = new Messenger(replyTo);
					handlingBusTime = true;
					while(handlingBusTime){
						ArrayList<String> busToStop = soapProvider.GetBusToStopTime(BusStop,busNumber);
						Message timeMessage = Message.obtain(null,ReplyMessage,0,0);
						Bundle b = new Bundle();
						b.putStringArrayList("busTime", busToStop);
						timeMessage.setData(b);
						
						try {
							mMessenger.send(timeMessage);				
							Thread.sleep(2000);
						}catch (Exception e) {
							e.printStackTrace();						
						}						
					}
				}
			});
			}catch(Exception e){
				Log.e("MyLog", e.getMessage());
			}
		if(timeThread != null)
			timeThread.start();
	}
	

	
	/**
	 * Used to force stop any while loops there might be running
	 */
	public void StopWork(){
		handlingBusPos = false;
		handlingBusTime = false;
		stopSelf();
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
		StopWork();
		super.onDestroy();			
	}	
	
}
