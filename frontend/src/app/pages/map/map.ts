import {
    AfterViewInit,
    Component,
    computed,
    ElementRef,
    OnDestroy,
    Signal,
    WritableSignal,
    inject,
    signal,
    viewChild,
} from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { BuildingDetailsPopup } from "@shared/components/building-details-popup/building-details-popup";
import { Button } from "@shared/components/button/button";
import { Input } from "@shared/components/input/input";
import { Range } from "@shared/components/range/range";
import { AuthService } from "@shared/services/auth.service";
import { BuildingService } from "@shared/services/building.service";
import { MapService } from "@shared/services/map.service";
import { BuildingType } from "@shared/types/BuildingType";
import { MapPointType } from "@shared/types/MapPointType";
import * as Leaflet from "leaflet";
import "leaflet.markercluster";

type MapLegendItem = {
    level: number;
    name: string;
    color: string;
    isAdminOnly: boolean;
};

@Component({
    selector: "app-map",
    imports: [BuildingDetailsPopup, Button, Input, Range],
    templateUrl: "./map.html",
    styleUrl: "./map.css",
})
export class Map implements AfterViewInit, OnDestroy {
    private readonly mapContainer: Signal<ElementRef<HTMLDivElement>> = viewChild.required("mapContainer");

    private readonly buildingDetailsPopup: Signal<BuildingDetailsPopup> = viewChild.required(BuildingDetailsPopup);

    protected readonly isLoading: WritableSignal<boolean> = signal<boolean>(true);
    protected readonly isGeneratingAnomalyReport: WritableSignal<boolean> = signal<boolean>(false);
    protected readonly errorMessage: WritableSignal<string | null> = signal<string | null>(null);
    protected readonly pointsCount: WritableSignal<number> = signal<number>(0);
    
    protected readonly isAdmin: WritableSignal<boolean> = signal<boolean>(false);
    protected readonly anomaliesOnly: WritableSignal<boolean> = signal<boolean>(false);
    protected readonly anomalyDeviationPercent: WritableSignal<number> = signal<number>(40);
    protected readonly appliedAnomalyDeviationPercent: WritableSignal<number> = signal<number>(40);
    protected readonly hasPendingAnomalyChanges: Signal<boolean> = computed(
        () => this.anomalyDeviationPercent() !== this.appliedAnomalyDeviationPercent(),
    );

    protected readonly searchAddress: WritableSignal<string> = signal<string>("");
    protected readonly legendItems: ReadonlyArray<MapLegendItem> = [
        { level: 1, name: "Замалі", color: "#94a3b8", isAdminOnly: false },
        { level: 2, name: "Рівень 1", color: "#15803d", isAdminOnly: false },
        { level: 3, name: "Рівень 2", color: "#f59e0b", isAdminOnly: false },
        { level: 4, name: "Рівень 3", color: "#f97316", isAdminOnly: false },
        { level: 5, name: "Рівень 4", color: "#ef4444", isAdminOnly: false },
        { level: 6, name: "Завеликі", color: "#6b0e0e", isAdminOnly: false },
        { level: 7, name: "Аномалія (адмін-фільтр)", color: "#7c3aed", isAdminOnly: true },
    ];
    protected readonly showSearchSuggestions: WritableSignal<boolean> = signal<boolean>(false);
    protected readonly loadingSearchSuggestions: WritableSignal<boolean> = signal<boolean>(false);
    protected readonly searchSuggestions: WritableSignal<BuildingType[]> = signal<BuildingType[]>([]);

    private mapInstance: Leaflet.Map | null = null;
    private clusterLayer: Leaflet.MarkerClusterGroup | null = null;
    private districtsLayer: Leaflet.GeoJSON | null = null;
    private hideSuggestionsTimeoutId: ReturnType<typeof setTimeout> | null = null;
    private searchSuggestionsTimeoutId: ReturnType<typeof setTimeout> | null = null;
    private searchSuggestionsRequestId: number = 0;
    private mapPointsRequestId: number = 0;

    private readonly mapService: MapService = inject(MapService);
    private readonly buildingService: BuildingService = inject(BuildingService);
    private readonly authService: AuthService = inject(AuthService);
    private readonly activatedRoute: ActivatedRoute = inject(ActivatedRoute);
    private readonly router: Router = inject(Router);

    public async ngAfterViewInit(): Promise<void> {
        await this.resolveAdminState();

        if (this.activatedRoute.snapshot.data["anomaliesOnly"] === true) {
            this.anomaliesOnly.set(true);
        }

        this.initializeMap();
        await this.loadMapPoints();
    }

    public ngOnDestroy(): void {
        if (this.hideSuggestionsTimeoutId !== null) {
            clearTimeout(this.hideSuggestionsTimeoutId);
            this.hideSuggestionsTimeoutId = null;
        }

        if (this.searchSuggestionsTimeoutId !== null) {
            clearTimeout(this.searchSuggestionsTimeoutId);
            this.searchSuggestionsTimeoutId = null;
        }

        this.destroyMap();
    }

