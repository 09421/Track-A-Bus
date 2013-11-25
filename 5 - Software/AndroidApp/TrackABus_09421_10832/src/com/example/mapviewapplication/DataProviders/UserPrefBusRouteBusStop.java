package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusRouteBusStop {
	public static final String BusRouteField = "BRBSfk_BusRoute";
	public static final String BusStopField = "BRBSfk_BusStop";
	public static final String  BusRouteBusStopIDField = "BRBSID";
	
	public static final String BusRouteColumn = "BRBSfk_BusRoute";
	public static final String BusStopColumn = "BRBSfk_BusStop";
	public static final String  BusRouteBusStopIDColumn = "BRBSID";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BusRoute_BusStop");
}
