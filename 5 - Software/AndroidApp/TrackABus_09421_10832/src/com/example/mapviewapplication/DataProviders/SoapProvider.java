package com.example.mapviewapplication.DataProviders;

import java.util.ArrayList;

import org.ksoap2.SoapEnvelope;
import org.ksoap2.serialization.SoapObject;
import org.ksoap2.serialization.SoapPrimitive;
import org.ksoap2.serialization.SoapSerializationEnvelope;
import org.ksoap2.transport.HttpTransportSE;

import com.example.mapviewapplication.TrackABus.BusStop;
import com.google.android.gms.maps.model.LatLng;

import android.util.Log;

public class SoapProvider {
	private SoapObject response;
	private String res;
	private ArrayList<String> resultlist;
	final String NAMESPACE = "http://TrackABus.dk/Webservice/"; //the namespace that you'll find in the header of your asmx webservice
	String METHOD_NAME= "HelloWorld"; //the webservice method that you want to call
	String SOAP_ACTION = NAMESPACE+METHOD_NAME;
	final String URL = "http://trackabus.dk/AndroidToMySQLWebService.asmx"; //the url of your webservice
	  
	  public SoapProvider()
	  {
		  this.SOAP_ACTION =NAMESPACE+METHOD_NAME;
	  }
	  
	  private SoapObject init(String METHOD_NAME){
		  
		  try{
			  SoapObject request = new SoapObject(NAMESPACE, METHOD_NAME);
			  request.addProperty("arg0", METHOD_NAME);
			  SoapSerializationEnvelope envelope = new SoapSerializationEnvelope(SoapEnvelope.VER11);
			  envelope.dotNet = true;
			  envelope.setOutputSoapObject(request);
			  HttpTransportSE androidHttpTransport = new HttpTransportSE(URL);
			  androidHttpTransport.call(NAMESPACE+METHOD_NAME, envelope);
			  response = (SoapObject)envelope.getResponse(); //get the response from your webservice
		  }	catch (Exception e) {
			  Log.d("Fail", e.getMessage());
			  return null;
		  }		  
		  return response;
	  }
	  
	  public ArrayList<String> GetBusList(){
		  try {
			  init("GetBusList");			  
			  resultlist = new ArrayList<String>();
			  for(int i = 0; i<response.getPropertyCount();i++){
				  SoapPrimitive f = (SoapPrimitive)response.getProperty(i);
				  resultlist.add(f.toString());				  
			  	}	  
			  }		  
		  catch (Exception e) {
			  resultlist.add(e.getMessage());
		  }		  
		 return resultlist;
	  }
	  
	  public ArrayList<BusStop> GetBusStops(String BusNumber){
		  ArrayList<BusStop> BusStoplist = new ArrayList<BusStop>();
		  
		  try{
			  SoapObject request = new SoapObject(NAMESPACE, "GetBusStops");
			  request.addProperty("busNumber", BusNumber);
			  SoapSerializationEnvelope envelope = new SoapSerializationEnvelope(SoapEnvelope.VER11);
			  envelope.dotNet = true;
			  envelope.setOutputSoapObject(request);
			  HttpTransportSE androidHttpTransport = new HttpTransportSE(URL);
			  androidHttpTransport.call(NAMESPACE+"GetBusStops", envelope);
			  response = (SoapObject)envelope.getResponse(); //get the response from your webservice

			  SoapObject lat = (SoapObject)response.getProperty(0);
			  SoapObject lng = (SoapObject)response.getProperty(1);
			  SoapObject name = (SoapObject)response.getProperty(2);

			  for(int h = 0; h<lat.getPropertyCount();h++){
				  BusStoplist.add(new BusStop(name.getProperty(h).toString(), new LatLng(Double.parseDouble(lat.getProperty(h).toString().replace(",", ".")), Double.parseDouble(lng.getProperty(h).toString().replace(",", ".")))));
				  }
		  }catch(Exception e){
			  Log.e("DEBUG!!", e.getMessage());
			  return null;
		  }

		  return BusStoplist;
	  }
	  
		public ArrayList<LatLng> GetBusRoute(String BusNumber){
				  
			  ArrayList<LatLng> Routelist = new ArrayList<LatLng>();
			  try{
				  SoapObject request = new SoapObject(NAMESPACE, "GetBusRoute");
				  request.addProperty("busNumber", BusNumber);
				  SoapSerializationEnvelope envelope = new SoapSerializationEnvelope(SoapEnvelope.VER11);
				  envelope.dotNet = true;
				  envelope.setOutputSoapObject(request);
				  HttpTransportSE androidHttpTransport = new HttpTransportSE(URL);
				  androidHttpTransport.call(NAMESPACE+"GetBusRoute", envelope);
				  response = (SoapObject)envelope.getResponse(); //get the response from your webservice
	
				  SoapObject lat = (SoapObject)response.getProperty(0);
				  SoapObject lng = (SoapObject)response.getProperty(1);
	
				  for(int h = 0; h<lat.getPropertyCount();h++){
					  Routelist.add(new LatLng(Double.parseDouble(lat.getProperty(h).toString().replace(",", ".")), Double.parseDouble(lng.getProperty(h).toString().replace(",", "."))));
				  }
			  }catch(Exception e){
				  Log.e("DEBUG!!", e.getMessage());
				  return null;
			  }

			  return Routelist;
		}
		
		public ArrayList<LatLng> GetBusPos(String BusNumber){
			  
			  ArrayList<LatLng> Routelist = new ArrayList<LatLng>();
			  try{
				  SoapObject request = new SoapObject(NAMESPACE, "GetbusPos");
				  request.addProperty("busNumber", BusNumber);
				  SoapSerializationEnvelope envelope = new SoapSerializationEnvelope(SoapEnvelope.VER11);
				  envelope.dotNet = true;
				  envelope.setOutputSoapObject(request);
				  HttpTransportSE androidHttpTransport = new HttpTransportSE(URL);
				  androidHttpTransport.call(NAMESPACE+"GetbusPos", envelope);
				  SoapObject testresponse = (SoapObject)envelope.getResponse(); //get the response from your webservice
	
				  Routelist.add(new LatLng(Double.parseDouble(testresponse.getProperty(0).toString().replace(",", ".")), Double.parseDouble(testresponse.getProperty(1).toString().replace(",", "."))));
			  }catch(Exception e){
				  return null;
			  }
			  return Routelist;
		}
		
		public ArrayList<String> GetBusToStopTime(String stopName, String routeNumber)
		{
			  ArrayList<String> RouteList = new ArrayList<String>();
			  try{
				  SoapObject request = new SoapObject(NAMESPACE, "GetBusTime");
				  request.addProperty("StopName", stopName);
				  request.addProperty("RouteNumber", routeNumber);
				  SoapSerializationEnvelope envelope = new SoapSerializationEnvelope(SoapEnvelope.VER11);
				  envelope.dotNet = true;
				  envelope.setOutputSoapObject(request);
				  HttpTransportSE androidHttpTransport = new HttpTransportSE(URL);
				  androidHttpTransport.call(NAMESPACE+"GetBusTime", envelope);
				  SoapObject testresponse = (SoapObject)envelope.getResponse(); //get the response from your webservice
				  try
				  {
					RouteList.add(testresponse.getProperty(0).toString());
					RouteList.add(testresponse.getProperty(1).toString());
				  }
				  catch(Exception e)
				  {
					return RouteList;  
				  }
			  }catch(Exception e){
				  return null;
			  }
			  return RouteList;
		
		}
}
