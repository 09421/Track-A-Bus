package dk.TrackABus;

import java.util.ArrayList;


import dk.TrackABus.DataProviders.TrackABusProvider;
import dk.TrackABus.Models.AdapterRunner;
import dk.TrackABus.Models.BusRoute;
import dk.TrackABus.Models.BusStop;
import dk.TrackABus.Models.ListBusData;

import android.annotation.SuppressLint;
import android.content.Context;
import android.os.Handler;
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


/**
 * Class used to describe a custom list item
 *
 */
public class BuslistAdapter extends BaseAdapter{
	private ArrayList<ListBusData> _data;
    public boolean favoriteRunning = true;
    TrackABusProvider BusProvider;
    public boolean isHandlingFavoring = false;
    private Handler handler = new Handler(Looper.getMainLooper());
    Context _c;
    
    
    
    /**
     * @param data: data to show on list
     * @param c: application context
     */
    public BuslistAdapter (ArrayList<ListBusData> data, Context c){
        _data = data;
        _c = c;
        BusProvider = new TrackABusProvider();
    }
   
  
	final static public int BUS_ROUTE_DONE = 1;
	/**
	 * Retrieves return messages from the TrackABusProvider class
	 *
	 */
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
        	    	{/* If already in progress of marking another route as favorite */
        	    		tBtn.setChecked(!isChecked); 
        	    		Toast.makeText(_c, "Please wait until other route is favorited/unfavorited", Toast.LENGTH_SHORT).show();
        	    		return;
        	    	}
        	    	isHandlingFavoring = true; 
	    	        if (isChecked) 
	    	        {
	    	        	if(ConnectivityChecker.hasInternet) 
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
				ContentProviderAcces.SetNewFavorite(_c, bRoute, sRoute);
				setSpinnerAndButton();
			}
		}).start();
		

	}
	private void RemoveFavorite(final String busNumber) {
	
		new Thread(new Runnable(){
			@Override
			public void run() {
				ContentProviderAcces.DeleteBusRoute(_c,busNumber);
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
			}
		};
		handler.post(Runn);
	}
}
















