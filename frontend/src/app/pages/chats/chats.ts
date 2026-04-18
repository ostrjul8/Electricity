import { CommonModule } from "@angular/common";
import { Component, computed, inject, OnInit, Signal, signal, WritableSignal } from "@angular/core";
import { ChatService } from "@shared/services/chat.service";
import { ChatType } from "@shared/types/ChatType";
import { MessageType } from "@shared/types/MessageType";
import { OpenChatResponseType } from "@shared/types/OpenChatResponseType";
import { PagedResultType } from "@shared/types/PagedResultType";
import { Button } from "@shared/components/button/button";
import { Textarea } from "@shared/components/textarea/textarea";
import { AuthService } from "@shared/services/auth.service";

@Component({
    selector: "app-chats",
    imports: [CommonModule, Button, Textarea],
    templateUrl: "./chats.html",
    styleUrl: "./chats.css",
})
export class Chats implements OnInit {
    protected readonly chats: WritableSignal<ChatType[]> = signal<ChatType[]>([]);

    protected readonly showOnlyUnread: WritableSignal<boolean> = signal<boolean>(false);
    protected readonly activeChatId: WritableSignal<number | null> = signal<number | null>(null);
    protected readonly isCreatingNewChat: WritableSignal<boolean> = signal<boolean>(false);

    protected readonly messages: WritableSignal<MessageType[]> = signal<MessageType[]>([]);

    protected readonly loadingChats: WritableSignal<boolean> = signal<boolean>(true);
    protected readonly loadingMessages: WritableSignal<boolean> = signal<boolean>(false);
    protected readonly sending: WritableSignal<boolean> = signal<boolean>(false);

    protected readonly errorMessage: WritableSignal<string | null> = signal<string | null>(null);
    protected readonly replyText: WritableSignal<string> = signal<string>("");

    protected readonly activeChat: Signal<ChatType | null> = computed<ChatType | null>(() => {
        const chatId: number | null = this.activeChatId();

        if (chatId === null) {
            return null;
        }

        return this.chats().find((chat: ChatType) => chat.id === chatId) ?? null;
    });

    protected readonly isAdmin: Signal<boolean> = computed<boolean>(() => {
        const user = this.authService.user();
        return user?.role === "Admin";
    });

    private readonly chatService: ChatService = inject(ChatService);
    private readonly authService: AuthService = inject(AuthService);

    public ngOnInit(): void {
        this.loadChats();
    }

    protected startNewChat(): void {
        this.activeChatId.set(null);
        this.isCreatingNewChat.set(true);
        this.messages.set([]);
        this.replyText.set("");
    }

    protected handleReplyInput(event: Event): void {
        this.replyText.set((event.target as HTMLTextAreaElement).value);
    }

    protected async selectChat(chatId: number): Promise<void> {
        this.activeChatId.set(chatId);
        this.isCreatingNewChat.set(false);
        await this.loadMessages(chatId);
    }

    protected async toggleOnlyUnread(event: Event): Promise<void> {
        const isChecked: boolean = (event.target as HTMLInputElement).checked;
        this.showOnlyUnread.set(isChecked);
        await this.loadChats();
    }

    protected async sendReply(): Promise<void> {
        const text: string = this.replyText().trim();

        if (!text || this.sending()) {
            return;
        }

        this.sending.set(true);
        this.errorMessage.set(null);

        try {
            if (this.isCreatingNewChat()) {
                const response: OpenChatResponseType = await this.chatService.createChat(text);
                this.isCreatingNewChat.set(false);
                this.replyText.set("");
                await this.loadChats();
                this.activeChatId.set(response.chatId);
                await this.loadMessages(response.chatId);
            } else {
                const chatId: number | null = this.activeChatId();

                if (chatId === null) {
                    return;
                }

                const newMessage: MessageType = await this.chatService.sendMessage(chatId, text);
                this.messages.update((items: MessageType[]) => [...items, newMessage]);
                this.replyText.set("");
            }
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося надіслати повідомлення."));
        } finally {
            this.sending.set(false);
        }
    }

    private async loadChats(): Promise<void> {
        this.loadingChats.set(true);
        this.errorMessage.set(null);

        try {
            const result: PagedResultType<ChatType> = await this.chatService.getChatsLazy(this.showOnlyUnread(), 1, 50);
            this.chats.set(result.items);

            const activeChatId: number | null = this.activeChatId();
            if (activeChatId !== null && !result.items.some((chat: ChatType) => chat.id === activeChatId)) {
                this.activeChatId.set(null);
                this.messages.set([]);
            }
        } catch (error) {
            this.errorMessage.set(this.getReadableError(error, "Не вдалося завантажити список чатів."));
        } finally {
            this.loadingChats.set(false);
        }
    }

    private async loadMessages(chatId: number): Promise<void> {
        this.loadingMessages.set(true);

        try {
            const result: PagedResultType<MessageType> = await this.chatService.getMessagesLazy(chatId, 1, 50);
            this.messages.set(result.items);
        } catch (error) {
            this.messages.set([]);
            this.errorMessage.set(this.getReadableError(error, "Не вдалося завантажити повідомлення."));
        } finally {
            this.loadingMessages.set(false);
        }
    }

    private getReadableError(error: unknown, fallback: string): string {
        if (error instanceof Error && error.message) {
            return error.message;
        }

        return fallback;
    }
}