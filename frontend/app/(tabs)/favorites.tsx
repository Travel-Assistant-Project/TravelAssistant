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

export default function FavoritesScreen() {
  const router = useRouter();
  const [trips, setTrips] = useState<Trip[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const fetchFavorites = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await api.get('/api/Favorites/itineraries');
      setTrips(response.data);
    } catch (error) {
      console.error('Error fetching favorites:', error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      fetchFavorites();
    }, [fetchFavorites])
  );

  const handleTripPress = (tripId: number) => {
    router.push({
      pathname: '/trip-detail',
      params: { itineraryId: tripId.toString() },
    });
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <View style={styles.headerRow}>
        <Text style={styles.title}>Favorites</Text>
        <TouchableOpacity onPress={fetchFavorites}>
          <IconSymbol name="arrow.clockwise" size={20} color="#0d9488" />
        </TouchableOpacity>
      </View>

      {isLoading && (
        <View style={styles.center}>
          <ActivityIndicator size="large" color="#0d9488" />
        </View>
      )}

      {!isLoading && trips.length === 0 && (
        <View style={styles.center}>
          <IconSymbol name="heart" size={36} color="#d1d5db" />
          <Text style={styles.emptyText}>No favorites yet</Text>
          <Text style={styles.emptySubtext}>Tap the heart on a trip in Home to add it here.</Text>
        </View>
      )}

      {!isLoading && trips.length > 0 && (
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

