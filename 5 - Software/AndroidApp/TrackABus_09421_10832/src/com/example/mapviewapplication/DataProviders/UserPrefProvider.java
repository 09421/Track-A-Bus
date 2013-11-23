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
	public int delete(Uri uri, String BusNumber, String[] nulled) {

		int deletedRows = 0;
		SQLiteDatabase db = dbHelper.getReadableDatabase();
		
		
		String getRouteIDQuery = String.format("Select {0} from {1} where {2} = {3}",
				UserPrefBusRoute.BusRouteIdField, BUSROUTE_TABLE,
				UserPrefBusRoute.BusRouteNumberField,BusNumber);

		String getRoutePointsIDsQuery = 
				"Select " + UserPrefRoutePoint.RoutePointIdField + " from " + ROUTEPOINT_TABLE
			    +" inner join " + BUSROUTE_ROUTEPOINT_CONTEXT + " on "
			    + UserPrefBusRouteRoutePoint.RoutePointField + " = " + UserPrefRoutePoint.RoutePointIdField
			    +" inner join " + BUSROUTE_CONTEXT + " on "
			    + UserPrefBusRoute.BusRouteIdField + "=" + UserPrefBusRouteRoutePoint.BusRouteField  
				+ " where " + UserPrefBusRoute.BusRouteNumberField + "=" + BusNumber;

		Cursor rIDcursor = db.rawQuery(getRouteIDQuery, null);
		Cursor pIDcursor = db.rawQuery(getRoutePointsIDsQuery, null);
		if(rIDcursor.getCount() == 0 || pIDcursor.getCount() == 0){return -1;}
		
		pIDcursor.moveToFirst();
		int pID = pIDcursor.getInt(0);
		
		deletedRows += db.delete(
				ROUTEPOINT_TABLE, 
				UserPrefRoutePoint.RoutePointIdField + "=" +Integer.toString(pID)
				,null);
		while(pIDcursor.moveToNext())
		{
			pID = pIDcursor.getInt(0);
			deletedRows += db.delete(
					ROUTEPOINT_TABLE, 
					UserPrefRoutePoint.RoutePointIdField + "=" +Integer.toString(pID)
					,null);
		}
		
		rIDcursor.moveToFirst();
		int rID = rIDcursor.getInt(0);
		deletedRows += db.delete(
				BUSROUTE_TABLE, 
				UserPrefBusRoute.BusRouteIdField+"="+Integer.toString(rID),
				null);
		while(rIDcursor.moveToNext())
		{
			rID = rIDcursor.getInt(0);
			deletedRows += db.delete(
					BUSROUTE_TABLE, 
					UserPrefBusRoute.BusRouteIdField+"="+Integer.toString(rID),
					null);
		}
		return deletedRows;
	}

	@Override
	public String getType(Uri uri) {
		return null;
	}

	@Override
	public int bulkInsert(Uri uri, ContentValues[] values) {
		
		String BusRoute = uri.getLastPathSegment();
		SQLiteDatabase db = dbHelper.getReadableDatabase();
		Cursor bCursor = db.rawQuery("select "+ UserPrefBusRoute.BusRouteIdField + " from "
				+ BUSROUTE_TABLE + " where " + UserPrefBusRoute.BusRouteNumberField + "=" + BusRoute,null);
		if(bCursor.getCount() == 0)
		{
			return -1;
		}
		db.close();
		db = dbHelper.getWritableDatabase();
		switch(uriMatcher.match(uri))
		{	
		case BUSROUTE_CONTEXT:
			for (ContentValues cv : values) {
				db.insert(BUSROUTE_TABLE, null, cv);
			}
			db.close();
			return 1;
		case ROUTEPOINT_CONTEXT:
			db = dbHelper.getWritableDatabase();
			for (ContentValues cv : values) {
				db.insert(ROUTEPOINT_TABLE, null, cv);
			}
			db.close();
			return 1;
		case BUSSTOP_CONTEXT:
			db = dbHelper.getWritableDatabase();
			for (ContentValues cv : values) {
				db.insert(BUSSTOP_TABLE, null, cv);
			}
			db.close();
			return 1;
		case BUSROUTE_ROUTEPOINT_CONTEXT:

			for (ContentValues cv : values) {
				db = dbHelper.getReadableDatabase();
				String stopName = cv.getAsString(UserPrefBusRoute.BusRouteNumberField);
				Cursor duplicateStop = db.rawQuery(
						"Select " + UserPrefBusStop.BusStopNameField + " from " + BUSSTOP_TABLE
						+ " where " + UserPrefBusRoute.BusRouteNumberField + "=" + stopName
						, null);
				db.close();
				if(duplicateStop.getCount() == 0)
				{
					db = dbHelper.getWritableDatabase();
					db.insert(BUSROUTE_ROUTEPOINT_TABLE, null, cv);
					db.close();
				}
				
			}
			db.close();
			return 1;
		case BUSROUTE_BUSSTOP_CONTEXT:
			db = dbHelper.getWritableDatabase();
			for (ContentValues cv : values) {
				db.insert(BUSROUTE_BUSSTOP_TABLE, null, cv);
			}
			db.close();
			return 1;
		default:
			return -1;
		}
	}

	@Override
	public Uri insert(Uri uri, ContentValues values) {
		// ONLY USES BulkInsert;
		return null;
	}

	@Override
	public boolean onCreate() {
		dbHelper = new UserPrefDB(getContext());
		return true;
	}

	@Override
	public Cursor query(Uri uri, String[] projection, String BusNumberOrID,
			String[] selectionArgs, String sortOrder) {
		String query;
		SQLiteDatabase db = dbHelper.getReadableDatabase();
		switch(uriMatcher.match(uri)){
			
		case BUSROUTE_CONTEXT:
			query = "Select " + UserPrefBusRoute.BusRouteIdField + " from " + BUSROUTE_TABLE
			+ " where " + UserPrefBusRoute.BusRouteNumberField + "=" + BusNumberOrID;
		    return db.rawQuery(query,null);
			
		case ROUTEPOINT_CONTEXT:
			query = "Select " + UserPrefRoutePoint.RoutePointLatField+","+UserPrefRoutePoint.RoutePointLonField
			+ " from " + BUSROUTE_TABLE
			+ " inner join " + BUSROUTE_ROUTEPOINT_TABLE + " on "
			+ UserPrefBusRouteRoutePoint.RoutePointField  + " = " + UserPrefRoutePoint.RoutePointIdField
			+ " where " + UserPrefBusRouteRoutePoint.BusRouteField + " = " + BusNumberOrID;
		    return db.rawQuery(query,null);		
		case BUSSTOP_CONTEXT:
			query = "Select " + UserPrefBusStop.BusStopNameField+","+UserPrefRoutePoint.RoutePointLatField
			+ ", " +UserPrefRoutePoint.RoutePointLonField +  " from " + BUSSTOP_TABLE
			+ " inner join " + ROUTEPOINT_TABLE + " on "
			+ UserPrefRoutePoint.RoutePointIdField  + " = " + UserPrefBusStop.BusStopForeignRoutePointField
			+ " inner join " + BUSROUTE_BUSSTOP_TABLE + " on "
			+ UserPrefBusRouteBusStop.BusStopField + " = " + UserPrefBusStop.BusStopIdField
			+ " where " + UserPrefBusRouteBusStop.BusRouteField + " = " + BusNumberOrID;
			return db.rawQuery(query,null);		
		
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
