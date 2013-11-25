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
	private static final int BUSSTOP_NUM_CONTEXT = 6;

	private static String DATABASE_NAME = "TrackABus_UserPrefs";
	private static int DATABASE_VERSION = 1;

	static {
		uriMatcher = new UriMatcher(UriMatcher.NO_MATCH);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_TABLE, BUSROUTE_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSSTOP_TABLE, BUSSTOP_CONTEXT);
		uriMatcher.addURI(AUTHORITY, ROUTEPOINT_TABLE, ROUTEPOINT_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_ROUTEPOINT_TABLE, BUSROUTE_ROUTEPOINT_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_BUSSTOP_TABLE, BUSROUTE_BUSSTOP_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_TABLE+"/#", BUSROUTE_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSSTOP_TABLE+"/#", BUSSTOP_NUM_CONTEXT);
		uriMatcher.addURI(AUTHORITY, ROUTEPOINT_TABLE+"/#", ROUTEPOINT_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_ROUTEPOINT_TABLE+"/#", BUSROUTE_ROUTEPOINT_CONTEXT);
		uriMatcher.addURI(AUTHORITY, BUSROUTE_BUSSTOP_TABLE+"/#", BUSROUTE_BUSSTOP_CONTEXT);
		
	}

	private static class UserPrefDB extends SQLiteOpenHelper {

		public UserPrefDB(Context context) {
			super(context, DATABASE_NAME, null, DATABASE_VERSION);
		}
		

		@Override
		public void onCreate(SQLiteDatabase db) {

			String BusRouteTableCreateQuery = "CREATE TABLE " + BUSROUTE_TABLE + "(" 
					+ UserPrefBusRoute.BusRouteIdColumn + " Integer primary key,"
					+ UserPrefBusRoute.BusRouteNumberColumn + " varchar(10)," 
					+ UserPrefBusRoute.BusRouteSubColumn + " integer" 
					+ ");";
			
			String RoutePointTableCreateQuery = "CREATE TABLE " + ROUTEPOINT_TABLE + "(" 
					+ UserPrefRoutePoint.RoutePointIdColumn + " Integer primary key,"
					+ UserPrefRoutePoint.RoutePointLatColumn + " Decimal(20,15)," 
					+ UserPrefRoutePoint.RoutePointLonColumn + " Decimal(20,15)" 
					+ ");";

			String BusStopTableCreateQuery = "CREATE TABLE " + BUSSTOP_TABLE + "(" 
					+ UserPrefBusStop.BusStopIdColumn + " Integer primary key,"
					+ UserPrefBusStop.BusStopNameColumn + " varchar(100)," 
					+ UserPrefBusStop.BusStopForeignRoutePointColumn + " integer references " +
							ROUTEPOINT_TABLE + "("+ UserPrefRoutePoint.RoutePointIdColumn + ") ON DELETE CASCADE "  
					+ ");";
			
			String BusRouteRoutePointTableCreateQuery = "CREATE TABLE " + BUSROUTE_ROUTEPOINT_TABLE + "("
					+ UserPrefBusRouteRoutePoint.BusRouteRoutePointIDColumn + " integer primary key, "
					+ UserPrefBusRouteRoutePoint.BusRouteColumn + " integer references " +
							BUSROUTE_TABLE + "("+ UserPrefBusRoute.BusRouteIdColumn + ") ON DELETE CASCADE, "  
					+ UserPrefBusRouteRoutePoint.RoutePointColumn + " integer references " +
							ROUTEPOINT_TABLE + "("+UserPrefRoutePoint.RoutePointIdColumn + ") ON DELETE CASCADE "  
					+ ");";
			
			String BusRouteBusStopTableCreateQuery = "CREATE TABLE " + BUSROUTE_BUSSTOP_TABLE + "("
					+ UserPrefBusRouteBusStop.BusRouteBusStopIDColumn + " integer primary key,"
					+ UserPrefBusRouteBusStop.BusRouteColumn + " integer references " +
							BUSROUTE_TABLE + "("+ UserPrefBusRoute.BusRouteIdColumn + ") ON DELETE CASCADE, "  
					+ UserPrefBusRouteBusStop.BusStopColumn + " integer references " +
							BUSSTOP_TABLE + "("+UserPrefBusStop.BusStopIdColumn + ") ON DELETE CASCADE "  
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
		@Override
		public void onOpen(SQLiteDatabase db) {
			db.setForeignKeyConstraintsEnabled(true);
		}
	}

	private UserPrefDB dbHelper;

	@Override
	public int delete(Uri uri, String BusNumber, String[] nulled) {

		int deletedRows = 0;
		SQLiteDatabase db = dbHelper.getReadableDatabase();
		SQLiteDatabase dbWriter = dbHelper.getWritableDatabase();
		String getRouteIDQuery = String.format("Select " + UserPrefBusRoute.BusRouteIdField + " from " + BUSROUTE_TABLE 
				+ " where " + UserPrefBusRoute.BusRouteNumberField + "='" + BusNumber + "'");

		String testQuery = "Select * from " + ROUTEPOINT_TABLE;
		String getRoutePointsIDsQuery = 
				"Select " + ROUTEPOINT_TABLE+"."+UserPrefRoutePoint.RoutePointIdField + " from " + ROUTEPOINT_TABLE
			    +" join " + BUSROUTE_ROUTEPOINT_TABLE + " on "
			    + BUSROUTE_ROUTEPOINT_TABLE+"."+UserPrefBusRouteRoutePoint.RoutePointField + " = " + ROUTEPOINT_TABLE+"."+UserPrefRoutePoint.RoutePointIdField
			    +" join " + BUSROUTE_TABLE + " on "
			    + BUSROUTE_TABLE+"."+UserPrefBusRoute.BusRouteIdField + "=" + BUSROUTE_ROUTEPOINT_TABLE+"."+UserPrefBusRouteRoutePoint.BusRouteField  
				+ " where " + BUSROUTE_TABLE+"."+UserPrefBusRoute.BusRouteNumberField + "='" +BusNumber+"'";

		Cursor rIDcursor = db.rawQuery(getRouteIDQuery, null);
		Cursor pIDcursor = db.rawQuery(getRoutePointsIDsQuery, null);
		Cursor testCursor = db.rawQuery(testQuery, null);
		testCursor.moveToFirst();

		
		pIDcursor.moveToFirst();
		int pID = pIDcursor.getInt(0);
		deletedRows += dbWriter.delete(
				ROUTEPOINT_TABLE, 
				UserPrefRoutePoint.RoutePointIdField + "=" +Integer.toString(pID)
				,null);
		while(pIDcursor.moveToNext())
		{
			pID = pIDcursor.getInt(0);
			deletedRows += dbWriter.delete(
					ROUTEPOINT_TABLE, 
					UserPrefRoutePoint.RoutePointIdField + "=" +Integer.toString(pID)
					,null);
		}
		
		rIDcursor.moveToFirst();
		int rID = rIDcursor.getInt(0);
		deletedRows += dbWriter.delete(
				BUSROUTE_TABLE, 
				UserPrefBusRoute.BusRouteIdField+"="+Integer.toString(rID),
				null);
		while(rIDcursor.moveToNext())
		{
			rID = rIDcursor.getInt(0);
			deletedRows += dbWriter.delete(
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
		
		SQLiteDatabase db;
		switch(uriMatcher.match(uri))
		{	
		case BUSROUTE_CONTEXT:			
			String BusRouteID = uri.getLastPathSegment();
			db = dbHelper.getReadableDatabase();
			Cursor bCursor = db.rawQuery("select "+ UserPrefBusRoute.BusRouteNumberField + " from "
					+ BUSROUTE_TABLE + " where " + UserPrefBusRoute.BusRouteIdField + "=" + BusRouteID,null);
			if(bCursor.getCount() != 0)
			{
				bCursor.moveToFirst();
				return 0;
			}
			
			for (ContentValues cv : values) {
				db.insert(BUSROUTE_TABLE, null, cv);
			}
			db.close();
			return 1;
		case ROUTEPOINT_CONTEXT:
			db = dbHelper.getWritableDatabase();
			for (ContentValues cv : values) {
				if(cv == null){continue;}
				db.insert(ROUTEPOINT_TABLE, null, cv);
			}
			db.close();
			return 1;
		case BUSSTOP_CONTEXT:
			db = dbHelper.getWritableDatabase();
			for (ContentValues cv : values) {
				if(cv == null){continue;}
				db.insert(BUSSTOP_TABLE, null, cv);
			}
			db.close();
			
			return 1;
		case BUSROUTE_ROUTEPOINT_CONTEXT:
			db = dbHelper.getWritableDatabase();
			for (ContentValues cv : values) {

					db.insert(BUSROUTE_ROUTEPOINT_TABLE, null, cv);
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
	public Cursor query(Uri uri, String[] projection, String selection,
			String[] selectionArgs, String sortOrder) {
		String query;
		SQLiteDatabase db = dbHelper.getReadableDatabase();
		Cursor returningCursor;
		switch(uriMatcher.match(uri))
		{
		case BUSROUTE_CONTEXT:
			if(selection == null)
			{
				query = "Select distinct " + UserPrefBusRoute.BusRouteNumberField + " from " + BUSROUTE_TABLE;
			}
			else
			{
				query = "Select " + UserPrefBusRoute.BusRouteIdField+","+UserPrefBusRoute.BusRouteSubField 
						+ " from " + BUSROUTE_TABLE + " where " + UserPrefBusRoute.BusRouteNumberField + "='" + selection+"'" ;
			}
			returningCursor = db.rawQuery(query,null);
			break;	
		   	
		case ROUTEPOINT_CONTEXT:
			query = "Select " + UserPrefRoutePoint.RoutePointLatField+","+UserPrefRoutePoint.RoutePointLonField
			+ " from " + ROUTEPOINT_TABLE
			+ " inner join " + BUSROUTE_ROUTEPOINT_TABLE + " on "
			+ UserPrefBusRouteRoutePoint.RoutePointField  + " = " + UserPrefRoutePoint.RoutePointIdField
			+ " where " + UserPrefBusRouteRoutePoint.BusRouteField + " = " + selection;
			returningCursor = db.rawQuery(query, null);
			break;
		case BUSSTOP_CONTEXT:
			query = "SELECT " +BUSSTOP_TABLE + "." + UserPrefBusStop.BusStopNameField+"," + ROUTEPOINT_TABLE + "." +UserPrefRoutePoint.RoutePointLatField
					+","+ROUTEPOINT_TABLE+"."+UserPrefRoutePoint.RoutePointLonField +" from " + BUSSTOP_TABLE 
					+ " inner join " + ROUTEPOINT_TABLE + " on " 
					+ BUSSTOP_TABLE + "."+UserPrefBusStop.BusStopForeignRoutePointField + "=" + ROUTEPOINT_TABLE+"."+UserPrefRoutePoint.RoutePointIdField
					+ " inner join " + BUSROUTE_BUSSTOP_TABLE + " on " 
					+ BUSROUTE_BUSSTOP_TABLE + "." + UserPrefBusRouteBusStop.BusStopField + "=" + BUSSTOP_TABLE+ "." + UserPrefBusStop.BusStopIdField
					+ " where " + BUSROUTE_BUSSTOP_TABLE+"."+UserPrefBusRouteBusStop.BusRouteField+ "=" + selection;
			returningCursor = db.rawQuery(query, null);
			break;
		case BUSSTOP_NUM_CONTEXT:
			query = "Select " + BUSSTOP_TABLE +"." +UserPrefBusStop.BusStopNameField+","+ROUTEPOINT_TABLE+"."+UserPrefRoutePoint.RoutePointLatField
			+ ", " +ROUTEPOINT_TABLE+"."+UserPrefRoutePoint.RoutePointLonField +  " from " + BUSSTOP_TABLE
			+ " join " + ROUTEPOINT_TABLE + " on "
			+ ROUTEPOINT_TABLE+"."+UserPrefRoutePoint.RoutePointIdField  + " = " + BUSSTOP_TABLE+"."+UserPrefBusStop.BusStopForeignRoutePointField
			+ " where " + BUSSTOP_TABLE+"."+UserPrefBusStop.BusStopIdField + " = " + uri.getLastPathSegment();
			returningCursor =  db.rawQuery(query,null);		
			break;
		default:
			return null;
		}
		return returningCursor;
	}

	@Override
	public int update(Uri uri, ContentValues values, String selection,
			String[] selectionArgs) {
		// TODO Auto-generated method stub
		return 0;
	}

}
