import pandas as pd
from sqlalchemy import create_engine

engine = create_engine('postgresql://postgres:postgres@localhost:5432/ElectricityDB')

query = """
SELECT 
    cr."Date", cr."BuildingId", b."Type", b."Floors", b."Material", b."Area",
    wr."MinTemp", wr."MaxTemp", wr."Condition", wr."WindSpeed", wr."Humidity",
    cr."HoursWithElectricity", cr."ConsumptionAmount"
FROM "ConsumptionRecords" cr
JOIN "Buildings" b ON cr."BuildingId" = b."Id"
JOIN "WeatherRecords" wr ON cr."WeatherRecordId" = wr."Id"
ORDER BY cr."BuildingId", cr."Date";
"""

print("Витягнення даних з БД.")
df = pd.read_sql(query, engine)

print("Збереження у формат Parquet.")
df.to_parquet('electricity_data.parquet', engine='pyarrow', index=False)

print(f"Збережено {len(df)} рядків у файл electricity_data.parquet")