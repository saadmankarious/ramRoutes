#import <CoreLocation/CoreLocation.h>
#import <UserNotifications/UserNotifications.h>

// Forward declaration for Unity's message function
extern void UnitySendMessage(const char* obj, const char* method, const char* msg);

@interface BackgroundLocationPlugin : NSObject <CLLocationManagerDelegate, UNUserNotificationCenterDelegate>
@property (strong, nonatomic) CLLocationManager *locationManager;
@property (copy, nonatomic) NSString *gameObjectName;
@property (assign, nonatomic) BOOL preciseTrackingActive;
@property (copy, nonatomic) NSString *deviceId;
@property (copy, nonatomic) NSString *userId;                           // Firebase user ID for currentPhysicalBuilding updates
@property (copy, nonatomic) NSString *currentPhysicalBuilding;          // last building sent to Firestore (avoid redundant writes)
@property (strong, nonatomic) NSDate *lastFirestoreLog;
@property (strong, nonatomic) NSDate *lastBuildingUpdate;               // throttle building updates
@property (strong, nonatomic) NSMutableDictionary *registeredBuildings; // name → {lat, lon, radius}
@property (strong, nonatomic) NSMutableSet *notifiedBuildings;          // buildings already notified (avoid spam)
@property (strong, nonatomic) NSDate *lastNotificationReset;            // reset notified set periodically
@end

static BackgroundLocationPlugin *_instance = nil;

@implementation BackgroundLocationPlugin

+ (BackgroundLocationPlugin *)sharedInstance {
    if (!_instance) {
        _instance = [[BackgroundLocationPlugin alloc] init];
    }
    return _instance;
}

- (void)startWithGameObject:(NSString *)goName distanceFilter:(float)distFilter {
    self.gameObjectName = goName;
    self.preciseTrackingActive = YES;
    self.deviceId = [[[UIDevice currentDevice] identifierForVendor] UUIDString];
    self.registeredBuildings = [NSMutableDictionary dictionary];
    self.notifiedBuildings = [NSMutableSet set];
    self.lastNotificationReset = [NSDate date];

    self.locationManager = [[CLLocationManager alloc] init];
    self.locationManager.delegate = self;
    self.locationManager.desiredAccuracy = kCLLocationAccuracyBest;
    self.locationManager.distanceFilter = distFilter;
    self.locationManager.allowsBackgroundLocationUpdates = YES;
    self.locationManager.pausesLocationUpdatesAutomatically = NO;

    // Just request authorization — the delegate callback will start updates
    [self.locationManager requestAlwaysAuthorization];

    // Request notification permission
    UNUserNotificationCenter *center = [UNUserNotificationCenter currentNotificationCenter];
    center.delegate = self;
    [center requestAuthorizationWithOptions:(UNAuthorizationOptionAlert | UNAuthorizationOptionSound | UNAuthorizationOptionBadge)
                          completionHandler:^(BOOL granted, NSError * _Nullable error) {
        if (granted) {
            NSLog(@"[BackgroundLocation] Notification permission granted");
        } else {
            NSLog(@"[BackgroundLocation] Notification permission denied: %@", error.localizedDescription);
        }
    }];
}

#pragma mark - UNUserNotificationCenterDelegate

// Show notification even when app is in foreground
- (void)userNotificationCenter:(UNUserNotificationCenter *)center
       willPresentNotification:(UNNotification *)notification
         withCompletionHandler:(void (^)(UNNotificationPresentationOptions))completionHandler {
    completionHandler(UNNotificationPresentationOptionBanner | UNNotificationPresentationOptionSound);
}

- (void)sendLocalNotification:(NSString *)buildingName {
    UNMutableNotificationContent *content = [[UNMutableNotificationContent alloc] init];
    content.title = @"Ram Routes";
    content.body = [NSString stringWithFormat:@"You're near %@! Open the app to start your adventure.", buildingName];
    content.sound = [UNNotificationSound defaultSound];

    // Fire immediately (1 second delay required for time-interval trigger)
    UNTimeIntervalNotificationTrigger *trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:1 repeats:NO];

    NSString *requestId = [NSString stringWithFormat:@"geofence-%@-%f", buildingName, [[NSDate date] timeIntervalSince1970]];
    UNNotificationRequest *request = [UNNotificationRequest requestWithIdentifier:requestId content:content trigger:trigger];

    [[UNUserNotificationCenter currentNotificationCenter] addNotificationRequest:request withCompletionHandler:^(NSError * _Nullable error) {
        if (error) {
            NSLog(@"[BackgroundLocation] Local notification error: %@", error.localizedDescription);
        } else {
            NSLog(@"[BackgroundLocation] Local notification scheduled for: %@", buildingName);
        }
    }];
}

