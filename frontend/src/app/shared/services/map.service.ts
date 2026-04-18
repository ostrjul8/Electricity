import { HttpClient, HttpHeaders, HttpResponse } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { environment } from "@shared/environments/environment";
import { MapPointType } from "@shared/types/MapPointType";
import { firstValueFrom } from "rxjs";

type MapPointApiType = {
    Id?: number;
    Latitude?: number;
    Longitude?: number;
    ColorLevel?: number;
    id?: number;
    lat?: number;
    lon?: number;
    color?: number;
};

@Injectable({
    providedIn: "root",
})
export class MapService {
    private readonly httpClient: HttpClient = inject(HttpClient);

    public async getMapPoints(): Promise<MapPointType[]> {
        const apiPoints: MapPointApiType[] = await this.getMapPointsPayload(
            `${environment.serverURL}/api/buildings/map-points`,
        );

        return apiPoints
            .map((point: MapPointApiType) => this.toMapPoint(point))
            .filter((point: MapPointType | null): point is MapPointType => point !== null);
    }

    public async getAnomalyMapPoints(deviationPercent: number): Promise<MapPointType[]> {
        const normalizedDeviationPercent: number = Math.max(0, deviationPercent);

        const apiPoints: MapPointApiType[] = await this.getMapPointsPayload(
            `${environment.serverURL}/api/buildings/map-points/anomalies?deviationPercent=${normalizedDeviationPercent}`,
        );

        return apiPoints
            .map((point: MapPointApiType) => this.toMapPoint(point))
            .filter((point: MapPointType | null): point is MapPointType => point !== null);
    }

    private async getMapPointsPayload(endpoint: string): Promise<MapPointApiType[]> {
        const response: HttpResponse<string> = await firstValueFrom(
            this.httpClient.get(endpoint, {
                observe: "response",
                responseType: "text",
                headers: this.createAuthorizationHeaders(),
            }),
        );

        return this.parseApiPoints(response);
    }

    private parseApiPoints(response: HttpResponse<string>): MapPointApiType[] {
        const body: string = response.body?.trim() ?? "[]";

        if (!body) {
            return [];
        }

        // Browsers transparently decompress Content-Encoding: br payloads.
        const parsedBody: unknown = JSON.parse(body);

        return Array.isArray(parsedBody) ? (parsedBody as MapPointApiType[]) : [];
    }

    private toMapPoint(point: MapPointApiType): MapPointType | null {
        const id: number | undefined = point.id ?? point.Id;
        const latitude: number | undefined = point.lat ?? point.Latitude;
        const longitude: number | undefined = point.lon ?? point.Longitude;
        const color: number = Math.trunc(point.color ?? point.ColorLevel ?? 3);

        if (!this.isFiniteNumber(id) || !this.isFiniteNumber(latitude) || !this.isFiniteNumber(longitude)) {
            return null;
        }

        return {
            id,
            lat: latitude,
            lon: longitude,
            color,
        };
    }

    private createAuthorizationHeaders(): HttpHeaders {
        const accessToken: string | null = sessionStorage.getItem("accessToken");

        if (!accessToken) {
            return new HttpHeaders();
        }

        return new HttpHeaders({
            Authorization: `Bearer ${accessToken}`,
        });
    }

    private isFiniteNumber(value: unknown): value is number {
        return typeof value === "number" && Number.isFinite(value);
    }
}
