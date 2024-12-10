import json
import argparse
from collections import defaultdict
import statistics

def calculate_statistics(dataset_file, condition_name):
    # Initialize counters and storage
    user_texts = defaultdict(list)
    total_sentences = 0
    total_words = 0

    with open(dataset_file, 'r', encoding='utf-8') as file:
        for line in file:
            try:
                # Parse each JSON line
                data = json.loads(line)
                username = data.get("username")
                posts = data.get("posts", [])
                
                if username and posts:
                    for post in posts:
                        selftext = post.get("selftext", "").strip()
                        if selftext:
                            # Group texts by user
                            user_texts[username].append(selftext)
                            
                            # Calculate sentence and word counts for the current text
                            sentences = selftext.split('.')
                            words = selftext.split()
                            total_sentences += len(sentences)
                            total_words += len(words)
            
            except json.JSONDecodeError:
                print(f"Skipping invalid JSON line: {line.strip()}")
    
    # Calculate overall statistics
    n_unique_users = len(user_texts)
    total_texts = sum(len(texts) for texts in user_texts.values())
    mean_texts_per_user = total_texts / n_unique_users if n_unique_users else 0
    mean_sentences_per_text = total_sentences / total_texts if total_texts else 0
    mean_words_per_text = total_words / total_texts if total_texts else 0

    # Print results
    print(f"Condition: {condition_name}")
    print(f"N Unique Users: {n_unique_users}")
    print(f"Total N Texts: {total_texts}")
    print(f"Total N Sentences: {total_sentences}")
    print(f"Total N Words: {total_words}")
    print(f"Mean N Texts per User: {mean_texts_per_user:.2f}")
    print(f"Mean N Sentences per Text: {mean_sentences_per_text:.2f}")
    print(f"Mean N Words per Text: {mean_words_per_text:.2f}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Analyze user statistics from a .jl dataset.")
    parser.add_argument("dataset_file", help="Path to the .jl dataset file.")
    parser.add_argument("condition_name", help="Name of the condition (e.g., Condition 1).")
    args = parser.parse_args()
    
    calculate_statistics(args.dataset_file, args.condition_name)
