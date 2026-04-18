import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { environment } from "@shared/environments/environment";
import { UserType } from "@shared/types/UserType";
import { catchError, switchMap } from "rxjs";

export const tokenInterceptor: HttpInterceptorFn = (request: HttpRequest<unknown>, next: HttpHandlerFn) => {
    const token: string | null = sessionStorage.getItem("token");

    if (!request.url.includes(environment.serverURL) && !request.url.includes("amazonaws") && token) {
        const modifiedRequest: HttpRequest<unknown> = request.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`,
            },
        });

        return next(modifiedRequest)
            .pipe(
                catchError((error: HttpErrorResponse) => {
                    if (error.status === 401) {
                        // return authService.login(sessionStorage.getItem("email")!, sessionStorage.getItem("password")!)
                        //     .pipe(switchMap((res: UserType) => {
                        //         sessionStorage.setItem("token", res.token);

                        //         console.log({ token: res.token }, new Date().toTimeString());

                        //         const modifiedRequest = request.clone({
                        //             headers: request.headers.set("Authorization", `Bearer ${res.token}`),
                        //         });

                        //         return next(modifiedRequest);
                        //     }));
                    }

                    throw error;
                })
            );
    }

    return next(request);
};
