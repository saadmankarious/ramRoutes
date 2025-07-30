package com.edgoanalytics.ramroutes.plugin;

import android.content.Context;

public class UnityGeofencingInterface {
    private static GeofencingPlugin geofencingPlugin;

    public static void initialize(Context unityContext) {
        if (geofencingPlugin == null) {
            geofencingPlugin = new GeofencingPlugin(unityContext);
        }
    }


    public static void addGeofence(String requestId, double latitude, double longitude, float radius) {
        if (geofencingPlugin != null) {
            geofencingPlugin.addGeofence(requestId, latitude, longitude, radius);
        }
    }
}