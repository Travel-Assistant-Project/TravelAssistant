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

interface UserRecommendations {
  totalFavoriteItineraries: number;
  totalFavoritePlaces: number;
  totalCreatedItineraries: number;
  preferredThemes: string[];
  preferredRegions: string[];
  preferredBudgetRange: string;
  preferredIntensity: string;
  aiRecommendations: string;
}

export default function HomeScreen() {
  const router = useRouter();
  const [userName, setUserName] = useState("");
  const [recentTrips, setRecentTrips] = useState<Trip[]>([]);
  const [isLoadingTrips, setIsLoadingTrips] = useState(false);
  const [recommendations, setRecommendations] = useState<UserRecommendations | null>(null);
  const [isLoadingRecommendations, setIsLoadingRecommendations] = useState(false);

  // App açıldığında bir kere çalışacak
  useEffect(() => {
    const loadUserInfo = async () => {
      const stored = await AsyncStorage.getItem("userInfo");
      if (stored) {
        const data = JSON.parse(stored);
        setUserName(data.name ?? "");
      }
    };

    const loadCachedRecommendations = async () => {
      try {
        const cached = await AsyncStorage.getItem("cachedRecommendations");
        if (cached) {
          setRecommendations(JSON.parse(cached));
        } else {
          // Cache yoksa, yeni öneri al
          await fetchRecommendations();
        }
      } catch (error) {
        console.error("Error loading cached recommendations:", error);
        await fetchRecommendations();
      }
    };

    loadUserInfo();
    loadCachedRecommendations();
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

  const fetchRecommendations = async () => {
    setIsLoadingRecommendations(true);
    try {
      const response = await api.get("/api/AI/user-recommendations");
      setRecommendations(response.data);
      
      // Yeni öneriyi cache'e kaydet
      await AsyncStorage.setItem("cachedRecommendations", JSON.stringify(response.data));
    } catch (error) {
      console.error("Error fetching recommendations:", error);
    } finally {
      setIsLoadingRecommendations(false);
    }
  };

  useFocusEffect(
    useCallback(() => {
      fetchRecentTrips();
      // fetchRecommendations'ı buradan çıkardık - sadece başta çalışacak
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

        {/* AI Recommendations Section */}
        {recommendations && (
          <View style={styles.recommendationsSection}>
            <View style={styles.recommendationsHeader}>
              <View style={styles.recommendationsHeaderLeft}>
                <IconSymbol name="sparkles" size={22} color="#0d9488" />
                <Text style={styles.recommendationsTitle}>Recommendations for You</Text>
              </View>
              <TouchableOpacity 
                style={styles.refreshButton}
                onPress={fetchRecommendations}
                disabled={isLoadingRecommendations}
                activeOpacity={0.7}
              >
                <IconSymbol 
                  name="arrow.clockwise" 
                  size={20} 
                  color={isLoadingRecommendations ? "#9ca3af" : "#0d9488"} 
                />
              </TouchableOpacity>
            </View>
            
            {/* User Stats */}
            <View style={styles.statsContainer}>
              <View style={styles.statItem}>
                <IconSymbol name="heart.fill" size={18} color="#0d9488" />
                <Text style={styles.statNumber}>{recommendations.totalFavoriteItineraries}</Text>
                <Text style={styles.statLabel}>Routes</Text>
              </View>
              <View style={styles.statItem}>
                <IconSymbol name="location.fill" size={18} color="#0d9488" />
                <Text style={styles.statNumber}>{recommendations.totalFavoritePlaces}</Text>
                <Text style={styles.statLabel}>Places</Text>
              </View>
              <View style={styles.statItem}>
                <IconSymbol name="map.fill" size={18} color="#0d9488" />
                <Text style={styles.statNumber}>{recommendations.totalCreatedItineraries}</Text>
                <Text style={styles.statLabel}>Created</Text>
              </View>
            </View>

            {/* Preferred Themes & Regions */}
            {(recommendations.preferredThemes.length > 0 || recommendations.preferredRegions.length > 0) && (
              <View style={styles.preferencesRow}>
                {recommendations.preferredThemes.length > 0 && (
                  <View style={styles.preferencesColumn}>
                    <Text style={styles.preferencesLabel}>Your Themes</Text>
                    <View style={styles.tagsContainer}>
                      {recommendations.preferredThemes.slice(0, 2).map((theme, index) => (
                        <View key={index} style={styles.tag}>
                          <Text style={styles.tagText}>{theme}</Text>
                        </View>
                      ))}
                    </View>
                  </View>
                )}
                
                {recommendations.preferredRegions.length > 0 && (
                  <View style={styles.preferencesColumn}>
                    <Text style={styles.preferencesLabel}>Top Regions</Text>
                    <View style={styles.tagsContainer}>
                      {recommendations.preferredRegions.slice(0, 2).map((region, index) => (
                        <View key={index} style={styles.regionTag}>
                          <Text style={styles.regionTagText}>{region}</Text>
                        </View>
                      ))}
                    </View>
                  </View>
                )}
              </View>
            )}

            {/* AI Recommendations Text */}
            {recommendations.aiRecommendations && (
              <View style={styles.aiTextContainer}>
                <Text style={styles.aiText}>{recommendations.aiRecommendations}</Text>
              </View>
            )}
          </View>
        )}

        {/* Show loading only if we don't have recommendations yet */}
        {!recommendations && isLoadingRecommendations && (
          <View style={styles.recommendationsLoading}>
            <ActivityIndicator size="small" color="#0d9488" />
            <Text style={styles.loadingText}>Loading recommendations...</Text>
          </View>
        )}

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

        {/* Suggestion
        <View style={styles.suggestionCard}>
          <IconSymbol name="lightbulb" size={18} color="#5C9B9B" />
          <Text style={styles.suggestionText}>
            Would you like a coastal trip this week?
          </Text>
        </View> */}
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
  // AI Recommendations Styles
  recommendationsSection: {
    marginTop: 2,
    marginBottom: 24,
    backgroundColor: "#F0F9FB",
    borderRadius: 20,
    padding: 20,
    borderWidth: 1,
    borderColor: "#d1fae5",
  },
  recommendationsHeader: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    marginBottom: 20,
  },
  recommendationsHeaderLeft: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
  },
  refreshButton: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: "#FFFFFF",
    alignItems: "center",
    justifyContent: "center",
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  recommendationsTitle: {
    fontSize: 18,
    fontWeight: "700",
    color: "#222222",
  },
  statsContainer: {
    flexDirection: "row",
    justifyContent: "space-around",
    alignItems: "center",
    paddingVertical: 16,
    paddingHorizontal: 8,
    backgroundColor: "#FFFFFF",
    borderRadius: 16,
    marginBottom: 16,
  },
  statItem: {
    alignItems: "center",
    gap: 6,
  },
  statNumber: {
    fontSize: 24,
    fontWeight: "700",
    color: "#0d9488",
  },
  statLabel: {
    fontSize: 11,
    color: "#6b7280",
    fontWeight: "500",
    textTransform: "uppercase",
  },
  preferencesRow: {
    flexDirection: "row",
    gap: 12,
    marginBottom: 12,
  },
  preferencesColumn: {
    flex: 1,
  },
  preferencesLabel: {
    fontSize: 13,
    fontWeight: "600",
    color: "#4A4A4A",
    marginBottom: 10,
    textTransform: "uppercase",
  },
  tagsContainer: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 6,
  },
  tag: {
    backgroundColor: "#FFFFFF",
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: "#0d9488",
  },
  tagText: {
    fontSize: 12,
    color: "#0d9488",
    fontWeight: "600",
  },
  regionTag: {
    backgroundColor: "#FFFFFF",
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: "#0d9488",
  },
  regionTagText: {
    fontSize: 12,
    color: "#0d9488",
    fontWeight: "600",
  },
  aiTextContainer: {
    marginTop: 4,
    padding: 16,
    backgroundColor: "#FFFFFF",
    borderRadius: 16,
    borderLeftWidth: 3,
    borderLeftColor: "#0d9488",
  },
  aiText: {
    fontSize: 13,
    color: "#4A4A4A",
    lineHeight: 20,
  },
  recommendationsLoading: {
    marginTop: 24,
    padding: 20,
    alignItems: "center",
    backgroundColor: "#F0F9FB",
    borderRadius: 16,
    gap: 12,
  },
  loadingText: {
    fontSize: 14,
    color: "#6b7280",
  },
});
