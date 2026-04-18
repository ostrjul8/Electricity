export type PagedResultType<T> = {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    items: T[];
};
