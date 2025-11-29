import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  TextInput,
  Image,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { IconSymbol } from '@/components/ui/icon-symbol';
import { api } from '@/lib/api';

interface Category {
  id: string;
  name: string;
  icon: string;
}

interface Destination {
  id: number;
  name: string;
  location: string;
  rating: number | null;
  image: string | null;
  category: string | null;
}

export default function ExploreScreen() {
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [destinations, setDestinations] = useState<Destination[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const categories: Category[] = [
    { id: '1', name: 'All', icon: '🌍' },
    { id: '2', name: 'Nature', icon: '🌲' },
    { id: '3', name: 'History', icon: '🎭' },
    { id: '4', name: 'Beach', icon: '🏖️' },
    { id: '5', name: 'Food', icon: '🍽️' },
    { id: '6', name: 'Photospot', icon: '📸' },
    { id: '7', name: 'Sea', icon: '⛰️' },
  ];

  useEffect(() => {
    fetchPlaces();
  }, [selectedCategory]);

  const fetchPlaces = async () => {
    try {
      setIsLoading(true);
      // Category'yi lowercase'e çevir (backend enum'lar lowercase)
      const categoryParam = selectedCategory === 'All' 
        ? null 
        : selectedCategory.toLowerCase();
      
      console.log('Fetching places with category:', categoryParam);
      
      const response = await api.get('/api/Places/explore', {
        params: {
          category: categoryParam,
          limit: 30,
        },
      });
      
      console.log('Places response:', response.data);
      
      const places: Destination[] = response.data.map((place: any) => ({
        id: place.id,
        name: place.name,
        location: place.location || `${place.city || ''}, ${place.country || ''}`.trim() || 'Unknown location',
        rating: place.googleRating || null,
        image: place.imageUrls && place.imageUrls.length > 0 ? place.imageUrls[0] : null,
        category: place.category ? place.category.charAt(0).toUpperCase() + place.category.slice(1).toLowerCase() : null,
      }));
      
      console.log('Mapped places:', places);
      setDestinations(places);
    } catch (error: any) {
      console.error('Error fetching places:', error);
      console.error('Error response:', error.response?.data);
      setDestinations([]);
    } finally {
      setIsLoading(false);
    }
  };

  // Sadece search query'ye göre filtrele (category zaten API'de filtrelenmiş)
  const filteredDestinations = destinations.filter((dest) => {
    if (searchQuery.trim() === '') return true;
    const queryLower = searchQuery.toLowerCase();
    return dest.name.toLowerCase().includes(queryLower) ||
           dest.location.toLowerCase().includes(queryLower);
  });

  return (
    <SafeAreaView style={styles.safeArea}>
      <ScrollView style={styles.container} showsVerticalScrollIndicator={false}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.title}>Explore</Text>
          <Text style={styles.subtitle}>Discover amazing destinations</Text>
        </View>

        {/* Search Bar */}
        <View style={styles.searchContainer}>
          <IconSymbol name="magnifyingglass" size={18} color="#8E8E8F" />
          <TextInput
            placeholder="Search destinations..."
            value={searchQuery}
            onChangeText={setSearchQuery}
            style={styles.searchInput}
          />
          {searchQuery.length > 0 && (
            <TouchableOpacity onPress={() => setSearchQuery('')}>
              <IconSymbol name="xmark.circle.fill" size={18} color="#8E8E8F" />
            </TouchableOpacity>
          )}
        </View>

        {/* Categories */}
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          style={styles.categoriesContainer}
          contentContainerStyle={styles.categoriesContent}
        >
          {categories.map((category) => (
            <TouchableOpacity
              key={category.id}
              style={[
                styles.categoryChip,
                selectedCategory === category.name && styles.categoryChipActive,
              ]}
              onPress={() => {
                setSelectedCategory(category.name);
              }}
            >
              <Text style={styles.categoryIcon}>{category.icon}</Text>
              <Text
                style={[
                  styles.categoryText,
                  selectedCategory === category.name && styles.categoryTextActive,
                ]}
              >
                {category.name}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>

        {/* Results Count */}
        <Text style={styles.resultsText}>
          {filteredDestinations.length} {filteredDestinations.length === 1 ? 'place' : 'places'} found
        </Text>

        {/* Destination Cards */}
        {isLoading && (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color="#0d9488" />
            <Text style={styles.loadingText}>Loading places...</Text>
          </View>
        )}
        {!isLoading && filteredDestinations.length === 0 && (
          <View style={styles.emptyContainer}>
            <IconSymbol name="map" size={48} color="#d1d5db" />
            <Text style={styles.emptyText}>No places found</Text>
            <Text style={styles.emptySubtext}>
              {selectedCategory === 'All' 
                ? 'Try searching for a specific place'
                : `No places found in ${selectedCategory} category. Try creating a trip with ${selectedCategory} theme to see places here.`}
            </Text>
          </View>
        )}
        {!isLoading && filteredDestinations.length > 0 && (
          <View style={styles.cardsContainer}>
            {filteredDestinations.map((destination) => (
              <TouchableOpacity key={destination.id} style={styles.card}>
                {destination.image ? (
                  <Image
                    source={{ uri: destination.image }}
                    style={styles.cardImage}
                    resizeMode="cover"
                  />
                ) : (
                  <View style={styles.cardImagePlaceholder}>
                    <IconSymbol name="photo" size={32} color="#999999" />
                  </View>
                )}
                <View style={styles.cardOverlay}>
                  {destination.rating && (
                    <View style={styles.ratingBadge}>
                      <Text style={styles.ratingText}>⭐ {destination.rating.toFixed(1)}</Text>
                    </View>
                  )}
                </View>
                <View style={styles.cardInfo}>
                  <Text style={styles.cardTitle}>{destination.name}</Text>
                  <View style={styles.locationRow}>
                    <IconSymbol name="mappin" size={14} color="#666666" />
                    <Text style={styles.cardLocation}>{destination.location}</Text>
                  </View>
                </View>
              </TouchableOpacity>
            ))}
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#FFFFFF',
  },
  container: {
    flex: 1,
  },
  header: {
    paddingHorizontal: 24,
    paddingTop: 16,
    paddingBottom: 16,
  },
  title: {
    fontSize: 32,
    fontWeight: '700',
    color: '#222222',
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 16,
    color: '#777777',
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F5F5F7',
    borderRadius: 16,
    paddingHorizontal: 14,
    paddingVertical: 12,
    marginHorizontal: 24,
    marginBottom: 20,
  },
  searchInput: {
    marginLeft: 8,
    flex: 1,
    fontSize: 15,
    color: '#222222',
  },
  categoriesContainer: {
    marginBottom: 16,
  },
  categoriesContent: {
    paddingHorizontal: 24,
    gap: 10,
  },
  categoryChip: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F5F5F7',
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 10,
    gap: 6,
  },
  categoryChipActive: {
    backgroundColor: '#0d9488',
  },
  categoryIcon: {
    fontSize: 16,
  },
  categoryText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#4A4A4A',
  },
  categoryTextActive: {
    color: '#FFFFFF',
  },
  resultsText: {
    fontSize: 14,
    color: '#777777',
    paddingHorizontal: 24,
    marginBottom: 16,
  },
  cardsContainer: {
    paddingHorizontal: 24,
    paddingBottom: 40,
    gap: 16,
  },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 20,
    overflow: 'hidden',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 3,
  },
  cardImage: {
    width: '100%',
    height: 200,
    backgroundColor: '#E5E5EA',
  },
  cardOverlay: {
    position: 'absolute',
    top: 12,
    right: 12,
  },
  ratingBadge: {
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    borderRadius: 12,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  ratingText: {
    fontSize: 13,
    fontWeight: '600',
    color: '#222222',
  },
  cardInfo: {
    padding: 16,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#222222',
    marginBottom: 6,
  },
  locationRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  cardLocation: {
    fontSize: 14,
    color: '#666666',
  },
  loadingContainer: {
    paddingVertical: 60,
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#666666',
  },
  emptyContainer: {
    paddingVertical: 60,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 24,
  },
  emptyText: {
    marginTop: 16,
    fontSize: 18,
    fontWeight: '600',
    color: '#666666',
  },
  emptySubtext: {
    marginTop: 8,
    fontSize: 14,
    color: '#999999',
    textAlign: 'center',
  },
  cardImagePlaceholder: {
    width: '100%',
    height: 200,
    backgroundColor: '#E5E5EA',
    alignItems: 'center',
    justifyContent: 'center',
  },
});
