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
		final Messenger mMessenger;
		SoapProvider soapProvider;
		Handler ReplyTo;
		boolean isBound = true;
		
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
		Log.e("MyLog", "onCreate");		
	}

	@Override
	public void onRebind(Intent intent) {
		super.onRebind(intent);
		Log.e("MyLog", "onRebind");			
		isBound = true;
	}	
	
	@Override
	public boolean onUnbind(Intent intent) {
		super.onUnbind(intent);	
		Log.e("MyLog", "onUnbind");
		isBound = false;			
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
	
	public void GetBusPos(final String busNumber, final int ReplyMessage){
		try{			
			new Thread(new Runnable() {
				public void run() {
					while(isBound){				
						Bundle b = new Bundle();
			        	ArrayList<LatLng> arg0 = soapProvider.GetBusPos(busNumber);
			        	b.putParcelableArrayList("BusPos", arg0);

						Message bMsg = Message.obtain(null, ReplyMessage, 0, 0);
						bMsg.setData(b);
						try {							
							Log.e("MyLog", "Pos send");
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
		Log.e("MyLog", "onBind");
		isBound = true;
		return mBinder;
	}
	
	@Override
	public void onDestroy() {
		Log.e("MyLog", "onDestroy");
		isBound = false;
		stopSelf();
		super.onDestroy();		
	}	
}
