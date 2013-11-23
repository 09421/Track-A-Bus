package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefRoutePoint {
	public static final String RoutePointIdField = "ID";
	public static final String RoutePointLatField = "Latitude";
	public static final String RoutePointLonField = "Longitude";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/ROUTEPOINT_TABLE");
}
