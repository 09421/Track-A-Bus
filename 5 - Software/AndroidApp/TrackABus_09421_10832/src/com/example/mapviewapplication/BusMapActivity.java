package com.example.mapviewapplication;

import java.util.ArrayList;
import java.util.Map;

import com.example.mapviewapplication.DataProviders.SoapProvider;
import com.example.mapviewapplication.DataProviders.TrackABusProvider;
import com.example.mapviewapplication.DataProviders.TrackABusProvider.LocalBinder;
import com.example.mapviewapplication.DataProviders.UserPrefBusRoute;
import com.google.android.gms.maps.CameraUpdateFactory;
import com.google.android.gms.maps.GoogleMap;
import com.google.android.gms.maps.GoogleMap.OnMarkerClickListener;
import com.google.android.gms.maps.MapFragment;
import com.google.android.gms.maps.SupportMapFragment;
import com.google.android.gms.maps.model.BitmapDescriptorFactory;
import com.google.android.gms.maps.model.LatLng;
import com.google.android.gms.maps.model.Marker;
import com.google.android.gms.maps.model.MarkerOptions;
import com.google.android.gms.maps.model.PolylineOptions;

import android.app.Activity;
import android.app.Fragment;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.database.Cursor;
import android.graphics.Color;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Message;
import android.text.format.Time;
import android.util.Log;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup.LayoutParams;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TableLayout;
import android.widget.TextView;
import android.widget.Toast;

public class BusMapActivity extends Activity {

	private GoogleMap map;
	private SoapProvider soapProvider;
	String SelectedBus;
	TrackABusProvider BusProvider;
	Boolean mBound = false;
	final static public int BUS_ROUTE_DONE = 1;
	final static public int BUS_STOP_DONE = 2;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.mapview);        
        Bundle extra = getIntent().getExtras();
        soapProvider = new SoapProvider();
        if(extra != null){        	
        	SelectedBus = extra.getString("SELECTED_BUS");
        	Log.e("MyLog", SelectedBus);
        	if(extra.getBoolean("isFavorite")){	
        		SetUpMap();
        		DrawFavoriteRoute(SelectedBus);       		
        	}
        	else{
      	      	mMapFragment = ((Fragment)getFragmentManager().findFragmentById(R.id.map));
      	      	mMapFragment.getView().setVisibility(View.GONE);
	        	pBar = (ProgressBar)findViewById(R.id.MapPBar);
	        	//TopBarLayout = (LinearLayout)findViewById(R.id.RouteInfo);
	            pBar.setVisibility(View.VISIBLE);
        		BusProvider = new TrackABusProvider(getApplicationContext(), new msgHandler());
        		BusProvider.GetBusRoute(extra.getString("SELECTED_BUS"), BUS_ROUTE_DONE);
        	}       	
        	
        	//LatLng BusLatLng = GetCurrentLatLngForBus(SelectedBus);
        	//SetUpMap(BusLatLng);
        }
        else{            
            SetUpMap();        	
        }
    }
    
    private Fragment mMapFragment;
    private ProgressBar pBar;
    private LinearLayout TopBarLayout;
	@Override
	protected void onStart() {
		super.onStart();
    	Intent intent = new Intent(getApplicationContext(), TrackABusProvider.class);
    	startService(intent);
    	bindService(intent, Connection, Context.BIND_AUTO_CREATE);
 
	}
    public String[] StopNames; 
	class msgHandler extends Handler{
		@Override
		public void handleMessage(Message msg) {				
			if(msg != null){
				switch(msg.what){
				case BUS_ROUTE_DONE:
			        pBar.setVisibility(View.GONE);
			        mMapFragment.getView().setVisibility(View.VISIBLE);
			        StopNames = msg.getData().getStringArray("StopName");
					SetUpMap(msg.getData().getFloatArray("RouteLat 0")[0], msg.getData().getFloatArray("RouteLng 0")[0]);
					Log.e("DEBUG SIZE", String.valueOf((msg.getData().size()-3)/2));
//					for(int i = 0; i<(msg.getData().size()-3)/2;i++){
//						DrawRoute(msg.getData().getFloatArray("RouteLat " + String.valueOf(i)), msg.getData().getFloatArray("RouteLng " + String.valueOf(i)));
//					}					
					DrawRoute(msg.getData().getFloatArray("RouteLat " + String.valueOf(0)), msg.getData().getFloatArray("RouteLng " + String.valueOf(0)));
					DrawBusStops(msg.getData().getFloatArray("StopLat"), msg.getData().getFloatArray("StopLng"));
					
					if(isOnline())
						UpdateBusLocation(msg.getData().getFloatArray("Lat"), msg.getData().getFloatArray("Lng"));
					else
						Toast.makeText(getApplicationContext(), "Not connected to internet, can only show route", Toast.LENGTH_LONG).show();					
					break;
				default:
					super.handleMessage(msg);
				}
			}
		}			
	}
    
    private LatLng GetCurrentLatLngForBus(String selectedBus) {    	
    	return new LatLng(56.177585, 10.218315);
	}
	private static final LatLng AARHUS = new LatLng(56.162939,10.203921);

	private void SetUpMap() {
		if(map == null){
			map = ((MapFragment) getFragmentManager().findFragmentById(R.id.map)).getMap();
		}
		if(map != null){
				map.moveCamera(CameraUpdateFactory.newLatLngZoom(AARHUS, 13));
			}
	}
	private void SetUpMap(float f, float g) {
		if(map == null){
			map = ((MapFragment) getFragmentManager().findFragmentById(R.id.map)).getMap();
		}
		if(map != null){
				map.moveCamera(CameraUpdateFactory.newLatLngZoom(new LatLng(f, g), 15));
			}
	}

	Thread t;
	ArrayList<LatLng> LatLngList;
	Marker mark;
	Handler handler = new Handler(Looper.getMainLooper());
	float fLat;
	float fLng;
	
