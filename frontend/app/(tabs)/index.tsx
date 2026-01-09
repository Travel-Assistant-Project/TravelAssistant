import React, { useEffect, useState, useCallback } from "react";
import {
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
  ActivityIndicator,
  Alert,
  Image,
  Dimensions,
  FlatList,
} from "react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { IconSymbol } from "@/components/ui/icon-symbol";
import { SafeAreaView } from "react-native-safe-area-context";
import { useRouter, useFocusEffect } from "expo-router";
import { api } from "@/lib/api";

const { width } = Dimensions.get('window');

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

// Popüler yerler mock verisi
interface PopularPlace {
  id: number;
  name: string;
  location: string;
  image: string;
  rating: number;
}

const popularPlaces: PopularPlace[] = [
  {
    id: 1,
    name: "Santorini",
    location: "Greece",
    image: "https://images.unsplash.com/photo-1570077188670-e3a8d69ac5ff?w=400",
    rating: 4.8,
  },

  {
    id: 3,
    name: "Bali",
    location: "Indonesia",
    image: "https://images.unsplash.com/photo-1537953773345-d172ccf13cf1?w=400",
    rating: 4.7,
  },
  {
    id: 4,
    name: "Maldives",
    location: "Indian Ocean",
    image: "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=400",
    rating: 4.9,
  },
  {
    id: 5,
    name: "Tokyo",
    location: "Japan",
    image: "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=400",
    rating: 4.6,
  },

  {
    id: 8,
    name: "Swiss Alps",
    location: "Switzerland",
    image: "https://images.unsplash.com/photo-1531366936337-7c912a4589a7?w=400",
    rating: 4.9,
  },
  {
    id: 9,
    name: "Dubai",
    location: "UAE",
    image: "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=400",
    rating: 4.6,
  },
  {
    id: 10,
    name: "New York",
    location: "USA",
    image: "https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?w=400",
    rating: 4.5,
  },
  {
    id: 11,
    name: "Barcelona",
    location: "Spain",
    image: "https://images.unsplash.com/photo-1539037116277-4db20889f2d4?w=400",
    rating: 4.7,
  },
  {
    id: 12,
    name: "Prague",
    location: "Czech Republic",
    image: "https://images.unsplash.com/photo-1541849546-216549ae216d?w=400",
    rating: 4.8,
  },
  {
    id: 13,
    name: "Machu Picchu",
    location: "Peru",
    image: "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=400",
    rating: 4.9,
  },
];

