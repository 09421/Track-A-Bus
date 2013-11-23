package com.example.mapviewapplication;

import java.util.ArrayList;

import com.example.mapviewapplication.DataProviders.TrackABusProvider;
import com.example.mapviewapplication.DataProviders.UserPrefBusRoute;
import com.example.mapviewapplication.DataProviders.UserPrefBusses;
import com.example.mapviewapplication.TrackABus.ListBusData;

import android.content.ContentValues;
import android.content.Context;
import android.graphics.Color;
import android.net.Uri;
import android.os.Handler;
import android.os.Message;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.CompoundButton;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.ToggleButton;

public class buslistadapter extends BaseAdapter{

	private ArrayList<ListBusData> _data;
    Context _c;
    
    public buslistadapter (ArrayList<ListBusData> data, Context c){
        _data = data;
        _c = c;
    }
    
	final static public int BUS_ROUTE_DONE = 1;
	class msgHandler extends Handler{
		
		@Override
		public void handleMessage(Message msg) {				
			if(msg != null){
				switch(msg.what){
				case BUS_ROUTE_DONE:
					SetBusRoute(msg.getData().getFloatArray("Lat"), msg.getData().getFloatArray("Lng"));
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

        final ToggleButton tBtn = (ToggleButton) v.findViewById(R.id.FavoriteToggle);
        TextView tView = (TextView)v.findViewById(R.id.tv_item);
                

	    final ListBusData LBD = _data.get(position);
	    final int p = position;
	    
	    tBtn.setChecked(LBD.IsFavorite);
	    tView.setText(LBD.BusNumber);	
	    
          tBtn.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
        	    public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
        	        if (isChecked) {
        	        	if(_c.getContentResolver().query(UserPrefBusses.CONTENT_URI, null,null, null, null).getCount() > 5){
        	        		tBtn.setChecked(false);
        	        		LBD.IsFavorite = false;
        	        		Toast t = Toast.makeText(_c, "Limit of favorite busses reached: (MAX 6)", Toast.LENGTH_SHORT);
        	        		TextView v = (TextView) t.getView().findViewById(android.R.id.message);
        	        		v.setTextColor(Color.RED);
        	        		t.show();
        	        	}else{
            	        	SetBusAsFavorite(LBD.BusNumber);
            	        	LBD.IsFavorite = true;
        	        	}
        	        } else {
        	        	RemoveFavorite(LBD.BusNumber);
        	        	LBD.IsFavorite = false;
        	        }
        	    }
        	});
                       
       return v;
	}

	String SelectedBus;
	private void SetBusAsFavorite(String BusNumber){
		SelectedBus = BusNumber;
	    final ContentValues cv1 = new ContentValues();         
	    cv1.put("BusNumber", BusNumber);
	    _c.getContentResolver().insert(UserPrefBusses.CONTENT_URI, cv1);  		
		TrackABusProvider BusProvider = new TrackABusProvider(_c, new msgHandler());
		BusProvider.GetBusRoute(BusNumber, BUS_ROUTE_DONE);
	}
	
	private void SetBusRoute(float[] lat, float[] lng){
		
		Log.e("MyLog", "Length of lat: " + String.valueOf(lat.length));
  		final ContentValues[] cv2 = new ContentValues[lat.length];
  		
  		for(int i = 0; i < lat.length; i++)
  		{
  			cv2[i] = new ContentValues();
  			cv2[i].put("RouteLat", lat[i]);
  			cv2[i].put("RouteLon", lng[i]);  		
  		}
  		
    	_c.getContentResolver().bulkInsert(Uri.parse(UserPrefBusRoute.CONTENT_URI.toString() + "/"+SelectedBus), cv2);
	}

	
	protected void RemoveFavorite(String busNumber) {
		_c.getContentResolver().delete(UserPrefBusses.CONTENT_URI, busNumber, null);
		
	}
}
















