package dk.TrackABus.Models;

import dk.TrackABus.DataProviders.UserPrefProvider;
import android.net.Uri;

public class UserPrefBusStop {

	public static final String BusStopIdField = "BSID";
	public static final String BusStopNameField = "BSStopName";
	public static final String BusStopForeignRoutePointField = "BSfk_RoutePoint";

	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/BusStop");
	

}
