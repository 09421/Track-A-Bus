package com.example.mapviewapplication.TrackABus;

import android.os.Parcel;
import android.os.Parcelable;

import com.google.android.gms.maps.model.LatLng;

public class RoutePoint implements Parcelable{

	public LatLng Position;
	public String ID;
	public RoutePoint(LatLng pos, String id)
	{
		Position = pos; ID = id;
	}
	
	public RoutePoint(Parcel in)
	{
		Position = in.readParcelable(LatLng.class.getClassLoader());
		ID = in.readString();
	}
	
	public static final Parcelable.Creator<RoutePoint> CREATOR = new Parcelable.Creator<RoutePoint>()
			{

				@Override
				public RoutePoint createFromParcel(Parcel source) {
					return new RoutePoint(source);
				}

				@Override
				public RoutePoint[] newArray(int size) {
					// TODO Auto-generated method stub
					return null;
				}
		
			};
	
	@Override
	public int describeContents() {
		// TODO Auto-generated method stub
		return 0;
	}
	@Override
	public void writeToParcel(Parcel dest, int flags) {
		// TODO Auto-generated method stub
		dest.writeParcelable(Position, flags);
		dest.writeString(ID);
	}
}
