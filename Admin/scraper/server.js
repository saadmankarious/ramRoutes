const express = require("express");
const path = require("path");
const fs = require("fs");
const { scrapeEvents, timestampedEventsPath } = require("./scrape");
const { matchAll } = require("./matcher");
const { tagEvent, TAGS } = require("./tagger");
const { computeRecommendations } = require("./recommend");

// Firebase Admin — requires Admin/serviceAccountKey.json
let db = null;
let FieldValue = null;
try {
  const admin = require("firebase-admin");
  const serviceAccount = require("../serviceAccountKey.json");
  admin.initializeApp({ credential: admin.credential.cert(serviceAccount) });
  db = admin.firestore();
  FieldValue = admin.firestore.FieldValue;
  console.log("Firebase Admin initialized.");
} catch {
  console.warn(
    "Warning: serviceAccountKey.json not found — Firestore endpoints unavailable."
  );
}

// --- Utilities ---

function generateBuildingId() {
  const chars = "abcdefghijklmnopqrstuvwxyz0123456789";
  let id = "BE";
  for (let i = 0; i < 8; i++) {
    id += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return id;
}


function reconcileEvent(event, building) {
  return {
    // Firestore document fields
    buildingId: generateBuildingId(),
    buildingName: building.buildingName,
    schoolId: building.schoolId || "",
    schoolName: building.schoolName || "",
    eventName: event.name,
    eventType: "scheduled",
    date: event.date || null,
    description: event.description || "",
    tags: tagEvent({ eventName: event.name, description: event.description }),
    gainedCoins: 0,
    gainedKb: 0,
    imageUrl: event.imageUrl || null,
    eventUrl: event.eventUrl,
    attendees: [],
    interestedUsers: [],
    createdBy: "scraper",
    // Reconciliation metadata (not uploaded)
    _raw: {
      name: event.name,
      dateRaw: event.date,
      location: event.location,
      matchedKeyword: event.matchedKeyword,
    },
    _issues: [
      ...(!event.description ? ["no description"] : []),
      ...(!event.imageUrl ? ["no image"] : []),
    ],
  };
}

// --- Express setup ---

const app = express();
app.use(express.json({ limit: "10mb" }));
app.use(express.static(__dirname));

app.get("/", (req, res) => {
  res.sendFile(path.join(__dirname, "index.html"));
});

// Stream scrape progress via SSE
app.get("/api/scrape", async (req, res) => {
  res.setHeader("Content-Type", "text/event-stream");
  res.setHeader("Cache-Control", "no-cache");
  res.setHeader("Connection", "keep-alive");

  const send = (type, payload) => {
    res.write(`data: ${JSON.stringify({ type, payload })}\n\n`);
  };

  try {
    const events = await scrapeEvents({
      onProgress: (msg) => send("progress", msg),
    });

    const outputFile = timestampedEventsPath();
    fs.writeFileSync(outputFile, JSON.stringify(events, null, 2));
    send("progress", `Saved ${events.length} events → ${path.basename(outputFile)}`);

    send("done", events);
  } catch (err) {
    send("error", err.message);
  } finally {
    res.end();
  }
});

// Load buildings from Firestore and compute keyword matches
app.post("/api/match", async (req, res) => {
  const { events } = req.body;
  if (!Array.isArray(events))
    return res.status(400).json({ error: "events must be an array" });
  if (!db)
    return res.status(503).json({ error: "Firebase not configured." });

  try {
    delete require.cache[require.resolve("./keywords.json")];
    const snap = await db.collection("buildings").get();
    const buildings = snap.docs.map((doc) => ({ id: doc.id, ...doc.data() }));
    const result = matchAll(events, buildings);

    const kw = require("./keywords.json");
    res.json({
      ...result,
      buildingCount: buildings.length,
      buildingDebug: buildings.map((b) => ({
        id: b.id,
        buildingName: b.buildingName,
        keywordCount: (
          kw[b.id] ||
          kw[b.buildingName] ||
          kw[(b.id || "").toLowerCase()] ||
          kw[(b.buildingName || "").toLowerCase()] ||
          []
        ).length,
      })),
    });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: err.message });
  }
});

