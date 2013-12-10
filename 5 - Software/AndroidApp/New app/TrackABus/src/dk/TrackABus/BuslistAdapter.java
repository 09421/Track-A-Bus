package dk.TrackABus;

import java.util.ArrayList;

import javax.xml.datatype.Duration;

import com.google.android.gms.maps.model.LatLng;
import com.google.android.gms.maps.model.Marker;

import dk.TrackABus.DataProviders.TrackABusProvider;
import dk.TrackABus.DataProviders.TrackABusProvider.LocalBinder;
import dk.TrackABus.Models.AdapterRunner;
import dk.TrackABus.Models.BusRoute;
import dk.TrackABus.Models.BusStop;
import dk.TrackABus.Models.ListBusData;
import dk.TrackABus.Models.UserPrefBusRoute;
import dk.TrackABus.Models.UserPrefBusRouteBusStop;
import dk.TrackABus.Models.UserPrefBusRouteRoutePoint;
import dk.TrackABus.Models.UserPrefBusStop;
import dk.TrackABus.Models.UserPrefRoutePoint;

import android.annotation.SuppressLint;
import android.content.BroadcastReceiver;
import android.content.ComponentName;
import android.content.ContentValues;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.graphics.Color;
import android.net.Uri;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Message;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.CompoundButton;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.ToggleButton;


public class BuslistAdapter extends BaseAdapter{
	private ArrayList<ListBusData> _data;
    public boolean favoriteRunning = true;
    TrackABusProvider BusProvider;
    public boolean isHandlingFavoring = false;
    private Handler handler = new Handler(Looper.getMainLooper());
    Context _c;
    
    
    
    public BuslistAdapter (ArrayList<ListBusData> data, Context c){
        _data = data;
        _c = c;
        //AdapterRunner.setter();
        BusProvider = new TrackABusProvider();
    }
   
  
	final static public int BUS_ROUTE_DONE = 1;
	@SuppressLint("HandlerLeak")
	class msgHandler extends Handler{
		@Override
		public void handleMessage(Message msg) {				
			if(msg != null){
				switch(msg.what){
				case BUS_ROUTE_DONE:
					ArrayList<BusRoute> BR = msg.getData().getParcelableArrayList("BusRoute");
					ArrayList<BusStop> St = msg.getData().getParcelableArrayList("BusStop");
					SetFavoriteBusRoute(BR, St);
					break;
				default:
					super.handleMessage(msg);
				}
			}
		}			
	}
	
	@Override
    public int getViewTypeCount() {         
        return _data.size();
    }

    @Override
    public int getItemViewType(int position) {
        return position;
    }
	
	@Override
	public int getCount() {
		return _data.size();
	}

	@Override
	public Object getItem(int position) {
		return _data.get(position);
	}

	@Override
	public long getItemId(int position) {
		return position;
	}

	Boolean savedState;
	
