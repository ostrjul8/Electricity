import pandas as pd
import numpy as np
from catboost import CatBoostRegressor
from sqlalchemy import create_engine
from sklearn.metrics import mean_absolute_error, mean_squared_error, mean_absolute_percentage_error

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
df = pd.read_sql(query, engine)
df['Date'] = pd.to_datetime(df['Date'], utc=True).dt.tz_localize(None)
df = df.sort_values(by=['BuildingId', 'Date']).reset_index(drop=True)

df['Month'] = df['Date'].dt.month
df['DayOfWeek'] = df['Date'].dt.dayofweek
df['IsWeekend'] = df['DayOfWeek'].isin([5, 6]).astype(int)

df['Target_Day1'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(-1)
df['Target_Day2'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(-2)
df['Target_Day3'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(-3)

df['Cons_Lag1'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(1)
df['Cons_Lag7'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(7)
df['Hours_Lag1'] = df.groupby('BuildingId')['HoursWithElectricity'].shift(1)

data = df.dropna(subset=['Target_Day1', 'Target_Day2', 'Target_Day3', 'Cons_Lag7']).copy()

test_days = 14
split_date = data['Date'].max() - pd.Timedelta(days=test_days)

train_set = data[data['Date'] < split_date]
test_set = data[data['Date'] >= split_date]

features = [
    'Type', 'Floors', 'Material', 'Area', 'MinTemp', 'MaxTemp',
    'Condition', 'WindSpeed', 'Humidity', 'Month', 'DayOfWeek',
    'IsWeekend', 'Cons_Lag1', 'Cons_Lag7', 'Hours_Lag1'
]
cat_features = ['Type', 'Material', 'Condition']

params = {
    'iterations': 500, 'depth': 10, 'learning_rate': 0.03, 'l2_leaf_reg': 3,
    'cat_features': cat_features, 'verbose': False, 'random_seed': 42
}

results = []

for day in [1, 2, 3]:
    model = CatBoostRegressor(**params)
    target_col = f'Target_Day{day}'

    model.fit(train_set[features], train_set[target_col])
    preds = model.predict(test_set[features]).clip(min=0)
    actuals = test_set[target_col]

    mae = mean_absolute_error(actuals, preds)
    rmse = np.sqrt(mean_squared_error(actuals, preds))
    mape = mean_absolute_percentage_error(actuals, preds) * 100

    results.append({
        'Горизонт': f'День {day}',
        'MAE (кВт·год)': round(mae, 2),
        'RMSE (кВт·год)': round(rmse, 2),
        'MAPE (%)': round(mape, 2),
        'Точність (%)': round(100 - mape, 2)
    })

performance_df = pd.DataFrame(results)
print("\nКомплексна оцінка точності моделей:")
print(performance_df.to_string(index=False))