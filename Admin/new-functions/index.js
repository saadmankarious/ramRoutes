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
    const creatorUserId = eventData.createdBy || eventData.userId; // Get the user who created the event
    
    // Check if the creator is a guest user (email ending with @ramroutes.com)
    if (creatorUserId) {
      const admin = require("firebase-admin");
      const db = admin.firestore();
      const userDoc = await db.collection("users").doc(creatorUserId).get();
      
      if (userDoc.exists) {
        const userData = userDoc.data();
        const userEmail = userData.email || '';
        
        if (userEmail.endsWith('@ramroutes.com')) {
          logger.info("Skipping notification for guest user", {
            eventId: eventId,
            creatorUserId: creatorUserId,
            email: userEmail
          });
          return null;
        }
      }
    }
    
    logger.info("New building event created", {
      eventId: eventId,
      eventName: eventData.eventName,
      buildingName: eventData.buildingName,
      date: eventData.date,
      createdBy: creatorUserId
    });

    // Send notification to all users subscribed to 'updates' topic
    const message = {
      topic: 'updates',
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

    const response = await getMessaging().send(message);
    logger.info("Successfully sent building event notification to 'updates' topic", {
      messageId: response,
      eventName: eventData.eventName,
      createdBy: creatorUserId
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
 * Triggers when a new user document is created in the users collection
 */
exports.sendUserJoinedNotification = onDocumentCreated(
    'users/{userId}',
    async (event) => {
        try {
            const userData = event.data.data();
            const userId = event.params.userId;
            const userEmail = userData.email || '';
            
            // Skip notification for guest users with @ramroutes.com emails
            if (userEmail.endsWith('@ramroutes.com')) {
                logger.info("Skipping user joined notification for guest user", {
                    userId: userId,
                    email: userEmail
                });
                return null;
            }
            
            logger.info("New user joined the game. Pushing a notification", {
                userId: userId,
                userName: userData.name,
                email: userData.email
            });

            // Send notification to all users subscribed to 'updates' topic
            const message = {
                topic: 'updates',
                notification: {
                    title: 'New Player Joined!',
                    body: `${userData.name || 'A new player'} has joined the game. Welcome them to the community!`
                },
                data: {
                    userId: userId,
                    userName: userData.name || "",
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
            
            const response = await getMessaging().send(message);
            logger.info("Successfully sent user joined notification to 'updates' topic", {
                messageId: response,
                userId: userId,
                userName: userData.name
            });

            return response;
        } catch (error) {
            logger.error('Error sending user joined notification', {
                error: error.message,
                userId: event.params.userId
            });
            throw error;
        }
    }
);

/**
 * Notify all users when someone unlocks a building
 * Triggers when a document is created in the unlocked-trials collection
 */
exports.notifyBuildingUnlocked = onDocumentCreated("unlocked-trials/{unlockId}", async (event) => {
    try {
        const unlockData = event.data.data();
        const unlockId = event.params.unlockId;
        const userId = unlockData.userId; // Get the user who unlocked the building
        
        // Check if the user is a guest user (email ending with @ramroutes.com)
        if (userId) {
            const admin = require("firebase-admin");
            const db = admin.firestore();
            const userDoc = await db.collection("users").doc(userId).get();
            
            if (userDoc.exists) {
                const userData = userDoc.data();
                const userEmail = userData.email || '';
                
                if (userEmail.endsWith('@ramroutes.com')) {
                    logger.info("Skipping building unlocked notification for guest user", {
                        unlockId: unlockId,
                        userId: userId,
                        email: userEmail
                    });
                    return null;
                }
            }
        }
        
        logger.info("Building unlocked by user", {
            unlockId: unlockId,
            userName: unlockData.userName,
            buildingName: unlockData.buildingName,
            userId: userId
        });

        // Send notification to all users subscribed to 'updates' topic
        const message = {
            topic: 'updates',
            notification: {
                title: 'New Building Unlocked! 🏢',
                body: `${unlockData.userName || 'A player'} has unlocked ${unlockData.buildingName || 'a building'}! Check it out!`
            },
            data: {
                unlockId: unlockId,
                userName: unlockData.userName || "",
                buildingName: unlockData.buildingName || "",
                userId: unlockData.userId || "",
                type: "building_unlocked"
            },
            android: {
                notification: {
                    icon: "ic_notification",
                    color: "#FF9800", // Orange color for building unlocks
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

        const response = await getMessaging().send(message);
        logger.info("Successfully sent building unlocked notification to 'updates' topic", {
            messageId: response,
            unlockId: unlockId,
            buildingName: unlockData.buildingName,
            userId: unlockData.userId
        });

        return response;

    } catch (error) {
        logger.error('Error sending building unlocked notification', {
            error: error.message,
            unlockId: event.params.unlockId
        });
        throw error;
    }
});

/**
 * Notify all users when someone reaches a higher rank
 * Triggers when a user document is updated in the users collection
 * Monitors changes to knowledgePoints and coins to detect rank changes
 */
exports.notifyRankAchievement = onDocumentUpdated("users/{userId}", async (event) => {
    try {
        const beforeData = event.data.before.data();
        const afterData = event.data.after.data();
        const userId = event.params.userId;
        const userEmail = afterData.email || '';
        
        // Skip notification for guest users with @ramroutes.com emails
        if (userEmail.endsWith('@ramroutes.com')) {
            logger.info("Skipping rank achievement notification for guest user", {
                userId: userId,
                email: userEmail
            });
            return null;
        }
        
        // Get point values before and after the update
        const beforeCoins = beforeData.coins || 0;
        const beforeKnowledgePoints = beforeData.knowledgePoints || 0;
        const beforeTotalPoints = beforeCoins + beforeKnowledgePoints;
        
        const afterCoins = afterData.coins || 0;
        const afterKnowledgePoints = afterData.knowledgePoints || 0;
        const afterTotalPoints = afterCoins + afterKnowledgePoints;
        
        // Calculate ranks using the same logic as GetUserAvatarBasedOnPoints
        const calculateRank = (totalPoints) => {
            const weightedScore = (0.7 * totalPoints.knowledgePoints) + (0.3 * totalPoints.coins);
            
            if (weightedScore >= 3000) {
                return 3;
            } else if (weightedScore >= 1500) {
                return 2;
            } else {
                return 1;
            }
        };
        
        const beforeRank = calculateRank(beforeTotalPoints);
        const afterRank = calculateRank(afterTotalPoints);
        
        // Only send notification if rank increased
        if (afterRank > beforeRank) {
            const userName = afterData.name || 'A player';
            const rankNames = {
                0: 'Beginner',
                1: 'Gold',
                2: 'Silver',
                3: 'Platinum'
            };
            
            logger.info("User achieved higher rank", {
                userId: userId,
                userName: userName,
                beforeRank: beforeRank,
                afterRank: afterRank,
                beforeTotalPoints: beforeTotalPoints,
                afterTotalPoints: afterTotalPoints,
                rankName: rankNames[afterRank]
            });

            // Send notification to all users subscribed to 'updates' topic
            const message = {
                topic: 'updates',
                notification: {
                    title: `🏆 ${userName} Reached ${rankNames[afterRank]} Rank!`,
                    body: `🎉 Amazing achievement! ${userName} just leveled up to ${rankNames[afterRank]} rank with an impressive ${afterTotalPoints} points! Who's next? 🚀`
                },
                data: {
                    userId: userId,
                    userName: userName,
                    newRank: afterRank.toString(),
                    rankName: rankNames[afterRank],
                    totalPoints: afterTotalPoints.toString(),
                    coins: afterCoins.toString(),
                    knowledgePoints: afterKnowledgePoints.toString(),
                    type: "rank_achievement"
                },
                android: {
                    notification: {
                        icon: "ic_notification",
                        color: "#FFD700", // Gold color for rank achievements
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

            const response = await getMessaging().send(message);
            logger.info("Successfully sent rank achievement notification to 'updates' topic", {
                messageId: response,
                userId: userId,
                userName: userName,
                newRank: afterRank,
                rankName: rankNames[afterRank],
                totalPoints: afterTotalPoints
            });

            return response;
        } else {
            // No rank change, log for debugging but don't send notification
            logger.info("User points updated but no rank change", {
                userId: userId,
                userName: afterData.name,
                beforeRank: beforeRank,
                afterRank: afterRank,
                beforeTotalPoints: beforeTotalPoints,
                afterTotalPoints: afterTotalPoints
            });
            
            return null;
        }

    } catch (error) {
        logger.error('Error processing rank achievement notification', {
            error: error.message,
            userId: event.params.userId
        });
        throw error;
    }
});