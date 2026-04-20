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

    public async downloadAnomalyCsvReport(deviationPercent: number): Promise<void> {
        const normalizedDeviationPercent: number = Math.max(0, deviationPercent);

        const response: HttpResponse<Blob> = await firstValueFrom(
            this.httpClient.get(
                `${environment.serverURL}/api/buildings/map-points/anomalies/csv-report?deviationPercent=${normalizedDeviationPercent}`,
                {
                    observe: "response",
                    responseType: "blob",
                    headers: this.createAuthorizationHeaders(),
                },
            ),
        );

        const csvBlob: Blob | null = response.body;
        if (csvBlob === null) {
            throw new Error("Порожній файл звіту.");
        }

        const contentDisposition: string | null = response.headers.get("Content-Disposition");
        const fileName: string =
            this.tryGetFileNameFromContentDisposition(contentDisposition)
            ?? `anomalies-report-${normalizedDeviationPercent}.csv`;

        const objectUrl: string = URL.createObjectURL(csvBlob);
        const anchor: HTMLAnchorElement = document.createElement("a");
        anchor.href = objectUrl;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
        URL.revokeObjectURL(objectUrl);
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

    private tryGetFileNameFromContentDisposition(contentDisposition: string | null): string | null {
        if (!contentDisposition) {
            return null;
        }

        const utf8Match: RegExpMatchArray | null = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
        if (utf8Match?.[1]) {
            return decodeURIComponent(utf8Match[1]);
        }

        const basicMatch: RegExpMatchArray | null = contentDisposition.match(/filename="?([^";]+)"?/i);
        return basicMatch?.[1] ?? null;
    }
}
