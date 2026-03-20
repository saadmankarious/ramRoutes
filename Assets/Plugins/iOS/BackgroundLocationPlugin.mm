#import <CoreLocation/CoreLocation.h>

// Forward declaration for Unity's message function
extern void UnitySendMessage(const char* obj, const char* method, const char* msg);

@interface BackgroundLocationPlugin : NSObject <CLLocationManagerDelegate>
@property (strong, nonatomic) CLLocationManager *locationManager;
@property (copy, nonatomic) NSString *gameObjectName;
@property (assign, nonatomic) BOOL preciseTrackingActive;
@property (copy, nonatomic) NSString *deviceId;
@property (strong, nonatomic) NSDate *lastFirestoreLog;
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

    self.locationManager = [[CLLocationManager alloc] init];
    self.locationManager.delegate = self;
    self.locationManager.desiredAccuracy = kCLLocationAccuracyBest;
    self.locationManager.distanceFilter = distFilter;
    self.locationManager.allowsBackgroundLocationUpdates = YES;
    self.locationManager.pausesLocationUpdatesAutomatically = NO;

    // Just request authorization — the delegate callback will start updates
    [self.locationManager requestAlwaysAuthorization];
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
        [self logToFirestore:@"location_update" latitude:loc.coordinate.latitude longitude:loc.coordinate.longitude accuracy:loc.horizontalAccuracy extra:@""];
    }

    UnitySendMessage([self.gameObjectName UTF8String], "OnNativeLocationUpdate", [msg UTF8String]);
}

- (void)locationManager:(CLLocationManager *)manager didEnterRegion:(CLRegion *)region {
    NSLog(@"[BackgroundLocation] Entered geofence: %@", region.identifier);

    // Log geofence entry to Firestore
    CLCircularRegion *circular = (CLCircularRegion *)region;
    [self logToFirestore:@"geofence_entered" latitude:circular.center.latitude longitude:circular.center.longitude accuracy:0 extra:region.identifier];

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
}