	@Override
	public View getView(final int position, View convertView, ViewGroup parent) {
		View v = convertView;
        if (v == null)
        {
           LayoutInflater vi = (LayoutInflater)_c.getSystemService(Context.LAYOUT_INFLATER_SERVICE);
           v = vi.inflate(R.layout.listview_layout, null);
           
        }

        final ToggleButton tBtn = (ToggleButton) v.findViewById(R.id.FavoriteToggle);; 
        final ProgressBar Fbar = (ProgressBar)v.findViewById(R.id.FProgressBar);
        TextView tView = (TextView)v.findViewById(R.id.tv_item);
               
	    final ListBusData LBD = _data.get(position);
	    if(AdapterRunner.currentData != null)
	    {
		    if(LBD.BusNumber.equals(AdapterRunner.currentData.BusNumber) && AdapterRunner.currentHandling)
		    {
		    	Log.e("test", "handling");
		    	tBtn.setChecked(AdapterRunner.currentData.IsFavorite);
		    	tBtn.setVisibility(ToggleButton.GONE);
		    	Fbar.setVisibility(ProgressBar.VISIBLE);
		    	AdapterRunner.currentButton = tBtn;
		    	AdapterRunner.currentBar = Fbar;
		    	LBD.IsFavorite = AdapterRunner.currentData.IsFavorite;
		    }
		      
	    }
	    tBtn.setChecked(LBD.IsFavorite);
	    tView.setText(LBD.BusNumber);	
          tBtn.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
        	    public void onCheckedChanged(CompoundButton buttonView, final boolean isChecked) 
        	    {	
        	    	isHandlingFavoring = AdapterRunner.currentHandling;
        	    	if(isHandlingFavoring)
        	    	{
        	    		tBtn.setChecked(!isChecked); 
        	    		Toast.makeText(_c, "Please wait until other route is favorited/unfavorited", Toast.LENGTH_SHORT).show();
        	    		return;
        	    	}
        	    	isHandlingFavoring = true; 
	    	        if (isChecked) 
	    	        {
	    	        	int test = _c.getContentResolver().query(UserPrefBusRoute.CONTENT_URI, null,null,null, null).getCount();
	    	        	if(test > 5){
	    	        		tBtn.setChecked(false); 
	    	        		LBD.IsFavorite = false;
	    	        		Toast t = Toast.makeText(_c, "Limit of favorite routes reached: (MAX 6)", Toast.LENGTH_SHORT);
	    	        		t.show();
	    	        	}
		    	        else if(ConnectivityChecker.hasInternet) 
		    	        { 
		    	        	HandleFavorite(LBD.BusNumber);
				    	    LBD.IsFavorite = true;
				    	    tBtn.setVisibility(ToggleButton.GONE);
				    	    Fbar.setVisibility(ProgressBar.VISIBLE);
				    	    AdapterRunner.currentData = LBD;
				    	    AdapterRunner.currentHandling = true;
		        	    	AdapterRunner.currentButton = tBtn;
		        	    	AdapterRunner.currentBar = Fbar;
		    	        }		
		    	        else
		    	        {
		    	        	tBtn.setChecked(false); 
		    	        	Toast.makeText(_c, "No connection to internet. Cannont set route to favorite.", Toast.LENGTH_SHORT).show();
		    	        }
	    	        }
	    	        else 
	    	        {
			    	    tBtn.setVisibility(ToggleButton.GONE);
			    	    Fbar.setVisibility(ProgressBar.VISIBLE);
	    	        	RemoveFavorite(LBD.BusNumber); 
	    	        	LBD.IsFavorite = false;
	        	    	AdapterRunner.currentData = LBD;
	        	    	AdapterRunner.currentHandling = true;
	        	    	AdapterRunner.currentButton = tBtn;
	        	    	AdapterRunner.currentBar = Fbar;
	    	        }
        	    }
        	});
       return v;
	}


	private void HandleFavorite(String BusNumber)
	{
		BusProvider.GetBusRoute(BusNumber, BUS_ROUTE_DONE, new msgHandler());
	}
	
	private void SetFavoriteBusRoute(final ArrayList<BusRoute> bRoute,final ArrayList<BusStop> sRoute){ 
		new Thread(new Runnable()
		{
			@Override
			public void run() 
			{
				if(bRoute.size() == 0 || sRoute.size() == 0)
				{
					Toast.makeText(_c, "Cannot favorite this route", Toast.LENGTH_SHORT).show();
					isHandlingFavoring = false;
					setSpinnerAndButton();
					return;
				}
				ContentValues[] BusRouteCV;
				ContentValues[] RoutePointCV;
				ContentValues[] BusRouteRoutePointCV;
				try
				{
					for(int i = 0; i < bRoute.size(); i++)
					{
						BusRouteCV = new ContentValues[1];
						BusRouteCV[0] = new ContentValues();
						BusRouteCV[0].put(UserPrefBusRoute.BusRouteIdField, bRoute.get(i).ID);
						BusRouteCV[0].put(UserPrefBusRoute.BusRouteNumberField, bRoute.get(i).RouteNumber);
						BusRouteCV[0].put(UserPrefBusRoute.BusRouteSubField, bRoute.get(i).SubRoute);
						RoutePointCV = new ContentValues[bRoute.get(i).points.size()];
						BusRouteRoutePointCV = new ContentValues[bRoute.get(i).BusRoute_RoutePointIDs.size()];
						for(int j = 0; j < bRoute.get(i).points.size(); j++)
						{
							RoutePointCV[j] = new ContentValues();
							BusRouteRoutePointCV[j] = new ContentValues();
							RoutePointCV[j].put(UserPrefRoutePoint.RoutePointIdField, bRoute.get(i).points.get(j).ID);
							RoutePointCV[j].put(UserPrefRoutePoint.RoutePointLatField, bRoute.get(i).points.get(j).Position.latitude);
							RoutePointCV[j].put(UserPrefRoutePoint.RoutePointLonField, bRoute.get(i).points.get(j).Position.longitude);
		
							BusRouteRoutePointCV[j].put(UserPrefBusRouteRoutePoint.BusRouteRoutePointIDField,
														bRoute.get(i).BusRoute_RoutePointIDs.get(j));
							BusRouteRoutePointCV[j].put(UserPrefBusRouteRoutePoint.BusRouteField, bRoute.get(i).ID);
							BusRouteRoutePointCV[j].put(UserPrefBusRouteRoutePoint.RoutePointField, bRoute.get(i).points.get(j).ID);
														
						}
						int checkVal = _c.getContentResolver().bulkInsert(Uri.parse(UserPrefBusRoute.CONTENT_URI.toString()+"/"+bRoute.get(i).ID), BusRouteCV);
						if(checkVal == 0)
						{
							setSpinnerAndButton();
							return;
						}
						_c.getContentResolver().bulkInsert(UserPrefRoutePoint.CONTENT_URI, RoutePointCV);
						_c.getContentResolver().bulkInsert(UserPrefBusRouteRoutePoint.CONTENT_URI, BusRouteRoutePointCV);
					}
			  		
					ContentValues[] BusStopPointsCV = new ContentValues[sRoute.size()];
					ContentValues[] BusStopCV = new ContentValues[sRoute.size()];
					ContentValues[] BusRouteBusStopCV = new ContentValues[sRoute.size()];
					for(int i = 0; i < sRoute.size(); i++)
					{
						BusRouteBusStopCV[i] = new ContentValues();
						if(_c.getContentResolver().query(Uri.parse(UserPrefBusStop.CONTENT_URI.toString() + "/"+sRoute.get(i).ID), null, null, null, null).getCount() == 0)
						{
							BusStopPointsCV[i] = new ContentValues();
							BusStopCV[i] = new ContentValues();
							BusStopPointsCV[i].put(UserPrefRoutePoint.RoutePointIdField, sRoute.get(i).Position.ID);
							BusStopPointsCV[i].put(UserPrefRoutePoint.RoutePointLatField, sRoute.get(i).Position.Position.latitude);
							BusStopPointsCV[i].put(UserPrefRoutePoint.RoutePointLonField, sRoute.get(i).Position.Position.longitude);
							BusStopCV[i].put(UserPrefBusStop.BusStopIdField, sRoute.get(i).ID);
							BusStopCV[i].put(UserPrefBusStop.BusStopNameField, sRoute.get(i).Name);
							BusStopCV[i].put(UserPrefBusStop.BusStopForeignRoutePointField,sRoute.get(i).Position.ID);
						}
							BusRouteBusStopCV[i].put(UserPrefBusRouteBusStop.BusRouteBusStopIDField, sRoute.get(i).BusRoute_BusStopID);
							BusRouteBusStopCV[i].put(UserPrefBusRouteBusStop.BusRouteField, sRoute.get(i).RouteID);
							BusRouteBusStopCV[i].put(UserPrefBusRouteBusStop.BusStopField, sRoute.get(i).ID);
					} 
					
					_c.getContentResolver().bulkInsert(UserPrefRoutePoint.CONTENT_URI, BusStopPointsCV);
					_c.getContentResolver().bulkInsert(UserPrefBusStop.CONTENT_URI, BusStopCV);
					_c.getContentResolver().bulkInsert(UserPrefBusRouteBusStop.CONTENT_URI, BusRouteBusStopCV);
				}
				
				catch(Exception e)
				{
					String err = (e.getMessage()==null)?"Save failed":e.getMessage();
					Log.e("DEBUG","FAVORITE ERR: " + err);
					setSpinnerAndButton();
				}
				setSpinnerAndButton();
			}
		}).start();
		

	}
	protected void RemoveFavorite(final String busNumber) {
	
		new Thread(new Runnable(){
			@Override
			public void run() {
				int d = _c.getContentResolver().delete(UserPrefBusRoute.CONTENT_URI, busNumber, null);
				setSpinnerAndButton();
			}
		}).start();
	}
	
	private void setSpinnerAndButton()
	{
		Runnable Runn = new Runnable()
		{
			@Override
			public void run() {
				AdapterRunner.currentBar.setVisibility(ProgressBar.GONE);
				AdapterRunner.currentButton.setVisibility(ToggleButton.VISIBLE);
				AdapterRunner.currentButton.setChecked(AdapterRunner.currentData.IsFavorite);
				AdapterRunner.currentHandling = false;
				isHandlingFavoring = false;
				Log.e("test", "SetChecked = " + String.valueOf(AdapterRunner.currentData.IsFavorite));
			}
		};
		handler.post(Runn);
	}
}
















