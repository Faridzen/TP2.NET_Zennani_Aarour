import csv
import re

# Read the original CSV
input_file = '/Users/mouna/Desktop/M2 MIAGE/Net/Projet/TP2.NET_Zennani_Aarour/Gauniv.WebServer/Data/games_original.csv'
output_file = '/Users/mouna/Desktop/M2 MIAGE/Net/Projet/TP2.NET_Zennani_Aarour/Gauniv.WebServer/Data/games.csv'

with open(input_file, 'r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    
    games = []
    for row in reader:
        try:
            # Extract required fields
            title = row['name'].strip() if row['name'] else ''
            description = row['desc_snippet'].strip() if row['desc_snippet'] else ''
            
            # Clean description - remove extra quotes and newlines
            description = description.replace('"', '').replace('\n', ' ').replace('\r', ' ')
            
            # Parse price
            price_str = row['original_price'].replace('$', '').replace(',', '').strip()
            if price_str and price_str != 'Free to Play' and price_str != 'Free':
                try:
                    price = float(price_str)
                except:
                    price = 0.0
            else:
                price = 0.0
            
            # Extract categories from popular_tags (first 3 tags)
            tags = row['popular_tags'].split(',')[:3] if row['popular_tags'] else []
            categories = ','.join([tag.strip() for tag in tags if tag.strip()])
            if not categories:
                categories = 'Action'
            
            # Generate image URL from header image pattern
            game_id = re.search(r'/app/(\d+)/', row['url'])
            if game_id:
                image_url = f"https://cdn.cloudflare.steamstatic.com/steam/apps/{game_id.group(1)}/header.jpg"
            else:
                image_url = "https://picsum.photos/seed/game/400/200"
            
            games.append({
                'Title': title,
                'Description': description,
                'Price': f"{price:.2f}",
                'Categories': categories,  # Remove extra quotes
                'ImageUrl': image_url
            })
            
            if len(games) >= 100:  # Limit to 100 games for now
                break
                
        except Exception as e:
            print(f"Error processing row: {e}")
            continue

# Write the converted CSV
with open(output_file, 'w', encoding='utf-8', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=['Title', 'Description', 'Price', 'Categories', 'ImageUrl'])
    writer.writeheader()
    writer.writerows(games)

print(f"Converted {len(games)} games to {output_file}")
