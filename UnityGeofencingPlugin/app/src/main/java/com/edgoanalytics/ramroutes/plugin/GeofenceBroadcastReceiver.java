package com.edgoanalytics.ramroutes.plugin;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.util.Log;
import com.google.android.gms.location.Geofence;
import com.google.android.gms.location.GeofencingEvent;
import java.util.List;

public class GeofenceBroadcastReceiver extends BroadcastReceiver {
    private static final String TAG = "UnityGeofencing";

    @Override
    public void onReceive(Context context, Intent intent) {
        GeofencingEvent geofencingEvent = GeofencingEvent.fromIntent(intent);
        if (geofencingEvent.hasError()) {
            Log.e(TAG, "Geofencing error: " + geofencingEvent.getErrorCode());
            return;
        }

        int transitionType = geofencingEvent.getGeofenceTransition();
        List<Geofence> triggeringGeofences = geofencingEvent.getTriggeringGeofences();

        // Create an intent to launch your Unity activity with the geofence data
        Intent launchIntent = context.getPackageManager()
                .getLaunchIntentForPackage("com.edgoanalytics.ramroutes");

        if (launchIntent != null) {
            for (Geofence geofence : triggeringGeofences) {
                String requestId = geofence.getRequestId();
                launchIntent.putExtra("geofence_event",
                        transitionType == Geofence.GEOFENCE_TRANSITION_ENTER ? "enter" : "exit");
                launchIntent.putExtra("geofence_id", requestId);
                launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                context.startActivity(launchIntent);
            }
        }
    }
}