export default function HomeScreen() {
  const router = useRouter();
  const [userName, setUserName] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
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

  const handleEventsPress = () => {
    router.push('/(tabs)/events');
  };

  const handleTripPress = (tripId: number) => {
    router.push({
      pathname: "/trip-detail",
      params: { itineraryId: tripId.toString() },
    });
  };

  const handleNotificationPress = () => {
    Alert.alert(
      "Notifications",
      "You have no new notifications at the moment.",
      [{ text: "OK", style: "default" }]
    );
  };

  const handleSearchSubmit = () => {
    if (searchQuery.trim()) {
      router.push({
        pathname: "/(tabs)/explore",
        params: { query: searchQuery.trim() },
      });
    } else {
      Alert.alert("Search", "Please enter a destination to search for trips.");
    }
  };

  const handleSearchPress = () => {
    router.push("/(tabs)/explore");
  };

  const renderPopularPlace = ({ item }: { item: PopularPlace }) => (
    <TouchableOpacity style={styles.placeCard} activeOpacity={0.8}>
      <Image source={{ uri: item.image }} style={styles.placeImage} />
      <View style={styles.placeGradient}>
        <View style={styles.placeInfo}>
          <Text style={styles.placeName}>{item.name}</Text>
          <Text style={styles.placeLocation}>{item.location}</Text>
          <View style={styles.ratingContainer}>
            <IconSymbol name="star.fill" size={12} color="#FFD700" />
            <Text style={styles.ratingText}>{item.rating}</Text>
          </View>
        </View>
      </View>
    </TouchableOpacity>
  );

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

          <TouchableOpacity 
            style={styles.bellButton}
            onPress={handleNotificationPress}
            activeOpacity={0.7}
          >
            <IconSymbol name="bell" size={20} color="#4A4A4A" />
          </TouchableOpacity>
        </View>

        {/* Search */}
        <TouchableOpacity 
          style={styles.searchContainer}
          onPress={handleSearchPress}
          activeOpacity={0.8}
        >
          <IconSymbol name="magnifyingglass" size={18} color="#8E8E8F" />
          <TextInput
            placeholder="Where do you want to go?"
            style={styles.searchInput}
            value={searchQuery}
            onChangeText={setSearchQuery}
            onSubmitEditing={handleSearchSubmit}
            returnKeyType="search"
            editable={true}
          />
        </TouchableOpacity>

        {/* Popular Places Section */}
        <View style={styles.popularPlacesSection}>
          <View style={styles.sectionHeader}>
            <Text style={styles.sectionTitle}>✨ Popular Destinations</Text>
            <TouchableOpacity onPress={() => router.push("/(tabs)/explore")}>
              <Text style={styles.seeAllText}>See All</Text>
            </TouchableOpacity>
          </View>
          
          <FlatList
            data={popularPlaces}
            renderItem={renderPopularPlace}
            keyExtractor={(item) => item.id.toString()}
            horizontal
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={styles.placesContainer}
            ItemSeparatorComponent={() => <View style={{ width: 16 }} />}
          />
        </View>

        {/* Quick Actions Section */}
        <View style={styles.quickActionsSection}>
          <Text style={styles.quickActionsTitle}>Quick Actions</Text>
          
          {/* My Trips Button */}
          <TouchableOpacity 
            style={styles.quickActionButton}
            onPress={handleMyTripsPress}
            activeOpacity={0.8}
          >
            <View style={styles.quickActionContent}>
              <IconSymbol name="map.fill" size={24} color="#0d9488" />
              <View style={styles.quickActionText}>
                <Text style={styles.quickActionTitle}>My Trips</Text>
                <Text style={styles.quickActionSubtitle}>View all your trips</Text>
              </View>
              <IconSymbol name="chevron.right" size={20} color="#9ca3af" />
            </View>
          </TouchableOpacity>

          {/* Events Button */}
          <TouchableOpacity 
            style={styles.quickActionButton}
            onPress={handleEventsPress}
            activeOpacity={0.8}
          >
            <View style={styles.quickActionContent}>
              <IconSymbol name="calendar" size={24} color="#0d9488" />
              <View style={styles.quickActionText}>
                <Text style={styles.quickActionTitle}>Find Events</Text>
                <Text style={styles.quickActionSubtitle}>Discover concerts & shows</Text>
              </View>
              <IconSymbol name="chevron.right" size={20} color="#9ca3af" />
            </View>
          </TouchableOpacity>
        </View>

        {/* AI Recommendations Section */}
        {recommendations && (
          <View style={styles.recommendationsSection}>
            <View style={styles.recommendationsHeader}>
              <View style={styles.recommendationsHeaderLeft}>
                <IconSymbol name="sparkles" size={25} color="#0d9488" />
                <Text style={styles.recommendationsTitle}>Your Best Matches</Text>
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
                  const colors = ["#E8F4F4", "#D4EDEE", "#BFE6E8", "#AAE0E2", "#94D9DC", "#7FB3D6"];
                  const backgroundColor = colors[index % 6];

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
    backgroundColor: "#FFFFFF", // Diğer sayfalarla tutarlı beyaz background
  },
  container: {
    flex: 1,
    backgroundColor: "#FFFFFF", // Diğer sayfalarla tutarlı beyaz background
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
    width: 40,
    height: 40,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: "#F0F4F8",
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#FAFBFC",
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 6,
  },
  searchContainer: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#F5F5F7", // Explore sayfası ile tutarlı gri background
    borderRadius: 16, // Explore sayfası ile tutarlı küçük radius
    paddingHorizontal: 14, // Explore sayfası ile tutarlı padding
    paddingVertical: 12, // Explore sayfası ile tutarlı padding
    marginBottom: 28,
    // Shadow ve border kaldırıldı - explore sayfası gibi sadeleştirildi
  },
  searchInput: {
    marginLeft: 8,
    flex: 1,
    fontSize: 15, // Explore sayfası ile tutarlı font size
    color: "#222222", // Explore sayfası ile tutarlı text color
  },
  quickActionsSection: {
    marginBottom: 24,
  },
  quickActionsTitle: {
    fontSize: 20,
    fontWeight: "700",
    color: "#222222",
    marginBottom: 16,
  },
  quickActionButton: {
    backgroundColor: "#FFFFFF",
    borderRadius: 20,
    padding: 18,
    marginBottom: 14,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.08,
    shadowRadius: 12,
    elevation: 6,
    borderWidth: 1,
    borderColor: "#F0F4F8", // Daha subtle border
    borderLeftWidth: 4, // Sol tarafına kalın yeşil çizgi
    borderLeftColor: "#5C9B9B", // Ana tema yeşil rengi
  },
  quickActionContent: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
  },
  quickActionText: {
    flex: 1,
  },
  quickActionTitle: {
    fontSize: 18,
    fontWeight: "700",
    color: "#222222",
    marginBottom: 2,
  },
  quickActionSubtitle: {
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
    height: 150,
    borderRadius: 20,
    padding: 16,
    justifyContent: "flex-end",
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.15,
    shadowRadius: 12,
    elevation: 8,
    borderWidth: 1,
    borderColor: "rgba(255, 255, 255, 0.3)",
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
    marginTop: 28,
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#F8FAFC",
    borderRadius: 20,
    padding: 18,
    gap: 12,
    borderWidth: 1,
    borderColor: "#E2E8F0",
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 10,
    elevation: 6,
  },
  suggestionText: {
    fontSize: 14,
    color: "#333333",
  },
  // AI Recommendations Styles
  recommendationsSection: {
    marginTop: 2,
    marginBottom: 28,
    backgroundColor: "#F8FBFF", // Daha subtle background
    borderRadius: 24,
    padding: 24,
    borderWidth: 1,
    borderColor: "#E6F3FF", // Daha subtle border
    borderLeftWidth: 4, // Sol tarafına kalın yeşil çizgi
    borderLeftColor: "#5C9B9B", // Ana tema yeşil rengi
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.1,
    shadowRadius: 16,
    elevation: 8,
  },
  recommendationsHeader: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    marginBottom: 20,
    paddingRight: 4,
  },
  recommendationsHeaderLeft: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
  },
  refreshButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: "transparent",
    alignItems: "center",
    justifyContent: "center",
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
    marginRight: 8,
  },
  recommendationsTitle: {
    fontSize: 20,
    fontWeight: "700",
    color: "#222222",
  },
  statsContainer: {
    flexDirection: "row",
    justifyContent: "space-around",
    alignItems: "center",
    paddingVertical: 18,
    paddingHorizontal: 12,
    backgroundColor: "#FFFFFF",
    borderRadius: 18,
    marginBottom: 18,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 10,
    elevation: 6,
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
    backgroundColor: "#F0F9FF",
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: "#BFDBFE",
  },
  tagText: {
    fontSize: 12,
    color: "#1E40AF",
    fontWeight: "600",
  },
  regionTag: {
    backgroundColor: "#F0FDF4",
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 20,
    borderWidth: 1,
    borderColor: "#BBF7D0",
  },
  regionTagText: {
    fontSize: 12,
    color: "#166534",
    fontWeight: "600",
  },
  aiTextContainer: {
    marginTop: 8,
    padding: 18,
    backgroundColor: "#FEFEFE",
    borderRadius: 18,
    borderLeftWidth: 4,
    borderLeftColor: "#60A5FA",
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.08,
    shadowRadius: 8,
    elevation: 4,
  },
  aiText: {
    fontSize: 13,
    color: "#4A4A4A",
    lineHeight: 20,
  },
  recommendationsLoading: {
    marginTop: 28,
    padding: 24,
    alignItems: "center",
    backgroundColor: "#F8FBFF",
    borderRadius: 20,
    gap: 12,
    borderWidth: 1,
    borderColor: "#E6F3FF",
  },
  loadingText: {
    fontSize: 14,
    color: "#6b7280",
  },
  // Popular Places Styles
  popularPlacesSection: {
    marginBottom: 28,
  },
  placesContainer: {
    paddingLeft: 7, // Daha soldan başlaması için 24'ten 16'ya düşürdüm
    paddingRight: 8,
  },
  placeCard: {
    width: 200,
    height: 140,
    borderRadius: 20,
    overflow: 'hidden',
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.15,
    shadowRadius: 12,
    elevation: 8,
  },
  placeImage: {
    width: '100%',
    height: '100%',
    resizeMode: 'cover',
  },
  placeGradient: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    height: '60%',
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end',
    padding: 12,
  },
  placeInfo: {
    gap: 2,
  },
  placeName: {
    fontSize: 16,
    fontWeight: '700',
    color: '#FFFFFF',
  },
  placeLocation: {
    fontSize: 13,
    color: '#E5E5E5',
    marginBottom: 4,
  },
  ratingContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  ratingText: {
    fontSize: 12,
    color: '#FFFFFF',
    fontWeight: '600',
  },
});
