package dk.TrackABus.Models;

import dk.TrackABus.DataProviders.UserPrefProvider;
import android.net.Uri;

/**
 * Model class for BusRoute table in SQLite
 */
public class UserPrefBusRoute {

	public static final String BusRouteIdField = "BRID";
	public static final String BusRouteNumberField = "BRRouteNumber";
	public static final String BusRouteSubField = "BRSubRoute";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BusRoute");
	

}
