package dk.TrackABus.Models;

import android.os.Parcel;
import android.os.Parcelable;

public class BusStop implements Parcelable {
	
	public RoutePoint Position;
	public String Name;
	public String ID;
	public String RouteID;
	public String BusRoute_BusStopID;
	
	public BusStop(RoutePoint pos, String name, String id, String rID, String BR_BSID){
		Position = pos;
		Name = name;
		ID = id;
		RouteID = rID;
		BusRoute_BusStopID = BR_BSID;
	}
	
	public BusStop(Parcel in)
	{
		Position = in.readParcelable(RoutePoint.class.getClassLoader());
		Name = in.readString();
		ID = in.readString();
		RouteID = in.readString();
		BusRoute_BusStopID = in.readString();
	}

	public static final Parcelable.Creator<BusStop> CREATOR = new Parcelable.Creator<BusStop>()
	{
		@Override
		public BusStop createFromParcel(Parcel source) {
			return new BusStop(source);
		}

		@Override
		public BusStop[] newArray(int size) {
			return null;
		}		
	};
	
	@Override
	public int describeContents() {
		return 0;
	}

	@Override
	public void writeToParcel(Parcel dest, int flags) {
		dest.writeParcelable(Position, flags);
		dest.writeString(Name);
		dest.writeString(ID);
		dest.writeString(RouteID);
		dest.writeString(BusRoute_BusStopID);
	}

}
