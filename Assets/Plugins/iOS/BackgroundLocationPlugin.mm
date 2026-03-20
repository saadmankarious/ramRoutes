#import <CoreLocation/CoreLocation.h>

// Forward declaration for Unity's message function
extern void UnitySendMessage(const char* obj, const char* method, const char* msg);

@interface BackgroundLocationPlugin : NSObject <CLLocationManagerDelegate>
@property (strong, nonatomic) CLLocationManager *locationManager;
@property (copy, nonatomic) NSString *gameObjectName;
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

    self.locationManager = [[CLLocationManager alloc] init];
    self.locationManager.delegate = self;
    self.locationManager.desiredAccuracy = kCLLocationAccuracyBest;
    self.locationManager.distanceFilter = distFilter;
    self.locationManager.allowsBackgroundLocationUpdates = YES;
    self.locationManager.pausesLocationUpdatesAutomatically = NO;

    if ([CLLocationManager authorizationStatus] == kCLAuthorizationStatusNotDetermined) {
        [self.locationManager requestAlwaysAuthorization];
    } else {
        [self.locationManager startUpdatingLocation];
    }
}

- (void)stop {
    [self.locationManager stopUpdatingLocation];
}

#pragma mark - CLLocationManagerDelegate

- (void)locationManager:(CLLocationManager *)manager didChangeAuthorizationStatus:(CLAuthorizationStatus)status {
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

    UnitySendMessage([self.gameObjectName UTF8String], "OnNativeLocationUpdate", [msg UTF8String]);
}

- (void)locationManager:(CLLocationManager *)manager didFailWithError:(NSError *)error {
    NSLog(@"[BackgroundLocation] Error: %@", error.localizedDescription);
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
}
