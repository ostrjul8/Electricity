import { BuildingType } from "@shared/types/BuildingType";
import { ForecastType } from "@shared/types/ForecastType";

export type BuildingDetailsType = {
    building: BuildingType;
    latestForecast: ForecastType;
};