//	private void ShowStopInformation(String BusStop, String DescEndPoint, String AscEndpoint)
//	{
//		((TextView)this.findViewById(R.id.StopInfo)).setText(BusStop);
//		((TextView)this.findViewById(R.id.DescendingBusEndStopInfo)).setText("Towards " + DescEndPoint);
//		((TextView)this.findViewById(R.id.AscendingBusEndStopInfo)).setText("Towards " + AscEndpoint);
//		((TextView)this.findViewById(R.id.DescendingBusTime)).setText("nn:nn:nn");
//		((TextView)this.findViewById(R.id.AscendingBusTime)).setText("nn:nn:nn");
//		
//		((TextView)this.findViewById(R.id.StopInfo)).setVisibility(TextView.VISIBLE);
//		((View)this.findViewById(R.id.RouteStopSeperator)).setVisibility(TextView.VISIBLE);
//		((LinearLayout)this.findViewById(R.id.DescendingBusTimeBar)).setVisibility(TextView.VISIBLE);
//		((LinearLayout)this.findViewById(R.id.AscendingBusTimeBar)).setVisibility(TextView.VISIBLE);
//	}
//	
//	private void HideStopInformation()
//	{
//		((TextView)this.findViewById(R.id.StopInfo)).setVisibility(TextView.GONE);
//		((View)this.findViewById(R.id.RouteStopSeperator)).setVisibility(TextView.GONE);
//		((LinearLayout)this.findViewById(R.id.DescendingBusTimeBar)).setVisibility(TextView.GONE);
//		((LinearLayout)this.findViewById(R.id.AscendingBusTimeBar)).setVisibility(TextView.GONE);
//	}
	
