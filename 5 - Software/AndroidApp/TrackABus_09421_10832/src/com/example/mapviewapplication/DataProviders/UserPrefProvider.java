package com.example.mapviewapplication.DataProviders;

import android.content.ContentProvider;
import android.content.ContentUris;
import android.content.ContentValues;
import android.content.Context;
import android.content.UriMatcher;
import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;
import android.net.Uri;
import android.util.Log;

public class UserPrefProvider extends ContentProvider {

	public static final String AUTHORITY = "com.example.mapviewapplication.DataProviders.UserPrefProvider";
	private static final UriMatcher uriMatcher;
	public static String BUSROUTE_TABLE = "BusRoute";
	public static String BUSSTOP_TABLE = "BusStop";
	public static String ROUTEPOINT_TABLE = "RoutePoint";
	public static String BUSROUTE_ROUTEPOINT_TABLE = "BusRoute_RoutePoint";
	public static String BUSROUTE_BUSSTOP_TABLE = "BusRoute_BusStop";

	private static final int BUSROUTE_CONTEXT = 1;
	private static final int BUSSTOP_CONTEXT = 2;
	private static final int ROUTEPOINT_CONTEXT = 3;
	private static final int BUSROUTE_ROUTEPOINT_CONTEXT = 4;
	private static final int BUSROUTE_BUSSTOP_CONTEXT = 5;

	private static String DATABASE_NAME = "TrackABus_UserPrefs";
	private static int DATABASE_VERSION = 1;

