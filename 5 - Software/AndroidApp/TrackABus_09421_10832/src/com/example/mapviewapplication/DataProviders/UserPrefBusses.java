package com.example.mapviewapplication.DataProviders;

import android.net.Uri;

public class UserPrefBusses {

	public static final String UserPrefIdField = "p_id";
	public static final String UserPrefBusNumberfield = "BusNumber";
	public static final Uri CONTENT_URI = Uri.parse("content://"
			+ UserPrefProvider.AUTHORITY + "/PrefBusTable");
}
