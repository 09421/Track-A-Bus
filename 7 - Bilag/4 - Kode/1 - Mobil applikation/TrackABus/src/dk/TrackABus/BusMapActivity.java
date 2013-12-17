package dk.TrackABus;

import java.util.ArrayList;
import java.util.Locale;

import dk.TrackABus.DataProviders.TrackABusProvider;
import dk.TrackABus.DataProviders.TrackABusProvider.LocalBinder;
import dk.TrackABus.Models.BusRoute;
import dk.TrackABus.Models.BusStop;
import dk.TrackABus.Models.RoutePoint;
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
	private boolean isFavorite;
	String SelectedBus;
	TrackABusProvider BusProvider;
	boolean mBound = false;
	boolean timeMsgShown = false;
	boolean posMsgShown = false;
	ArrayList<Marker> marks;

	final static public int BUS_ROUTE_DONE = 1;
	final static public int BUS_POS_DONE = 2;
	final static public int BUS_TIME_DONE = 3;
	
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.mapview);        
        Bundle extra = getIntent().getExtras();
        marks = new ArrayList<Marker>();
        if(extra != null){        	
        	SelectedBus = extra.getString("SELECTED_BUS"); /*Get name for selected route*/
        	Log.e("MyLog", SelectedBus);
        	if(extra.getBoolean("isFavorite")){/*If selected route is favorite*/
	        	TopBarLayout = (LinearLayout)findViewById(R.id.TopInformationBar);
		        TopBarLayout.setVisibility(LinearLayout.VISIBLE);
	        	BusRouteView = (TextView)findViewById(R.id.RouteInfo);
	        	BusRouteView.setText(SelectedBus);
		        mMapFragment = ((Fragment)getFragmentManager().findFragmentById(R.id.map));
		        mMapFragment.getView().setVisibility(View.VISIBLE);
        		isFavorite = true;
        		SetUpMap();
        	}
        	else{
	        	pBar = (ProgressBar)findViewById(R.id.MapPBar);
	        	pBar.setVisibility(View.VISIBLE);
	        	TopBarLayout = (LinearLayout)findViewById(R.id.TopInformationBar);
	        	BusRouteView = (TextView)findViewById(R.id.RouteInfo);
	        	BusRouteView.setText(SelectedBus);
      	      	mMapFragment = ((Fragment)getFragmentManager().findFragmentById(R.id.map));
      	      	mMapFragment.getView().setVisibility(View.GONE);
	        	isFavorite = false;
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
    Intent intent;
	@Override
	protected void onStart() {
		super.onStart();
		/*Set up binding to TrackABusProvider*/
    	intent = new Intent(BusMapActivity.this, TrackABusProvider.class);
    	startService(intent);
    	bindService(intent, Connection, Context.BIND_AUTO_CREATE);
 
	}
    public String[] StopNames; 
    /*Message handler used to retrive messages from TrackABusProvider*/
	@SuppressLint("HandlerLeak")
	class msgHandler extends Handler{
		@Override
		public void handleMessage(Message msg) {				
			if(msg != null){
				switch(msg.what){
				case BUS_ROUTE_DONE:
			        View mapFragment = mMapFragment.getView();
			        //return if user closes window during loading.
			        if(mapFragment == null)
			        	return;
			        pBar.setVisibility(View.GONE);
			        TopBarLayout.setVisibility(LinearLayout.VISIBLE);
			        mapFragment.setVisibility(View.VISIBLE);
			        ((View)findViewById(R.id.TopInformationBarFrame)).setVisibility(View.VISIBLE);
			        
					ArrayList<BusRoute> BusRoutes = msg.getData().getParcelableArrayList("BusRoute");
					ArrayList<BusStop> BusStops = msg.getData().getParcelableArrayList("BusStop");
			        
					if(BusRoutes == null || BusStops == null){
						Toast.makeText(getApplicationContext(), "Not connected to internet, can only show route", Toast.LENGTH_LONG).show();
						return;
					}
					SetUpMap();
					for(int i = 0; i<BusRoutes.size();i++){
						DrawRoute(BusRoutes.get(i).points);
					}
					DrawBusStops(BusStops);
					
					if(ConnectivityChecker.hasInternet)
						UpdateBusLocation();
					else
						Toast.makeText(getApplicationContext(), "Not connected to internet, can only show route", Toast.LENGTH_LONG).show();					
					break;
				case BUS_POS_DONE:
					ArrayList<LatLng> Pos =  msg.getData().getParcelableArrayList("BusPos");
    				if(Pos != null){
    					for(int j = 0; j<marks.size(); j++){
    						marks.get(j).remove();
    					}
    					marks = new ArrayList<Marker>();
    					for(int i = 0; i<Pos.size(); i++){
    						marks.add(map.addMarker(new MarkerOptions()
							.position(Pos.get(i))
							.icon(BitmapDescriptorFactory.fromResource(R.drawable.bus))));
    					}
    					posMsgShown = false;
    				}
    				else if(!posMsgShown && !ConnectivityChecker.hasInternet)
    				{
    					Toast.makeText(BusMapActivity.this, "No connection to internet. Cannot update position", Toast.LENGTH_LONG).show();
    					posMsgShown = true;
    				}
					break;
				case BUS_TIME_DONE:
					ArrayList<String> busToStop = msg.getData().getStringArrayList("busTime");
					if(busToStop.size() != 0)
					{								
						String asc = busToStop.get(0);
						String desc = busToStop.get(1);
						String ascStop = busToStop.get(2);
						String descStop = busToStop.get(3);
						
						timeMsgShown=false;
						UpdateTime(asc, desc, ascStop,descStop);
					}
					else if(!timeMsgShown)
					{
						Toast.makeText(BusMapActivity.this, "No connection to internet. Cannot update time", Toast.LENGTH_LONG).show();
						timeMsgShown = true;
					}
					break;
				default:
					super.handleMessage(msg);
				}
			}
		}			
	}    
	
	/*GPS coordinates used to center map on Aarhus*/
	private static final LatLng AARHUS = new LatLng(56.162939,10.203921);

	private void SetUpMap() {
		if(map == null){
			map = ((MapFragment) getFragmentManager().findFragmentById(R.id.map)).getMap();
		}
		if(map != null){
				mUiSettings = map.getUiSettings();
				/*Disables unwanted map features*/
				mUiSettings.setZoomControlsEnabled(false);
				mUiSettings.setCompassEnabled(false);
				mUiSettings.setRotateGesturesEnabled(false);
				/*Zoom the map to Aarhus*/
				map.moveCamera(CameraUpdateFactory.newLatLngZoom(AARHUS, 13));
			}
	}

	
	ArrayList<LatLng> LatLngList;
	Marker mark;
	Handler handler = new Handler(Looper.getMainLooper());
	float fLat;
	float fLng;
	
	/*Set up all information for a chosen bus stop*/
	private void InitStopInformation(String BusStop)
	{
		((TextView)this.findViewById(R.id.StopInfo)).setText(BusStop);
		((TextView)this.findViewById(R.id.DescendingBusEndStopInfo)).setText("Loading");
		((TextView)this.findViewById(R.id.AscendingBusEndStopInfo)).setText("Loading");
		((TextView)this.findViewById(R.id.DescendingBusTime)).setText("nn:nn:nn");
		((TextView)this.findViewById(R.id.AscendingBusTime)).setText("nn:nn:nn"); 
		
		((TextView)this.findViewById(R.id.StopInfo)).setVisibility(TextView.VISIBLE);
		((View)this.findViewById(R.id.RouteStopSeperator)).setVisibility(TextView.VISIBLE);
		((View)this.findViewById(R.id.DescAscSeperator)).setVisibility(View.VISIBLE);
		((View)this.findViewById(R.id.DescAscFrame)).setVisibility(View.VISIBLE);
		((LinearLayout)this.findViewById(R.id.DescendingBusTimeBar)).setVisibility(TextView.VISIBLE);
		((LinearLayout)this.findViewById(R.id.AscendingBusTimeBar)).setVisibility(TextView.VISIBLE);
	}	
	
	/*updates the time, in descending order*/
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
	/*updates the time, in ascending order*/
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
	
	/*Callto the TrackABusProvider to retive current position for all busses on a chosen route*/
	private void UpdateBusLocation() {
		if(ConnectivityChecker.hasInternet)
			BusProvider.GetBusPos(SelectedBus, BUS_POS_DONE, new msgHandler());
		else
			Toast.makeText(BusMapActivity.this, "No connection to internet. Cannot update position", Toast.LENGTH_LONG).show();
	}	
	
	/*Uses a Polyline to draw a bus route on the map using points*/
	private void DrawRoute(ArrayList<RoutePoint> points) {

		 PolylineOptions pOption = new PolylineOptions().width(10).color(0x66ff0000);
		 for(int i = 0; i < points.size(); i++){
			 pOption.add(points.get(i).Position);
		 }
		 map.addPolyline(pOption);
	}
	
	/*Uses a Polyline to draw a bus route on the map using floats of Lat and Lng*/
	private void DrawRoute(float[] Lat, float[] Lng)
	{
		 PolylineOptions pOption = new PolylineOptions().width(10).color(0x66ff0000);
		 for(int i = 0; i < Lat.length; i++){
			 pOption.add(new LatLng(Lat[i],Lng[i]));
		 }
		 map.addPolyline(pOption);
	}

	/*Uses to draw markers, representing bus stops, on the map*/
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
			
			/*Click event for all markers, drawn on the map*/
			@Override
			public boolean onMarkerClick(Marker marker) {
				
				if(marker.getTitle() == null)
					return false;
				InitStopInformation(marker.getTitle());
	
				if(!ConnectivityChecker.hasInternet)
				{
					Toast.makeText(BusMapActivity.this, "No connection to internet. Cannot update time", Toast.LENGTH_SHORT).show();
					return false;
				}
				if(BusProvider.timeThread != null)
				{
					BusProvider.handlingBusTime = false;
					BusProvider.timeThread.interrupt();
					
					while(BusProvider.timeThread.isAlive())
					{
						try {
							Thread.sleep(10);
						} catch (InterruptedException e) {
							e.printStackTrace();
						}						
					}
				}
				String stop = ((TextView)findViewById(R.id.StopInfo)).getText().toString();
				String route=((TextView)findViewById(R.id.RouteInfo)).getText().toString();
				BusProvider.GetBusTime(route, stop, BUS_TIME_DONE, new msgHandler());
				return false;
			}
		});
	}
	
	/*If chosen route is favorit, get in from SQLite database, and draw it*/
	private void DrawFavoriteRoute(final String selectedBus){		
		Runnable DrawRouter = new Runnable(){
			@Override
			public void run() {
				ArrayList<String> Routes = ContentProviderAcces.GetBusRoutes(getApplicationContext(), selectedBus);
				Runnable[] drawingRoute = new Runnable[Routes.size()];
				for(int i = 0; i < drawingRoute.length; i++)
				{
					final ArrayList<float[]> routePoints = ContentProviderAcces.GetBusRoutePoints(getApplicationContext(), Routes.get(i));
					final ArrayList<BusStop> stops = ContentProviderAcces.GetBusStops(getApplicationContext(), Routes.get(i));
					drawingRoute[i] = new Runnable(){
						@Override
						public void run() { 
							DrawRoute(routePoints.get(0), routePoints.get(1));
							DrawBusStops(stops);	
						}
					};
					handler.post(drawingRoute[i]);
				}						
			}
		};		
		
		UpdateBusLocation();
		Thread DrawRoutet = new Thread(DrawRouter);
		DrawRoutet.start();

	}
	/*Update the time on the map*/
	private void UpdateTime(String a, String d, String aS, String dS)
	{
		String currentAscText = (String) ((TextView)findViewById(R.id.AscendingBusEndStopInfo)).getText();
		String currentDescText = (String) ((TextView)findViewById(R.id.DescendingBusEndStopInfo)).getText();
		if(currentAscText != "No bus going this direction")	 
		{
			if(aS.contains("anyType"))
			{
				((TextView)findViewById(R.id.AscendingBusEndStopInfo)).setText("No bus going this direction");
				UpdateTimeAsc("-1");
			}
			else
			{
				((TextView)findViewById(R.id.AscendingBusEndStopInfo)).setText("Towards " + aS);
				UpdateTimeAsc(a);
			} 
		}
		if(currentDescText != "No bus going this direction")	
		{
			if(dS.contains("anyType"))
			{
				((TextView)findViewById(R.id.DescendingBusEndStopInfo)).setText("No bus going this direction");
				UpdateTimeDesc("-1");
			}
			else
			{
				((TextView)findViewById(R.id.DescendingBusEndStopInfo)).setText("Towards " + dS);
				UpdateTimeDesc(d);
			}
		}
	}
	
	@Override
	protected void onDestroy() {		
		mBound = false;	
		super.onDestroy();
	}

	@Override
	protected void onPause() {
		super.onPause();
	}
	
	@Override
	protected void onRestart() {
		super.onRestart();
	}

	@Override
	protected void onResume() {
		super.onResume();

	}
	
	@Override
	protected void onStop() {
		BusProvider.StopWork();
		unbindService(Connection);/*unbind service, TrackABusProvider*/
		super.onStop();
		mBound = false;	
		
	}
	
	/*Connection used to bind to TrackABusProvider*/
	private ServiceConnection Connection = new ServiceConnection(){
		
		/*When async bind is done*/
		@Override
		public void onServiceConnected(ComponentName name, IBinder service) {
			LocalBinder  binder = (LocalBinder ) service;
			BusProvider = binder.getService();
			mBound = true;
			if(!isFavorite)
				BusProvider.GetBusRoute(SelectedBus, BUS_ROUTE_DONE, new msgHandler());
			else
				DrawFavoriteRoute(SelectedBus);
			
			if(((TextView)findViewById(R.id.StopInfo)).getVisibility() != TextView.GONE)
			{
				String stop = ((TextView)findViewById(R.id.StopInfo)).getText().toString();
				String route=((TextView)findViewById(R.id.RouteInfo)).getText().toString();
				BusProvider.GetBusTime(route, stop, BUS_TIME_DONE, new msgHandler());
			}	
		}

		@Override
		public void onServiceDisconnected(ComponentName name) {
			mBound = false;
		}
	};

}

