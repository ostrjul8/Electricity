import { CommonModule } from "@angular/common";
import { Component, computed, inject, input, InputSignal, OnDestroy, signal } from "@angular/core";
import { ChatService } from "@shared/services/chat.service";
import { MessageType } from "@shared/types/MessageType";
import { OpenChatResponseType } from "@shared/types/OpenChatResponseType";
import { PagedResultType } from "@shared/types/PagedResultType";
import { Button } from "../button/button";
import { Textarea } from "../textarea/textarea";
import { AuthService } from "@shared/services/auth.service";

@Component({
    selector: "app-floating-chat",
    imports: [CommonModule, Button, Textarea],
    templateUrl: "./floating-chat.html",
    styleUrl: "./floating-chat.css",
})
export class FloatingChat implements OnDestroy {
    private readonly messagesPollingIntervalMs: number = 10000;

    protected readonly isOpen = signal<boolean>(false);

    public readonly isEnabled: InputSignal<boolean> = input<boolean>(true);

    protected readonly activeChatId = signal<number | null>(null);
    protected readonly pinnedChatId = computed<number | null>(() => this.chatService.getPinnedChatId());
    protected readonly guestAccessToken = signal<string | null>(null);
    protected readonly messages = signal<MessageType[]>([]);
    protected readonly loadingMessages = signal<boolean>(false);
    protected readonly sending = signal<boolean>(false);
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly draft = signal<string>("");

    protected readonly headerText = computed<string>(() => {
        const activeId: number | null = this.activeChatId();

        if (activeId === null) {
            return "Чат підтримки";
        }

        return `Чат #${activeId}`;
    });

    private messagesPollingTimer: ReturnType<typeof setInterval> | null = null;
    private isPollingInProgress: boolean = false;

    private readonly authService: AuthService = inject(AuthService);
    private readonly chatService: ChatService = inject(ChatService);

    public ngOnDestroy(): void {
        this.stopMessagesPolling();
    }

    protected async toggle(): Promise<void> {
        this.isOpen.update((value: boolean) => !value);

        if (this.isOpen()) {
            if (this.isAuthorizedUser()) {
                await this.loadPinnedChatMessages();
            } else {
                await this.loadGuestChatMessages();
            }

            this.startMessagesPolling();
        } else {
            this.stopMessagesPolling();
        }
    }

    protected close(): void {
        this.isOpen.set(false);
        this.stopMessagesPolling();
    }

    protected handleDraftInput(event: Event): void {
        this.draft.set((event.target as HTMLTextAreaElement).value);
    }

    protected async send(): Promise<void> {
        const text: string = this.draft().trim();

        if (!text || this.sending()) {
            return;
        }

        this.sending.set(true);
        this.errorMessage.set(null);

        try {
            const isAuthorizedUser: boolean = this.isAuthorizedUser();
            const selectedChatId: number | null = this.activeChatId();

            if (!isAuthorizedUser) {
                const currentGuestAccessToken: string | null = this.guestAccessToken();

                if (selectedChatId !== null && currentGuestAccessToken) {
                    const newMessage: MessageType = await this.chatService.sendGuestMessage(
                        selectedChatId,
                        text,
                        currentGuestAccessToken,
                    );

                    this.messages.update((items: MessageType[]) => [...items, newMessage]);
                    this.draft.set("");
                    this.startMessagesPolling();

                    return;
                }

                const createdChat: OpenChatResponseType = await this.chatService.createChat(text);

                this.draft.set("");
                this.activeChatId.set(createdChat.chatId);
                this.guestAccessToken.set(createdChat.guestAccessToken ?? null);
                this.messages.set([createdChat.message]);
                this.startMessagesPolling();

                return;
            }

            if (selectedChatId === null) {
                const createdChat: OpenChatResponseType = await this.chatService.createChat(text);

                this.draft.set("");

                this.activeChatId.set(createdChat.chatId);
                this.chatService.setPinnedChatId(createdChat.chatId);

                await this.loadMessages(createdChat.chatId);
                this.startMessagesPolling();

                return;
            }

            const newMessage: MessageType = await this.chatService.sendMessage(selectedChatId, text);
            this.messages.update((items: MessageType[]) => [...items, newMessage]);
            this.draft.set("");
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося надіслати повідомлення."));
        } finally {
            this.sending.set(false);
        }
    }

    private async loadPinnedChatMessages(): Promise<void> {
        const pinnedChatId: number | null = this.pinnedChatId();

        if (pinnedChatId === null) {
            this.activeChatId.set(null);
            this.messages.set([]);
            return;
        }

        this.activeChatId.set(pinnedChatId);

        await this.loadMessages(pinnedChatId);
    }

    private async loadGuestChatMessages(): Promise<void> {
        const chatId: number | null = this.activeChatId();
        const accessToken: string | null = this.guestAccessToken();

        if (chatId === null || !accessToken) {
            this.messages.set([]);
            return;
        }

        await this.loadMessages(chatId);
    }

    private async loadMessages(chatId: number, silent: boolean = false): Promise<void> {
        if (!silent) {
            this.loadingMessages.set(true);
        }

        try {
            let result: PagedResultType<MessageType>;

            if (this.isAuthorizedUser()) {
                result = await this.chatService.getMessagesLazy(chatId, 1, 30);
            } else {
                const accessToken: string | null = this.guestAccessToken();

                if (!accessToken) {
                    this.messages.set([]);
                    return;
                }

                result = await this.chatService.getGuestMessagesLazy(chatId, accessToken, 1, 30);
            }

            this.messages.set(result.items);
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося завантажити повідомлення."));
        } finally {
            if (!silent) {
                this.loadingMessages.set(false);
            }
        }
    }

    private startMessagesPolling(): void {
        if (this.messagesPollingTimer !== null) {
            return;
        }

        if (!this.canPollMessages()) {
            return;
        }

        this.messagesPollingTimer = setInterval(() => {
            this.pollMessages();
        }, this.messagesPollingIntervalMs);
    }

    private stopMessagesPolling(): void {
        if (this.messagesPollingTimer === null) {
            return;
        }

        clearInterval(this.messagesPollingTimer);
        this.messagesPollingTimer = null;
    }

    private async pollMessages(): Promise<void> {
        if (this.isPollingInProgress || !this.isOpen()) {
            return;
        }

        if (!this.canPollMessages()) {
            this.stopMessagesPolling();
            return;
        }

        const pollingChatId: number | null = this.isAuthorizedUser()
            ? this.pinnedChatId()
            : this.activeChatId();

        if (pollingChatId === null) {
            return;
        }

        this.isPollingInProgress = true;

        try {
            await this.loadMessages(pollingChatId, true);
        } finally {
            this.isPollingInProgress = false;
        }
    }

    private getReadableError(error: unknown, fallback: string): string {
        if (error instanceof Error && error.message) {
            return error.message;
        }

        return fallback;
    }

    private isAuthorizedUser(): boolean {
        return this.authService.isLoggedIn();
    }

    private canPollMessages(): boolean {
        if (this.isAuthorizedUser()) {
            return this.pinnedChatId() !== null;
        }

        return this.activeChatId() !== null && Boolean(this.guestAccessToken());
    }
}
