const express = require("express");
const path = require("path");
const { scrapeEvents } = require("./scrape");
const { matchAll } = require("./matcher");

// Firebase Admin — requires Admin/serviceAccountKey.json
let db = null;
try {
  const admin = require("firebase-admin");
  const serviceAccount = require("../serviceAccountKey.json");
  admin.initializeApp({ credential: admin.credential.cert(serviceAccount) });
  db = admin.firestore();
  console.log("Firebase Admin initialized.");
} catch {
  console.warn(
    "Warning: serviceAccountKey.json not found — /api/match will not load buildings from Firestore."
  );
}

const app = express();
app.use(express.json());
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
  if (!Array.isArray(events)) {
    return res.status(400).json({ error: "events must be an array" });
  }
  if (!db) {
    return res
      .status(503)
      .json({ error: "Firebase not configured — add serviceAccountKey.json to Admin/" });
  }

  try {
    // Re-require keywords.json each time so edits take effect without restart
    delete require.cache[require.resolve("./keywords.json")];
    const snap = await db.collection("buildings").get();
    const buildings = snap.docs.map((doc) => ({ id: doc.id, ...doc.data() }));
    const result = matchAll(events, buildings);
    res.json({ ...result, buildingCount: buildings.length });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: err.message });
  }
});

const PORT = 3001;
app.listen(PORT, () => console.log(`Scraper UI → http://localhost:${PORT}`));
