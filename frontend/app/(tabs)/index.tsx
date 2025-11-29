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
  createdAt: string;
}

export default function HomeScreen() {
  const router = useRouter();
  const [userName, setUserName] = useState("");
  const [recentTrips, setRecentTrips] = useState<Trip[]>([]);
  const [isLoadingTrips, setIsLoadingTrips] = useState(false);

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

  const fetchRecentTrips = useCallback(async () => {
    setIsLoadingTrips(true);
    try {
      const response = await api.get("/api/Routes/user");
      const trips: Trip[] = response.data || [];
      
      // En yeni 3 trip'i al ve createdAt'e göre ters sırala
      const sortedTrips = trips
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
        .slice(0, 3);
      
      setRecentTrips(sortedTrips);
    } catch (error) {
      console.error("Error fetching recent trips:", error);
    } finally {
      setIsLoadingTrips(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      fetchRecentTrips();
    }, [fetchRecentTrips])
  );

  const handleMyTripsPress = () => {
    router.push('/(tabs)/my-trips');
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

        {/* My Trips Button */}
        <TouchableOpacity 
          style={styles.myTripsButton}
          onPress={handleMyTripsPress}
          activeOpacity={0.8}
        >
          <View style={styles.myTripsButtonContent}>
            <IconSymbol name="map.fill" size={24} color="#0d9488" />
            <View style={styles.myTripsButtonText}>
              <Text style={styles.myTripsButtonTitle}>My Trips</Text>
              <Text style={styles.myTripsButtonSubtitle}>View all your trips</Text>
            </View>
            <IconSymbol name="chevron.right" size={20} color="#9ca3af" />
          </View>
        </TouchableOpacity>

        {/* Recent Trips */}
        {recentTrips.length > 0 && (
          <View style={styles.recentTripsSection}>
            <View style={styles.sectionHeader}>
              <Text style={styles.sectionTitle}>Recent Trips</Text>
              <TouchableOpacity onPress={handleMyTripsPress}>
                <Text style={styles.seeAllText}>See All</Text>
              </TouchableOpacity>
            </View>
            
            {isLoadingTrips ? (
              <View style={styles.loadingContainer}>
                <ActivityIndicator size="small" color="#0d9488" />
              </View>
            ) : (
              <View style={styles.tripsGrid}>
                {recentTrips.map((trip, index) => {
                  const colors = ["#97D8FF", "#D8B389", "#FFB6C1", "#B4E7CE"];
                  const backgroundColor = colors[index % 4];

                  return (
                    <TouchableOpacity
                      key={trip.id}
                      style={[styles.tripCard, { backgroundColor }]}
                      onPress={() => handleTripPress(trip.id)}
                      activeOpacity={0.9}
                    >
                      <View style={styles.tripCardContent}>
                        <Text style={styles.tripTitle} numberOfLines={2}>
                          {trip.name}
                        </Text>
                        <Text style={styles.tripMeta}>{trip.region}</Text>
                        <View style={styles.tripFooter}>
                          <IconSymbol name="calendar" size={14} color="#4A4A4A" />
                          <Text style={styles.tripDays}>{trip.daysCount} days</Text>
                        </View>
                      </View>
                    </TouchableOpacity>
                  );
                })}
              </View>
            )}
          </View>
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
  myTripsButton: {
    backgroundColor: "#F0F9FB",
    borderRadius: 16,
    padding: 16,
    marginBottom: 24,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  myTripsButtonContent: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
  },
  myTripsButtonText: {
    flex: 1,
  },
  myTripsButtonTitle: {
    fontSize: 18,
    fontWeight: "700",
    color: "#222222",
    marginBottom: 2,
  },
  myTripsButtonSubtitle: {
    fontSize: 13,
    color: "#6b7280",
  },
  recentTripsSection: {
    marginTop: 24,
    marginBottom: 24,
  },
  sectionHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 16,
  },
  sectionTitle: {
    fontSize: 20,
    fontWeight: "700",
    color: "#222222",
  },
  seeAllText: {
    fontSize: 14,
    fontWeight: "600",
    color: "#0d9488",
  },
  loadingContainer: {
    paddingVertical: 20,
    alignItems: "center",
  },
  tripsGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 12,
    justifyContent: "space-between",
  },
  tripCard: {
    width: "31%",
    height: 140,
    borderRadius: 16,
    padding: 12,
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
  tripTitle: {
    fontSize: 14,
    fontWeight: "700",
    color: "#222222",
    marginBottom: 4,
  },
  tripMeta: {
    fontSize: 12,
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
