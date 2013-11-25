package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusRoute {

	public static final String BusRouteIdField = "BRID";
	public static final String BusRouteNumberField = "BRRouteNumber";
	public static final String BusRouteSubField = "BRSubRoute";
	
	public static final String BusRouteIdColumn = "BRID";
	public static final String BusRouteNumberColumn = "BRRouteNumber";
	public static final String BusRouteSubColumn = "BRSubRoute";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BusRoute");
	

}