//	private void UpdateTime(String DescSeconds, String AscSeconds)
//	{
//		int DescRemainder = 0;
//		int AscRemainder = 0;
//		int intDescSeconds = Integer.parseInt(DescSeconds);
//		int intAscSeconds = Integer.parseInt(AscSeconds);
//		
//		int DescHour = (int) intDescSeconds/3600;
//		int AscHour = (int) intAscSeconds/3600;
//		DescRemainder = intDescSeconds - DescHour*3600;
//		AscRemainder = intAscSeconds - AscHour*3600;
//		
//		int DescMin = (int) DescRemainder/60;
//		int AscMin = (int) AscRemainder/60;
//		DescRemainder = DescRemainder - DescMin * 60;
//		AscRemainder = AscRemainder - AscMin * 60;
//		
//		int DescSec = DescRemainder;
//		int AscSec = AscRemainder;
//		String DescTime = String.format("%02d:%02d%02d", DescHour,DescMin,DescSec);
//		String AscTime = String.format("%02d:%02d%02d", AscHour,AscMin,AscSec);
//		((TextView)this.findViewById(R.id.DescendingBusTime)).setText(DescTime);
//		((TextView)this.findViewById(R.id.AscendingBusTime)).setText(AscTime);
//		
//	}
	
	private void UpdateBusLocation(float[] Lat, float[] Lng) {
		
		new Thread(new Runnable() {
	        public void run() {

	        	final LatLng test = soapProvider.GetBusPos(SelectedBus).get(0);

		    		Runnable setFirstMark = new Runnable(){
	
		    			@Override
		    			public void run() {
		    				mark = map.addMarker(new MarkerOptions().position(test));
		    				Log.e("DEBUG!", "MARKER ADDED");
		    			}			
		    		};
		    		
	    		handler.post(setFirstMark);
	    		t = new Thread(new updateMarker());
	    		t.start();
	        	}
	        }).start();	
	}	


	Boolean Running = true;
	public class updateMarker implements Runnable{
		
		@Override
		public void run() {
			while(Running){				
					CurrentLatLng = soapProvider.GetBusPos(SelectedBus).get(0);
					handler.post(setMark);
					try {
						Thread.sleep(1000);
						if(!Running)
							break;
					} catch (InterruptedException e) {
						e.printStackTrace();
					}				
			}
		}
		LatLng CurrentLatLng;
		Runnable setMark = new Runnable(){

			@Override
			public void run() {
				mark.remove();
				mark = map.addMarker(new MarkerOptions().position(CurrentLatLng));
			}			
		};		
	}
	
	
	private void DrawRoute(float[] Lat, float[] Lng) {
		
		 PolylineOptions pOption = new PolylineOptions().width(10).color(0x66ff0000);
		 for(int i = 0; i < Lat.length; i++){
			 pOption.add(new LatLng(Lat[i], Lng[i]));
		 }

		 map.addPolyline(pOption);
	}
	
	private void DrawBusStops(float[] Lat, float[] Lng) {
		
		for(int i = 0; i<Lat.length; i++){
			map.addMarker(new MarkerOptions()
	        .position(new LatLng(Lat[i], Lng[i])).title(StopNames[i]).icon(BitmapDescriptorFactory.fromResource(R.drawable.teststop)));
		}

		map.setOnMarkerClickListener(new OnMarkerClickListener() {
			
			@Override
			public boolean onMarkerClick(Marker marker) {
				Log.e("DEBUG!!", "Marker click : " + String.valueOf(marker.getTitle()));
				return false;
			}
		});
	}
	
	private void DrawFavoriteRoute(final String selectedBus){
		
		Runnable r = new Runnable(){
			@Override
			public void run() {
				Cursor c = getContentResolver().query(UserPrefBusRoute.CONTENT_URI,null, selectedBus, null, null);
				c.moveToFirst();
				
				final float[] Lat = new float[c.getCount()];
				final float[] Lng = new float[c.getCount()];
				for(int i = 0; i < c.getCount(); i++)
				{
					Lat[i] = c.getFloat(0);
					Lng[i] = c.getFloat(1);
					c.moveToNext();
				}
				
				UpdateBusLocation(Lat, Lng);
				Runnable SetRoute = new Runnable(){

					@Override
					public void run() {
						DrawRoute(Lat, Lng);
					}			
				};
				handler.post(SetRoute);				
			}
		};		
		
		Thread t = new Thread(r);
		t.start();
	}
	
	@Override
	protected void onDestroy() {
		super.onDestroy();
		Running = false;
	}

	@Override
	protected void onPause() {
		super.onPause();
		Running = false;		
	}
	
	@Override
	protected void onRestart() {
		super.onRestart();
	}

	@Override
	protected void onResume() {
		super.onResume();
		if(t != null && !t.isAlive())
		{
	  	   	t = new Thread(new updateMarker());
	  	   	Running = true;
	  	   	t.start();
		}		
	}
	
	@Override
	protected void onStop() {
		super.onStop();
		mBound = false;
		unbindService(Connection);
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
	

	
    public boolean isOnline() {
        ConnectivityManager cm =
            (ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE);
        NetworkInfo netInfo = cm.getActiveNetworkInfo();
        if (netInfo != null && netInfo.isConnectedOrConnecting()) {
            return true;
        }
        return false;
    }
}


