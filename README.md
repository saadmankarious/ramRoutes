# Trials of Venus!

I aim to develop a 2D video game called *Trials Of Venus*, based on an adaptation of Greek mythology set at Cornell in the 21st century. The game aims to raise awareness about pressing environmental issues in a fun, mythical way.  

## Alpha Release
### Teleportation (Week 1)
This feature enables the player to travel instantly from one point in the level to another. We introduced this feature for two reasons: by the fourth level, the platform gets very long and it gets boring if the player walks from one end to the other and because it looks fun! 
There are three pieces into this feature:
1. The player walks into one of the four designated teleport station (positioned around Jupyter) and a message appears to prompt them to teleport
2. An animation that plays at the spawn and destination slot 
3. Shaking of the camera to create dynamic effect
   
### Level Transition (Week 2)
The whole game is on one unity Scene, one level essentially. However, this one level starts small but expands as the player advances through the four trials. To implement this transition and platform expansion, I extend the camera frame through swithcing the Cinemachineconfiner component while syncing this with the teleportation effect.

### Spaceship (Week 2)
We are currently working on implementing another feature that helps players navigate the game level easily. This is a spaceship concept that the player sits on thinly able to move from one end of the game to another. This feature is crucial because it expands the game from one dimensional platform to two dimensional. We finalized the design for the spaceship I still need to work on implementing the logic for it.

## Challenges
### Making player visible at end of level
One big challenge I faced this week was to stop Player movement at the end of the CineMachineConfiner (camera) to limit them from advancing to next level until they finish the task specified. I was able to make the player stop moving by detecting the interaction between the player sprite and the frame of the camera. However, the player will be invisible by the time they stop. I tried including a padding between the camera and the border of detection but it did not work. I also tried  
### Player can teleport from anywhere
I had the teleport feature configured so that when the player hits the T key, teleportation begins. However, this will happen even if the player was not at one of the designated teleport stations. To fix this, I created an enum and instantiated from it two objects to track where the player location and movement then overrided the methods that detect colision with the teleport stations:
```csharp
void OnTriggerEnter2D(Collider2D other)
{
    if(other.CompareTag("Player")){
        currentCollidedStation = teleportStation;
        ShowDialog("You are now on " + teleportStation + ". Hit T to time travel!", other);
    }
}

void OnTriggerExit2D(Collider2D other)
{
    if(other.CompareTag("Player")){
        currentCollidedStation = null;
        HideDialog();
    }
}
```


## Interesting Code
### Teleportation
```csharp
private IEnumerator TeleportWithDelay()
{
    int switchIndex = GetNextConfinerIndex();
    switchConfiner.SwitchToConfiner(switchIndex);

    // Play teleport effect (particles) at the current location
    if (teleportEffect != null)
    {
        Instantiate(teleportEffect, player.transform.position, Quaternion.identity);
    }

    // Play teleport sound
    if (teleportSound != null)
    {
        AudioSource.PlayClipAtPoint(teleportSound, player.transform.position);
    }

    // Shake the camera using Cinemachine Impulse
    if (impulseSource != null)
    {
        impulseSource.GenerateImpulse();
        Debug.Log("generated impulse");
    }

    // Wait for the specified delay
    yield return new WaitForSeconds(teleportDelay);

    // Teleport the player to the target location
    player.transform.position = targetLocation.position;

    // Play teleport effect at the new location
    if (teleportEffect != null)
    {
        Instantiate(teleportEffect, targetLocation.position, Quaternion.identity);
    }
}

```
### Confiner Switching
```csharp
if (index >= 0 && index < confiners.Length && confiners[index] != null)
{
   Debug.Log("Switching confiner to index: " + index + " (" + confiners[index].name + ")");

   // Assign the new Collider2D to the CinemachineConfiner's Bounding Shape 2D
   confiner.m_BoundingShape2D = confiners[index];

   // Refresh the Cinemachine Confiner
   confiner.InvalidatePathCache();

   // Update the current confiner index
   currentConfinerIndex = index;
}
else
{
   Debug.LogWarning("Invalid confiner index or confiner is null!");
}
```
### Stop player at edge of level
```csharp
if (boundary.OverlapPoint(newPosition))
{
    move.x = inputX; // Allow movement
    Debug.Log("overlaping now");
}
else
{
    // Optional: Try sliding along walls
    Vector2 edgePosition = boundary.ClosestPoint(newPosition);
    if (Mathf.Abs(edgePosition.x - transform.position.x) > 0.05f)
    {
        move.x = inputX * 0.3f; // Reduced speed when sliding
    }
    else
    {
        move.x = 0; // Stop completely when directly against wall
    }
}
```



