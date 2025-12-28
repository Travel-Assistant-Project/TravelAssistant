import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  Image,
  ActivityIndicator,
  TouchableOpacity,
  Dimensions,
  Linking,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { IconSymbol } from '@/components/ui/icon-symbol';
import { api } from '@/lib/api';

const { width } = Dimensions.get('window');

interface Review {
  authorName: string;
  comment: string;
  rating: number;
  profilePhotoUrl?: string;
  reviewTime?: string;
}

interface PlaceDetail {
  id: number;
  name: string;
  description?: string;
  category?: string;
  city?: string;
  country?: string;
  location: string;
  imageUrls?: string[];
  googleRating?: number;
  userRatingsTotal?: number;
  priceLevel?: number;
  googleMapsUrl?: string;
  formattedAddress?: string;
  googleReviews: Review[];
}

export default function PlaceDetailScreen() {
  const params = useLocalSearchParams();
  const router = useRouter();
  const placeId = params.id as string;

  const [place, setPlace] = useState<PlaceDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [currentImageIndex, setCurrentImageIndex] = useState(0);

  useEffect(() => {
    fetchPlaceDetail();
  }, [placeId]);

  const fetchPlaceDetail = async () => {
    try {
      setIsLoading(true);
      const response = await api.get(`/api/Places/${placeId}`);
      setPlace(response.data);
    } catch (error) {
      console.error('Error fetching place detail:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const openInMaps = () => {
    if (place?.googleMapsUrl) {
      Linking.openURL(place.googleMapsUrl);
    }
  };

  const renderStars = (rating: number) => {
    const stars = [];
    for (let i = 1; i <= 5; i++) {
      stars.push(
        <Text key={i} style={styles.starIcon}>
          {i <= rating ? '⭐' : '☆'}
        </Text>
      );
    }
    return stars;
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  if (isLoading) {
    return (
      <SafeAreaView style={styles.safeArea}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#0d9488" />
          <Text style={styles.loadingText}>Loading place details...</Text>
        </View>
      </SafeAreaView>
    );
  }

  if (!place) {
    return (
      <SafeAreaView style={styles.safeArea}>
        <View style={styles.errorContainer}>
          <IconSymbol name="exclamationmark.triangle" size={48} color="#ef4444" />
          <Text style={styles.errorText}>Place not found</Text>
          <TouchableOpacity style={styles.backButton} onPress={() => router.back()}>
            <Text style={styles.backButtonText}>Go Back</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safeArea} edges={['bottom']}>
      <ScrollView style={styles.container} showsVerticalScrollIndicator={false}>
        {/* Image Gallery */}
        <View style={styles.imageContainer}>
          {place.imageUrls && place.imageUrls.length > 0 ? (
            <>
              <ScrollView
                horizontal
                pagingEnabled
                showsHorizontalScrollIndicator={false}
                onScroll={(e) => {
                  const x = e.nativeEvent.contentOffset.x;
                  const index = Math.round(x / width);
                  setCurrentImageIndex(index);
                }}
                scrollEventThrottle={16}
              >
                {place.imageUrls.map((url, index) => (
                  <Image
                    key={index}
                    source={{ uri: url }}
                    style={styles.placeImage}
                    resizeMode="cover"
                  />
                ))}
              </ScrollView>
              {place.imageUrls.length > 1 && (
                <View style={styles.imageIndicator}>
                  {place.imageUrls.map((_, index) => (
                    <View
                      key={index}
                      style={[
                        styles.indicatorDot,
                        currentImageIndex === index && styles.indicatorDotActive,
                      ]}
                    />
                  ))}
                </View>
              )}
            </>
          ) : (
            <View style={styles.placeholderImage}>
              <IconSymbol name="photo" size={64} color="#999999" />
            </View>
          )}

          {/* Back Button */}
          <TouchableOpacity style={styles.backButtonOverlay} onPress={() => router.back()}>
            <IconSymbol name="chevron.left" size={24} color="#FFFFFF" />
          </TouchableOpacity>
        </View>

        {/* Content */}
        <View style={styles.content}>
          {/* Header */}
          <View style={styles.header}>
            <View style={styles.titleContainer}>
              <Text style={styles.placeName}>{place.name}</Text>
              {place.category && (
                <View style={styles.categoryBadge}>
                  <Text style={styles.categoryText}>{place.category}</Text>
                </View>
              )}
            </View>

            {/* Location */}
            <View style={styles.locationContainer}>
              <IconSymbol name="mappin.circle.fill" size={20} color="#0d9488" />
              <Text style={styles.locationText}>{place.location}</Text>
            </View>

            {/* Rating */}
            {place.googleRating && (
              <View style={styles.ratingContainer}>
                <View style={styles.ratingStars}>{renderStars(Math.round(place.googleRating))}</View>
                <Text style={styles.ratingNumber}>{place.googleRating.toFixed(1)}</Text>
                {place.userRatingsTotal && (
                  <Text style={styles.ratingCount}>({place.userRatingsTotal} reviews)</Text>
                )}
              </View>
            )}

            {/* Price Level */}
            {place.priceLevel && (
              <View style={styles.priceLevelContainer}>
                <Text style={styles.priceLevelText}>
                  {'$'.repeat(place.priceLevel)}
                  <Text style={styles.priceLevelInactive}>{'$'.repeat(4 - place.priceLevel)}</Text>
                </Text>
              </View>
            )}
          </View>

          {/* Description */}
          {place.description && (
            <View style={styles.section}>
              <Text style={styles.sectionTitle}>About</Text>
              <Text style={styles.description}>{place.description}</Text>
            </View>
          )}

          {/* Map Button */}
          {place.googleMapsUrl && (
            <TouchableOpacity style={styles.mapButton} onPress={openInMaps}>
              <IconSymbol name="map.fill" size={20} color="#FFFFFF" />
              <Text style={styles.mapButtonText}>Open in Google Maps</Text>
            </TouchableOpacity>
          )}

          {/* Reviews */}
          {place.googleReviews && place.googleReviews.length > 0 && (
            <View style={styles.section}>
              <Text style={styles.sectionTitle}>
                Reviews ({place.googleReviews.length})
              </Text>
              {place.googleReviews.map((review, index) => (
                <View key={index} style={styles.reviewCard}>
                  <View style={styles.reviewHeader}>
                    <View style={styles.reviewAuthorContainer}>
                      {review.profilePhotoUrl ? (
                        <Image
                          source={{ uri: review.profilePhotoUrl }}
                          style={styles.reviewAuthorPhoto}
                        />
                      ) : (
                        <View style={styles.reviewAuthorPhotoPlaceholder}>
                          <Text style={styles.reviewAuthorInitial}>
                            {review.authorName.charAt(0).toUpperCase()}
                          </Text>
                        </View>
                      )}
                      <View style={styles.reviewAuthorInfo}>
                        <Text style={styles.reviewAuthorName}>{review.authorName}</Text>
                        {review.reviewTime && (
                          <Text style={styles.reviewDate}>{formatDate(review.reviewTime)}</Text>
                        )}
                      </View>
                    </View>
                    <View style={styles.reviewRating}>{renderStars(review.rating)}</View>
                  </View>
                  <Text style={styles.reviewComment}>{review.comment}</Text>
                </View>
              ))}
            </View>
          )}
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
  loadingContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#666666',
  },
  errorContainer: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  errorText: {
    marginTop: 16,
    fontSize: 18,
    fontWeight: '600',
    color: '#666666',
  },
  backButton: {
    marginTop: 24,
    backgroundColor: '#0d9488',
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 12,
  },
  backButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
  imageContainer: {
    position: 'relative',
    width: width,
    height: 350,
  },
  placeImage: {
    width: width,
    height: 350,
  },
  placeholderImage: {
    width: width,
    height: 350,
    backgroundColor: '#E5E5EA',
    alignItems: 'center',
    justifyContent: 'center',
  },
  imageIndicator: {
    position: 'absolute',
    bottom: 16,
    left: 0,
    right: 0,
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 6,
  },
  indicatorDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: 'rgba(255, 255, 255, 0.5)',
  },
  indicatorDotActive: {
    backgroundColor: '#FFFFFF',
    width: 24,
  },
  backButtonOverlay: {
    position: 'absolute',
    top: 16,
    left: 16,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    width: 40,
    height: 40,
    borderRadius: 20,
    alignItems: 'center',
    justifyContent: 'center',
  },
  content: {
    padding: 24,
  },
  header: {
    marginBottom: 24,
  },
  titleContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 12,
  },
  placeName: {
    flex: 1,
    fontSize: 28,
    fontWeight: '700',
    color: '#222222',
    marginRight: 12,
  },
  categoryBadge: {
    backgroundColor: '#0d9488',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 12,
  },
  categoryText: {
    color: '#FFFFFF',
    fontSize: 12,
    fontWeight: '600',
    textTransform: 'capitalize',
  },
  locationContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginBottom: 12,
  },
  locationText: {
    fontSize: 16,
    color: '#666666',
    flex: 1,
  },
  ratingContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 8,
  },
  ratingStars: {
    flexDirection: 'row',
  },
  starIcon: {
    fontSize: 16,
  },
  ratingNumber: {
    fontSize: 18,
    fontWeight: '700',
    color: '#222222',
  },
  ratingCount: {
    fontSize: 14,
    color: '#666666',
  },
  priceLevelContainer: {
    marginTop: 4,
  },
  priceLevelText: {
    fontSize: 18,
    fontWeight: '700',
    color: '#10b981',
  },
  priceLevelInactive: {
    color: '#d1d5db',
  },
  section: {
    marginBottom: 24,
  },
  sectionTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#222222',
    marginBottom: 12,
  },
  description: {
    fontSize: 16,
    color: '#444444',
    lineHeight: 24,
  },
  mapButton: {
    backgroundColor: '#0d9488',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 16,
    borderRadius: 16,
    marginBottom: 24,
  },
  mapButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
  reviewCard: {
    backgroundColor: '#F5F5F7',
    borderRadius: 16,
    padding: 16,
    marginBottom: 12,
  },
  reviewHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 12,
  },
  reviewAuthorContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    flex: 1,
  },
  reviewAuthorPhoto: {
    width: 40,
    height: 40,
    borderRadius: 20,
  },
  reviewAuthorPhotoPlaceholder: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#0d9488',
    alignItems: 'center',
    justifyContent: 'center',
  },
  reviewAuthorInitial: {
    color: '#FFFFFF',
    fontSize: 18,
    fontWeight: '600',
  },
  reviewAuthorInfo: {
    flex: 1,
  },
  reviewAuthorName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#222222',
    marginBottom: 2,
  },
  reviewDate: {
    fontSize: 12,
    color: '#666666',
  },
  reviewRating: {
    flexDirection: 'row',
  },
  reviewComment: {
    fontSize: 14,
    color: '#444444',
    lineHeight: 20,
  },
});
