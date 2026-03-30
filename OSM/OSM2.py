import osmnx as ox
import os
import pandas as pd

PLACE_NAME = "Kyiv, Ukraine"
OUTPUT_FILE = "kyiv_human_buildings.json"

BUILDING_TAGS = {"building": True}

INCLUDE_TYPES = [
    'yes', 'apartments', 'residential', 'house', 'detached',
    'commercial', 'office', 'retail', 'school', 'university',
    'college', 'kindergarten', 'hospital', 'clinic', 'public',
    'government', 'dormitory', 'hotel'
]

COLUMNS_TO_KEEP = [
    'geometry',
    'building',
    'addr:street',
    'addr:housenumber',
    'name',
    'amenity',
    'shop',
    'office',
    'building:levels',
    'material',
    'area_sqm'
]

AMENITY_MAP = {
    'sustenance': ['bar', 'cafe', 'fast_food', 'food_court', 'pub', 'restaurant'],
    'education': ['college', 'dancing_school', 'driving_school', 'first_aid_school', 'kindergarten', 'language_school', 'library', 'toy_library', 'research_institute', 'training', 'music_school', 'school', 'university'],
    'transportation': ['bus_station', 'car_wash', 'vehicle_inspection', 'charging_station', 'ferry_terminal', 'fuel', 'weighbridge'],
    'financial': ['bank', 'bureau_de_change', 'money_transfer', 'payment_centre'],
    'healthcare': ['clinic', 'dentist', 'hospital', 'nursing_home', 'pharmacy', 'social_facility', 'veterinary', 'crematorium', 'animal_shelter'],
    'entertainment': ['arts_centre', 'casino', 'cinema', 'community_centre', 'conference_centre', 'events_venue', 'exhibition_centre', 'gambling', 'love_hotel', 'music_venue', 'nightclub', 'planetarium', 'social_centre', 'stage', 'stripclub', 'studio', 'theatre', 'marketplace'],
    'public': ['courthouse', 'fire_station', 'police', 'post_depot', 'post_office', 'prison', 'ranger_station', 'townhall', 'monastery'],
    'waste': ['recycling', 'waste_transfer_station'],
}

AMENITY_LOOKUP = {val.strip(): key for key, values in AMENITY_MAP.items() for val in values}


def determine_building_type(row):
    building_val = row.get('building', '')

    if building_val != 'yes' and building_val != '':
        return building_val

    office_val = row.get('office', '')
    if office_val != '':
        return 'office'

    shop_val = row.get('shop', '')
    if shop_val != '':
        return 'shop'

    amenity_val = row.get('amenity', '')
    if amenity_val != '':
        return AMENITY_LOOKUP.get(amenity_val, 'Others (Amenity)')

    return 'building'


def get_first_coordinate(geom):
    try:
        if geom.geom_type == 'Polygon':
            coord = geom.exterior.coords[0]
        elif geom.geom_type == 'MultiPolygon':
            coord = geom.geoms[0].exterior.coords[0]
        else:
            return None

        return [round(coord[0], 6), round(coord[1], 6)]
    except:
        return None


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

        print("\nРозрахунок площі та відсів дрібні об'єкти (< 60 кв. м).")
        gdf_proj = gdf_filtered.to_crs(gdf_filtered.estimate_utm_crs())

        gdf_filtered['area_sqm'] = gdf_proj.geometry.area.round(2)

        gdf_filtered = gdf_filtered[gdf_filtered['area_sqm'] >= 60].copy()

        filtered_count = len(gdf_filtered)
        removed_count = total_found - filtered_count
        print(f"Відфільтровано сміття та дрібні будівлі. Видалено {removed_count} об'єктів.")
        print(f"Цільових будівель залишилося: {filtered_count}.")

        existing_columns = [col for col in COLUMNS_TO_KEEP if col in gdf_filtered.columns]
        gdf_final = gdf_filtered[existing_columns].copy()

        for col in gdf_final.columns:
            if col not in ['geometry', 'area_sqm']:
                gdf_final[col] = gdf_final[col].fillna('').astype(str)
                gdf_final[col] = gdf_final[col].replace(['nan', 'None', '<NA>', 'NaN'], '')

        gdf_final['building'] = gdf_final.apply(determine_building_type, axis=1)

        cols_to_drop = ['amenity', 'shop', 'office']
        cols_exist = [col for col in cols_to_drop if col in gdf_final.columns]
        gdf_final = gdf_final.drop(columns=cols_exist)

        gdf_final['coordinates'] = gdf_final['geometry'].apply(get_first_coordinate)

        gdf_final = gdf_final.drop(columns=['geometry'])
        df_final = pd.DataFrame(gdf_final)

        df_final.to_json(OUTPUT_FILE, orient='records', force_ascii=False, indent=2)

        print(f"\nФайл збережено: {os.path.abspath(OUTPUT_FILE)}.")

    except Exception as e:
        print(f"\nПомилка: {e}")


if __name__ == "__main__":
    main()