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
const logger = require("firebase-functions/logger");

const { onDocumentCreated } = require("firebase-functions/v2/firestore");
const admin = require('firebase-admin');

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

 
// Initialize Firebase Admin with the service account
const serviceAccount = require('./serviceAccountKey.json');
admin.initializeApp({
    credential: admin.credential.cert(serviceAccount)
});

// Listen to new documents in the building-events collection
exports.sendBuildingEventNotification = onDocumentCreated(
    'building-events/{eventId}',
    async (event) => {
        try {
            const eventData = event.data.data();
            logger.info("Event created. Pushing a notification", eventData);

            // Create notification message
            // Handle Firestore Timestamp
            const eventDate = eventData.date instanceof admin.firestore.Timestamp 
                ? eventData.date.toDate() 
                : new Date(eventData.date);

            logger.info("Processing date:", { 
                rawDate: eventData.date,
                parsedDate: eventDate,
                isTimestamp: eventData.date instanceof admin.firestore.Timestamp
            });

            const message = {
                notification: {
                    title: `New Event at ${eventData.buildingName}`,
                    body: `${eventData.eventName} scheduled for ${eventDate.toLocaleDateString('en-US', {
                        weekday: 'long',
                        year: 'numeric',
                        month: 'long',
                        day: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    })}`
                },
                topic: 'updates' // The topic clients are subscribed to
            };
            // Send the notification
            const response = await admin.messaging().send(message);
            logger.info('Successfully sent notification:', response);
            return response;
        } catch (error) {
            logger.error('Error sending notification:', error);
            throw error;
        }
    }
);