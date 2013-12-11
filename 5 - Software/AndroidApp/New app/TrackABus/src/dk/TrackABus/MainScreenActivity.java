package dk.TrackABus;


import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.AdapterView;
import android.widget.AdapterView.OnItemClickListener;
import android.widget.ArrayAdapter;
import android.widget.ListView;

public class MainScreenActivity extends Activity{

	ListView l;
	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		ConnectivityChecker.setInternetConn(getApplicationContext());
		setContentView(R.layout.mainscreenactivity);
		l = (ListView)findViewById(R.id.FavoriteList);	
		SetFavoriteBusListAdapter(l);
		SetBusListOnClickListener(l);
	}

	private void SetFavoriteBusListAdapter(ListView l) {
		ArrayAdapter<String> adapter = new ArrayAdapter<String>(this,
				R.layout.favoritelist_layout, ContentProviderAcces.GetBusRoutes(getApplicationContext(),null));
		l.setAdapter(adapter);		
	}
	
	private void SetBusListOnClickListener(ListView l) {
		l.setOnItemClickListener(new OnItemClickListener(){
			@Override
			public void onItemClick(AdapterView<?> parent, View view, int position,
                    long id) {
				Intent myIntent = new Intent(getApplicationContext(), BusMapActivity.class);
				myIntent.putExtra("SELECTED_BUS", parent.getItemAtPosition(position).toString());
				myIntent.putExtra("isFavorite", true);
				startActivityForResult(myIntent, 0);				
			}			
		});
	}

	public void ViewAllBusRoutes(View view) {
		
		Intent myIntent = new Intent(getApplicationContext(), BuslistMenuActivity.class);
		startActivityForResult(myIntent, 0);
	 }

	@Override
	protected void onPause() {
		super.onPause();
	}

	@Override
	protected void onRestart() {
		super.onRestart();
	}

	@Override
	protected void onResume() {
		super.onResume();
		SetFavoriteBusListAdapter(l);
		SetBusListOnClickListener(l);	
	}

	@Override
	protected void onStart() {
		super.onStart();
	}

	@Override
	protected void onStop() {
		super.onStop();
		
	}
}
