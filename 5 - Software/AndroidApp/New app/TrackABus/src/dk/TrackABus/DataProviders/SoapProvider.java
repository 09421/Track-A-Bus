package dk.TrackABus.DataProviders;

import java.util.ArrayList;

import org.ksoap2.SoapEnvelope;
import org.ksoap2.serialization.SoapObject;
import org.ksoap2.serialization.SoapPrimitive;
import org.ksoap2.serialization.SoapSerializationEnvelope;
import org.ksoap2.transport.HttpTransportSE;

import dk.TrackABus.Models.BusRoute;
import dk.TrackABus.Models.BusStop;
import dk.TrackABus.Models.RoutePoint;
import com.google.android.gms.maps.model.LatLng;

import android.util.Log;

public class SoapProvider {
	private SoapObject response;
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
			  int test = response.getPropertyCount();
			  if(test <= 1)
			  {
				  Log.e("DEBUG!!", ((SoapObject)response.getProperty(0)).getProperty(0).toString());
				  return null;
			  }
			  for(int i = 0; i < response.getPropertyCount(); i++)
			  {			  
				  SoapObject BusStopSoap = (SoapObject)response.getProperty(i);
				  SoapObject pBSSoap = (SoapObject)BusStopSoap.getProperty(0);
				  
				  RoutePoint bsRP = new RoutePoint(
						  new LatLng(
								  Double.parseDouble(pBSSoap.getProperty(0).toString().replace(',', '.')),
								  Double.parseDouble(pBSSoap.getProperty(1).toString().replace(',', '.'))),
						  pBSSoap.getProperty(2).toString());
				  BusStoplist.add(new BusStop(
						  bsRP,
						  BusStopSoap.getProperty(1).toString(),
						  BusStopSoap.getProperty(2).toString(),
						  BusStopSoap.getProperty(3).toString(),
						  BusStopSoap.getProperty(4).toString()));
			  }
		  }
		  catch(Exception e){
			  Log.e("DEBUG!!", e.getMessage());
			  return null;
		  }

		  return BusStoplist;
	  }
	  
		public ArrayList<BusRoute> GetBusRoute(String BusNumber){

			ArrayList<BusRoute> Route = new ArrayList<BusRoute>();
			  try{
				  SoapObject request = new SoapObject(NAMESPACE, "GetBusRoute");
				  request.addProperty("busNumber", BusNumber);
				  SoapSerializationEnvelope envelope = new SoapSerializationEnvelope(SoapEnvelope.VER11);
				  envelope.dotNet = true;
				  envelope.setOutputSoapObject(request);
				  HttpTransportSE androidHttpTransport = new HttpTransportSE(URL);
				  androidHttpTransport.call(NAMESPACE+"GetBusRoute", envelope);

				  response = (SoapObject)envelope.getResponse(); //get the response from your webservice


				  ArrayList<RoutePoint> points;
				  ArrayList<String> bs_rpID;
				  SoapObject busRoute;
				  for(int i = 0; i < response.getPropertyCount(); i++)
				  {
					  busRoute = (SoapObject)response.getProperty(i);
					  points = new ArrayList<RoutePoint>();
					  bs_rpID = new ArrayList<String>();
					  SoapObject rPListSoap = (SoapObject)busRoute.getProperty(0);
					  SoapObject BrRpIDListSoap = (SoapObject)busRoute.getProperty(1);
					  SoapObject rPSoap;
					  for(int j = 0; j < rPListSoap.getPropertyCount(); j++)
					  {
						  rPSoap = (SoapObject)rPListSoap.getProperty(j);
						  points.add(new RoutePoint(
								  new LatLng(
										  Double.parseDouble(rPSoap.getProperty(0).toString().replace(",", ".")),
										  Double.parseDouble(rPSoap.getProperty(1).toString().replace(",", "."))),
								  rPSoap.getProperty(2).toString()));
					  }
					  for(int k = 0; k < BrRpIDListSoap.getPropertyCount(); k++)
					  {
						  bs_rpID.add(BrRpIDListSoap.getProperty(k).toString());
					  }
					  Route.add(new BusRoute(
							  points,
							  bs_rpID,
							  busRoute.getProperty(2).toString(),
							  busRoute.getProperty(3).toString(),
							  busRoute.getProperty(4).toString()));
				  }
			  }catch(Exception e){
				  Log.e("DEBUG!!", e.getMessage());
				  return null;
			  }

			  return Route;
		}
		
		public ArrayList<LatLng> GetBusPos(String BusNumber){
			  
			  ArrayList<LatLng> BusPoint = new ArrayList<LatLng>();;
			  try{
				  SoapObject request = new SoapObject(NAMESPACE, "GetbusPos");
				  request.addProperty("busNumber", BusNumber);
				  SoapSerializationEnvelope envelope = new SoapSerializationEnvelope(SoapEnvelope.VER11);
				  envelope.dotNet = true;
				  envelope.setOutputSoapObject(request);
				  HttpTransportSE androidHttpTransport = new HttpTransportSE(URL);
				  androidHttpTransport.call(NAMESPACE+"GetbusPos", envelope);
				  SoapObject testresponse = (SoapObject)envelope.getResponse(); //get the response from your webservice
				  
				  for(int i = 0; i<testresponse.getPropertyCount(); i++){
					  double a = Double.parseDouble(((SoapObject)testresponse.getProperty(i)).getProperty(0).toString().replace(",", "."));
					  double b = Double.parseDouble(((SoapObject)testresponse.getProperty(i)).getProperty(1).toString().replace(",", "."));
					  BusPoint.add(new LatLng(a, b));
				  }

			  }catch(Exception e){
				  return null;
			  }
			  return BusPoint;
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
					RouteList.add(testresponse.getProperty(2).toString());
					RouteList.add(testresponse.getProperty(1).toString());
					RouteList.add(testresponse.getProperty(3).toString());
					Log.e("DEBUG", RouteList.get(0) + " : "+ RouteList.get(1) + " : " + RouteList.get(2) + " : " + RouteList.get(3));
				  }
				  catch(Exception e)
				  {
					Log.e("DEBUG", e.getMessage());
				  }
			  }catch(Exception e){
				  return null;
			  }
			  return RouteList;
		
		}
}
