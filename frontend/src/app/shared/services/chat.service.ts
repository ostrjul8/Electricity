import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";
import { inject, Injectable, signal, WritableSignal } from "@angular/core";
import { environment } from "@shared/environments/environment";
import { ChatType } from "@shared/types/ChatType";
import { MessageType } from "@shared/types/MessageType";
import { OpenChatResponseType } from "@shared/types/OpenChatResponseType";
import { PagedResultType } from "@shared/types/PagedResultType";
import { firstValueFrom } from "rxjs";

@Injectable({
    providedIn: "root",
})
export class ChatService {
    private readonly pinnedChatId: WritableSignal<number | null> = signal<number | null>(null);

    private readonly httpClient: HttpClient = inject(HttpClient);

    public getPinnedChatId(): number | null {
        return this.pinnedChatId();
    }

    public setPinnedChatId(chatId: number | null): void {
        this.pinnedChatId.set(chatId);
    }

    public async createChat(text: string): Promise<OpenChatResponseType> {
        return await firstValueFrom(
            this.httpClient.post<OpenChatResponseType>(
                `${environment.serverURL}/api/chats`,
                { text },
                { headers: this.createAuthorizationHeaders() },
            ),
        );
    }

    public async sendMessage(chatId: number, text: string): Promise<MessageType> {
        return await firstValueFrom(
            this.httpClient.post<MessageType>(
                `${environment.serverURL}/api/chats/${chatId}/messages`,
                { text },
                { headers: this.createAuthorizationHeaders() },
            ),
        );
    }

    public async sendGuestMessage(chatId: number, text: string, accessToken: string): Promise<MessageType> {
        const params: HttpParams = new HttpParams().set("accessToken", accessToken);

        return await firstValueFrom(
            this.httpClient.post<MessageType>(
                `${environment.serverURL}/api/chats/${chatId}/messages/guest`,
                { text },
                { params },
            ),
        );
    }

    public async getChatsLazy(
        onlyUnread: boolean = false,
        page: number = 1,
        pageSize: number = 20,
    ): Promise<PagedResultType<ChatType>> {
        const normalizedPage: number = Math.max(1, Math.trunc(page));
        const normalizedPageSize: number = Math.min(100, Math.max(1, Math.trunc(pageSize)));

        const params: HttpParams = new HttpParams()
            .set("onlyUnread", onlyUnread)
            .set("page", normalizedPage)
            .set("pageSize", normalizedPageSize);

        return await firstValueFrom(
            this.httpClient.get<PagedResultType<ChatType>>(`${environment.serverURL}/api/chats/lazy`, {
                params,
                headers: this.createAuthorizationHeaders(),
            }),
        );
    }

    public async getMessagesLazy(
        chatId: number,
        page: number = 1,
        pageSize: number = 30,
    ): Promise<PagedResultType<MessageType>> {
        const normalizedPage: number = Math.max(1, Math.trunc(page));
        const normalizedPageSize: number = Math.min(200, Math.max(1, Math.trunc(pageSize)));

        const params: HttpParams = new HttpParams()
            .set("page", normalizedPage)
            .set("pageSize", normalizedPageSize);

        return await firstValueFrom(
            this.httpClient.get<PagedResultType<MessageType>>(
                `${environment.serverURL}/api/chats/${chatId}/messages/lazy`,
                {
                    params,
                    headers: this.createAuthorizationHeaders(),
                },
            ),
        );
    }

    public async getGuestMessagesLazy(
        chatId: number,
        accessToken: string,
        page: number = 1,
        pageSize: number = 30,
    ): Promise<PagedResultType<MessageType>> {
        const normalizedPage: number = Math.max(1, Math.trunc(page));
        const normalizedPageSize: number = Math.min(200, Math.max(1, Math.trunc(pageSize)));

        const params: HttpParams = new HttpParams()
            .set("accessToken", accessToken)
            .set("page", normalizedPage)
            .set("pageSize", normalizedPageSize);

        return await firstValueFrom(
            this.httpClient.get<PagedResultType<MessageType>>(
                `${environment.serverURL}/api/chats/${chatId}/messages/guest/lazy`,
                {
                    params,
                },
            ),
        );
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
}
