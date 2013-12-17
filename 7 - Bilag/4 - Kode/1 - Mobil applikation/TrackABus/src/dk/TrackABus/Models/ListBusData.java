package dk.TrackABus.Models;

/**
 * class used to represent a single item in a list adapter
 *
 */
public class ListBusData {
	
	public Boolean IsFavorite;
	public String BusNumber;
	
	public ListBusData(Boolean favorite, String number){
		IsFavorite = favorite;
		BusNumber = number;
	}

}
