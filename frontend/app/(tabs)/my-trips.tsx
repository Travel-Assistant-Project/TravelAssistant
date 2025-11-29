import React, { useState, useCallback } from "react";
import {
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
  ActivityIndicator,
  StatusBar,
} from "react-native";
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

export default function MyTripsScreen() {
  const router = useRouter();
  const [trips, setTrips] = useState<Trip[]>([]);
  const [favoriteIds, setFavoriteIds] = useState<number[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchUserTrips = useCallback(async () => {
    setIsLoading(true);
    try {
      const [favoritesResponse, tripsResponse] = await Promise.all([
        api.get("/api/Favorites/itineraries/ids"),
        api.get("/api/Routes/user"),
      ]);

      const favIds: number[] = favoritesResponse.data || [];
      setFavoriteIds(favIds);

      const data: Trip[] = tripsResponse.data.map((t: Trip) => ({
        ...t,
        isFavorite: favIds.includes(t.id),
      }));

      // En yeni trip'i en başta göstermek için createdAt'e göre ters sırala
      const sortedData = data.sort((a, b) => 
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      );

      setTrips(sortedData);
    } catch (error: any) {
      console.error("Error fetching trips:", error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Fetch trips when screen comes into focus
  useFocusEffect(
    useCallback(() => {
      fetchUserTrips();
    }, [fetchUserTrips])
  );

  const toggleFavorite = async (tripId: number, current: boolean) => {
    try {
      if (current) {
        // Remove from favorites
        await api.delete(`/api/Favorites/itineraries/${tripId}`);
        setFavoriteIds(prev => prev.filter(id => id !== tripId));
      } else {
        // Add to favorites
        await api.post(`/api/Favorites/itineraries/${tripId}`);
        setFavoriteIds(prev => [...prev, tripId]);
      }

      setTrips(prev =>
        prev.map(t =>
          t.id === tripId ? { ...t, isFavorite: !current } : t
        )
      );
    } catch (error) {
      console.error("Error toggling favorite:", error);
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
      <StatusBar barStyle="dark-content" backgroundColor="#FFFFFF" />
      
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity 
          style={styles.backButton}
          onPress={() => router.back()}
        >
          <IconSymbol name="chevron.left" size={24} color="#222222" />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>My Trips</Text>
        <TouchableOpacity 
          style={styles.refreshButton}
          onPress={fetchUserTrips}
        >
          <IconSymbol name="arrow.clockwise" size={20} color="#0d9488" />
        </TouchableOpacity>
      </View>

      <ScrollView
        style={styles.container}
        contentContainerStyle={styles.content}
        showsVerticalScrollIndicator={false}
      >
        {isLoading && (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color="#0d9488" />
            <Text style={styles.loadingText}>Loading your trips...</Text>
          </View>
        )}
        
        {!isLoading && trips.length === 0 && (
          <View style={styles.emptyContainer}>
            <IconSymbol name="map" size={64} color="#d1d5db" />
            <Text style={styles.emptyText}>No trips yet</Text>
            <Text style={styles.emptySubtext}>Create your first trip to see it here!</Text>
            <TouchableOpacity 
              style={styles.createButton}
              onPress={() => router.push('/(tabs)/new-trip')}
            >
              <Text style={styles.createButtonText}>Create New Trip</Text>
            </TouchableOpacity>
          </View>
        )}
        
        {!isLoading && trips.length > 0 && (
          <View style={styles.tripsGrid}>
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
                      size={18}
                      color={trip.isFavorite ? "#dc2626" : "#6b7280"}
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
                        <IconSymbol name="calendar" size={14} color="#FFFFFF" />
                        <Text style={styles.tripDays}>{trip.daysCount} days</Text>
                      </View>
                    </View>
                  </TouchableOpacity>
                </View>
              );
            })}
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: "#FFFFFF",
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: "#F3F4F6",
  },
  backButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: "#F5F5F7",
    alignItems: "center",
    justifyContent: "center",
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: "700",
    color: "#222222",
  },
  refreshButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: "#F5F5F7",
    alignItems: "center",
    justifyContent: "center",
  },
  container: {
    flex: 1,
  },
  content: {
    paddingHorizontal: 20,
    paddingTop: 20,
    paddingBottom: 40,
  },
  loadingContainer: {
    paddingVertical: 60,
    alignItems: "center",
    gap: 16,
  },
  loadingText: {
    fontSize: 16,
    color: "#6b7280",
  },
  emptyContainer: {
    paddingVertical: 60,
    alignItems: "center",
    backgroundColor: "#f9fafb",
    borderRadius: 16,
    marginTop: 40,
  },
  emptyText: {
    fontSize: 20,
    fontWeight: "600",
    color: "#6b7280",
    marginTop: 16,
  },
  emptySubtext: {
    fontSize: 14,
    color: "#9ca3af",
    marginTop: 8,
    marginBottom: 24,
  },
  createButton: {
    backgroundColor: "#0d9488",
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 12,
    marginTop: 8,
  },
  createButtonText: {
    color: "#FFFFFF",
    fontSize: 16,
    fontWeight: "600",
  },
  tripsGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 16,
    justifyContent: "space-between",
  },
  tripCard: {
    width: "47%",
    height: 160,
    borderRadius: 18,
    padding: 16,
    justifyContent: "flex-end",
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
    top: 12,
    right: 12,
    width: 36,
    height: 36,
    borderRadius: 18,
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
    fontSize: 16,
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
    gap: 6,
    marginTop: 8,
  },
  tripDays: {
    fontSize: 13,
    color: "#4A4A4A",
    fontWeight: "500",
  },
});

