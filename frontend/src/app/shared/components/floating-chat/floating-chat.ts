import { CommonModule } from "@angular/common";
import { Component, computed, inject, input, InputSignal, OnDestroy, signal } from "@angular/core";
import { ChatService } from "@shared/services/chat.service";
import { MessageType } from "@shared/types/MessageType";
import { OpenChatResponseType } from "@shared/types/OpenChatResponseType";
import { Button } from "../button/button";
import { Textarea } from "../textarea/textarea";

@Component({
    selector: "app-floating-chat",
    imports: [CommonModule, Button, Textarea],
    templateUrl: "./floating-chat.html",
    styleUrl: "./floating-chat.css",
})
export class FloatingChat implements OnDestroy {
    private readonly pinnedChatStorageKey: string = "";
    private readonly messagesPollingIntervalMs: number = 10000;

    protected readonly isOpen = signal<boolean>(false);

    public readonly isEnabled: InputSignal<boolean> = input<boolean>(true);

    protected readonly activeChatId = signal<number | null>(this.getPinnedChatId());
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

    private readonly chatService: ChatService = inject(ChatService);
    private messagesPollingTimer: ReturnType<typeof setInterval> | null = null;
    private isPollingInProgress: boolean = false;

    public ngOnDestroy(): void {
        this.stopMessagesPolling();
    }

    protected async toggle(): Promise<void> {
        this.isOpen.update((value: boolean) => !value);

        if (this.isOpen()) {
            await this.loadPinnedChatMessages();
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
            const selectedChatId: number | null = this.activeChatId();

            if (selectedChatId === null) {
                const createdChat: OpenChatResponseType = await this.chatService.createChat(text);

                this.draft.set("");

                this.activeChatId.set(createdChat.chatId);
                this.persistPinnedChatId(createdChat.chatId);

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
        const pinnedChatId: number | null = this.activeChatId();

        if (pinnedChatId === null) {
            this.messages.set([]);
            return;
        }

        await this.loadMessages(pinnedChatId);
    }

    private async loadMessages(chatId: number, silent: boolean = false): Promise<void> {
        if (!silent) {
            this.loadingMessages.set(true);
        }

        try {
            const result = await this.chatService.getMessagesLazy(chatId, 1, 30);

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

        const pinnedChatId: number | null = this.activeChatId();

        if (pinnedChatId === null) {
            return;
        }

        this.isPollingInProgress = true;

        try {
            await this.loadMessages(pinnedChatId, true);
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

    private getPinnedChatId(): number | null {
        const rawPinnedChatId: string | null = localStorage.getItem(this.pinnedChatStorageKey);

        if (!rawPinnedChatId) {
            return null;
        }

        const parsedPinnedChatId: number = Number.parseInt(rawPinnedChatId, 10);

        return Number.isFinite(parsedPinnedChatId) ? parsedPinnedChatId : null;
    }

    private persistPinnedChatId(chatId: number): void {
        localStorage.setItem(this.pinnedChatStorageKey, chatId.toString());
    }
}
