import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  TextInput,
  Image,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { IconSymbol } from '@/components/ui/icon-symbol';

interface Category {
  id: string;
  name: string;
  icon: string;
}

interface Destination {
  id: string;
  name: string;
  location: string;
  rating: number;
  image: string;
  category: string;
}

export default function ExploreScreen() {
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [searchQuery, setSearchQuery] = useState<string>('');

  const categories: Category[] = [
    { id: '1', name: 'All', icon: '🌍' },
    { id: '2', name: 'Nature', icon: '🌲' },
    { id: '3', name: 'Culture', icon: '🎭' },
    { id: '4', name: 'Beach', icon: '🏖️' },
    { id: '5', name: 'Gastronomy', icon: '🍽️' },
    { id: '6', name: 'Photography', icon: '📸' },
    { id: '7', name: 'Mountain', icon: '⛰️' },
    { id: '8', name: 'Historical', icon: '🏛️' },
  ];

  const destinations: Destination[] = [
    // Beach
    {
      id: '1',
      name: 'Kaputaş Beach',
      location: 'Antalya, Turkey',
      rating: 4.8,
      image: 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e',
      category: 'Beach',
    },
    {
      id: '2',
      name: 'Ölüdeniz',
      location: 'Muğla, Turkey',
      rating: 4.8,
      image: 'https://images.unsplash.com/photo-1559827260-dc66d52bef19',
      category: 'Beach',
    },
    {
      id: '3',
      name: 'Patara Beach',
      location: 'Antalya, Turkey',
      rating: 4.7,
      image: 'https://images.unsplash.com/photo-1519046904884-53103b34b206',
      category: 'Beach',
    },
    
    // Nature
    {
      id: '4',
      name: 'Cappadocia',
      location: 'Nevşehir, Turkey',
      rating: 4.9,
      image: 'https://images.unsplash.com/photo-1609137144813-7d9921338f24',
      category: 'Nature',
    },
    {
      id: '5',
      name: 'Pamukkale',
      location: 'Denizli, Turkey',
      rating: 4.7,
      image: 'https://images.unsplash.com/photo-1524231757912-21f4fe3a7200',
      category: 'Nature',
    },
    {
      id: '6',
      name: 'Butterfly Valley',
      location: 'Muğla, Turkey',
      rating: 4.6,
      image: 'https://images.unsplash.com/photo-1501785888041-af3ef285b470',
      category: 'Nature',
    },
    
    // Culture
    {
      id: '7',
      name: 'Hagia Sophia',
      location: 'Istanbul, Turkey',
      rating: 4.9,
      image: 'https://images.unsplash.com/photo-1524231757912-21f4fe3a7200',
      category: 'Culture',
    },
    {
      id: '8',
      name: 'Whirling Dervishes',
      location: 'Konya, Turkey',
      rating: 4.8,
      image: 'https://images.unsplash.com/photo-1578632292335-df3abbb0d586',
      category: 'Culture',
    },
    
    // Gastronomy
    {
      id: '9',
      name: 'Spice Bazaar',
      location: 'Istanbul, Turkey',
      rating: 4.7,
      image: 'https://images.unsplash.com/photo-1555396273-367ea4eb4db5',
      category: 'Gastronomy',
    },
    {
      id: '10',
      name: 'Karaköy Food Tour',
      location: 'Istanbul, Turkey',
      rating: 4.8,
      image: 'https://images.unsplash.com/photo-1504674900247-0877df9cc836',
      category: 'Gastronomy',
    },
    
    // Photography
    {
      id: '11',
      name: 'Hot Air Balloons',
      location: 'Cappadocia, Turkey',
      rating: 5.0,
      image: 'https://images.unsplash.com/photo-1609137144813-7d9921338f24',
      category: 'Photography',
    },
    {
      id: '12',
      name: 'Blue Mosque',
      location: 'Istanbul, Turkey',
      rating: 4.9,
      image: 'https://images.unsplash.com/photo-1578632292335-df3abbb0d586',
      category: 'Photography',
    },
    
    // Mountain
    {
      id: '13',
      name: 'Mount Nemrut',
      location: 'Adıyaman, Turkey',
      rating: 4.6,
      image: 'https://images.unsplash.com/photo-1506905925346-21bda4d32df4',
      category: 'Mountain',
    },
    {
      id: '14',
      name: 'Uludağ',
      location: 'Bursa, Turkey',
      rating: 4.5,
      image: 'https://images.unsplash.com/photo-1551632811-561732d1e306',
      category: 'Mountain',
    },
    
    // Historical
    {
      id: '15',
      name: 'Ephesus',
      location: 'İzmir, Turkey',
      rating: 4.9,
      image: 'https://images.unsplash.com/photo-1541963463532-d68292c34b19',
      category: 'Historical',
    },
    {
      id: '16',
      name: 'Troy Ancient City',
      location: 'Çanakkale, Turkey',
      rating: 4.6,
      image: 'https://images.unsplash.com/photo-1581801959971-e8d60d7c0b5a',
      category: 'Historical',
    },
  ];

  const filteredDestinations = destinations.filter((dest) => {
    const matchesCategory = selectedCategory === 'All' || dest.category === selectedCategory;
    const matchesSearch = dest.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         dest.location.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesCategory && matchesSearch;
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
              onPress={() => setSelectedCategory(category.name)}
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
        <View style={styles.cardsContainer}>
          {filteredDestinations.map((destination) => (
            <TouchableOpacity key={destination.id} style={styles.card}>
              <Image
                source={{ uri: destination.image }}
                style={styles.cardImage}
                resizeMode="cover"
              />
              <View style={styles.cardOverlay}>
                <View style={styles.ratingBadge}>
                  <Text style={styles.ratingText}>⭐ {destination.rating}</Text>
                </View>
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
});
