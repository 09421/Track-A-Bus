package dk.TrackABus.Models;

import android.widget.ProgressBar;
import android.widget.ToggleButton;

public class AdapterRunner {

	public static ToggleButton currentButton;
	public static ProgressBar currentBar;
	public static ListBusData currentData;
	public static boolean currentHandling;
	
	public static void removeCurrent()
	{
		currentButton = null; currentBar = null; currentData = null;
	}
	public static void setter()
	{
		if(currentButton == null && currentBar == null)
			return;
		currentButton.setVisibility(ToggleButton.GONE);
		currentBar.setVisibility(ProgressBar.VISIBLE);
	}
	public static void remover()
	{
		if(currentButton == null && currentBar == null)
			return;
		currentButton.setVisibility(ToggleButton.VISIBLE);
		currentBar.setVisibility(ProgressBar.GONE);
	}
}
