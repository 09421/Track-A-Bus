package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusRoute {

	public static final String BusRouteIdField = "p_id";
	public static final String BusRouteFBusIdField = "f_BusID";
	public static final String BusRouteLatField = "RouteLat";
	public static final String BusRouteLonField = "RouteLon";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/PrefBusRouteTable");
	

}
