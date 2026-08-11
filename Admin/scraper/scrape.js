const { chromium } = require("playwright");
const fs = require("fs");
const path = require("path");

const EVENTS_URL =
  "https://gwu.campuslabs.com/engage/events?showpastevents=true";
const OUTPUT_FILE = path.join(__dirname, "events.json");
const MAX_LOAD_MORE = 5;

async function scrapeEvents({ onProgress } = {}) {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  onProgress?.("Navigating to GWU events page...");
  await page.goto(EVENTS_URL, { waitUntil: "networkidle", timeout: 60000 });

  let clicks = 0;
  while (clicks < MAX_LOAD_MORE) {
    const btn = page.locator('button:has-text("Load more")');
    const visible = await btn.isVisible().catch(() => false);
    if (!visible) break;

    clicks++;
    onProgress?.(`Clicking "Load more" (${clicks})...`);
    await btn.click();
    await page.waitForTimeout(2000);
    await page.waitForLoadState("networkidle").catch(() => {});
  }

  onProgress?.("Parsing events...");

  const events = await page.evaluate(() => {
    const cards = document.querySelectorAll('a[href^="/engage/event/"]');
    const results = [];

    cards.forEach((card) => {
      const nameEl = card.querySelector("h3");
      const name = nameEl ? nameEl.textContent.trim() : null;

      const dateEl = card.querySelector('[aria-label^="happening on"]');
      const dateRaw = dateEl ? dateEl.getAttribute("aria-label") : null;
      const date = dateRaw ? dateRaw.replace(/^happening on\s*/i, "") : null;

      const locationEl = card.querySelector('[aria-label^="located at"]');
      const locationRaw = locationEl
        ? locationEl.getAttribute("aria-label")
        : null;
      const location = locationRaw
        ? locationRaw.replace(/^located at\s*/i, "")
        : null;

      const imgDiv = card.querySelector('[role="img"]');
      let imageUrl = null;
      if (imgDiv) {
        const m = imgDiv.style.backgroundImage.match(/url\("?([^")\s]+)"?\)/);
        if (m) imageUrl = m[1];
      }

      const eventUrl =
        "https://gwu.campuslabs.com" + card.getAttribute("href");

      if (name) results.push({ name, date, location, imageUrl, eventUrl });
    });

    return results;
  });

  await browser.close();
  return events;
}

// Standalone mode
if (require.main === module) {
  scrapeEvents({ onProgress: console.log })
    .then((events) => {
      fs.writeFileSync(OUTPUT_FILE, JSON.stringify(events, null, 2));
      console.log(`Scraped ${events.length} events → ${OUTPUT_FILE}`);
    })
    .catch((err) => {
      console.error("Scraper failed:", err);
      process.exit(1);
    });
}

module.exports = { scrapeEvents };
