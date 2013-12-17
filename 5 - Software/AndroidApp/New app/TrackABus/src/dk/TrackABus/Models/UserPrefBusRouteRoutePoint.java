package dk.TrackABus.Models;

import dk.TrackABus.DataProviders.UserPrefProvider;
import android.net.Uri;

/**
 * Model class for busRoute_RoutePoint table in SQLite
 */
public class UserPrefBusRouteRoutePoint {

	public static final String BusRouteRoutePointIDField = "BRRPID";
	public static final String BusRouteField = "BRRPfk_BusRoute";
	public static final String RoutePointField = "BRRPfk_RoutePoint";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BusRoute_RoutePoint");
}
