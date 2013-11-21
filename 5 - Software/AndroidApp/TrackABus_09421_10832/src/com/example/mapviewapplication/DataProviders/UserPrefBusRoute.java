package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusRoute {

	public static final String BusRouteIdField = "ID";
	public static final String BusRouteNumberField = "RouteNumber";
	public static final String BusRouteSubField = "SubRoute";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BUSROUTE_TABLE");
	

}
