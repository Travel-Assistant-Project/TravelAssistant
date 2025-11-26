// app/(auth)/register.tsx
import React, { useState } from "react";
import { View, Text, TextInput, TouchableOpacity, Alert, StyleSheet } from "react-native";
import { useRouter } from "expo-router";
import { register } from "../../services/auth";

export default function RegisterScreen() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [age, setAge] = useState("23");
  const [country, setCountry] = useState("Turkey");
  const [city, setCity] = useState("Istanbul");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);

  const handleRegister = async () => {
    try {
      setLoading(true);
      await register({
        name,
        email,
        password,
        age: Number(age),
        country,
        city,
      });

      Alert.alert("Success", "Account created!");
      router.replace("/(tabs)");
    } catch (err: any) {
      console.log("Register error:", err?.response?.data ?? err.message);
      Alert.alert(
        "Register failed",
        err?.response?.data?.message ?? err.message ?? "Unknown error"
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Create an account</Text>

      <TextInput placeholder="Name" value={name} onChangeText={setName} style={styles.input} />
      <TextInput
        placeholder="Age"
        value={age}
        onChangeText={setAge}
        keyboardType="number-pad"
        style={styles.input}
      />
      <TextInput
        placeholder="Country"
        value={country}
        onChangeText={setCountry}
        style={styles.input}
      />
      <TextInput placeholder="City" value={city} onChangeText={setCity} style={styles.input} />

      <TextInput
        placeholder="Email"
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        keyboardType="email-address"
        style={styles.input}
      />

      <TextInput
        placeholder="Password"
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        style={styles.input}
      />

      <TouchableOpacity
        onPress={handleRegister}
        disabled={loading}
        style={[styles.button, loading && { opacity: 0.7 }]}
      >
        <Text style={styles.buttonText}>{loading ? "Creating..." : "Register"}</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, padding: 24, justifyContent: "center", gap: 12 },
  title: { fontSize: 24, fontWeight: "600", marginBottom: 16 },
  input: {
    borderWidth: 1,
    borderColor: "#ccc",
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
  },
  button: {
    backgroundColor: "#5C9B9B",
    paddingVertical: 14,
    borderRadius: 8,
    alignItems: "center",
    marginTop: 8,
  },
  buttonText: { color: "#fff", fontWeight: "600" },
});
