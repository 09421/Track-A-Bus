package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusRouteRoutePoint {

	public static final String BusRouteRoutePointIDField = "ID";
	public static final String BusRouteField = "fk_BusRoute";
	public static final String RoutePointField = "fk_RoutePoint";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BUSROUTE_ROUTEPOINT_TABLE");
}
