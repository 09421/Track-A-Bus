package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefRoutePoint {
	public static final String RoutePointIdField = "RPID";
	public static final String RoutePointLatField = "RPLatitude";
	public static final String RoutePointLonField = "RPLongitude";

	public static final String RoutePointIdColumn = "RPID";
	public static final String RoutePointLatColumn = "RPLatitude";
	public static final String RoutePointLonColumn = "RPLongitude";
	
	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/RoutePoint");
}
