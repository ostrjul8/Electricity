import json

FILE_NAME = 'kyiv_human_buildings.json'


def print_unique_building_types(file_path):
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            data = json.load(file)

        unique_types = set()

        for item in data:
            b_type = item.get('building')

            if b_type:
                unique_types.add(b_type)

        print(f"Знайдено {len(unique_types)} унікальних типів будівель:\n.")

        for b_type in sorted(unique_types):
            print(f"'{b_type}',")

    except FileNotFoundError:
        print(f"Файл '{file_path}' не знайдено.")
    except json.JSONDecodeError:
        print("Не вдалося прочитати JSON.")
    except Exception as e:
        print(f"Помилка: {e}")


if __name__ == "__main__":
    print_unique_building_types(FILE_NAME)