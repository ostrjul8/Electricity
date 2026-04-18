import { CommonModule } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { Popup } from "@shared/components/popup/popup";
import { BuildingService } from "@shared/services/building.service";
import { BuildingDetailsType } from "@shared/types/BuildingDetailsType";
import { BuildingType } from "@shared/types/BuildingType";
import { ForecastType } from "@shared/types/ForecastType";

@Component({
    selector: "app-building-details-popup",
    standalone: true,
    imports: [CommonModule, Popup],
    templateUrl: "./building-details-popup.html",
    styleUrl: "./building-details-popup.css",
})
export class BuildingDetailsPopup {
    private readonly buildingService: BuildingService = inject(BuildingService);

    protected readonly isOpen = signal<boolean>(false);
    protected readonly loading = signal<boolean>(false);
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly exportLoading = signal<boolean>(false);
    protected readonly building = signal<BuildingType | null>(null);
    protected readonly forecast = signal<ForecastType | null>(null);

    protected readonly forecastBars = computed<Array<{ label: string; value: number; maxValue: number }>>(() => {
        const currentForecast: ForecastType | null = this.forecast();

        if (!currentForecast) {
            return [];
        }

        const values: Array<{ label: string; value: number }> = [
            { label: "День 1", value: currentForecast.consumptionDay1 },
            { label: "День 2", value: currentForecast.consumptionDay2 },
            { label: "День 3", value: currentForecast.consumptionDay3 },
        ];

        const maxValue: number = Math.max(...values.map((item: { value: number }) => item.value), 1);

        return values.map((item: { label: string; value: number }) => ({
            label: item.label,
            value: item.value,
            maxValue,
        }));
    });

    protected readonly forecastDate = computed<string>(() => {
        const currentForecast: ForecastType | null = this.forecast();

        if (!currentForecast) {
            return "";
        }

        return new Intl.DateTimeFormat("uk-UA", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit",
        }).format(new Date(currentForecast.createdAt));
    });

    public async open(buildingId: number): Promise<void> {
        this.isOpen.set(true);
        this.loading.set(true);
        this.errorMessage.set(null);
        this.building.set(null);
        this.forecast.set(null);

        try {
            const details: BuildingDetailsType = await this.buildingService.getById(buildingId);
            
            this.building.set(details.building);
            this.forecast.set(details.latestForecast);
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося завантажити деталі будівлі."));
        } finally {
            this.loading.set(false);
        }
    }

    public close(): void {
        this.isOpen.set(false);
    }

    protected async exportCsvReport(): Promise<void> {
        const currentBuilding: BuildingType | null = this.building();

        if (!currentBuilding || this.exportLoading()) {
            return;
        }

        this.exportLoading.set(true);

        try {
            const report = await this.buildingService.downloadCsvReport(currentBuilding.id);
            const objectUrl: string = URL.createObjectURL(report.blob);

            const link: HTMLAnchorElement = document.createElement("a");
            link.href = objectUrl;
            link.download = report.fileName;
            link.rel = "noopener";
            link.click();

            URL.revokeObjectURL(objectUrl);
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося сформувати CSV-звіт."));
        } finally {
            this.exportLoading.set(false);
        }
    }

    protected getBarHeight(value: number, maxValue: number): number {
        return Math.max(12, Math.round((value / maxValue) * 100));
    }

    protected getBarLabel(value: number): string {
        return value.toFixed(2);
    }

    private getReadableError(error: unknown, fallbackMessage: string): string {
        if (error instanceof Error && error.message) {
            return error.message;
        }

        return fallbackMessage;
    }
}