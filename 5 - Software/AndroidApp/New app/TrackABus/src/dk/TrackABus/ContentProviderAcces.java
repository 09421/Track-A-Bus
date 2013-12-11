package dk.TrackABus;

import java.util.ArrayList;

import com.google.android.gms.maps.model.LatLng;

import dk.TrackABus.Models.BusRoute;
import dk.TrackABus.Models.BusStop;
import dk.TrackABus.Models.ListBusData;
import dk.TrackABus.Models.RoutePoint;
import dk.TrackABus.Models.UserPrefBusRoute;
import dk.TrackABus.Models.UserPrefBusRouteBusStop;
import dk.TrackABus.Models.UserPrefBusRouteRoutePoint;
import dk.TrackABus.Models.UserPrefBusStop;
import dk.TrackABus.Models.UserPrefRoutePoint;
import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.net.Uri;
import android.util.Log;

public class ContentProviderAcces
{
	ArrayList<String> AllBusses = null;

	public static ArrayList<ListBusData> ListBusDataCreator(Context context, ArrayList<String> busList)
	{
		Cursor c = context.getContentResolver().query(UserPrefBusRoute.CONTENT_URI, null,null, null, null);
		ArrayList<String> FavoriteBusses = new ArrayList<String>();
		ArrayList<ListBusData> AllBusses = new ArrayList<ListBusData>();;
		c.moveToFirst();
		for(int i = 0; i < c.getCount(); i++)
		{
			FavoriteBusses.add(c.getString(0));
			c.moveToNext();
		}	    	


    	for(int i = 0; i < busList.size(); i++)
    	{
    		AllBusses.add(i, new ListBusData(FavoriteBusses.contains(busList.get(i)), busList.get(i)));
    	}
    	return AllBusses;
	}
	
