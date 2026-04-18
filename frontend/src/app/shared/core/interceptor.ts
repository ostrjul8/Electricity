import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { environment } from "@shared/environments/environment";
import { AuthService } from "@shared/services/auth.service";
import { catchError, switchMap, throwError } from "rxjs";

export const tokenInterceptor: HttpInterceptorFn = (request: HttpRequest<unknown>, next: HttpHandlerFn) => {
    const authService: AuthService = inject(AuthService);
    const token: string | null = sessionStorage.getItem("accessToken");

    const isApiRequest: boolean = request.url.includes(environment.serverURL);
    const isRefreshRequest: boolean = request.url.includes("/api/auth/refresh");
    const isAuthRequest: boolean = request.url.includes("/api/auth/login") || request.url.includes("/api/auth/register");

    if (isApiRequest && token && !isAuthRequest && !isRefreshRequest) {
        const modifiedRequest: HttpRequest<unknown> = request.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`,
            },
        });

        return next(modifiedRequest)
            .pipe(
                catchError((error: HttpErrorResponse) => {
                    const alreadyRetried: boolean = modifiedRequest.headers.has("X-Retry");

                    if (error.status === 401 && !alreadyRetried) {
                        return authService.refreshToken().pipe(
                            switchMap((response) => {
                                const retriedRequest: HttpRequest<unknown> = request.clone({
                                    setHeaders: {
                                        Authorization: `Bearer ${response.accessToken}`,
                                        "X-Retry": "1",
                                    },
                                });

                                return next(retriedRequest);
                            }),
                            catchError(async () => {
                                await authService.logout();
                                throw error;
                            }),
                        );
                    }

                    return throwError(() => error);
                })
            );
    }

    return next(request);
};
