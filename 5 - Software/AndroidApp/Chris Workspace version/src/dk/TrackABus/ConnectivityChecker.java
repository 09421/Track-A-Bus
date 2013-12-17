package dk.TrackABus;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.util.Log;

public  class ConnectivityChecker extends BroadcastReceiver{

	
	public static boolean hasInternet;
	@Override
	public void onReceive(Context context, Intent intent) {
		ConnectivityManager cMan = (ConnectivityManager)context.getSystemService(Context.CONNECTIVITY_SERVICE);
		NetworkInfo nInfo = cMan.getActiveNetworkInfo();
		if(nInfo != null && nInfo.isConnected())
		{
			Log.e("connect", "Connected to the internet!");
			hasInternet = true;
		}
		else
		{
			Log.e("connect", "Disconnected...");
			hasInternet = false;
		}
	}
	
	public static void setInternetConn(Context c)
	{
		ConnectivityManager cMan = (ConnectivityManager)c.getSystemService(Context.CONNECTIVITY_SERVICE);
		NetworkInfo nInfo = cMan.getActiveNetworkInfo();
		if(nInfo != null && nInfo.isConnected())
		{
			Log.e("connect", "Connected to the internet!");
			hasInternet = true;
		}
		else
		{
			Log.e("connect", "Disconnected...");
			hasInternet = false;
		
		}
	}

}
