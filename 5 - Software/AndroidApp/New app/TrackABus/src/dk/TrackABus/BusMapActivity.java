package dk.TrackABus;

import java.util.ArrayList;
import java.util.Locale;

import dk.TrackABus.DataProviders.SoapProvider;
import dk.TrackABus.DataProviders.TrackABusProvider;
import dk.TrackABus.DataProviders.TrackABusProvider.LocalBinder;
import dk.TrackABus.Models.BusRoute;
import dk.TrackABus.Models.BusStop;
import dk.TrackABus.Models.RoutePoint;
import dk.TrackABus.Models.UserPrefBusRoute;
import dk.TrackABus.Models.UserPrefBusStop;
import dk.TrackABus.Models.UserPrefRoutePoint;

import com.google.android.gms.maps.CameraUpdateFactory;
import com.google.android.gms.maps.GoogleMap;
import com.google.android.gms.maps.GoogleMap.OnMarkerClickListener;
import com.google.android.gms.maps.MapFragment;
import com.google.android.gms.maps.UiSettings;
import com.google.android.gms.maps.model.BitmapDescriptorFactory;
import com.google.android.gms.maps.model.LatLng;
import com.google.android.gms.maps.model.Marker;
import com.google.android.gms.maps.model.MarkerOptions;
import com.google.android.gms.maps.model.PolylineOptions;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.Fragment;
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
import android.os.Looper;
import android.os.Message;
import android.util.Log;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

public class BusMapActivity extends Activity {
	
