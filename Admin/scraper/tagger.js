const TAGS = {
  academic:  ["study", "research", "lecture", "workshop", "seminar", "tutoring", "thesis", "gre", "lsat", "academic", "exam", "quiz"],
  sports:    ["game", "match", "tournament", "intramural", "practice", "volleyball", "soccer", "basketball", "lacrosse", "tennis", "swim", "sport", "athlete"],
  arts:      ["concert", "performance", "dance", "art", "exhibit", "theater", "tango", "music", "gallery", "recital", "show", "play", "improv", "choir", "orchestra"],
  social:    ["mixer", "social", "hangout", "networking", "party", "welcome", "gathering", "meetup", "happy hour", "bonding", "celebration", "reunion"],
  food:      ["food", "tasting", "dining", "cook", "bake", "cuisine", "restaurant", "eat", "meal", "snack", "potluck", "brunch"],
  career:    ["career", "internship", "job", "resume", "interview", "professional", "employer", "recruiting", "hiring", "industry"],
  cultural:  ["culture", "heritage", "international", "diversity", "identity", "pride", "tradition", "multicultural", "global", "awareness"],
  spiritual: ["prayer", "faith", "chapel", "meditation", "spiritual", "mass", "worship", "religious", "interfaith", "dharma"],
  tech:      ["hack", "hackathon", "code", "programming", "software", "ai", "data", "esports", "gaming", "cybersecurity", "robotics", "stem"],
  fitness:   ["workout", "run", "hike", "yoga", "gym", "exercise", "wellness", "cardio", "strength", "fitness", "bootcamp"],
};

// Returns array of matching tag strings for a given event
function tagEvent({ eventName = "", description = "" }) {
  const text = `${eventName} ${description}`.toLowerCase();
  return Object.entries(TAGS)
    .filter(([, keywords]) => keywords.some((kw) => text.includes(kw)))
    .map(([tag]) => tag);
}

module.exports = { tagEvent, TAGS };
