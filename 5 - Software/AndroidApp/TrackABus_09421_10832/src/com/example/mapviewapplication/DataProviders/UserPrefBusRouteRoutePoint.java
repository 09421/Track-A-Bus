package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusRouteRoutePoint {

	public static final String BusRouteRoutePointIDField = "BRRPID";
	public static final String BusRouteField = "BRRPfk_BusRoute";
	public static final String RoutePointField = "BRRPfk_RoutePoint";
	
	public static final String BusRouteRoutePointIDColumn= "BRRPID";
	public static final String BusRouteColumn = "BRRPfk_BusRoute";
	public static final String RoutePointColumn = "BRRPfk_RoutePoint";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BusRoute_RoutePoint");
}
