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
	public static String BUS_ROUTE_TABLE = "PrefBusRouteTable";
	public static String BUS_NUMBER_TABLE = "PrefBusTable";
	public static String BUS_SPECIFIC_NUMBER_ID = "BUS_ID";

	private static final int TABLE1 = 1;
	private static final int TABLE2 = 2;
	private static final int TABLE3 = 3;

	private static String DATABASE_NAME = "UserPrefs";
	private static int DATABASE_VERSION = 1;

	static {
		uriMatcher = new UriMatcher(UriMatcher.NO_MATCH);
		uriMatcher.addURI(AUTHORITY, BUS_NUMBER_TABLE, TABLE1);
		uriMatcher.addURI(AUTHORITY, BUS_ROUTE_TABLE, TABLE2);
	}

	private static class UserPrefDB extends SQLiteOpenHelper {

		public UserPrefDB(Context context) {
			super(context, DATABASE_NAME, null, DATABASE_VERSION);
		}

		@Override
		public void onCreate(SQLiteDatabase db) {

			String busNumberTableCreateQuery = "CREATE TABLE "
					+ BUS_NUMBER_TABLE + "(" + UserPrefBusses.UserPrefIdField
					+ " Integer primary key AUTOINCREMENT,"
					+ UserPrefBusses.UserPrefBusNumberfield + " varchar(4)"
					+ ");";

			String busRouteTableCreateQuery = "CREATE TABLE " + BUS_ROUTE_TABLE
					+ " (" + UserPrefBusRoute.BusRouteIdField
					+ " integer primary key AUTOINCREMENT,"
					+ UserPrefBusRoute.BusRouteFBusIdField + " integer,"
					+ UserPrefBusRoute.BusRouteLatField + " float,"
					+ UserPrefBusRoute.BusRouteLonField + " float,"
					+ " foreign key (" + UserPrefBusRoute.BusRouteFBusIdField
					+ ") references " + BUS_NUMBER_TABLE + "("
					+ UserPrefBusses.UserPrefIdField + "));";

			db.execSQL(busNumberTableCreateQuery);
			db.execSQL(busRouteTableCreateQuery);
		}

		@Override
		public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {
			// TODO Auto-generated method stub

		}
	}

	private UserPrefDB dbHelper;

	@Override
	public int delete(Uri uri, String BusNumber, String[] nulled) {


		SQLiteDatabase db = dbHelper.getReadableDatabase();
		String getBusIDQuery = "Select " + UserPrefBusses.UserPrefIdField
				+ " from " + BUS_NUMBER_TABLE + " Where "
				+ UserPrefBusses.UserPrefBusNumberfield + " = '" + BusNumber
				+ "'";
		Cursor c = db.rawQuery(getBusIDQuery, null);

		if (c.getCount() > 0) {
			c.moveToFirst();
			int busID = c.getInt(0);
			int deletedRows = 0;
			deletedRows = db.delete(
					BUS_ROUTE_TABLE,
					UserPrefBusRoute.BusRouteFBusIdField + "="
							+ String.valueOf(busID), null);
			deletedRows += db.delete(BUS_NUMBER_TABLE,
					UserPrefBusses.UserPrefBusNumberfield + "='" + BusNumber
							+ "'", null);
			return deletedRows;
		}
		return -1;
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