// Reconcile matched events and optionally upload to Firestore
// POST /api/upload  body: { buildings: [...matchAll result] }
// query: ?preview=true  → returns reconciled data without uploading
app.post("/api/upload", async (req, res) => {
  if (!db) return res.status(503).json({ error: "Firebase not configured." });

  const { buildings } = req.body;
  if (!Array.isArray(buildings))
    return res.status(400).json({ error: "buildings must be an array" });

  const preview = req.query.preview === "true";

  try {
    // Fetch all existing eventUrls to detect duplicates
    const existingSnap = await db
      .collection("building-events")
      .where("eventUrl", "!=", null)
      .select("eventUrl")
      .get();
    const existingUrls = new Set(
      existingSnap.docs.map((d) => d.data().eventUrl).filter(Boolean)
    );

    const reconciled = [];

    for (const group of buildings) {
      const building = {
        buildingName: group.buildingName,
        schoolId: group.schoolId || "",
        schoolName: group.schoolName || "",
      };

      for (const event of group.matched) {
        const rec = reconcileEvent(event, building);
        rec._duplicate = existingUrls.has(event.eventUrl);
        reconciled.push(rec);
      }
    }

    if (preview) {
      // Strip non-serialisable values and return for UI review
      return res.json({
        total: reconciled.length,
        ready: reconciled.filter((r) => r._issues.length === 0 && !r._duplicate).length,
        duplicates: reconciled.filter((r) => r._duplicate).length,
        withIssues: reconciled.filter((r) => r._issues.length > 0 && !r._duplicate).length,
        events: reconciled.map((r) => ({
          eventName: r.eventName,
          buildingName: r.buildingName,
          date: r.date,
          description: r.description ? r.description.slice(0, 80) : null,
          tags: r.tags,
          imageUrl: r.imageUrl,
          eventUrl: r.eventUrl,
          issues: r._issues,
          duplicate: r._duplicate,
        })),
      });
    }

    // Upload: skip duplicates, upload the rest
    let uploaded = 0;
    let skipped = 0;
    const errors = [];

    for (const rec of reconciled) {
      if (rec._duplicate) { skipped++; continue; }

      const doc = {
        buildingId: rec.buildingId,
        buildingName: rec.buildingName,
        schoolId: rec.schoolId,
        schoolName: rec.schoolName,
        eventName: rec.eventName,
        eventType: rec.eventType,
        date: rec.date || null,
        description: rec.description,
        tags: rec.tags,
        gainedCoins: rec.gainedCoins,
        gainedKb: rec.gainedKb,
        imageUrl: rec.imageUrl,
        eventUrl: rec.eventUrl,
        attendees: rec.attendees,
        interestedUsers: rec.interestedUsers,
        createdBy: rec.createdBy,
        createdAt: FieldValue.serverTimestamp(),
      };

      try {
        await db.collection("building-events").add(doc);
        uploaded++;
      } catch (err) {
        errors.push({ event: rec.eventName, error: err.message });
      }
    }

    res.json({ uploaded, skipped, errors });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: err.message });
  }
});

// Run recommendation computation, stream progress via SSE
app.get("/api/recommend", async (req, res) => {
  res.setHeader("Content-Type", "text/event-stream");
  res.setHeader("Cache-Control", "no-cache");
  res.setHeader("Connection", "keep-alive");

  const send = (type, payload) => {
    res.write(`data: ${JSON.stringify({ type, payload })}\n\n`);
  };

  try {
    const result = await computeRecommendations({
      onProgress: (msg) => send("progress", msg),
    });
    send("done", result);
  } catch (err) {
    send("error", err.message);
  } finally {
    res.end();
  }
});

// --- Manual tagging eval sample (used by annotate.html) ---

const EVAL_SAMPLE_FILE = path.join(__dirname, "eval-sample.json");

app.get("/api/eval-sample", (req, res) => {
  if (!fs.existsSync(EVAL_SAMPLE_FILE)) {
    return res.status(404).json({
      error: "eval-sample.json not found. Run: node build-eval-sample.js",
    });
  }
  const events = JSON.parse(fs.readFileSync(EVAL_SAMPLE_FILE, "utf8"));
  res.json({ tags: Object.keys(TAGS), events });
});

app.post("/api/eval-sample", (req, res) => {
  const { events } = req.body;
  if (!Array.isArray(events)) {
    return res.status(400).json({ error: "events must be an array" });
  }
  fs.writeFileSync(EVAL_SAMPLE_FILE, JSON.stringify(events, null, 2));
  res.json({ saved: events.length });
});

const PORT = 3001;
app.listen(PORT, () => console.log(`Scraper UI → http://localhost:${PORT}`));
