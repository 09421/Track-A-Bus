package dk.TrackABus.Models;


import java.util.ArrayList;
import java.util.Arrays;

import android.os.Parcel;
import android.os.Parcelable;

//Model class for a busroute
public class BusRoute implements Parcelable {
	
	public ArrayList<RoutePoint> points;
	public ArrayList<String> BusRoute_RoutePointIDs;
	public String SubRoute;
	public String RouteNumber;
	public String ID;
	
	public BusRoute(ArrayList<RoutePoint> rPoints, ArrayList<String> BusRoute_RoutePointID, String sub, String rNum, String id)
	{
		points = rPoints;
		BusRoute_RoutePointIDs = BusRoute_RoutePointID;
		ID = id;
		SubRoute = sub;
		RouteNumber = rNum;
	}

	@Override
	public int describeContents() {
		return 0;
	}

	@Override
	public void writeToParcel(Parcel dest, int flags) {
		dest.writeParcelableArray(((RoutePoint[])points.toArray()), flags);
		dest.writeStringArray(((String[])BusRoute_RoutePointIDs.toArray()));
		dest.writeString(ID);
		dest.writeString(SubRoute);
		dest.writeString(RouteNumber);

	}
	
	public static final Parcelable.Creator<BusRoute> CREATOR	= new Parcelable.Creator<BusRoute>()
	{
		@Override
		public BusRoute createFromParcel(Parcel source) {
			return new BusRoute(source);
		}

		@Override
		public BusRoute[] newArray(int size) {
			return new BusRoute[size];
		}
	};
	
	private BusRoute(Parcel in)
	{
		points = new ArrayList<RoutePoint>(Arrays.asList((RoutePoint[])in.readParcelableArray(RoutePoint.class.getClassLoader())));
		String[] bsRp = null;
		in.readStringArray(bsRp);
		BusRoute_RoutePointIDs = new ArrayList<String>(Arrays.asList(bsRp));
		ID = in.readString();
		SubRoute = in.readString();
		RouteNumber = in.readString();
	}	
}
