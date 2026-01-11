import React, { useCallback, useState } from 'react';
import { View, Text, StyleSheet, ScrollView, ActivityIndicator, TouchableOpacity } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useRouter, useFocusEffect } from 'expo-router';
import { IconSymbol } from '@/components/ui/icon-symbol';
import { api } from '@/lib/api';

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

interface Place {
  id: number;
  googlePlaceId: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  category: number;
  rating: number;
  userRatingsTotal: number;
  priceLevel?: number;
  openingHours?: string;
  googleMapsUrl?: string;
  photoUrls?: string[];
  photos?: { imageUrl: string }[];
  favoritedAt: string;
}

type TabType = 'routes' | 'places';

export default function FavoritesScreen() {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<TabType>('routes');
  const [trips, setTrips] = useState<Trip[]>([]);
  const [places, setPlaces] = useState<Place[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchFavoriteRoutes = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await api.get('/api/Favorites/itineraries');
      setTrips(response.data);
    } catch (error) {
      console.error('Error fetching favorite routes:', error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const fetchFavoritePlaces = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await api.get('/api/Favorites/places');
      setPlaces(response.data);
    } catch (error) {
      console.error('Error fetching favorite places:', error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      if (activeTab === 'routes') {
        fetchFavoriteRoutes();
      } else {
        fetchFavoritePlaces();
      }
    }, [activeTab, fetchFavoriteRoutes, fetchFavoritePlaces])
  );

  const handleTabChange = (tab: TabType) => {
    setActiveTab(tab);
  };

  const handleRefresh = () => {
    if (activeTab === 'routes') {
      fetchFavoriteRoutes();
    } else {
      fetchFavoritePlaces();
    }
  };

  const handleTripPress = (tripId: number) => {
    router.push({
      pathname: '/trip-detail',
      params: { itineraryId: tripId.toString() },
    });
  };

  const handlePlacePress = (place: Place) => {
    // TODO: Navigate to place detail screen when implemented
    console.log('Place pressed:', place.name);
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.headerRow}>
        <Text style={styles.title}>Favorites</Text>
        <TouchableOpacity onPress={handleRefresh}>
          <IconSymbol name="arrow.clockwise" size={20} color="#0d9488" />
        </TouchableOpacity>
      </View>

      {/* Tab Navigation */}
      <View style={styles.tabContainer}>
        <TouchableOpacity
          style={[styles.tab, activeTab === 'routes' && styles.activeTab]}
          onPress={() => handleTabChange('routes')}
        >
          <IconSymbol 
            name="map" 
            size={18} 
            color={activeTab === 'routes' ? '#0d9488' : '#6b7280'} 
          />
          <Text style={[styles.tabText, activeTab === 'routes' && styles.activeTabText]}>
            Routes
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.tab, activeTab === 'places' && styles.activeTab]}
          onPress={() => handleTabChange('places')}
        >
          <IconSymbol 
            name="location" 
            size={18} 
            color={activeTab === 'places' ? '#0d9488' : '#6b7280'} 
          />
          <Text style={[styles.tabText, activeTab === 'places' && styles.activeTabText]}>
            Places
          </Text>
        </TouchableOpacity>
      </View>

      {isLoading && (
        <View style={styles.center}>
          <ActivityIndicator size="large" color="#0d9488" />
        </View>
      )}

      {/* Routes Tab Content */}
      {!isLoading && activeTab === 'routes' && (
        <>
          {trips.length === 0 ? (
            <View style={styles.center}>
              <IconSymbol name="heart" size={36} color="#d1d5db" />
              <Text style={styles.emptyText}>Favori rota yok</Text>
              <Text style={styles.emptySubtext}>Rotaları favorilere eklemek için kalp simgesine dokunun.</Text>
            </View>
          ) : (
            <ScrollView contentContainerStyle={styles.list}>
              {trips.map((trip, index) => {
                const colors = ['#FFB6C1', '#B4E7CE', '#97D8FF', '#D8B389'];
                const backgroundColor = colors[index % colors.length];

                return (
                  <TouchableOpacity
                    key={trip.id}
                    style={[styles.card, { backgroundColor }]}
                    onPress={() => handleTripPress(trip.id)}
                  >
                    <View style={styles.cardHeader}>
                      <Text style={styles.cardTitle} numberOfLines={2}>{trip.name}</Text>
                      <IconSymbol name="heart.fill" size={18} color="#dc2626" />
                    </View>
                    <Text style={styles.cardRegion}>{trip.region}</Text>
                    <View style={styles.cardFooter}>
                      <IconSymbol name="calendar" size={12} color="#4B5563" />
                      <Text style={styles.cardDays}>{trip.daysCount} days</Text>
                    </View>
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          )}
        </>
      )}

      {/* Places Tab Content */}
      {!isLoading && activeTab === 'places' && (
        <>
          {places.length === 0 ? (
            <View style={styles.center}>
              <IconSymbol name="location" size={36} color="#d1d5db" />
              <Text style={styles.emptyText}>Favori mekan yok</Text>
              <Text style={styles.emptySubtext}>Mekanları favorilere eklemek için kalp simgesine dokunun.</Text>
            </View>
          ) : (
            <ScrollView contentContainerStyle={styles.list}>
              {places.map((place, index) => {
                const colors = ['#FFE5E5', '#E5F5F0', '#E5F0FF', '#FFF5E5'];
                const backgroundColor = colors[index % colors.length];

                return (
                  <TouchableOpacity
                    key={place.id}
                    style={[styles.card, { backgroundColor }]}
                    onPress={() => handlePlacePress(place)}
                  >
                    <View style={styles.cardHeader}>
                      <Text style={styles.cardTitle} numberOfLines={2}>{place.name}</Text>
                      <IconSymbol name="heart.fill" size={18} color="#dc2626" />
                    </View>
                    <Text style={styles.cardRegion} numberOfLines={1}>{place.address}</Text>
                    <View style={styles.cardFooter}>
                      {place.rating > 0 && (
                        <>
                          <IconSymbol name="star.fill" size={12} color="#F59E0B" />
                          <Text style={styles.cardDays}>{place.rating.toFixed(1)}</Text>
                        </>
                      )}
                    </View>
                  </TouchableOpacity>
                );
              })}
            </ScrollView>
          )}
        </>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#f9fafb',
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
    backgroundColor: '#ffffff',
  },
  title: {
    fontSize: 20,
    fontWeight: '700',
    color: '#111827',
  },
  tabContainer: {
    flexDirection: 'row',
    backgroundColor: '#ffffff',
    paddingHorizontal: 16,
    paddingTop: 12,
    paddingBottom: 8,
    gap: 8,
  },
  tab: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderRadius: 12,
    backgroundColor: '#f3f4f6',
    gap: 6,
  },
  activeTab: {
    backgroundColor: '#d1fae5',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#6b7280',
  },
  activeTabText: {
    color: '#0d9488',
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 24,
  },
  emptyText: {
    marginTop: 12,
    fontSize: 16,
    fontWeight: '600',
    color: '#6b7280',
  },
  emptySubtext: {
    marginTop: 4,
    fontSize: 13,
    color: '#9ca3af',
    textAlign: 'center',
  },
  list: {
    padding: 20,
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
  },
  card: {
    width: '48%',
    borderRadius: 16,
    padding: 12,
    marginBottom: 14,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 4,
    elevation: 3,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  cardTitle: {
    fontSize: 15,
    fontWeight: '700',
    color: '#111827',
    flex: 1,
    marginRight: 8,
  },
  cardRegion: {
    fontSize: 13,
    color: '#374151',
    marginTop: 2,
  },
  cardFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 8,
    gap: 4,
  },
  cardDays: {
    fontSize: 12,
    color: '#4B5563',
    fontWeight: '500',
  },
});