## Beta Release
### Improve Play UI
Added stats that track progress towards finishing the game including indicator of current trial and knowldge points. I also adde directions to remind player of mission objective.

### Spaceship
I finalized implementation of spaceship feature including animation and navigation. Upon hitting the E key, the spaceship finds the player location and moves towards it. The player gets picked up autmatically by the space ship before ascending to space. The spaceship then waits for player's inputs to move along the X axis.

## Project Organization
All assets for creating the game are included under the Assets folder. Scripts are organized under the Assets/Scripts subfolder. The game has three Scenes, LevelFall (containing all 4 trials), Landing, and Onboarding.

## Challenges
After pitching the game to a couple people, I realzied the objective of the game is not clear to the player at the beginning of the game. I consulted with my parter to see ways to fix this. We decided to enhance the UI and add extra elements to resolve this issue. 

We also worked on setting fixed objectives for each trial. For example to pass trial one, 20 pieces of trash need to be collected.

## Interesting Code
```csharp
private IEnumerator TypeText(string message, float activeFor)
 {
     dialogText.text = ""; // Clear previous text
     
     foreach (char letter in message.ToCharArray())
     {
         dialogText.text += letter; // Add one character at a time
         yield return new WaitForSeconds(typingSpeed); // Pause between letters
     }
     
     // Auto-hide after delay (if activeFor > 0)
     if (activeFor > 0)
     {
         yield return new WaitForSeconds(activeFor);
         HideDialog();
     }
 }
```


Let me explore the project thoroughly first. 

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts)

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model)

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Services)

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Gameplay)

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/View)

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/User.cs#1-1), lines 1 to 80

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/Chat.cs#1-1), lines 1 to 50

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/ShoutOut.cs)

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/BuildingEvent.cs#1-1), lines 1 to 50

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/FriendRequest.cs#1-1), lines 1 to 50

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/AttendanceRecord.cs#1-1), lines 1 to 50

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/ChatManager.cs#1-1), lines 1 to 100

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/FriendsManager.cs#1-1), lines 1 to 80

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Gameplay/BuildingInteraction.cs#1-1), lines 1 to 100

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/NpcAutoMovement.cs#1-1), lines 1 to 80

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/DailyEvents.cs#1-1), lines 1 to 60

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Onboarding.cs#1-1), lines 1 to 60

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/StoreManager.cs#1-1), lines 1 to 100

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/GameManager.cs#1-1), lines 1 to 100

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/GameStage.cs#1-1), lines 1 to 40

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/InventoryItem.cs)

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/NotificationManager.cs#1-1), lines 1 to 50

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/LoginManager.cs#1-1), lines 1 to 50

Read [](file:///home/oem/code/ramroutes/ramroutes/README.md#1-1), lines 1 to 60

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/onboarding-story.md)