- (void)stop {
    [self.locationManager stopUpdatingLocation];
    self.preciseTrackingActive = NO;
    // Geofences keep running — they survive app kill
}

- (void)registerGeofenceWithId:(NSString *)identifier latitude:(double)lat longitude:(double)lon radius:(double)radius {
    if (![CLLocationManager isMonitoringAvailableForClass:[CLCircularRegion class]]) {
        NSLog(@"[BackgroundLocation] Geofencing not available on this device");
        return;
    }

    CLLocationCoordinate2D center = CLLocationCoordinate2DMake(lat, lon);
    // iOS caps radius at locationManager.maximumRegionMonitoringDistance (~400m typically)
    CLLocationDistance clampedRadius = MIN(radius, self.locationManager.maximumRegionMonitoringDistance);
    CLCircularRegion *region = [[CLCircularRegion alloc] initWithCenter:center radius:clampedRadius identifier:identifier];
    region.notifyOnEntry = YES;
    region.notifyOnExit = NO;

    [self.locationManager startMonitoringForRegion:region];
    NSLog(@"[BackgroundLocation] Geofence registered: %@ (%.6f, %.6f, %.0fm)", identifier, lat, lon, clampedRadius);

    // Store building location for proximity checks in didUpdateLocations
    self.registeredBuildings[identifier] = @{
        @"latitude": @(lat),
        @"longitude": @(lon),
        @"radius": @(radius)  // use the original requested radius for proximity (not the clamped geofence radius)
    };
}

#pragma mark - Universal Building Mapping

/// Returns the name of the nearest building within its radius for the given lat/lon, or nil if none.
- (NSString *)nearestBuildingForLatitude:(double)lat longitude:(double)lon {
    __block NSString *nearest = nil;
    __block CLLocationDistance minDistance = DBL_MAX;
    CLLocation *loc = [[CLLocation alloc] initWithLatitude:lat longitude:lon];

    [self.registeredBuildings enumerateKeysAndObjectsUsingBlock:^(NSString *name, NSDictionary *info, BOOL *stop) {
        double bLat = [info[@"latitude"] doubleValue];
        double bLon = [info[@"longitude"] doubleValue];
        double bRadius = [info[@"radius"] doubleValue];
        CLLocation *bLoc = [[CLLocation alloc] initWithLatitude:bLat longitude:bLon];
        CLLocationDistance dist = [loc distanceFromLocation:bLoc];
        if (dist <= bRadius && dist < minDistance) {
            minDistance = dist;
            nearest = name;
        }
    }];
    return nearest;
}

/// Updates currentPhysicalBuilding on the user's Firestore document via REST PATCH.
/// Only fires when the resolved building changes and userId is set.
- (void)updateCurrentPhysicalBuilding:(NSString *)buildingName {
    if (!self.userId || self.userId.length == 0) {
        NSLog(@"[BackgroundLocation] Cannot update currentPhysicalBuilding — no userId set");
        return;
    }
    // Avoid redundant writes
    if ([buildingName isEqualToString:self.currentPhysicalBuilding]) return;

    // Throttle: at most once every 10 seconds
    NSDate *now = [NSDate date];
    if (self.lastBuildingUpdate && [now timeIntervalSinceDate:self.lastBuildingUpdate] < 10.0) return;
    self.lastBuildingUpdate = now;

    self.currentPhysicalBuilding = buildingName;

    NSString *projectId = @"trials-of-venus";
    NSString *url = [NSString stringWithFormat:
        @"https://firestore.googleapis.com/v1/projects/%@/databases/(default)/documents/users/%@?updateMask.fieldPaths=currentPhysicalBuilding",
        projectId, self.userId];

    NSDictionary *body = @{
        @"fields": @{
            @"currentPhysicalBuilding": @{@"stringValue": buildingName ?: @""}
        }
    };

    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:body options:0 error:nil];
    NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:url]];
    request.HTTPMethod = @"PATCH";
    request.HTTPBody = jsonData;
    [request setValue:@"application/json" forHTTPHeaderField:@"Content-Type"];

    [[NSURLSession.sharedSession dataTaskWithRequest:request completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
        if (error) {
            NSLog(@"[BackgroundLocation] currentPhysicalBuilding update error: %@", error.localizedDescription);
        } else {
            NSLog(@"[BackgroundLocation] currentPhysicalBuilding → %@ for user %@", buildingName, self.userId);
        }
    }] resume];

    // Also tell Unity so it can update in-memory state
    NSString *msg = [NSString stringWithFormat:@"%@", buildingName];
    UnitySendMessage([self.gameObjectName UTF8String], "OnPhysicalBuildingChangedNative", [msg UTF8String]);
}

