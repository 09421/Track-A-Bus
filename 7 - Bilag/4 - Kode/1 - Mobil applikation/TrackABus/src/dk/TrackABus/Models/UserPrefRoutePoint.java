package dk.TrackABus.Models;

import dk.TrackABus.DataProviders.UserPrefProvider;
import android.net.Uri;

/**
 * Model class for RoutePoint table in SQLite
 */
public class UserPrefRoutePoint {
	public static final String RoutePointIdField = "RPID";
	public static final String RoutePointLatField = "RPLatitude";
	public static final String RoutePointLonField = "RPLongitude";
	
	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/RoutePoint");
}
