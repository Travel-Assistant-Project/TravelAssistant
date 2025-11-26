// src/constants/api.ts
import { Platform } from "react-native";

const LOCAL_BASE_URL =
  Platform.OS === "android"
    ? "http://10.0.2.2:5201"   // Android Emulator
    : "http://localhost:5201"; // iOS Simulator / web

// Eğer gerçek telefonda test edeceksen:
// Mac'inin IP'sini bul (ör: 192.168.1.34) ve aşağıyı kullan:
// const LOCAL_BASE_URL = "http://192.168.1.34:5191";

export const API_BASE_URL = LOCAL_BASE_URL;
