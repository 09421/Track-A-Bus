package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

class UserPrefBusRouteBusStop {
	public static final String BusRouteField = "fk_BusRoute";
	public static final String BusStopField = "fk_BusStop";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BUSROUTE_BUSSTOP_TABLE");
}
