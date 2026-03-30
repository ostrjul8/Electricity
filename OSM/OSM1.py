import osmnx as ox
import os
import json

PLACE_NAME = "Kyiv, Ukraine"
OUTPUT_FILE = "kyiv_clean_buildings.json"

BUILDING_TAGS = {"building": True}

INCLUDE_TYPES = [
    'yes',
]

def main():

    try:
        gdf = ox.features_from_place(PLACE_NAME, tags=BUILDING_TAGS)

        if gdf.empty:
            print("Будівель не знайдено.")
            return

        total_found = len(gdf)
        print(f"Знайдено всього об'єктів на карті: {total_found}.")

        gdf_filtered = gdf[gdf['building'].isin(INCLUDE_TYPES)].copy()

        gdf_filtered = gdf_filtered[gdf_filtered.geometry.type.isin(['Polygon', 'MultiPolygon'])]

        print(f"Залишилося будівель після фільтрації: {len(gdf_filtered)}.")

        gdf_final = gdf_filtered.copy()
        for col in gdf_final.columns:
            if col != 'geometry':
                gdf_final[col] = gdf_final[col].apply(lambda x: ', '.join(map(str, x)) if isinstance(x, list) else x)

        geojson_dict = json.loads(gdf_final.to_json())

        for feature in geojson_dict['features']:
            clean_properties = {k: v for k, v in feature['properties'].items() if v is not None}
            feature['properties'] = clean_properties

        with open(OUTPUT_FILE, 'w', encoding='utf-8') as f:
            json.dump(geojson_dict, f, ensure_ascii=False)

        print(f"\nФайл збережено: {os.path.abspath(OUTPUT_FILE)}.")

    except Exception as e:
            print(f"\nПомилка: {e}")


if __name__ == "__main__":
    main()