	static {
		uriMatcher = new UriMatcher(UriMatcher.NO_MATCH);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_TABLE, BUSROUTE_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSSTOP_TABLE, BUSSTOP_CONTEXT);
		uriMatcher.addURI(AUTHORITY, ROUTEPOINT_TABLE, ROUTEPOINT_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_ROUTEPOINT_TABLE, BUSROUTE_ROUTEPOINT_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_BUSSTOP_TABLE, BUSROUTE_BUSSTOP_CONTEXT);
		
	}

	private static class UserPrefDB extends SQLiteOpenHelper {

		public UserPrefDB(Context context) {
			super(context, DATABASE_NAME, null, DATABASE_VERSION);
		}

		@Override
		public void onCreate(SQLiteDatabase db) {

			String BusRouteTableCreateQuery = "CREATE TABLE " + BUSROUTE_TABLE + "(" 
					+ UserPrefBusRoute.BusRouteIdField + " Integer primary key,"
					+ UserPrefBusRoute.BusRouteNumberField + " varchar(10)," 
					+ UserPrefBusRoute.BusRouteSubField + " integer" 
					+ ");";
			
			String RoutePointTableCreateQuery = "CREATE TABLE " + ROUTEPOINT_TABLE + "(" 
					+ UserPrefRoutePoint.RoutePointIdField + " Integer primary key,"
					+ UserPrefRoutePoint.RoutePointLatField + " Decimal(20,15)," 
					+ UserPrefRoutePoint.RoutePointLonField + " Decimal(20,15)" 
					+ ");";

			String BusStopTableCreateQuery = "CREATE TABLE " + BUSSTOP_TABLE + "(" 
					+ UserPrefBusStop.BusStopIdField + " Integer primary key,"
					+ UserPrefBusStop.BusStopNameField + " varchar(100)," 
					+ UserPrefBusStop.BusStopForeignRoutePointField + " integer references " +
							ROUTEPOINT_TABLE + "("+ UserPrefRoutePoint.RoutePointIdField + ") ON DELETE CASCADE "  
					+ ");";
			
			String BusRouteRoutePointTableCreateQuery = "CREATE TABLE " + BUSROUTE_ROUTEPOINT_TABLE + "("
					+ UserPrefBusRouteRoutePoint.BusRouteRoutePointIDField + " integer primary key,"
					+ UserPrefBusRouteRoutePoint.BusRouteField + " integer references " +
							BUSROUTE_TABLE + "("+ UserPrefBusRoute.BusRouteIdField + ") ON DELETE CASCADE "  
					+ UserPrefBusRouteRoutePoint.RoutePointField + " integer references " +
							ROUTEPOINT_TABLE + "("+UserPrefRoutePoint.RoutePointIdField + ") ON DELETE CASCADE "  
					+ ");";
			
			String BusRouteBusStopTableCreateQuery = "CREATE TABLE " + BUSROUTE_BUSSTOP_TABLE + "("
					+ UserPrefBusRouteBusStop.BusRouteField + " integer references " +
							BUSROUTE_TABLE + "("+ UserPrefBusRoute.BusRouteIdField + ") ON DELETE CASCADE "  
					+ UserPrefBusRouteBusStop.BusStopField + " integer references " +
							BUSSTOP_TABLE + "("+UserPrefBusStop.BusStopIdField + ") ON DELETE CASCADE "  
					+ ");";
					

			db.execSQL(BusRouteTableCreateQuery);
			db.execSQL(RoutePointTableCreateQuery);
			db.execSQL(BusStopTableCreateQuery);
			db.execSQL(BusRouteRoutePointTableCreateQuery);
			db.execSQL(BusRouteBusStopTableCreateQuery);
		}

		@Override
		public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {
			// TODO Auto-generated method stub

		}
	}

	private UserPrefDB dbHelper;

	@Override
	public int delete(Uri uri, String BusRouteSubRoute, String[] nulled) {

		int deletedRows = 0;
		SQLiteDatabase db = dbHelper.getReadableDatabase();
		
		String[] BusRoute_SubRoute = BusRouteSubRoute.split(";");
		String BusRoute = BusRoute_SubRoute[0];
		String SubRoute = BusRoute_SubRoute[1];
		
		String getRouteIDQuery = "Select " + UserPrefBusRoute.BusRouteIdField + " from " + BUSROUTE_TABLE
								+ " where " + UserPrefBusRoute.BusRouteNumberField + " = '" + BusRoute
								+ " ' and " + UserPrefBusRoute.BusRouteSubField + " = " + SubRoute;
		Cursor rIDcursor = db.rawQuery(getRouteIDQuery, null);
		if(rIDcursor.getCount() == 0 ){return -1;}
		rIDcursor.moveToFirst();
		int rID = rIDcursor.getInt(0); 
		String getRoutePointsIDsQuery = 
				"Select " + UserPrefRoutePoint.RoutePointIdField + " from " + ROUTEPOINT_TABLE
			    +" inner join " + BUSROUTE_ROUTEPOINT_CONTEXT + "on "
			    + BUSROUTE_ROUTEPOINT_CONTEXT+"."+UserPrefBusRouteRoutePoint.RoutePointField + " = "
			    + ROUTEPOINT_TABLE+"."+UserPrefRoutePoint.RoutePointIdField
				+ "where " + BUSROUTE_ROUTEPOINT_CONTEXT+"."+UserPrefBusRouteRoutePoint.BusRouteField
				+ " = " + Integer.toString(rID);
		Cursor pIDcursor = db.rawQuery(getRoutePointsIDsQuery, null);
		if(pIDcursor.getCount() == 0){return -1;}
		
		deletedRows += db.delete(
				BUSROUTE_TABLE, 
				UserPrefBusRoute.BusRouteIdField+"="+Integer.toString(rID),
				null);
		pIDcursor.moveToFirst();
		deletedRows += db.delete(
				ROUTEPOINT_TABLE, 
				UserPrefRoutePoint.RoutePointIdField + "=" +Integer.toString(pIDcursor.getInt(0))
				,null);
		while(pIDcursor.moveToNext())
		{
			deletedRows += db.delete(
					ROUTEPOINT_TABLE, 
					UserPrefRoutePoint.RoutePointIdField + "=" +Integer.toString(pIDcursor.getInt(0))
					,null);
		}
		return deletedRows;
	}

	@Override
	public String getType(Uri uri) {
		return null;
	}

	@Override
	public int bulkInsert(Uri uri, ContentValues[] values) {

		String busName = uri.getLastPathSegment();
		Log.e("MyLog", "BulkLastBus: " + busName);
		Log.e("MyLog", String.valueOf(values.length));
		SQLiteDatabase db = dbHelper.getReadableDatabase();
		Cursor existsBus = db.rawQuery("SELECT "
				+ UserPrefBusses.UserPrefIdField + " FROM " + BUS_NUMBER_TABLE
				+ " WHERE " + UserPrefBusses.UserPrefBusNumberfield + " = '"
				+ busName + "'", null);
		existsBus.moveToFirst();

		if (existsBus.getCount() > 0) {
			int idNum = existsBus.getInt(0);
			db.close();
			db = dbHelper.getWritableDatabase();
			int num = 0;

			for (ContentValues cv : values) {
				cv.put(UserPrefBusRoute.BusRouteFBusIdField,
						String.valueOf(idNum));
				db.insert(BUS_ROUTE_TABLE, null, cv);
				num++;
			}
			db.close();
			return num;
		} else {
			return 0;
		}
	}

	@Override
	public Uri insert(Uri uri, ContentValues values) {
		Cursor existsBus;

		switch (uriMatcher.match(uri)) {
		case TABLE1:
			SQLiteDatabase db = dbHelper.getReadableDatabase();
			existsBus = db
					.rawQuery(
							"SELECT "
									+ UserPrefBusses.UserPrefIdField
									+ " FROM "
									+ BUS_NUMBER_TABLE
									+ " WHERE "
									+ UserPrefBusses.UserPrefBusNumberfield
									+ " = '"
									+ values.getAsString(UserPrefBusses.UserPrefBusNumberfield)
									+ "'", null);
			if (existsBus.getCount() == 0) {
				db.close();
				db = dbHelper.getWritableDatabase();
				long id = db.insert(BUS_NUMBER_TABLE, null, values);
				db.close();
				if (id > 0) {
					Uri newUr = ContentUris.withAppendedId(
							UserPrefBusses.CONTENT_URI, id);
					return newUr;
				} else
					return null;
			} else
				return null;
		default:
			return null;
		}
	}

	@Override
	public boolean onCreate() {
		dbHelper = new UserPrefDB(getContext());
		return true;
	}

	@Override
	public Cursor query(Uri uri, String[] projection, String selection,
			String[] selectionArgs, String sortOrder) {
		SQLiteDatabase db;
		String query;
		Cursor c;
		switch (uriMatcher.match(uri)) {
		case TABLE1:
			db = dbHelper.getReadableDatabase();
			query = "SELECT " + UserPrefBusses.UserPrefBusNumberfield
					+ " FROM " + BUS_NUMBER_TABLE;
			c = db.rawQuery(query, null);
			return c;

		case TABLE2:
			db = dbHelper.getReadableDatabase();
			query = "SELECT " + UserPrefBusses.UserPrefIdField + " From "
					+ BUS_NUMBER_TABLE + " WHERE "
					+ UserPrefBusses.UserPrefBusNumberfield + " = " + "'"
					+ selection + "'";

			c = db.rawQuery(query, null);
			if (c.getCount() > 0) {
				c.moveToFirst();
				int busId = c.getInt(0);
				query = "SELECT " + UserPrefBusRoute.BusRouteLatField + ", "
						+ UserPrefBusRoute.BusRouteLonField + " From "
						+ BUS_ROUTE_TABLE + " WHERE "
						+ UserPrefBusRoute.BusRouteFBusIdField + " = "
						+ String.valueOf(busId);	
				c = db.rawQuery(query, null);
				return c;
			} else
				return null;

		default:
			return null;
		}

	}

	@Override
	public int update(Uri uri, ContentValues values, String selection,
			String[] selectionArgs) {
		// TODO Auto-generated method stub
		return 0;
	}

}
