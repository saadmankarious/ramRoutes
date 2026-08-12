const express = require("express");
const path = require("path");
const { scrapeEvents } = require("./scrape");
const { matchAll } = require("./matcher");

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

// "Saturday, July 18 at 2:30PM EDT" → Date (infers year)
function parseEventDate(dateStr) {
  if (!dateStr) return null;
  const match = dateStr.match(
    /([A-Za-z]+)\s+(\d+)\s+at\s+(\d+:\d+(?:AM|PM))\s*([A-Z]+)/i
  );
  if (!match) return null;
  const [, month, day, time, tz] = match;
  const now = new Date();
  // Try current year; if it lands >30 days in the future, use last year
  // (we're scraping past events so they shouldn't be far in the future)
  let date = new Date(`${month} ${day}, ${now.getFullYear()} ${time} ${tz}`);
  if (isNaN(date)) return null;
  if (date.getTime() > now.getTime() + 30 * 24 * 60 * 60 * 1000) {
    date = new Date(`${month} ${day}, ${now.getFullYear() - 1} ${time} ${tz}`);
  }
  return isNaN(date) ? null : date;
}

function reconcileEvent(event, building) {
  const parsedDate = parseEventDate(event.date);
  return {
    // Firestore document fields
    buildingId: generateBuildingId(),
    buildingName: building.buildingName,
    schoolId: building.schoolId || "",
    schoolName: building.schoolName || "",
    eventName: event.name,
    eventType: "scheduled",
    date: parsedDate,
    description: event.description || "",
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
      ...(!parsedDate ? ["unparseable date"] : []),
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
          dateRaw: r._raw.dateRaw,
          dateParsed: r.date ? r.date.toISOString() : null,
          description: r.description ? r.description.slice(0, 80) : null,
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

const PORT = 3001;
app.listen(PORT, () => console.log(`Scraper UI → http://localhost:${PORT}`));
