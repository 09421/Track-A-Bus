package dk.TrackABus;

import java.util.ArrayList;
import dk.TrackABus.DataProviders.TrackABusProvider;
import dk.TrackABus.DataProviders.TrackABusProvider.LocalBinder;
import dk.TrackABus.Models.ListBusData;
import dk.TrackABus.Models.UserPrefBusRoute;

import android.annotation.SuppressLint;
import android.app.ListActivity;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.database.Cursor;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Message;
import android.util.Log;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.ListView;
import android.widget.ProgressBar;
import android.widget.Toast;

public class BuslistMenuActivity extends ListActivity {
		ArrayList<String> BusList;
		ArrayAdapter<String> adapter;
		TrackABusProvider BusProvider;
		BuslistAdapter busListAdapter = null;
		Boolean mBound = false;
		ProgressBar pBar; 
		
		final static public int BUSSES_DONE = 1;		
		
	    @Override
	    protected void onCreate(Bundle savedInstanceState) {
	        super.onCreate(savedInstanceState);
	        setContentView(R.layout.activity_mainmenu); 
	        pBar = (ProgressBar)findViewById(R.id.PBar);
	        pBar.setVisibility(View.VISIBLE);
	        BusList = new ArrayList<String>(); 
	    }
	    
	    @Override
	    protected void onStart(){
	    	super.onStart();
	    	if(ConnectivityChecker.hasInternet){
		    	Intent intent = new Intent(getApplicationContext(), TrackABusProvider.class);
		    	startService(intent);
		    	bindService(intent, Connection, Context.BIND_AUTO_CREATE);
		    	
		    	UpdateArrayOfBusses();	    		
	    	}
	    	else{
	    		pBar.setVisibility(View.GONE);
	    		Toast.makeText(getApplicationContext(), "Please check internet connection, and try Again", Toast.LENGTH_LONG).show();
	    	}
	    }

		private void UpdateArrayOfBusses() {			
			TrackABusProvider BusProvider = new TrackABusProvider(getApplicationContext(), new msgHandler());
			BusProvider.GetBusNumber(BUSSES_DONE);
			if(BusList == null){
				Log.e("MyLog", "NULL");
			}			
		}
		
		ArrayList<ListBusData> AllBusses;
		private void UpdateBusList(ArrayList<String> busList) {
			
			AllBusses = new ArrayList<ListBusData>();	    	
			Cursor c = getContentResolver().query(UserPrefBusRoute.CONTENT_URI, null,null, null, null);
			ArrayList<String> FavoriteBusses = new ArrayList<String>();		

			c.moveToFirst();
			for(int i = 0; i < c.getCount(); i++)
			{
				FavoriteBusses.add(c.getString(0));
				c.moveToNext();
			}	    	
 	
	    	for(int i = 0; i < busList.size(); i++)
	    		AllBusses.add(i, new ListBusData(FavoriteBusses.contains(busList.get(i)), busList.get(i)));   	
	    	busListAdapter = new BuslistAdapter(AllBusses, getApplicationContext());
			setListAdapter(busListAdapter);
	    	pBar.setVisibility(View.GONE);			
		}

		@Override
		protected void onListItemClick(ListView l, View v, int position, long id) {
			super.onListItemClick(l, v, position, id);	

			
			Intent myIntent = new Intent(getApplicationContext(), BusMapActivity.class);
			ListBusData a = (ListBusData)l.getItemAtPosition(position);	
			if(!ConnectivityChecker.hasInternet && !a.IsFavorite)
			{
				Toast.makeText(this, "No connection to internet. Can only show route for favorite bus " , Toast.LENGTH_SHORT).show();
				return;
			}
			
			myIntent.putExtra("SELECTED_BUS", a.BusNumber);
			myIntent.putExtra("isFavorite",a.IsFavorite);
			startActivityForResult(myIntent, 0);
		}
		
		@SuppressLint("HandlerLeak")
		class msgHandler extends Handler{			
			@Override
			public void handleMessage(Message msg) {				
				if(msg != null){
					switch(msg.what){
					case BUSSES_DONE:
						if(msg.getData().getStringArrayList("1").size() > 0)
							UpdateBusList(msg.getData().getStringArrayList("1"));
						else{
							Toast.makeText(getApplicationContext(), "No bus routes found", Toast.LENGTH_SHORT).show();
							pBar.setVisibility(View.GONE);
						}
						break;
					default:
						super.handleMessage(msg);
					}
				}
			}			
		}
		
		private ServiceConnection Connection = new ServiceConnection(){
			
			@Override
			public void onServiceConnected(ComponentName name, IBinder service) {
				LocalBinder  binder = (LocalBinder ) service;
				BusProvider = binder.getService();
				mBound = true;
			}

			@Override
			public void onServiceDisconnected(ComponentName name) {
				mBound = false;				
			}
		};
		
		@Override
		protected void onDestroy() {
			try{
				if(mBound)
					Log.e("DEBUG", "SERVICE UNBOUND");
					unbindService(Connection);
			}catch(Exception e){
				Log.e("DEBUG", e.getMessage());				
			}

			super.onDestroy();
		}

		@Override
		protected void onPause() {
			super.onPause();

		}
		
		@Override
		protected void onStop() {
			super.onStop();
				

		}
}