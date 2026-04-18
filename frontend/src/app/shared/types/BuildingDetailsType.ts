import { BuildingType } from "@shared/types/BuildingType";
import { ConsumptionPointType } from "@shared/types/ConsumptionPointType";
import { ForecastType } from "@shared/types/ForecastType";

export type BuildingDetailsType = {
    building: BuildingType;
    latestForecast: ForecastType;
    recentConsumptions: ConsumptionPointType[];
};