    protected async refreshMapPoints(): Promise<void> {
        await this.loadMapPoints();
    }

    protected async handleAnomaliesOnlyChange(event: Event): Promise<void> {
        const nextValue: boolean = (event.target as HTMLInputElement).checked;

        if (nextValue && !this.isAdmin()) {
            this.errorMessage.set("Перегляд аномалій доступний лише адміну.");
            this.anomaliesOnly.set(false);
            return;
        }

        this.anomaliesOnly.set(nextValue);

        if (nextValue) {
            this.appliedAnomalyDeviationPercent.set(this.anomalyDeviationPercent());
        }

        if (!nextValue && this.activatedRoute.snapshot.data["anomaliesOnly"] === true) {
            await this.router.navigate(["/map"]);
            return;
        }

        await this.loadMapPoints();
    }

    protected handleDeviationPercentChange(nextValue: number): void {
        this.anomalyDeviationPercent.set(nextValue);
    }

    protected async applyAnomalyFilters(): Promise<void> {
        if (!this.anomaliesOnly()) {
            return;
        }

        if (!this.hasPendingAnomalyChanges()) {
            return;
        }

        const previousAppliedPercent: number = this.appliedAnomalyDeviationPercent();
        this.appliedAnomalyDeviationPercent.set(this.anomalyDeviationPercent());
        await this.loadMapPoints();

        if (this.errorMessage() !== null) {
            this.appliedAnomalyDeviationPercent.set(previousAppliedPercent);
        }
    }

