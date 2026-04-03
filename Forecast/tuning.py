import pandas as pd
from catboost import CatBoostRegressor
from sklearn.model_selection import RandomizedSearchCV
from sqlalchemy import create_engine

df = pd.read_parquet('electricity_data.parquet')

df['Date'] = pd.to_datetime(df['Date'])
df = df.sort_values(by=['BuildingId', 'Date']).reset_index(drop=True)

df['Month'] = df['Date'].dt.month
df['DayOfWeek'] = df['Date'].dt.dayofweek
df['IsWeekend'] = df['DayOfWeek'].isin([5, 6]).astype(int)

df['Target_Day1'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(-1)
df['Cons_Lag1'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(1)
df['Cons_Lag7'] = df.groupby('BuildingId')['ConsumptionAmount'].shift(7)
df['Hours_Lag1'] = df.groupby('BuildingId')['HoursWithElectricity'].shift(1)

train_df = df.dropna(subset=['Target_Day1', 'Cons_Lag7']).copy()

tune_sample = train_df.sample(n=100000, random_state=42)

features = [
    'Type', 'Floors', 'Material', 'Area', 'MinTemp', 'MaxTemp',
    'Condition', 'WindSpeed', 'Humidity', 'Month', 'DayOfWeek',
    'IsWeekend', 'Cons_Lag1', 'Cons_Lag7', 'Hours_Lag1'
]
cat_features = ['Type', 'Material', 'Condition']

param_dist = {
    'iterations': [500, 1000],
    'depth': [4, 6, 8, 10],
    'learning_rate': [0.01, 0.03, 0.05, 0.1],
    'l2_leaf_reg': [1, 3, 5, 7, 9],
    'random_strength': [1, 2, 5]
}

model = CatBoostRegressor(verbose=False, random_seed=42)

random_search = RandomizedSearchCV(
    estimator=model,
    param_distributions=param_dist,
    n_iter=15,
    cv=3,
    scoring='neg_mean_absolute_error',
    verbose=2,
    n_jobs=-1,
    random_state=42
)

random_search.fit(
    tune_sample[features], 
    tune_sample['Target_Day1'],
    cat_features=cat_features
)

print("\n" + "="*30)
print("Найвигідніші параметри:")
print(random_search.best_params_)
print(f"Найкраща середня помилка: {-random_search.best_score_:.4f}")
print("="*30)