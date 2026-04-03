import pandas as pd
from catboost import CatBoostRegressor
from datetime import datetime
from sqlalchemy import create_engine

engine = create_engine('postgresql://postgres:postgres@localhost:5432/ElectricityDB')

print("Завантаження даних з Parquet.")
df = pd.read_parquet('electricity_data.parquet')

print("Підготовка даних.")
df['Date'] = pd.to_datetime(df['Date'])
df = df.sort_values(by=['BuildingId', 'Date']).reset_index(drop=True)

df['Month'] = df['Date'].dt.month
df['DayOfWeek'] = df['Date'].dt.dayofweek
df['IsWeekend'] = df['DayOfWeek'].isin([5, 6]).astype(int)

# --- СТВОРЕННЯ TARGET
df['Target_Day1'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(-1)
df['Target_Day2'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(-2)
df['Target_Day3'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(-3)

# --- СТВОРЕННЯ LAGS
df['Cons_Lag1'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(1)
df['Cons_Lag7'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(7)
df['Hours_Lag1'] = df.groupby('BuildingId')['HoursWithElectricity'].shift(1)

train_df = df.dropna(subset=['Target_Day1', 'Target_Day2', 'Target_Day3', 'Cons_Lag7']).copy()

# Для фінального прогнозу - останній відомий день для кожної будівлі
predict_df = df.dropna(subset=['Cons_Lag7']).groupby('BuildingId').tail(1).copy()

cat_features = ['Type', 'Material', 'Condition']

features = [
    'Type', 'Floors', 'Material', 'Area', 
    'MinTemp', 'MaxTemp', 'Condition', 'WindSpeed', 'Humidity', 
    'Month', 'DayOfWeek', 'IsWeekend',
    'Cons_Lag1', 'Cons_Lag7', 'Hours_Lag1'
]

params = {
    'iterations': 500, 
    'depth': 10, 
    'learning_rate': 0.03,
    'l2_leaf_reg': 3,
    'random_strength': 1,
    'cat_features': cat_features, 
    'verbose': 100, 
    'random_seed': 42
}
model_day1 = CatBoostRegressor(**params)
model_day2 = CatBoostRegressor(**params)
model_day3 = CatBoostRegressor(**params)

print(f"Навчання на {len(train_df)} рядках.")
print("--- Модель для Дня 1 ---")
model_day1.fit(train_df[features], train_df['Target_Day1'])

print("--- Модель для Дня 2 ---")
model_day2.fit(train_df[features], train_df['Target_Day2'])

print("--- Модель для Дня 3 ---")
model_day3.fit(train_df[features], train_df['Target_Day3'])

print("--- Фінальний прогноз ---")
preds_day1 = model_day1.predict(predict_df[features])
preds_day2 = model_day2.predict(predict_df[features])
preds_day3 = model_day3.predict(predict_df[features])

forecasts_df = pd.DataFrame({
    'BuildingId': predict_df['BuildingId'],
    'ConsumptionDay1': preds_day1.clip(min=0).round(2),
    'ConsumptionDay2': preds_day2.clip(min=0).round(2),
    'ConsumptionDay3': preds_day3.clip(min=0).round(2),
    'CreatedAt': pd.Timestamp.now().replace(microsecond=0)
})

print("Перші 5 рядків прогнозу:")
print(forecasts_df.head())

forecasts_df.to_sql(
    'Forecasts', 
    engine, 
    if_exists='append',
    index=False
)