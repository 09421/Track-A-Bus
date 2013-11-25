package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusStop {

	public static final String BusStopIdField = "BSID";
	public static final String BusStopNameField = "BSStopName";
	public static final String BusStopForeignRoutePointField = "BSfk_RoutePoint";
	
	public static final String BusStopIdColumn = "BSID";
	public static final String BusStopNameColumn= "BSStopName";
	public static final String BusStopForeignRoutePointColumn = "BSfk_RoutePoint";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BusStop");
	

}