	public static ArrayList<String> GetBusRoutes(Context context, String selectedBusOpt)
	{
		Cursor c = context.getContentResolver().query(UserPrefBusRoute.CONTENT_URI, null,selectedBusOpt, null, null);
		ArrayList<String> values = new ArrayList<String>();		

		c.moveToFirst();
		for(int i = 0; i < c.getCount(); i++)
		{
			values.add(c.getString(0));
			c.moveToNext();
		}
    	return values;
	}
	

	
	public static void SetNewFavorite(Context context, final ArrayList<BusRoute> bRoute,final ArrayList<BusStop> sRoute)
	{
		ContentValues[] BusRouteCV;
		ContentValues[] RoutePointCV;
		ContentValues[] BusRouteRoutePointCV;
		try
		{
			for(int i = 0; i < bRoute.size(); i++)
			{
				BusRouteCV = new ContentValues[1];
				BusRouteCV[0] = new ContentValues();
				BusRouteCV[0].put(UserPrefBusRoute.BusRouteIdField, bRoute.get(i).ID);
				BusRouteCV[0].put(UserPrefBusRoute.BusRouteNumberField, bRoute.get(i).RouteNumber);
				BusRouteCV[0].put(UserPrefBusRoute.BusRouteSubField, bRoute.get(i).SubRoute);
				RoutePointCV = new ContentValues[bRoute.get(i).points.size()];
				BusRouteRoutePointCV = new ContentValues[bRoute.get(i).BusRoute_RoutePointIDs.size()];
				for(int j = 0; j < bRoute.get(i).points.size(); j++)
				{
					RoutePointCV[j] = new ContentValues();
					BusRouteRoutePointCV[j] = new ContentValues();
					RoutePointCV[j].put(UserPrefRoutePoint.RoutePointIdField, bRoute.get(i).points.get(j).ID);
					RoutePointCV[j].put(UserPrefRoutePoint.RoutePointLatField, bRoute.get(i).points.get(j).Position.latitude);
					RoutePointCV[j].put(UserPrefRoutePoint.RoutePointLonField, bRoute.get(i).points.get(j).Position.longitude);

					BusRouteRoutePointCV[j].put(UserPrefBusRouteRoutePoint.BusRouteRoutePointIDField,
												bRoute.get(i).BusRoute_RoutePointIDs.get(j));
					BusRouteRoutePointCV[j].put(UserPrefBusRouteRoutePoint.BusRouteField, bRoute.get(i).ID);
					BusRouteRoutePointCV[j].put(UserPrefBusRouteRoutePoint.RoutePointField, bRoute.get(i).points.get(j).ID);
												
				}
				int checkVal = context.getContentResolver().bulkInsert(Uri.parse(UserPrefBusRoute.CONTENT_URI.toString()+"/"+bRoute.get(i).ID), BusRouteCV);
				if(checkVal == 0)
				{
					return;
				}
				context.getContentResolver().bulkInsert(UserPrefRoutePoint.CONTENT_URI, RoutePointCV);
				context.getContentResolver().bulkInsert(UserPrefBusRouteRoutePoint.CONTENT_URI, BusRouteRoutePointCV);
			}
	  		
			ContentValues[] BusStopPointsCV = new ContentValues[sRoute.size()];
			ContentValues[] BusStopCV = new ContentValues[sRoute.size()];
			ContentValues[] BusRouteBusStopCV = new ContentValues[sRoute.size()];
			for(int i = 0; i < sRoute.size(); i++)
			{
				BusRouteBusStopCV[i] = new ContentValues();
				if(context.getContentResolver().query(Uri.parse(UserPrefBusStop.CONTENT_URI.toString() + "/"+sRoute.get(i).ID), null, null, null, null).getCount() == 0)
				{
					BusStopPointsCV[i] = new ContentValues();
					BusStopCV[i] = new ContentValues();
					BusStopPointsCV[i].put(UserPrefRoutePoint.RoutePointIdField, sRoute.get(i).Position.ID);
					BusStopPointsCV[i].put(UserPrefRoutePoint.RoutePointLatField, sRoute.get(i).Position.Position.latitude);
					BusStopPointsCV[i].put(UserPrefRoutePoint.RoutePointLonField, sRoute.get(i).Position.Position.longitude);
					BusStopCV[i].put(UserPrefBusStop.BusStopIdField, sRoute.get(i).ID);
					BusStopCV[i].put(UserPrefBusStop.BusStopNameField, sRoute.get(i).Name);
					BusStopCV[i].put(UserPrefBusStop.BusStopForeignRoutePointField,sRoute.get(i).Position.ID);
				}
					BusRouteBusStopCV[i].put(UserPrefBusRouteBusStop.BusRouteBusStopIDField, sRoute.get(i).BusRoute_BusStopID);
					BusRouteBusStopCV[i].put(UserPrefBusRouteBusStop.BusRouteField, sRoute.get(i).RouteID);
					BusRouteBusStopCV[i].put(UserPrefBusRouteBusStop.BusStopField, sRoute.get(i).ID);
			} 
			
			context.getContentResolver().bulkInsert(UserPrefRoutePoint.CONTENT_URI, BusStopPointsCV);
			context.getContentResolver().bulkInsert(UserPrefBusStop.CONTENT_URI, BusStopCV);
			context.getContentResolver().bulkInsert(UserPrefBusRouteBusStop.CONTENT_URI, BusRouteBusStopCV);
		}
		
		catch(Exception e)
		{
			String err = (e.getMessage()==null)?"Save failed":e.getMessage();
			Log.e("DEBUG","FAVORITE ERR: " + err);
		}
	}
	
	public static void DeleteBusRoute(Context context,String bNumber)
	{
		context.getContentResolver().delete(UserPrefBusRoute.CONTENT_URI, bNumber, null);
	}
	
	public static ArrayList<float[]> GetBusRoutePoints(Context context, String currRoute)
	{
		Cursor Route = context.getContentResolver().query(UserPrefRoutePoint.CONTENT_URI, null, currRoute, null, null);
		float[] LatRoute = new float[Route.getCount()];
		float[] LngRoute = new float[Route.getCount()];
		Route.moveToFirst();
		
		ArrayList<float[]> points = new ArrayList<float[]>();

		for(int j = 0; j < Route.getCount(); j++)
		{
			LatRoute[j] = Route.getFloat(0);
			LngRoute[j] = Route.getFloat(1);
			Route.moveToNext();
		}
		points.add(LatRoute);
		points.add(LngRoute);
		return points;
	}
	
	public static ArrayList<BusStop> GetBusStops(Context context, String currRoute)
	{
		Cursor BusStops = context.getContentResolver().query(UserPrefBusStop.CONTENT_URI, null, currRoute, null, null);
		BusStops.moveToFirst();
		ArrayList<BusStop> stops = new ArrayList<BusStop>();	

		for(int k = 0; k < BusStops.getCount(); k++)
		{
			stops.add(new BusStop(new RoutePoint(new LatLng(BusStops.getFloat(1),BusStops.getFloat(2)),null)
			, BusStops.getString(0), null, null, null));
			BusStops.moveToNext();
		}
		return stops;
	}
	
}


