import { CommonModule } from "@angular/common";
import { Component, computed, inject, OnInit, signal } from "@angular/core";
import { ChatService } from "@shared/services/chat.service";
import { ChatType } from "@shared/types/ChatType";
import { MessageType } from "@shared/types/MessageType";
import { OpenChatResponseType } from "@shared/types/OpenChatResponseType";
import { PagedResultType } from "@shared/types/PagedResultType";
import { Button } from "@shared/components/button/button";
import { Textarea } from "@shared/components/textarea/textarea";

@Component({
    selector: "app-chats",
    imports: [CommonModule, Button, Textarea],
    templateUrl: "./chats.html",
    styleUrl: "./chats.css",
})
export class Chats implements OnInit {
    private readonly chatService: ChatService = inject(ChatService);

    protected readonly chats = signal<ChatType[]>([]);
    protected readonly activeChatId = signal<number | null>(null);
    protected readonly isCreatingNewChat = signal<boolean>(false);
    protected readonly messages = signal<MessageType[]>([]);
    protected readonly loadingChats = signal<boolean>(true);
    protected readonly loadingMessages = signal<boolean>(false);
    protected readonly sending = signal<boolean>(false);
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly replyText = signal<string>("");

    protected readonly activeChat = computed<ChatType | null>(() => {
        const chatId: number | null = this.activeChatId();

        if (chatId === null) {
            return null;
        }

        return this.chats().find((chat: ChatType) => chat.id === chatId) ?? null;
    });

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

    protected async sendReply(): Promise<void> {
        const text: string = this.replyText().trim();

        if (!text || this.sending()) {
            return;
        }

        this.sending.set(true);
        this.errorMessage.set(null);

        try {
            // Якщо це новий чат, спочатку створюємо його з першим повідомленням
            if (this.isCreatingNewChat()) {
                const response: OpenChatResponseType = await this.chatService.createChat(text);
                this.isCreatingNewChat.set(false);
                this.replyText.set("");
                await this.loadChats();
                this.activeChatId.set(response.chatId);
                await this.loadMessages(response.chatId);
            } else {
                // Інакше відправляємо звичайне повідомлення до існуючого чату
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
            const result: PagedResultType<ChatType> = await this.chatService.getChatsLazy(false, 1, 50);
            this.chats.set(result.items);
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