    protected async generateAnomalyReport(): Promise<void> {
        if (!this.anomaliesOnly() || this.isGeneratingAnomalyReport()) {
            return;
        }

        this.isGeneratingAnomalyReport.set(true);

        try {
            await this.mapService.downloadAnomalyCsvReport(this.anomalyDeviationPercent());
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error));
        } finally {
            this.isGeneratingAnomalyReport.set(false);
        }
    }

    protected handleSearchInput(value: string): void {
        this.searchAddress.set(value);

        if (this.searchSuggestionsTimeoutId !== null) {
            clearTimeout(this.searchSuggestionsTimeoutId);
        }

        const query: string = value.trim();

        if (query.length < 2) {
            this.searchSuggestions.set([]);
            this.loadingSearchSuggestions.set(false);
            this.showSearchSuggestions.set(false);
            return;
        }

        this.loadingSearchSuggestions.set(true);
        const requestId: number = ++this.searchSuggestionsRequestId;

        this.searchSuggestionsTimeoutId = setTimeout(() => {
            this.loadSearchSuggestions(query, requestId);
        }, 300);
    }

    protected handleSearchFocus(): void {
        if (this.searchSuggestions().length > 0) {
            this.showSearchSuggestions.set(true);
        }
    }

    protected handleSearchBlur(): void {
        this.hideSuggestionsTimeoutId = setTimeout(() => {
            this.showSearchSuggestions.set(false);
        }, 140);
    }

    protected async handleSearchEnter(): Promise<void> {
        const suggestions: BuildingType[] = this.searchSuggestions();

        if (suggestions.length === 0) {
            return;
        }

        await this.selectSuggestion(suggestions[0]);
    }

    protected async handleSuggestionMouseDown(event: MouseEvent, building: BuildingType): Promise<void> {
        event.preventDefault();
        event.stopPropagation();

        await this.selectSuggestion(building);
    }

    protected async selectSuggestion(building: BuildingType, event?: Event): Promise<void> {
        event?.preventDefault();
        event?.stopPropagation();

        if (this.hideSuggestionsTimeoutId !== null) {
            clearTimeout(this.hideSuggestionsTimeoutId);
            this.hideSuggestionsTimeoutId = null;
        }

        this.searchAddress.set(building.address);
        this.showSearchSuggestions.set(false);

        const hasValidCoordinates: boolean = Number.isFinite(building.latitude) && Number.isFinite(building.longitude);

        if (this.mapInstance && hasValidCoordinates) {
            this.mapInstance.flyTo([building.latitude, building.longitude], 17, { animate: true, duration: 0.6 });
        }

        await this.buildingDetailsPopup().open(building.id);
    }

    private async loadSearchSuggestions(query: string, requestId: number): Promise<void> {
        try {
            const suggestions: BuildingType[] = await this.buildingService.searchByAddress(query, 5);

            if (requestId !== this.searchSuggestionsRequestId) {
                return;
            }

            this.searchSuggestions.set(suggestions);
            this.showSearchSuggestions.set(true);
        } catch {
            this.searchSuggestions.set([]);
            this.showSearchSuggestions.set(false);
        } finally {
            this.loadingSearchSuggestions.set(false);
        }
    }

    private initializeMap(): void {
        if (!this.mapContainer) {
            this.errorMessage.set("Не вдалося ініціалізувати контейнер карти.");
            return;
        }

        this.mapInstance = Leaflet.map(this.mapContainer().nativeElement, {
            zoomControl: true,
            minZoom: 11,
            maxZoom: 18,
        }).setView([50.4501, 30.5234], 11);

        Leaflet.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            subdomains: "abc",
        }).addTo(this.mapInstance);

        this.clusterLayer = Leaflet.markerClusterGroup({
            chunkedLoading: true,
            showCoverageOnHover: true,
            spiderfyOnMaxZoom: false,
            disableClusteringAtZoom: 17,
        });

        this.mapInstance.addLayer(this.clusterLayer);

        queueMicrotask(() => {
            this.mapInstance?.invalidateSize();
        });
    }

    private async loadMapPoints(): Promise<void> {
        if (!this.mapInstance || !this.clusterLayer) {
            return;
        }

        const requestId: number = ++this.mapPointsRequestId;

        this.isLoading.set(true);
        this.errorMessage.set(null);

        try {
            const points: MapPointType[] = this.anomaliesOnly()
                ? await this.mapService.getAnomalyMapPoints(this.appliedAnomalyDeviationPercent())
                : await this.mapService.getMapPoints();

            if (requestId !== this.mapPointsRequestId || !this.clusterLayer || !this.mapInstance) {
                return;
            }

            const markers: Leaflet.Marker[] = points.map((point: MapPointType) => this.createMarker(point));

            this.clusterLayer.clearLayers();
            this.clusterLayer.addLayers(markers);
            this.pointsCount.set(points.length);

            const kyivDistrictsData = await this.loadKyivDistrictsData();

            if (this.districtsLayer) {
                this.districtsLayer.remove();
                this.districtsLayer = null;
            }

            this.districtsLayer = Leaflet.geoJSON(kyivDistrictsData, {
                style: () => ({
                    color: "var(--white-color)",
                    weight: 2,
                    fillColor: "var(--secondary-color)",
                    fillOpacity: 0.3,
                }),
            }).addTo(this.mapInstance);

            if (markers.length > 0) {
                const bounds: Leaflet.LatLngBounds = Leaflet.featureGroup(markers).getBounds();
                this.mapInstance.fitBounds(bounds.pad(0.08), { animate: false });
            } else {
                this.mapInstance.setView([50.4501, 30.5234], 11);
            }
        } catch (error) {
            if (requestId !== this.mapPointsRequestId || !this.clusterLayer) {
                return;
            }

            this.clusterLayer.clearLayers();
            this.pointsCount.set(0);
            this.errorMessage.set(this.getReadableError(error));
        } finally {
            if (requestId === this.mapPointsRequestId) {
                this.isLoading.set(false);
                this.mapInstance?.invalidateSize();
            }
        }
    }

    private async loadKyivDistrictsData(): Promise<GeoJSON.FeatureCollection> {
        const response: Response = await fetch("/kyiv-districts.geojson");

        if (!response.ok) {
            throw new Error(`Не вдалося завантажити райони Києва (${response.status}).`);
        }

        return (await response.json()) as GeoJSON.FeatureCollection;
    }

    private createMarker(point: MapPointType): Leaflet.Marker {
        const marker: Leaflet.Marker = Leaflet.marker([point.lat, point.lon], {
            icon: Leaflet.divIcon({
                className: "map-point-marker",
                html: this.buildMarkerDot(point.color),
                iconSize: [16, 16],
                iconAnchor: [8, 8],
            }),
        });

        const levelName =
            this.legendItems.find((item: MapLegendItem) => item.level === point.color)?.name ?? "Невідомо";

        marker.bindTooltip(`Будинок #${point.id} | ${levelName}`, {
            direction: "top",
            offset: [0, -10],
        });

        marker.on("click", () => {
            this.buildingDetailsPopup()?.open(point.id);
        });

        return marker;
    }

    private buildMarkerDot(colorLevel: number): string {
        const color: string = this.getPointColor(colorLevel);

        return `
            <span style="
                display: block;
                width: 16px;
                height: 16px;
                border-radius: 8px;
                border: 2px solid #ffffff;
                background: ${color};
                box-shadow: 0 0 4px 4px var(--shadow-color-10);
            "></span>
        `;
    }

    private getPointColor(colorLevel: number): string {
        const legendItem: MapLegendItem | undefined = this.legendItems.find(
            (item: MapLegendItem) => item.level === colorLevel,
        );
        return legendItem?.color ?? "#3b82f6";
    }

    private getReadableError(error: unknown): string {
        if (error instanceof Error && error.message) {
            return `Не вдалося завантажити точки: ${error.message}`;
        }

        return "Не вдалося завантажити точки для карти.";
    }

    private async resolveAdminState(): Promise<void> {
        try {
            const user = this.authService.user() ?? await this.authService.getAuthorizedUser();
            this.isAdmin.set(user.role === "Admin");
        } catch {
            this.isAdmin.set(false);
        }
    }

    private destroyMap(): void {
        if (!this.mapInstance) {
            return;
        }

        if (this.districtsLayer) {
            this.districtsLayer.remove();
            this.districtsLayer = null;
        }

        this.mapInstance.remove();
        this.mapInstance = null;
        this.clusterLayer = null;
    }
}
