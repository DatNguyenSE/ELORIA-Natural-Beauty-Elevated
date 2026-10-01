import { Order, OrderItem } from "./order.model";

export type User = {
    id: string;
    userName?: string;
    fullName?: string;
    email: string;
    token: string;
    imageUrl?: string;
    points?: number;
    roles: string[];
}

export type UserProfile = {
    id: string;
    userName?: string;
    fullName: string;
    email: string;
    token: string;
    imageUrl?: string;
    address?: string;
    phoneNumber?: string;
    gender?: 'male' | 'female' | 'other';
    dateOfBirth?: string;
    orders : Order[];
}

export type authenLoginCreds = {
    email: string;
    password: string;
    fullname: string;
}

export type authenResponse = {
    user: User;
    message: string;
    isNewUser : boolean;
    requireOtp?: boolean;
}

export type RegisterCreds = {
    email: string;
    password: string;
    username: string
}

export interface VerifyOtpCreds {
  email: string;
  otpCode: string;
}