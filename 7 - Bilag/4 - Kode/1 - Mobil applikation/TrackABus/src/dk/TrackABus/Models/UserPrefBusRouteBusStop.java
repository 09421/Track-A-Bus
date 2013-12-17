package dk.TrackABus.Models;

import dk.TrackABus.DataProviders.UserPrefProvider;
import android.net.Uri;

/**
 * Model class for BusRoute_BusStop table in SQLite
 */
public class UserPrefBusRouteBusStop {
	public static final String BusRouteField = "BRBSfk_BusRoute";
	public static final String BusStopField = "BRBSfk_BusStop";
	public static final String  BusRouteBusStopIDField = "BRBSID";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BusRoute_BusStop");
}
