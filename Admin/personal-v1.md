# Personalization v1 — Event Recommendations

## Goal
Suggest relevant building events to students based on their stated interests and their friends' activity. Simple, no ML, no weighted maps.

## How It Works

```
event.tags[]  ∩  user.interests[]  →  score  →  user.recommendedEvents[]
```

Score for an event = number of overlapping tags + 1 if any friend has RSVPed.  
Top 10 events written nightly to each user's Firestore document.

---

## Data Model Changes

### `building-events` — add field
```
tags: ["social", "arts"]   // added at scrape/upload time by tagger
```

### `users` — add field
```
interests: ["social", "arts", "sports"]  // set during signup
recommendedEvents: ["eventId1", "eventId2", ...]  // written nightly by Cloud Function
```

---

## Tag Taxonomy

| Tag | Matches keywords in event name/description |
|---|---|
| `academic` | study, research, lecture, workshop, seminar, tutoring, thesis, gre, lsat |
| `sports` | game, match, tournament, intramural, practice, volleyball, soccer, basketball, lacrosse |
| `arts` | concert, performance, dance, art, exhibit, theater, tango, music, gallery, recital |
| `social` | mixer, social, hangout, networking, party, welcome, gathering, meetup |
| `food` | food, tasting, dining, cook, bake, cuisine, restaurant, eat |
| `career` | career, internship, job, resume, interview, professional, employer, recruiting |
| `cultural` | culture, heritage, international, diversity, identity, pride, tradition, multicultural |
| `spiritual` | prayer, faith, chapel, meditation, spiritual, mass, worship |
| `tech` | hack, hackathon, code, programming, software, ai, data, esports, gaming |
| `fitness` | workout, run, hike, yoga, gym, exercise, wellness, cardio |
| `mental-health` | mental health, counseling, therapy, self-care, stress relief, anxiety, depression, mindfulness, meditation, wellbeing, support group |

---

## Implementation

### Phase 1 — Tag events at upload (scraper)
**Files:** `Admin/scraper/tagger.js` (new), `Admin/scraper/server.js` (updated)

- `tagger.js` exports `tagEvent({ eventName, description }) → string[]`
- Called inside `reconcileEvent()` in `server.js`
- `tags[]` written to Firestore alongside every uploaded event

### Phase 2 — Collect interests at signup (Unity)
**Files:** New signup screen step in Unity

- After residence hall selection, show multi-select interest picker
- Writes `interests[]` to `users/{uid}` on Firestore

### Phase 3 — Nightly recommendation batch (Cloud Function)
**Files:** `Admin/new-functions/index.js` (append)

- Scheduled function `computeRecommendations` runs every 24 hours
- Reads all users with `interests.length > 0`
- For each user: score all tagged events, take top 10, write to `user.recommendedEvents`
- Scoring: `tagOverlap + (anyFriendInterested ? 1 : 0)`

### Phase 4 — Surface in Unity (Unity)
**Files:** New `RecommendationService.cs`, new UI panel in daily events scene

- Read `recommendedEvents[]` from user doc → fetch event documents
- Display as "For You" section above daily events

---

## Scoring Formula

```
for each event with tags:
  score = count(event.tags ∩ user.interests)

recommendedEvents = top 10 events by score where score > 0
```

---

## Implementation — all in `Admin/scraper/`

| File | Role |
|---|---|
| `tagger.js` | `tagEvent({ eventName, description }) → tags[]` |
| `recommend.js` | reads Firestore, scores events per user, writes `recommendedEvents[]` |
| `server.js` | exposes `/api/recommend` SSE endpoint |
| `index.html` | "Run Recommendations" button (Step 4) |

Run standalone: `node recommend.js`
Run via UI: click **Run Recommendations** at `http://localhost:3001`

---

## What Is Not In v1
- No behavioral weights
- No NLP or embeddings
- No real-time scoring (manual trigger or cron)
- No Unity UI (phase 4 deferred)
