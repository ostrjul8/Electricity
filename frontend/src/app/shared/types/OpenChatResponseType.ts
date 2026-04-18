import { MessageType } from "@shared/types/MessageType";

export type OpenChatResponseType = {
    chatId: number;
    isNewChatCreated: boolean;
    message: MessageType;
};