Read [](file:///home/oem/code/ramroutes/ramroutes/Assets/Scripts/Model/Points.cs)

Searched text for `class.*Leaderboard|class.*Rank|GetLeaderboard|GetTopPlayers` (`**/Assets/Scripts/**`), no results

Read [](file:///home/oem/code/ramroutes/ramroutes/Admin/src)

Read [](file:///home/oem/code/ramroutes/ramroutes/Admin/src/components)

I now have a comprehensive picture. Here's my report:

---

# RamRoutes (Trials of Venus) — Project Report & MVP Strategy

## What This Is

A **2D RPG mobile game** built in Unity, set on a real college campus (Cornell). Players control a **Ram** character navigating a pixel-art version of campus. The world is mapped to **real campus buildings** tied to GPS coordinates. The narrative frames the journey through Greek mythology — the Ram searching for scattered pieces of Aros' soul across campus locations.

**Backend:** Firebase (Auth, Firestore, Cloud Messaging)  
**Admin Panel:** React web app for managing schools, buildings, events, store items  
**Platforms:** iOS (with native background location plugin), likely Android

---

## Current Feature Inventory

### ✅ Core Identity & Navigation
| Feature | Status | Notes |
|---|---|---|
| Firebase Auth (email/password) | Complete | Login, signup, password reset |
| Campus map as 2D RPG world | Complete | 5 stages: EasternCampus, FirstStreet, Pedmall, TC, Terminal |
| Building interaction (enter/view) | Complete | Per-building events, player counts, NPC spawning |
| GPS geofencing + background location | Complete | iOS native plugin, local notifications on building proximity |
| Player teleports to physical building on launch | Complete | Maps real GPS to in-game position |
| Stage progression with gates | Complete | Onboarding narrative per stage |
| Mobile joystick (4-direction) | Complete | RPG-style cardinal movement |

### ✅ Student-to-Student Interaction (YOUR CORE)
| Feature | Status | Depth |
|---|---|---|
| **See who's in a building** | Complete | Real-time player count + RAMs (avatars) shown at buildings |
| **Whispers** (preset emoji-like messages) | Complete | Send Greeting, Heart, HaveANiceLift, etc. to other players |
| **Shout-outs** | Complete | Send shout-out to another player (+10 coins, +10 KB to recipient) |
| **Friend requests** | Complete | Send/accept/decline, friends list panel |
| **Footprints** | Complete | Leave a 10-60 char text note on a building, visible to others (newest 3, overlapping cards, pop-in animation) |
| **Chat (whisper-based)** | Complete | 1-to-1 whisper conversations viewable in chat panel |
| **Building entry notifications** | Complete | "X entered Y building — go say hi!" push notifications |

### ✅ Progression & Economy
| Feature | Status |
|---|---|
| Coins + Knowledge Points (KB) currencies | Complete |
| Store (buy skins, whispers, accessories) | Complete |
| Inventory (equip/unequip/sell) | Complete |
| Skins (Default, Rainbow, Summer, Winter, Cat) | Complete |
| Accessories (Torch, Horns) | Complete |
| NPC interactions (conversation + rewards) | Complete |
| Event check-in + attendance tracking | Complete |
| Daily events display | Complete |

### ✅ Admin
| Feature | Status |
|---|---|
| School/Building/Event/StoreItem CRUD | Complete |
| Admin panel (React) | Complete |

---

## MVP Strategy: Maximum Student-to-Student Engagement

### 🎯 THE NORTH STAR
> *"I opened the app because my friend is in the library right now."*

Every feature decision should be: **does this make a student check the app because of another student?**

---

### 🟢 DOUBLE DOWN (Build more here — these are your engagement engines)

#### 1. **Presence & Awareness** — "Who's where right now?"
This is your #1 killer feature. No other app shows you which friends are at which campus building in a playful, non-creepy way. **Build obsessively here:**
- **Live activity indicator on buildings** — pulsing glow / particle count scaled to occupancy
- **Friend-specific notifications** — "Your friend Alex just entered the Library" (not strangers)
- **"Join" button** — one tap to navigate your Ram to a friend's building
- **Building "vibe" status** — auto-generated from occupancy: "empty", "a few people", "packed 🔥"

#### 2. **Footprints → Campus Conversation Wall**
Footprints are your **Twitter for physical spaces**. This is gold. Expand it:
- **React to footprints** — single-tap emoji react (🔥 ❤️ 😂) so it's zero-friction interaction
- **"Hot" footprints** — show most-reacted footprint at top, creates competition for wit
- **Anonymous option** — let students post anonymously to lower inhibition barrier
- **Building-specific prompts** — "What's the vibe at the library tonight?" rotating prompts to seed content

#### 3. **Shout-outs → Public Micro-Appreciation**
Shout-outs are social proof. They make people feel seen. Expand:
- **Public shout-out feed** — visible on the building where it was sent ("Alex shouted out Jordan at the Library")
- **Streak system** — "You and Alex have a 5-day shout-out streak 🔥"
- **"Most loved" leaderboard per building** — who got the most shout-outs this week at Stoner House?

#### 4. **Lightweight Coordination** — "Who wants to..."
The missing piece between "seeing who's where" and actually meeting up:
- **Quick status / intention** — "Studying until 5pm" / "Looking for lunch buddy" / "Down to hang" — one-tap status on your Ram visible to nearby players
- **Building ping** — "I'm heading to the Library" broadcast to friends, they can tap "me too"

---

### 🟡 KEEP BUT DON'T EXPAND (Good enough for MVP)

| Feature | Why keep as-is |
|---|---|
| **Whisper chat** | It works, but don't compete with iMessage/Instagram DMs. Keep it playful preset-only — that's the charm. Don't add free-text chat. |
| **Skins & Store** | Gives a reason to earn coins. Current 5 skins + 2 accessories is plenty for MVP. |
| **NPC conversations** | Fun flavour. Keep the ones you have, don't add more. |
| **Onboarding narrative** | Beautiful writing. Ship what you have. |
| **Event check-ins** | Keep it. One-tap check-in at events is lightweight enough. |
| **Friend system** | Works. Don't add groups/circles complexity yet. |

---

### 🔴 DEPRIORITIZE / EXCLUDE FROM MVP

| Feature | Why cut |
|---|---|
| **Trials/Quests (GameManager trials)** | Single-player PvE content. Doesn't drive student-to-student interaction. Cool for v2 when retention matters, but not for viral adoption. |
| **Complex economy balancing** | Don't agonize over coin/KB rates. Generous > balanced. You want students active, not grinding. |
| **Admin panel polish** | It works. Ship ugly admin. Students never see it. |
| **More stages/maps** | Don't build more map before you've validated engagement on current map. |
| **Accessories/skin variety** | 5 skins is enough. Don't art-pipeline more until you have 500+ DAU. |
| **Leaderboards (global)** | Can feel discouraging for new users. Building-specific micro-leaderboards (shout-out counts) are better. |
| **Free-text chat** | Opens moderation hellscape. Whispers + footprints give enough expression with natural content limits. |
| **Background location logging to Firestore** | You already commented most of it out. Good instinct. Only geofence triggers matter. |

---

### 📐 Engagement Architecture (Priority Order for Next Sprint)

| # | Feature | Effort | Impact | Why |
|---|---|---|---|---|
| 1 | **Friend presence alerts** ("Alex is at Library") | Medium | 🔥🔥🔥 | THE reason to open the app. Turns passive into active. |
| 2 | **Quick status on Ram** ("studying" / "down to hang") | Small | 🔥🔥🔥 | Lowers coordination friction from "I see you're there" to "let's meet up" |
| 3 | **Footprint reactions** (emoji react) | Small | 🔥🔥 | Turns read-only content into interaction. 1-tap = lowest friction social action |
| 4 | **"Join" / "Heading there"** button on friend's building | Small | 🔥🔥 | Closes the loop: see → decide → go |
| 5 | **Public shout-out feed per building** | Small | 🔥🔥 | Social proof, makes buildings feel alive |
| 6 | **Building vibe indicator** (occupancy-based) | Small | 🔥 | Visual pull — "the Library is packed" makes you curious |

---

### 🧠 The Engagement Loop You're Building

```
See friend is somewhere → Read footprints/vibe → Set status "heading there" →
Friend gets notified → You arrive, GPS triggers → You're both in-game at the building →
Send shout-out → Leave footprint → Others see the activity → Loop restarts
```

**The physical-to-digital bridge is your moat.** No other college app makes "being at the same building" feel like a *shared experience*. Lean all the way into that.