package com.example.mapviewapplication.TrackABus;

public class ListBusData {
	
	public Boolean IsFavorite;
	public String BusNumber;
	
	public ListBusData(Boolean favorite, String number){
		IsFavorite = favorite;
		BusNumber = number;
	}
	
	public Boolean getIsFavorite(){
		return IsFavorite;
	}
	
	public String getBusNumber(){
		return BusNumber;
	}

}
