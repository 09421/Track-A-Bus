package com.example.mapviewapplication.TrackABus;

import com.google.android.gms.maps.model.LatLng;

public class BusStop {
	
	public BusStop(String name, LatLng pos){
		Name = name;
		Position = pos;
	}
	public String Name;
	public LatLng Position;	
}