	private GoogleMap map;
	private UiSettings mUiSettings;
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
	        	pBar = (ProgressBar)findViewById(R.id.MapPBar);
	        	TopBarLayout = (LinearLayout)findViewById(R.id.TopInformationBar);
	        	BusRouteView = (TextView)findViewById(R.id.RouteInfo);
		        TopBarLayout.setVisibility(LinearLayout.VISIBLE);
		        mMapFragment = ((Fragment)getFragmentManager().findFragmentById(R.id.map));
		        mMapFragment.getView().setVisibility(View.VISIBLE);
	        	BusRouteView.setText(SelectedBus);
        		SetUpMap();
        		DrawFavoriteRoute(SelectedBus);
        	}
        	else{
      	      	mMapFragment = ((Fragment)getFragmentManager().findFragmentById(R.id.map));
      	      	mMapFragment.getView().setVisibility(View.GONE);
	        	pBar = (ProgressBar)findViewById(R.id.MapPBar);
	        	TopBarLayout = (LinearLayout)findViewById(R.id.TopInformationBar);
	        	BusRouteView = (TextView)findViewById(R.id.RouteInfo);
	        	BusRouteView.setText(SelectedBus);
	        	BusProvider = new TrackABusProvider(getApplicationContext(), new msgHandler()); 
	        	pBar.setVisibility(View.VISIBLE);
	        	BusProvider.GetBusRoute(SelectedBus, BUS_ROUTE_DONE);
        	}       	
        }
        else{            
            SetUpMap();       	
        }
    }
    
    private Fragment mMapFragment;
    private ProgressBar pBar;
    private LinearLayout TopBarLayout;
    private TextView BusRouteView;

	@Override
	protected void onStart() {
		super.onStart();
    	Intent intent = new Intent(getApplicationContext(), TrackABusProvider.class);
    	startService(intent);
    	bindService(intent, Connection, Context.BIND_AUTO_CREATE);
 
	}
    public String[] StopNames; 
	@SuppressLint("HandlerLeak")
	class msgHandler extends Handler{
		@Override
		public void handleMessage(Message msg) {				
			if(msg != null){
				switch(msg.what){
				case BUS_ROUTE_DONE:
			        pBar.setVisibility(View.GONE);
			        TopBarLayout.setVisibility(LinearLayout.VISIBLE);
			        mMapFragment.getView().setVisibility(View.VISIBLE);
			        ((View)findViewById(R.id.TopInformationBarFrame)).setVisibility(View.VISIBLE);
			        
					ArrayList<BusRoute> BusRoutes = msg.getData().getParcelableArrayList("BusRoute");
					ArrayList<BusStop> BusStops = msg.getData().getParcelableArrayList("BusStop");
			        
					SetUpMap();
					for(int i = 0; i<BusRoutes.size();i++){
						DrawRoute(BusRoutes.get(i).points);
					}
					DrawBusStops(BusStops);
					
					if(isOnline())
						UpdateBusLocation();
					else
						Toast.makeText(getApplicationContext(), "Not connected to internet, can only show route", Toast.LENGTH_LONG).show();					
					break;
				default:
					super.handleMessage(msg);
				}
			}
		}			
	}
    

	private static final LatLng AARHUS = new LatLng(56.162939,10.203921);

	private void SetUpMap() {
		if(map == null){
			map = ((MapFragment) getFragmentManager().findFragmentById(R.id.map)).getMap();
		}
		if(map != null){
				mUiSettings = map.getUiSettings();
				mUiSettings.setZoomControlsEnabled(false);
				mUiSettings.setCompassEnabled(false);
				mUiSettings.setRotateGesturesEnabled(false);
				map.moveCamera(CameraUpdateFactory.newLatLngZoom(AARHUS, 13));
			}
	}

	Thread t;
	ArrayList<LatLng> LatLngList;
	Marker mark;
	Handler handler = new Handler(Looper.getMainLooper());
	float fLat;
	float fLng;
	
	private void InitStopInformation(String BusStop)
	{
		((TextView)this.findViewById(R.id.StopInfo)).setText(BusStop);
		((TextView)this.findViewById(R.id.DescendingBusEndStopInfo)).setText("-------------");
		((TextView)this.findViewById(R.id.AscendingBusEndStopInfo)).setText("-------------");
		((TextView)this.findViewById(R.id.DescendingBusTime)).setText("nn:nn:nn");
		((TextView)this.findViewById(R.id.AscendingBusTime)).setText("nn:nn:nn"); 
		
		((TextView)this.findViewById(R.id.StopInfo)).setVisibility(TextView.VISIBLE);
		((View)this.findViewById(R.id.RouteStopSeperator)).setVisibility(TextView.VISIBLE);
		((View)this.findViewById(R.id.DescAscSeperator)).setVisibility(View.VISIBLE);
		((View)this.findViewById(R.id.DescAscFrame)).setVisibility(View.VISIBLE);
		((LinearLayout)this.findViewById(R.id.DescendingBusTimeBar)).setVisibility(TextView.VISIBLE);
		((LinearLayout)this.findViewById(R.id.AscendingBusTimeBar)).setVisibility(TextView.VISIBLE);
	}
	
	private void UpdateTimeDesc(String DescSeconds)
	{
		if(Integer.parseInt(DescSeconds) != -1)
		{
			int DescRemainder = 0;
			int intDescSeconds = Integer.parseInt(DescSeconds);
			int DescHour = (int) intDescSeconds/3600;
			DescRemainder = intDescSeconds - DescHour*3600;
			int DescMin = (int) DescRemainder/60;
			DescRemainder = DescRemainder - DescMin * 60;
			int DescSec = DescRemainder;
			String DescTime = String.format(Locale.getDefault(), "%02d:%02d:%02d", DescHour,DescMin,DescSec);
			((TextView)this.findViewById(R.id.DescendingBusTime)).setText(DescTime);
		}
		else
		{
			((TextView)this.findViewById(R.id.DescendingBusTime)).setText("nn:nn:nn");
		}
	}
	private void UpdateTimeAsc(String AscSeconds)
	{
		if(Integer.parseInt(AscSeconds) != -1)
		{
			int AscRemainder = 0;
			int intAscSeconds = Integer.parseInt(AscSeconds);
			int AscHour = (int) intAscSeconds/3600;
			AscRemainder = intAscSeconds - AscHour*3600;
			int AscMin = (int) AscRemainder/60;
			AscRemainder = AscRemainder - AscMin * 60;
			int AscSec = AscRemainder;
			String AscTime = String.format(Locale.getDefault(),"%02d:%02d:%02d", AscHour,AscMin,AscSec);
			((TextView)this.findViewById(R.id.AscendingBusTime)).setText(AscTime);
		}
		else
		{
			((TextView)this.findViewById(R.id.AscendingBusTime)).setText("nn:nn:nn");
		}		
	}
	
	private void UpdateBusLocation() {
		new Thread(new Runnable() {
	        public void run() { 

	        	final ArrayList<LatLng> SelectedBusPos = soapProvider.GetBusPos(SelectedBus);
	
	    		Runnable setFirstMark = new Runnable(){
	
	    			@Override
	    			public void run() {
	    				marks = new ArrayList<Marker>();
	    				if(SelectedBusPos != null)
	    					for(int i = 0; i<SelectedBusPos.size(); i++)
	    						marks.add(map.addMarker(new MarkerOptions()
								    						.position(SelectedBusPos.get(i))
								    						.icon(BitmapDescriptorFactory.fromResource(R.drawable.bus))));
	    				else
	    					Toast.makeText(getApplicationContext(), "No bus found on route", Toast.LENGTH_SHORT).show();
	    			}			
	    		};
	    		handler.post(setFirstMark);
			
			t = new Thread(new updateMarker());
			t.start();
			}	
	    }).start();	
	}	

	ArrayList<Marker> marks;
	Boolean Running = true;
	public class updateMarker implements Runnable{
		
		@Override
		public void run() {
			while(Running){				
				CurrentLatLng = soapProvider.GetBusPos(SelectedBus);
				if(CurrentLatLng != null){
					handler.post(setMark);
				}
				try {
					Thread.sleep(1000);
					if(!Running)
						break;
				} catch (InterruptedException e) {
					e.printStackTrace();
				}				
			}
		}
		ArrayList<LatLng> CurrentLatLng;
		Runnable setMark = new Runnable(){

			@Override
			public void run() {
				for(int j = 0; j<marks.size(); j++){
					marks.get(j).remove();
				}
				marks = new ArrayList<Marker>();
				for(int i = 0; i<CurrentLatLng.size(); i++){					
					marks.add(map.addMarker(new MarkerOptions()
					.position(CurrentLatLng.get(i))
					.icon(BitmapDescriptorFactory.fromResource(R.drawable.bus))));
				}			
			}			
		};		
	}	
	
	private void DrawRoute(ArrayList<RoutePoint> points) {

		 PolylineOptions pOption = new PolylineOptions().width(10).color(0x66ff0000);
		 for(int i = 0; i < points.size(); i++){
			 pOption.add(points.get(i).Position);
		 }
		 map.addPolyline(pOption);
	}
	
	private void DrawRoute(float[] Lat, float[] Lng)
	{
		 PolylineOptions pOption = new PolylineOptions().width(10).color(0x66ff0000);
		 for(int i = 0; i < Lat.length; i++){
			 pOption.add(new LatLng(Lat[i],Lng[i]));
		 }
		 map.addPolyline(pOption);
	}
	
	private boolean timeUpdating;
	private Thread updateTimeThread;
	private void DrawBusStops(final ArrayList<BusStop> stops)
	{
		if(stops != null){
			for(int i = 0; i<stops.size(); i++){
				map.addMarker(new MarkerOptions()
		        .position(stops.get(i).Position.Position).title(stops.get(i).Name)
				.icon(BitmapDescriptorFactory.fromResource(R.drawable.teststop)));
			}
		}else{
			Toast.makeText(getApplicationContext(), "There are no bus stops on chosen route", Toast.LENGTH_LONG).show();
		}
		
		map.setOnMarkerClickListener(new OnMarkerClickListener() {
			
			@Override
			public boolean onMarkerClick(Marker marker) {
				
				if(marker.getTitle() == null)
					return false;
				InitStopInformation(marker.getTitle());
			    
				Runnable ru = new Runnable(){
					@Override
					public void run(){
						
						class UpdateTimeRunnable implements Runnable{
							private String ascS;
							private String descS;
							private String aStop;
							private String dStop;
							public UpdateTimeRunnable(String ascSec, String descSec, String ascStop, String descStop)
							{
								ascS = ascSec; descS = descSec;
								aStop = ascStop; dStop = descStop;
							}
							@Override
							public void run() {
								String currentAscText = (String) ((TextView)findViewById(R.id.AscendingBusEndStopInfo)).getText();
								String currentDescText = (String) ((TextView)findViewById(R.id.DescendingBusEndStopInfo)).getText();
								if(currentAscText!= aStop || currentAscText != "No bus going this direction")	
								{
									if(aStop.contains("anyType"))
									{
										((TextView)findViewById(R.id.AscendingBusEndStopInfo)).setText("No bus going this direction");
										
									}
									else
									{
										((TextView)findViewById(R.id.AscendingBusEndStopInfo)).setText("Towards " + aStop);
									} 
								}
								if(!aStop.contains("anyType"))
								{
									UpdateTimeAsc(ascS);
								}
								if(currentDescText != dStop || currentDescText != "No bus going this direction")	
								{
									if(dStop.contains("anyType"))
									{
										((TextView)findViewById(R.id.DescendingBusEndStopInfo)).setText("No bus going this direction");
									
									}
									else
									{
										((TextView)findViewById(R.id.DescendingBusEndStopInfo)).setText("Towards " + dStop);
									}
								}
								if(!dStop.contains("anyType"))
								{
									UpdateTimeDesc(descS);
								}
							}							
						}
						
						while(timeUpdating)
						{  
							Log.e("busToStop","Update!");
							String stop = ((TextView)findViewById(R.id.StopInfo)).getText().toString();
							String route=((TextView)findViewById(R.id.RouteInfo)).getText().toString();
							ArrayList<String> busToStop = soapProvider.GetBusToStopTime(stop,route);
							if(busToStop.size() != 1)
							{								
								String asc = busToStop.get(0);
								String desc = busToStop.get(1);
								String ascStop = busToStop.get(2);
								String descStop = busToStop.get(3);
								Log.e("busToStop", asc);
								Log.e("busToStop", desc);

								UpdateTimeRunnable r = new UpdateTimeRunnable(asc,desc,ascStop,descStop);
								handler.post(r);
							}
							else
								Log.e("busToStop",busToStop.get(0) );//Something went wrong in the service
							
							try {
								Thread.sleep(2000);
							} catch (InterruptedException e) {
								e.printStackTrace();
							}
						}
					}
				};
				timeUpdating = false;
				
				if(updateTimeThread != null)
				{
					updateTimeThread.interrupt();//Breaks Thread If Sleeping
					
					while(updateTimeThread.isAlive())
					{
						try {
							Thread.sleep(1);
						} catch (InterruptedException e) {
							e.printStackTrace();
						}						
					}
				}
				
				timeUpdating = true;
			    updateTimeThread = new Thread(ru);
			    updateTimeThread.start();
				return false;
			}
		});
	}
	
	private void DrawFavoriteRoute(final String selectedBus){
		
		Runnable r = new Runnable(){
			@Override
			public void run() {
				final Cursor RouteCursor = getContentResolver().query(UserPrefBusRoute.CONTENT_URI,null, selectedBus, null, null);
				RouteCursor.moveToFirst();
				
				Runnable[] drawingRoute = new Runnable[RouteCursor.getCount()];
				for(int i = 0; i < drawingRoute.length; i++)
				{
					Cursor Route = getContentResolver().query(UserPrefRoutePoint.CONTENT_URI, null, RouteCursor.getString(0), null, null);
					Cursor BusStops = getContentResolver().query(UserPrefBusStop.CONTENT_URI, null, RouteCursor.getString(0), null, null);
					final float[] LatRoute = new float[Route.getCount()];
					final float[] LngRoute = new float[Route.getCount()];
					Route.moveToFirst();
					BusStops.moveToFirst();
					final ArrayList<BusStop> stops = new ArrayList<BusStop>();
					for(int j = 0; j < Route.getCount(); j++)
					{
						LatRoute[j] = Route.getFloat(0);
						LngRoute[j] = Route.getFloat(1);
						Route.moveToNext();
					}
					for(int k = 0; k < BusStops.getCount(); k++)
					{
						stops.add(new BusStop(new RoutePoint(new LatLng(BusStops.getFloat(1),BusStops.getFloat(2)),null)
										, BusStops.getString(0), null, null, null));
						BusStops.moveToNext();
					}
					drawingRoute[i] = new Runnable(){
						@Override
						public void run() {

							DrawRoute(LatRoute, LngRoute);
							DrawBusStops(stops);
						}
					};
					handler.post(drawingRoute[i]);
					RouteCursor.moveToNext();
				}				
				UpdateBusLocation();				
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
		timeUpdating = false;
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