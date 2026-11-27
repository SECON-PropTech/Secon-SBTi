export interface LoginUser {
    id: string;
    firstName: string;
    lastName: string;
    email: string | null;
}

export interface CurrentUser {
    id: string;
    name: string | null;
    email: string | null;
    roles: string[];
}

export interface ApiError {
    message: string;
}