- (void)logToFirestore:(NSString *)eventType latitude:(double)lat longitude:(double)lon accuracy:(double)acc extra:(NSString *)extra {
    // Firestore REST API — no SDK needed, works even when Unity is not initialized
    NSString *projectId = @"trials-of-venus";
    NSString *url = [NSString stringWithFormat:
        @"https://firestore.googleapis.com/v1/projects/%@/databases/(default)/documents/background-location-logs",
        projectId];

    NSString *now = [[NSISO8601DateFormatter new] stringFromDate:[NSDate date]];

    NSDictionary *body = @{
        @"fields": @{
            @"deviceId":  @{@"stringValue": self.deviceId ?: @"unknown"},
            @"eventType": @{@"stringValue": eventType},
            @"latitude":  @{@"doubleValue": @(lat)},
            @"longitude": @{@"doubleValue": @(lon)},
            @"accuracy":  @{@"doubleValue": @(acc)},
            @"extra":     @{@"stringValue": extra ?: @""},
            @"timestamp": @{@"timestampValue": now}
        }
    };

    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:body options:0 error:nil];
    NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:[NSURL URLWithString:url]];
    request.HTTPMethod = @"POST";
    request.HTTPBody = jsonData;
    [request setValue:@"application/json" forHTTPHeaderField:@"Content-Type"];

    [[NSURLSession.sharedSession dataTaskWithRequest:request completionHandler:^(NSData *data, NSURLResponse *response, NSError *error) {
        if (error) {
            NSLog(@"[BackgroundLocation] Firestore log error: %@", error.localizedDescription);
        } else {
            NSLog(@"[BackgroundLocation] Firestore log OK: %@", eventType);
        }
    }] resume];
}

#pragma mark - CLLocationManagerDelegate

- (void)locationManagerDidChangeAuthorization:(CLLocationManager *)manager {
    CLAuthorizationStatus status = manager.authorizationStatus;
    NSLog(@"[BackgroundLocation] Authorization status: %d", (int)status);
    if (status == kCLAuthorizationStatusAuthorizedAlways || status == kCLAuthorizationStatusAuthorizedWhenInUse) {
        [self.locationManager startUpdatingLocation];
    }
}

- (void)locationManager:(CLLocationManager *)manager didUpdateLocations:(NSArray<CLLocation *> *)locations {
    CLLocation *loc = [locations lastObject];
    NSString *msg = [NSString stringWithFormat:@"%f,%f,%f",
                     loc.coordinate.latitude,
                     loc.coordinate.longitude,
                     loc.horizontalAccuracy];

    NSLog(@"[BackgroundLocation] Update: %@", msg);

    // Throttle Firestore logging to once every 30 seconds
    NSDate *now = [NSDate date];
    if (!self.lastFirestoreLog || [now timeIntervalSinceDate:self.lastFirestoreLog] >= 30.0) {
        self.lastFirestoreLog = now;
        // [self logToFirestore:@"location_update" latitude:loc.coordinate.latitude longitude:loc.coordinate.longitude accuracy:loc.horizontalAccuracy extra:@""];
    }

    // Reset notified buildings every 10 minutes so they can re-trigger
    if (!self.lastNotificationReset || [now timeIntervalSinceDate:self.lastNotificationReset] >= 600.0) {
        [self.notifiedBuildings removeAllObjects];
        self.lastNotificationReset = now;
        NSLog(@"[BackgroundLocation] Notification cooldowns reset");
    }

    // Check proximity to each registered building
    [self.registeredBuildings enumerateKeysAndObjectsUsingBlock:^(NSString *name, NSDictionary *info, BOOL *stop) {
        double bLat = [info[@"latitude"] doubleValue];
        double bLon = [info[@"longitude"] doubleValue];
        double bRadius = [info[@"radius"] doubleValue];

        CLLocation *buildingLoc = [[CLLocation alloc] initWithLatitude:bLat longitude:bLon];
        CLLocationDistance distance = [loc distanceFromLocation:buildingLoc];

        if (distance <= bRadius && ![self.notifiedBuildings containsObject:name]) {
            [self.notifiedBuildings addObject:name];
            [self sendLocalNotification:name];
            // [self logToFirestore:@"proximity_notification" latitude:loc.coordinate.latitude longitude:loc.coordinate.longitude accuracy:loc.horizontalAccuracy extra:name];
            NSLog(@"[BackgroundLocation] Proximity notification fired for %@ (%.0fm away)", name, distance);
        }
    }];

    // Update currentPhysicalBuilding based on nearest building within radius
    NSString *nearestBuilding = [self nearestBuildingForLatitude:loc.coordinate.latitude longitude:loc.coordinate.longitude];
    if (nearestBuilding) {
        [self updateCurrentPhysicalBuilding:nearestBuilding];
    }

    UnitySendMessage([self.gameObjectName UTF8String], "OnNativeLocationUpdate", [msg UTF8String]);
}

