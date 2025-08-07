/**
 * Import function triggers from their respective submodules:
 *
 * const {onCall} = require("firebase-functions/v2/https");
 * const {onDocumentWritten} = require("firebase-functions/v2/firestore");
 *
 * See a full list of supported triggers at https://firebase.google.com/docs/functions
 */

const {setGlobalOptions} = require("firebase-functions");
const {onRequest} = require("firebase-functions/https");
const {onDocumentCreated, onDocumentUpdated} = require("firebase-functions/v2/firestore");
const {initializeApp} = require("firebase-admin/app");
const {getMessaging} = require("firebase-admin/messaging");
const logger = require("firebase-functions/logger");

// Initialize Firebase Admin SDK
initializeApp();

// For cost control, you can set the maximum number of containers that can be
// running at the same time. This helps mitigate the impact of unexpected
// traffic spikes by instead downgrading performance. This limit is a
// per-function limit. You can override the limit for each function using the
// `maxInstances` option in the function's options, e.g.
// `onRequest({ maxInstances: 5 }, (req, res) => { ... })`.
// NOTE: setGlobalOptions does not apply to functions using the v1 API. V1
// functions should each use functions.runWith({ maxInstances: 10 }) instead.
// In the v1 API, each function can only serve one request per container, so
// this will be the maximum concurrent request count.
setGlobalOptions({ maxInstances: 10 });

// Create and deploy your first functions
// https://firebase.google.com/docs/functions/get-started

// exports.helloWorld = onRequest((request, response) => {
//   logger.info("Hello logs!", {structuredData: true});
//   response.send("Hello from Firebase!");
// });

/**
 * Notify all users when a new building event is added
 * Triggers when a document is created in the BuildingEvents collection
 */
exports.notifyNewBuildingEvent = onDocumentCreated("building-events/{eventId}", async (event) => {
  try {
    const eventData = event.data.data();
    const eventId = event.params.eventId;
    
    logger.info("New building event created", {
      eventId: eventId,
      eventName: eventData.eventName,
      buildingName: eventData.buildingName,
      date: eventData.date
    });

    // Create notification message
    const message = {
      topic: "updates", // Send to all users subscribed to "general" topic
      notification: {
        title: `New Event: ${eventData.eventName}`,
        body: `Check out the new event at ${eventData.buildingName}!`
      },
      data: {
        eventId: eventId,
        eventName: eventData.eventName || "",
        buildingName: eventData.buildingName || "",
        date: eventData.date ? eventData.date.toString() : "",
        type: "building_event"
      },
      android: {
        notification: {
          icon: "ic_notification",
          color: "#4CAF50",
          sound: "default"
        }
      },
      apns: {
        payload: {
          aps: {
            badge: 1,
            sound: "default"
          }
        }
      }
    };

    // Send the notification
    const response = await getMessaging().send(message);
    logger.info("Successfully sent building event notification", {
      messageId: response,
      topic: "updates",
      eventName: eventData.eventName
    });

    return response;

  } catch (error) {
    logger.error("Error sending building event notification", {
      error: error.message,
      eventId: event.params.eventId
    });
    throw error;
  }
});

/**
 * Notify all users when someone joins for the first time
 * Triggers when a user document is updated in the users collection
 */
exports.sendUserJoinedNotification = onDocumentUpdated(
    'users/{userId}',
    async (event) => {
        try {
            const beforeData = event.data.before.data();
            const afterData = event.data.after.data();
            const userId = event.params.userId;
            
            // Check if lastLogin was added for the first time (didn't exist before, exists now)
            if (!beforeData.lastLogin && afterData.lastLogin) {
                logger.info("User signed in for the first time. Pushing a notification", {
                    userId: userId,
                    userName: afterData.name,
                    email: afterData.email
                });

                // Create notification message
                const message = {
                    topic: 'updates', // Use same topic as building events for consistency
                    notification: {
                        title: 'New Player Joined!',
                        body: `${afterData.name || 'A new player'} has joined the game. Welcome them to the community!`
                    },
                    data: {
                        userId: userId,
                        userName: afterData.name || "",
                        type: "user_joined"
                    },
                    android: {
                        notification: {
                            icon: "ic_notification",
                            color: "#4CAF50",
                            sound: "default"
                        }
                    },
                    apns: {
                        payload: {
                            aps: {
                                badge: 1,
                                sound: "default"
                            }
                        }
                    }
                };
                
                // Send the notification
                const response = await getMessaging().send(message);
                logger.info('Successfully sent user joined notification', {
                    messageId: response,
                    userId: userId,
                    userName: afterData.name
                });
                return response;
            }
            
            // If lastLogin already existed, don't send notification
            return null;
        } catch (error) {
            logger.error('Error sending user joined notification', {
                error: error.message,
                userId: event.params.userId
            });
            throw error;
        }
    }
);