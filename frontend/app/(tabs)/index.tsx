import React, { useEffect, useState, useCallback } from "react";
import {
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
  ActivityIndicator,
} from "react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { IconSymbol } from "@/components/ui/icon-symbol";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter, useFocusEffect } from "expo-router";
import { api } from "@/lib/api";

interface Trip {
  id: number;
  name: string;
  region: string;
  daysCount: number;
  theme: number;
  budget: number;
  intensity: number;
  transport: number;
  isAiGenerated: boolean;
  isFavorite: boolean;
  createdAt: string;
}

export default function HomeScreen() {
  const router = useRouter();
  const [userName, setUserName] = useState("");
  const [trips, setTrips] = useState<Trip[]>([]);
  const [favoriteIds, setFavoriteIds] = useState<number[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadUserInfo = async () => {
      const stored = await AsyncStorage.getItem("userInfo");
      if (stored) {
        const data = JSON.parse(stored);
        setUserName(data.name ?? "");
      }
    };

    loadUserInfo();
  }, []);

  const fetchUserTrips = useCallback(async () => {
    setIsLoading(true);
    try {
      const [stored, response] = await Promise.all([
        AsyncStorage.getItem("favoriteTripIds"),
        api.get("/api/Routes/user"),
      ]);

      const favIds: number[] = stored ? JSON.parse(stored) : [];
      setFavoriteIds(favIds);

      const data: Trip[] = response.data.map((t: Trip) => ({
        ...t,
        isFavorite: favIds.includes(t.id),
      }));

      setTrips(data);
    } catch (error: any) {
      console.error("Error fetching trips:", error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Fetch trips when screen comes into focus (e.g., after creating a new trip)
  useFocusEffect(
    useCallback(() => {
      fetchUserTrips();
    }, [fetchUserTrips])
  );

  const toggleFavorite = async (tripId: number, current: boolean) => {
    try {
      const newIds = current
        ? favoriteIds.filter(id => id !== tripId)
        : [...favoriteIds, tripId];

      setFavoriteIds(newIds);
      await AsyncStorage.setItem("favoriteTripIds", JSON.stringify(newIds));

      setTrips(prev =>
        prev.map(t =>
          t.id === tripId ? { ...t, isFavorite: !current } : t
        )
      );
    } catch (error) {
      console.log("Error toggling favorite (local):", error);
    }
  };

  const handleTripPress = (tripId: number) => {
    router.push({
      pathname: "/trip-detail",
      params: { itineraryId: tripId.toString() },
    });
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      {/* kaymayı çözüyor */}
      <ScrollView
        style={styles.container}
        contentContainerStyle={styles.content}
        showsVerticalScrollIndicator={false}
      >
  
        {/* Header */}
        <View style={styles.headerRow}>
          <View>
            <Text style={styles.greeting}>Hello, {userName} 👋</Text>
            <Text style={styles.subtitle}>Where would you like to go?</Text>
          </View>

          <TouchableOpacity style={styles.bellButton}>
            <IconSymbol name="bell" size={20} color="#4A4A4A" />
          </TouchableOpacity>
        </View>

        {/* Search */}
        <View style={styles.searchContainer}>
          <IconSymbol name="magnifyingglass" size={18} color="#8E8E8F" />
          <TextInput
            placeholder="Where do you want to go?"
            style={styles.searchInput}
          />
        </View>

        {/* My Trips */}
        <View style={styles.sectionHeader}>
          <Text style={styles.sectionTitle}>My Trips</Text>
          {trips.length > 0 && (
            <TouchableOpacity onPress={fetchUserTrips}>
              <IconSymbol name="arrow.clockwise" size={18} color="#0d9488" />
            </TouchableOpacity>
          )}
        </View>

        {isLoading && (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="small" color="#0d9488" />
          </View>
        )}
        
        {!isLoading && trips.length === 0 && (
          <View style={styles.emptyContainer}>
            <IconSymbol name="map" size={40} color="#d1d5db" />
            <Text style={styles.emptyText}>No trips yet</Text>
            <Text style={styles.emptySubtext}>Create your first trip to see it here!</Text>
          </View>
        )}
        
        {!isLoading && trips.length > 0 && (
          <ScrollView horizontal showsHorizontalScrollIndicator={false}>
            {trips.map((trip, index) => {
              const colors = ["#97D8FF", "#D8B389", "#FFB6C1", "#B4E7CE"];
              const backgroundColor = colors[index % 4];

              return (
                <View
                  key={trip.id}
                  style={[
                    styles.tripCard,
                    { backgroundColor },
                  ]}
                >
                  <TouchableOpacity
                    style={styles.favoriteButton}
                    onPress={() => toggleFavorite(trip.id, trip.isFavorite)}
                    activeOpacity={0.8}
                  >
                    <IconSymbol
                      name={trip.isFavorite ? "heart.fill" : "heart"}
                      size={16}
                      color={trip.isFavorite ? "#dc2626" : "#4A4A4A"}
                    />
                  </TouchableOpacity>

                  <TouchableOpacity
                    style={{ flex: 1 }}
                    onPress={() => handleTripPress(trip.id)}
                    activeOpacity={0.9}
                  >
                    <View style={styles.tripCardContent}>
                      <Text style={styles.tripTitle} numberOfLines={2}>
                        {trip.name}
                      </Text>
                      <Text style={styles.tripMeta}>{trip.region}</Text>
                      <View style={styles.tripFooter}>
                        <IconSymbol name="calendar" size={12} color="#4A4A4A" />
                        <Text style={styles.tripDays}>{trip.daysCount} days</Text>
                      </View>
                    </View>
                  </TouchableOpacity>
                </View>
              );
            })}
          </ScrollView>
        )}

        {/* Suggestion */}
        <View style={styles.suggestionCard}>
          <IconSymbol name="lightbulb" size={18} color="#5C9B9B" />
          <Text style={styles.suggestionText}>
            Would you like a coastal trip this week?
          </Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: "#FFFFFF",
  },
  container: {
    flex: 1,
  },
  content: {
    paddingHorizontal: 24,
    paddingBottom: 40,
  },
  headerRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 24,
    marginTop: 16, // 🔥 Yeterli boşluk, kaymayı engelliyor
  },
  greeting: {
    fontSize: 24,
    fontWeight: "700",
    color: "#222222",
  },
  subtitle: {
    fontSize: 14,
    color: "#777777",
    marginTop: 4,
  },
  bellButton: {
    width: 36,
    height: 36,
    borderRadius: 18,
    borderWidth: 1,
    borderColor: "#E5E5EA",
    alignItems: "center",
    justifyContent: "center",
  },
  searchContainer: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#F5F5F7",
    borderRadius: 16,
    paddingHorizontal: 14,
    paddingVertical: 10,
    marginBottom: 24,
  },
  searchInput: {
    marginLeft: 8,
    flex: 1,
    fontSize: 14,
  },
  sectionHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 12,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: "600",
    color: "#222222",
  },
  loadingContainer: {
    paddingVertical: 40,
    alignItems: "center",
  },
  emptyContainer: {
    paddingVertical: 40,
    alignItems: "center",
    backgroundColor: "#f9fafb",
    borderRadius: 16,
    marginBottom: 24,
  },
  emptyText: {
    fontSize: 16,
    fontWeight: "600",
    color: "#6b7280",
    marginTop: 12,
  },
  emptySubtext: {
    fontSize: 14,
    color: "#9ca3af",
    marginTop: 4,
  },
  tripCard: {
    width: 160,
    height: 140,
    borderRadius: 18,
    padding: 16,
    justifyContent: "flex-end",
    marginRight: 12,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  tripCardContent: {
    flex: 1,
    justifyContent: "space-between",
  },
  favoriteButton: {
    position: "absolute",
    right: 10,
    bottom: 10,
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: "rgba(255,255,255,0.95)",
    alignItems: "center",
    justifyContent: "center",
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 3,
    elevation: 3,
    zIndex: 10,
  },
  tripTitle: {
    fontSize: 15,
    fontWeight: "700",
    color: "#222222",
    marginBottom: 4,
  },
  tripMeta: {
    fontSize: 13,
    color: "#4A4A4A",
    marginTop: 4,
  },
  tripFooter: {
    flexDirection: "row",
    alignItems: "center",
    gap: 4,
    marginTop: 8,
  },
  tripDays: {
    fontSize: 12,
    color: "#4A4A4A",
    fontWeight: "500",
  },
  suggestionCard: {
    marginTop: 24,
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#F0F9FB",
    borderRadius: 16,
    padding: 16,
    gap: 8,
  },
  suggestionText: {
    fontSize: 14,
    color: "#333333",
  },
});
