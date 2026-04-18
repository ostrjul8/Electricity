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
    private readonly useMockPoints: boolean = true;
    private readonly mockPoints: MapPointType[] = this.createMockMapPoints();

    public async getMapPoints(): Promise<MapPointType[]> {
        if (this.useMockPoints) {
            await this.wait(180);
            return this.mockPoints;
        }

        const apiPoints: MapPointApiType[] = await this.getMapPointsPayload();

        return apiPoints
            .map((point: MapPointApiType) => this.toMapPoint(point))
            .filter((point: MapPointType | null): point is MapPointType => point !== null);
    }

    private async getMapPointsPayload(): Promise<MapPointApiType[]> {
        const endpointCandidates: string[] = [
            `${environment.serverURL}/api/buildings/map-points`,
        ];

        let lastError: unknown;

        for (const endpoint of endpointCandidates) {
            try {
                const response: HttpResponse<string> = await firstValueFrom(
                    this.httpClient.get(endpoint, {
                        observe: "response",
                        responseType: "text",
                        headers: this.createAuthorizationHeaders(),
                    }),
                );

                return this.parseApiPoints(response);
            } catch (error) {
                lastError = error;
            }
        }

        throw lastError ?? new Error("Map points request failed.");
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

    private createMockMapPoints(): MapPointType[] {
        let id: number = 1;
        let seed: number = 20260415;

        const nextRandom = (): number => {
            seed = (seed * 1664525 + 1013904223) >>> 0;
            return seed / 4294967296;
        };

        const clusters: Array<{
            lat: number;
            lon: number;
            count: number;
            spread: number;
            baseColor: number;
        }> = [
            { lat: 50.4501, lon: 30.5234, count: 300, spread: 0.018, baseColor: 3 },
            { lat: 50.4365, lon: 30.5154, count: 220, spread: 0.014, baseColor: 4 },
            { lat: 50.4638, lon: 30.5332, count: 180, spread: 0.012, baseColor: 2 },
            { lat: 50.4875, lon: 30.6025, count: 160, spread: 0.019, baseColor: 5 },
            { lat: 50.4062, lon: 30.5171, count: 140, spread: 0.015, baseColor: 1 },
            { lat: 50.3924, lon: 30.6381, count: 120, spread: 0.014, baseColor: 6 },
            { lat: 50.5101, lon: 30.4482, count: 110, spread: 0.02, baseColor: 2 },
        ];

        const points: MapPointType[] = [];

        for (const cluster of clusters) {
            for (let index: number = 0; index < cluster.count; index += 1) {
                const angle: number = nextRandom() * Math.PI * 2;
                const radius: number = Math.sqrt(nextRandom()) * cluster.spread;

                const lat: number = cluster.lat + Math.sin(angle) * radius;
                const lon: number = cluster.lon + Math.cos(angle) * radius * 1.35;

                const colorShiftChance: number = nextRandom();
                const colorShift: number =
                    colorShiftChance < 0.16 ? -1 : colorShiftChance > 0.84 ? 1 : 0;

                points.push({
                    id,
                    lat: Number(lat.toFixed(6)),
                    lon: Number(lon.toFixed(6)),
                    color: Math.max(1, Math.min(6, cluster.baseColor + colorShift)),
                });

                id += 1;
            }
        }

        return points;
    }

    private async wait(delayMs: number): Promise<void> {
        await new Promise((resolve) => {
            setTimeout(resolve, delayMs);
        });
    }
}
