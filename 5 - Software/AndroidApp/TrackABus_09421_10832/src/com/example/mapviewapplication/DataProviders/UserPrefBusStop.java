package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusStop {

	public static final String BusStopIdField = "ID";
	public static final String BusStopNameField = "StopName";
	public static final String BusStopForeignRoutePointField = "fk_RoutePoint";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BUSSTOP_TABLE");
	

}