- (void)locationManager:(CLLocationManager *)manager didEnterRegion:(CLRegion *)region {
    NSLog(@"[BackgroundLocation] Entered geofence: %@", region.identifier);

    // Log geofence entry to Firestore
    CLCircularRegion *circular = (CLCircularRegion *)region;
    [self logToFirestore:@"geofence_entered" latitude:circular.center.latitude longitude:circular.center.longitude accuracy:0 extra:region.identifier];

    // Fire local notification (works even after app kill)
    [self sendLocalNotification:region.identifier];

    // Mark as notified so didUpdateLocations doesn't double-notify
    [self.notifiedBuildings addObject:region.identifier];

    // Update currentPhysicalBuilding in Firestore
    [self updateCurrentPhysicalBuilding:region.identifier];

    // Notify Unity
    UnitySendMessage([self.gameObjectName UTF8String], "OnGeofenceEntered", [region.identifier UTF8String]);

    // Start precise GPS tracking if not already running
    if (!self.preciseTrackingActive) {
        self.preciseTrackingActive = YES;
        self.locationManager.desiredAccuracy = kCLLocationAccuracyBest;
        self.locationManager.distanceFilter = 5.0;
        self.locationManager.allowsBackgroundLocationUpdates = YES;
        self.locationManager.pausesLocationUpdatesAutomatically = NO;
        [self.locationManager startUpdatingLocation];
        NSLog(@"[BackgroundLocation] Precise tracking started from geofence trigger");
    }
}

- (void)locationManager:(CLLocationManager *)manager didFailWithError:(NSError *)error {
    NSLog(@"[BackgroundLocation] Error: %@", error.localizedDescription);
}

- (void)locationManager:(CLLocationManager *)manager monitoringDidFailForRegion:(CLRegion *)region withError:(NSError *)error {
    NSLog(@"[BackgroundLocation] Geofence monitoring failed for %@: %@", region.identifier, error.localizedDescription);
}

@end

// C functions called from Unity via DllImport
extern "C" {
    void _StartBackgroundLocation(const char* gameObjectName, float distanceFilter) {
        NSString *goName = [NSString stringWithUTF8String:gameObjectName];
        [[BackgroundLocationPlugin sharedInstance] startWithGameObject:goName distanceFilter:distanceFilter];
    }

    void _StopBackgroundLocation() {
        [[BackgroundLocationPlugin sharedInstance] stop];
    }

    void _RegisterGeofence(const char* identifier, double latitude, double longitude, double radius) {
        NSString *idStr = [NSString stringWithUTF8String:identifier];
        [[BackgroundLocationPlugin sharedInstance] registerGeofenceWithId:idStr latitude:latitude longitude:longitude radius:radius];
    }

    void _SetUserId(const char* userId) {
        NSString *uid = [NSString stringWithUTF8String:userId];
        [BackgroundLocationPlugin sharedInstance].userId = uid;
        NSLog(@"[BackgroundLocation] userId set: %@", uid);
    }
}
