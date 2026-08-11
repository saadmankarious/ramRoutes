// keywords.json maps buildingName → array of keyword strings
// A scraped location matches a building if it contains ANY of its keywords (case-insensitive)
const keywordsMap = require("./keywords.json");

function matchAll(events, buildings) {
  const groups = {};
  buildings.forEach((b) => {
    groups[b.id] = {
      buildingId: b.id,
      buildingName: b.buildingName,
      schoolName: b.schoolName || "",
      matched: [],
    };
  });

  const unmatched = [];

  events.forEach((event) => {
    const loc = (event.location || "").toLowerCase();
    if (!loc) {
      unmatched.push(event);
      return;
    }

    // Find the first building whose keywords appear in the scraped location
    let matchedBuilding = null;
    let matchedKeyword = null;

    for (const building of buildings) {
      const keywords = keywordsMap[building.buildingName] || [];
      for (const kw of keywords) {
        if (loc.includes(kw.toLowerCase())) {
          matchedBuilding = building;
          matchedKeyword = kw;
          break;
        }
      }
      if (matchedBuilding) break;
    }

    if (matchedBuilding) {
      groups[matchedBuilding.id].matched.push({
        ...event,
        matchedKeyword,
      });
    } else {
      unmatched.push(event);
    }
  });

  return {
    buildings: Object.values(groups).filter((g) => g.matched.length > 0),
    unmatched,
  };
}

module.exports = { matchAll };
