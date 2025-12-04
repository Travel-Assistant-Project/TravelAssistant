// src/lib/api.ts
import axios from "axios";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { API_BASE_URL } from "../constants/api";

export const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 60000, // 60 seconds for AI requests
});

// Her istekten önce accessToken'ı ekleyen interceptor
api.interceptors.request.use(async (config) => {
  const token = await AsyncStorage.getItem("accessToken");
  console.log('[API] Token from storage:', token ? 'exists' : 'NOT FOUND');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
    console.log('[API] Authorization header set');
  } else {
    console.log('[API] WARNING: No token found, request will be unauthorized');
  }
  return config;
});
