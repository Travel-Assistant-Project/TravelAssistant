// src/services/auth.ts
import { api } from "../lib/api";
import AsyncStorage from "@react-native-async-storage/async-storage";

export interface RegisterPayload {
  name: string;
  email: string;
  password: string;
  age: number;
  country: string;
  city: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export async function register(payload: RegisterPayload) {
  const response = await api.post("/api/auth/register", payload);
  const data = response.data;

  const token = data.token ?? data.accessToken;
  if (!token) {
    throw new Error("Token not found in register response");
  }

  const userInfo = {
    name: data.name,
    email: data.email,
  };

  await AsyncStorage.multiSet([
    ["accessToken", token],
    ["userInfo", JSON.stringify(userInfo)],
  ]);

  return data;
}

export async function login(payload: LoginPayload) {
  const response = await api.post("/api/auth/login", payload);
  const data = response.data;

  const token = data.token ?? data.accessToken;
  if (!token) {
    throw new Error("Token not found in login response");
  }

  const userInfo = {
    name: data.name,
    email: data.email,
  };

  await AsyncStorage.multiSet([
    ["accessToken", token],
    ["userInfo", JSON.stringify(userInfo)],
  ]);

  return data;
}

export async function logout() {
  await AsyncStorage.multiRemove(["accessToken", "userInfo"]);
}
