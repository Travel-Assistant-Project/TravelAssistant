// frontend/app/trip-detail.tsx
import React, { useEffect, useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Image,
  Modal,
  Dimensions,
  StatusBar,
} from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { IconSymbol } from '@/components/ui/icon-symbol';
import { api } from '@/lib/api';
import MapView, { Marker, Polyline, PROVIDER_GOOGLE } from 'react-native-maps';

interface Activity {
  title: string;
  description: string;
  reason: string;
  startTime: string;
  endTime: string;
  place: {
    name: string;
    description?: string;
    city?: string;
    country?: string;
    imageUrls?: string[];
    googleRating?: number;
    latitude?: number;
    longitude?: number;
  } | null;
}

interface WeatherInfo {
  temp?: {
    day?: number;
    min?: number;
    max?: number;
  };
  weather?: {
    main?: string;
    description?: string;
  };
  humidity?: number;
  windSpeed?: number;
}

interface DayPlan {
  dayNumber: number;
  activities: Activity[];
  weatherInfo?: WeatherInfo;
}

interface TripData {
  itineraryId: number;
  planName: string;
  region: string;
  daysCount: number;
  days: DayPlan[];
}

export default function TripDetailScreen() {
  const router = useRouter();
  const params = useLocalSearchParams();
  const itineraryId = params.itineraryId as string;
  const insets = useSafeAreaInsets();

  const [tripData, setTripData] = useState<TripData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedDay, setSelectedDay] = useState(1);
  const [showMapModal, setShowMapModal] = useState(false);

  useEffect(() => {
    fetchTripDetails();
  }, []);

  const fetchTripDetails = async () => {
    try {
      console.log('Fetching trip details for ID:', itineraryId);
      const response = await api.get(`/api/Routes/${itineraryId}`);
      console.log('Trip details response:', JSON.stringify(response.data, null, 2));
      console.log('Days array:', response.data?.days);
      if (response.data?.days && response.data.days.length > 0) {
        console.log('Day 1 data:', response.data.days[0]);
        console.log('Day 1 weatherInfo:', response.data.days[0]?.weatherInfo);
        console.log('Day 1 weatherInfo type:', typeof response.data.days[0]?.weatherInfo);
      }
      setTripData(response.data);
      setIsLoading(false);
    } catch (error: any) {
      console.error('Error fetching trip details:', error);
      console.error('Error response:', error.response?.data);
      console.error('Error status:', error.response?.status);
      setIsLoading(false);
    }
  };

  const getCurrentDayData = () => {
    if (!tripData) return null;
    const day = tripData.days.find(d => d.dayNumber === selectedDay);
    if (day) {
      console.log(`Day ${selectedDay} data:`, day);
      console.log(`Day ${selectedDay} weatherInfo:`, day.weatherInfo);
    }
    return day;
  };

  const formatTime = (time: string) => {
    // "09:00" formatında geliyorsa direkt göster
    return time;
  };

  if (isLoading) {
    return (
      <View style={styles.root}>
        <StatusBar barStyle="light-content" backgroundColor="#0d9488" />
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#0d9488" />
          <Text style={styles.loadingText}>Loading your trip...</Text>
        </View>
      </View>
    );
  }

  if (!tripData) {
    return (
      <View style={styles.root}>
        <StatusBar barStyle="light-content" backgroundColor="#0d9488" />
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>Trip not found</Text>
          <TouchableOpacity style={styles.backButton} onPress={() => router.back()}>
            <Text style={styles.backButtonText}>Go Back</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  const currentDay = getCurrentDayData();

  // Map için koordinatları hesapla
  const getMapRegion = () => {
    if (!currentDay?.activities || currentDay.activities.length === 0) {
      return {
        latitude: 36.201667,
        longitude: 29.645556,
        latitudeDelta: 0.15,
        longitudeDelta: 0.15,
      };
    }

    // Koordinatları olan activity'leri filtrele
    const activitiesWithCoords = currentDay.activities.filter(
      (a) => a.place?.latitude != null && a.place?.longitude != null
    );

    if (activitiesWithCoords.length === 0) {
      // Koordinat yoksa default değer
      return {
        latitude: 36.201667,
        longitude: 29.645556,
        latitudeDelta: 0.15,
        longitudeDelta: 0.15,
      };
    }

    // Tüm koordinatların min/max değerlerini bul
    const latitudes = activitiesWithCoords.map((a) => a.place?.latitude ?? 0);
    const longitudes = activitiesWithCoords.map((a) => a.place?.longitude ?? 0);

    const minLat = Math.min(...latitudes);
    const maxLat = Math.max(...latitudes);
    const minLng = Math.min(...longitudes);
    const maxLng = Math.max(...longitudes);

    // Merkez noktası
    const centerLat = (minLat + maxLat) / 2;
    const centerLng = (minLng + maxLng) / 2;

    // Delta değerleri (padding ekle)
    const latDelta = Math.max((maxLat - minLat) * 1.5, 0.01);
    const lngDelta = Math.max((maxLng - minLng) * 1.5, 0.01);

    return {
      latitude: centerLat,
      longitude: centerLng,
      latitudeDelta: latDelta,
      longitudeDelta: lngDelta,
    };
  };

  const mapRegion = getMapRegion();

  return (
    <View style={styles.root}>
      <StatusBar barStyle="light-content" backgroundColor="#0d9488" />
      {/* Header */}
      <SafeAreaView edges={['top']} style={styles.headerSafeArea}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => router.back()} style={styles.backBtn}>
            <IconSymbol name="chevron.left" size={24} color="#FFFFFF" />
          </TouchableOpacity>
          <View style={styles.headerCenter}>
            <Text style={styles.headerTitle}>{tripData.planName}</Text>
            <Text style={styles.headerSubtitle}>{tripData.region} · {tripData.daysCount} days</Text>
          </View>
          <TouchableOpacity style={styles.mapBtn} onPress={() => setShowMapModal(true)}>
            <IconSymbol name="map" size={20} color="#FFFFFF" />
          </TouchableOpacity>
        </View>
      </SafeAreaView>

        {/* Day Selector */}
        <View style={styles.daySelectorContainer}>
          <ScrollView
            horizontal
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={styles.daySelectorContent}
          >
            {Array.from({ length: tripData.daysCount }, (_, i) => i + 1).map((day) => (
              <TouchableOpacity
                key={day}
                style={[
                  styles.dayChip,
                  selectedDay === day && styles.dayChipActive,
                ]}
                onPress={() => setSelectedDay(day)}
              >
                <Text
                  style={[
                    styles.dayChipText,
                    selectedDay === day && styles.dayChipTextActive,
                  ]}
                >
                  Day {day}
                </Text>
              </TouchableOpacity>
            ))}
          </ScrollView>
          
          {/* Activity Count */}
          <View style={styles.activityCountContainer}>
            <IconSymbol name="list.bullet" size={16} color="#0d9488" />
            <Text style={styles.activityCountText}>
              {currentDay?.activities.length || 0} activities
            </Text>
          </View>
        </View>

        {/* Activities List */}
        <ScrollView 
          style={styles.activitiesList} 
          contentContainerStyle={styles.activitiesContent}
          showsVerticalScrollIndicator={false}
        >
          {currentDay?.activities.map((activity, index) => (
            <View key={index} style={styles.activityCard}>
              <View style={styles.timeContainer}>
                <Text style={styles.timeText}>{formatTime(activity.startTime)}</Text>
              </View>

              <View style={styles.activityContent}>
                {/* Activity Image */}
                <View style={styles.activityImageContainer}>
                  {activity.place?.imageUrls && activity.place.imageUrls.length > 0 ? (
                    <Image
                      source={{ uri: activity.place.imageUrls[0] }}
                      style={styles.activityImage}
                      resizeMode="cover"
                    />
                  ) : (
                    <View style={styles.activityImagePlaceholder}>
                      <IconSymbol name="photo" size={32} color="#999999" />
                    </View>
                  )}
                  <View style={styles.activityBadge}>
                    <IconSymbol name="star.fill" size={12} color="#FFB800" />
                    <Text style={styles.activityRating}>
                      {activity.place?.googleRating?.toFixed(1) || '4.8'}
                    </Text>
                  </View>
                </View>

                {/* Activity Info */}
                <View style={styles.activityInfo}>
                  <Text style={styles.activityTitle}>{activity.place?.name || activity.title}</Text>
                  <View style={styles.activityMeta}>
                    <IconSymbol name="tag.fill" size={12} color="#666666" />
                    <Text style={styles.activityCategory}>
                      {activity.description.split(' ').slice(0, 2).join(' ')}
                    </Text>
                  </View>
                  
                  {activity.reason && (
                    <View style={styles.reasonContainer}>
                      <Text style={styles.reasonLabel}>Advice</Text>
                      <Text style={styles.reasonText}>{activity.reason}</Text>
                    </View>
                  )}

                  <View style={styles.activityFooter}>
                    <Text style={styles.transportText}>
                      {activity.place?.city || 'Driving'}
                    </Text>
                    <Text style={styles.durationText}>
                      {activity.startTime} - {activity.endTime}
                    </Text>
                  </View>
                </View>
              </View>
            </View>
          ))}

          {/* Why Suggested Section - Inside ScrollView */}
          <View style={styles.whySuggestedSection}>
            <Text style={styles.whySuggestedTitle}>Why suggested?</Text>
            <Text style={styles.whySuggestedText}>
              This large national garden offers a profound nature escape within the city, perfectly aligning with the theme. It has a modest entrance fee (medium budget) and allows for a leisurely, unhurried exploration, matching relaxed intensity.
            </Text>
          </View>
        </ScrollView>

      {/* Map Modal */}
      <Modal
        visible={showMapModal}
        animationType="slide"
        onRequestClose={() => setShowMapModal(false)}
      >
        <View style={[styles.mapModalContainer, { paddingTop: insets.top }]}>
          {/* Map Header */}
          <View style={styles.mapHeader}>
            <View style={styles.mapHeaderLeft}>
              <Text style={styles.mapDayTitle}>Day {selectedDay}</Text>
              {currentDay?.weatherInfo && (
                <View style={styles.mapWeatherBadge}>
                  <IconSymbol name="cloud.sun.fill" size={14} color="#0d9488" />
                  <Text style={styles.mapWeatherBadgeText}>
                    {currentDay.weatherInfo.temp?.day ? `${Math.round(currentDay.weatherInfo.temp.day)}°` : '—'}
                  </Text>
                </View>
              )}
            </View>
            <TouchableOpacity 
              onPress={() => {
                console.log('Close button pressed');
                setShowMapModal(false);
              }}
              style={styles.closeButton}
              activeOpacity={0.7}
              hitSlop={{ top: 20, bottom: 20, left: 20, right: 20 }}
            >
              <IconSymbol name="xmark" size={24} color="#222222" />
            </TouchableOpacity>
          </View>

          {/* Weather Info Card - Map Modal içinde */}
          {currentDay?.weatherInfo && (
            <View style={styles.mapWeatherCard}>
              <View style={styles.mapWeatherHeader}>
                <View style={styles.mapWeatherTitleRow}>
                  <IconSymbol name="cloud.sun.fill" size={20} color="#0d9488" />
                  <Text style={styles.mapWeatherTitle}>Weather Info</Text>
                </View>
              </View>
              
              <View style={styles.mapWeatherContent}>
                {/* Temperature Row */}
                {currentDay.weatherInfo.temp && (
                  <View style={styles.mapWeatherTempRow}>
                    {currentDay.weatherInfo.temp.day !== undefined && (
                      <View style={styles.mapWeatherTempItem}>
                        <Text style={styles.mapWeatherTempValue}>
                          {Math.round(currentDay.weatherInfo.temp.day)}°
                        </Text>
                        <Text style={styles.mapWeatherTempLabel}>Average</Text>
                      </View>
                    )}
                    {currentDay.weatherInfo.temp.min !== undefined && (
                      <View style={styles.mapWeatherTempItem}>
                        <Text style={styles.mapWeatherTempValue}>
                          {Math.round(currentDay.weatherInfo.temp.min)}°
                        </Text>
                        <Text style={styles.mapWeatherTempLabel}>Min</Text>
                      </View>
                    )}
                    {currentDay.weatherInfo.temp.max !== undefined && (
                      <View style={styles.mapWeatherTempItem}>
                        <Text style={styles.mapWeatherTempValue}>
                          {Math.round(currentDay.weatherInfo.temp.max)}°
                        </Text>
                        <Text style={styles.mapWeatherTempLabel}>Max</Text>
                      </View>
                    )}
                  </View>
                )}

                {/* Weather Details */}
                <View style={styles.mapWeatherDetails}>
                  {currentDay.weatherInfo.weather && (
                    <View style={styles.mapWeatherDetailItem}>
                      <IconSymbol name="cloud" size={16} color="#6b7280" />
                      <Text style={styles.mapWeatherDetailText} numberOfLines={1}>
                        {currentDay.weatherInfo.weather.main || currentDay.weatherInfo.weather.description || 'N/A'}
                      </Text>
                    </View>
                  )}
                  {currentDay.weatherInfo.humidity !== undefined && (
                    <View style={styles.mapWeatherDetailItem}>
                      <IconSymbol name="drop.fill" size={16} color="#6b7280" />
                      <Text style={styles.mapWeatherDetailText}>
                        {Math.round(currentDay.weatherInfo.humidity)}%
                      </Text>
                    </View>
                  )}
                  {currentDay.weatherInfo.windSpeed !== undefined && (
                    <View style={styles.mapWeatherDetailItem}>
                      <IconSymbol name="wind" size={16} color="#6b7280" />
                      <Text style={styles.mapWeatherDetailText}>
                        {Math.round(currentDay.weatherInfo.windSpeed)} km/h
                      </Text>
                    </View>
                  )}
                </View>
              </View>
            </View>
          )}

          {/* Map */}
          <MapView
            style={styles.map}
            provider={PROVIDER_GOOGLE}
            initialRegion={mapRegion}
            region={mapRegion}
          >
            {/* Activity Markers */}
            {currentDay?.activities
              .filter((activity) => activity.place?.latitude != null && activity.place?.longitude != null)
              .map((activity, index) => {
                const lat = activity.place?.latitude ?? 0;
                const lng = activity.place?.longitude ?? 0;
                return (
                  <Marker
                    key={index}
                    coordinate={{
                      latitude: lat,
                      longitude: lng,
                    }}
                    title={activity.place?.name || activity.title}
                    description={activity.description}
                  >
                    <View style={styles.markerContainer}>
                      <View style={styles.marker}>
                        <Text style={styles.markerText}>{index + 1}</Text>
                      </View>
                    </View>
                  </Marker>
                );
              })}

            {/* Route Line */}
            {currentDay && currentDay.activities.length > 1 && (
              <Polyline
                coordinates={currentDay.activities
                  .filter((activity) => activity.place?.latitude != null && activity.place?.longitude != null)
                  .map((activity) => ({
                    latitude: activity.place?.latitude ?? 0,
                    longitude: activity.place?.longitude ?? 0,
                  }))}
                strokeColor="#0d9488"
                strokeWidth={3}
              />
            )}
          </MapView>

          {/* Bottom Activities List */}
          <View style={styles.mapBottomSheet}>
            <Text style={styles.mapBottomTitle}>Plan for Day {selectedDay}</Text>
            <ScrollView style={styles.mapActivityList}>
              {currentDay?.activities.map((activity, index) => (
                <View key={index} style={styles.mapActivityItem}>
                  <View style={styles.mapActivityTime}>
                    <Text style={styles.mapActivityTimeText}>{activity.startTime}</Text>
                  </View>
                  <View style={styles.mapActivityContent}>
                    <Text style={styles.mapActivityTitle}>{activity.place?.name || activity.title}</Text>
                    <Text style={styles.mapActivityDuration}>
                      {activity.startTime} - {activity.endTime}
                    </Text>
                  </View>
                  {activity.place?.imageUrls && activity.place.imageUrls.length > 0 && (
                    <Image
                      source={{ uri: activity.place.imageUrls[0] }}
                      style={styles.mapActivityImage}
                      resizeMode="cover"
                    />
                  )}
                </View>
              ))}
            </ScrollView>
          </View>
        </View>
      </Modal>

    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: '#f9fafb',
  },
  headerSafeArea: {
    backgroundColor: '#0d9488',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: 16,
    backgroundColor: '#f9fafb',
  },
  loadingText: {
    fontSize: 16,
    color: '#666666',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
    backgroundColor: '#f9fafb',
  },
  errorText: {
    fontSize: 18,
    color: '#EF4444',
    marginBottom: 16,
  },
  backButton: {
    backgroundColor: '#0d9488',
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 999,
  },
  backButtonText: {
    color: '#FFFFFF',
    fontWeight: '600',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 20,
    paddingVertical: 8,
  },
  backBtn: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerCenter: {
    flex: 1,
    alignItems: 'center',
    paddingHorizontal: 12,
  },
  headerTitle: {
    fontSize: 17,
    fontWeight: '700',
    color: '#FFFFFF',
    textAlign: 'center',
  },
  headerSubtitle: {
    fontSize: 12,
    color: 'rgba(255, 255, 255, 0.85)',
    marginTop: 2,
  },
  mapBtn: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  daySelectorContainer: {
    backgroundColor: '#FFFFFF',
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: '#F0F0F0',
  },
  daySelectorContent: {
    paddingHorizontal: 20,
    gap: 10,
  },
  activityCountContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    marginTop: 12,
  },
  activityCountText: {
    fontSize: 14,
    color: '#0d9488',
    fontWeight: '600',
  },
  dayChip: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
    backgroundColor: '#F5F5F7',
  },
  dayChipActive: {
    backgroundColor: '#0d9488',
  },
  dayChipText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#666666',
  },
  dayChipTextActive: {
    color: '#FFFFFF',
  },
  activitiesList: {
    flex: 1,
    backgroundColor: '#f9fafb',
  },
  activitiesContent: {
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 20,
  },
  activityCard: {
    flexDirection: 'row',
    marginBottom: 20,
    gap: 12,
  },
  timeContainer: {
    width: 60,
    paddingTop: 4,
  },
  timeText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#222222',
  },
  activityContent: {
    flex: 1,
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    overflow: 'hidden',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 3,
  },
  activityImageContainer: {
    position: 'relative',
  },
  activityImage: {
    width: '100%',
    height: 140,
  },
  activityImagePlaceholder: {
    width: '100%',
    height: 140,
    backgroundColor: '#E5E7EB',
    justifyContent: 'center',
    alignItems: 'center',
  },
  activityBadge: {
    position: 'absolute',
    top: 12,
    right: 12,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 12,
    gap: 4,
  },
  activityRating: {
    fontSize: 13,
    fontWeight: '600',
    color: '#222222',
  },
  activityInfo: {
    padding: 14,
  },
  activityTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#222222',
    marginBottom: 6,
  },
  activityMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginBottom: 10,
  },
  activityCategory: {
    fontSize: 13,
    color: '#666666',
  },
  reasonContainer: {
    backgroundColor: '#FFF9E6',
    padding: 10,
    borderRadius: 8,
    marginBottom: 10,
  },
  reasonLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: '#E67E22',
    marginBottom: 4,
  },
  reasonText: {
    fontSize: 13,
    color: '#666666',
    lineHeight: 18,
  },
  activityFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  transportText: {
    fontSize: 13,
    color: '#666666',
  },
  durationText: {
    fontSize: 13,
    color: '#999999',
  },
  whySuggestedSection: {
    backgroundColor: '#F9FAFB',
    padding: 20,
    borderRadius: 16,
    marginTop: 24,
    marginBottom: 8,
  },
  whySuggestedTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#222222',
    marginBottom: 12,
  },
  whySuggestedText: {
    fontSize: 14,
    color: '#666666',
    lineHeight: 22,
  },
  bottomCard: {
    backgroundColor: '#F0F9FB',
    margin: 20,
    padding: 16,
    borderRadius: 16,
  },
  bottomCardTitle: {
    fontSize: 15,
    fontWeight: '700',
    color: '#222222',
    marginBottom: 6,
  },
  bottomCardText: {
    fontSize: 14,
    color: '#666666',
    lineHeight: 20,
  },
  // Map Modal Styles
  mapModalContainer: {
    flex: 1,
    backgroundColor: '#FFFFFF',
  },
  mapHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 20,
    paddingVertical: 12,
    backgroundColor: '#FFFFFF',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  mapHeaderLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  mapDayTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#222222',
  },
  mapWeatherBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    backgroundColor: '#F0F9FB',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 12,
  },
  mapWeatherBadgeText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#0d9488',
  },
  closeButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#F5F5F7',
    alignItems: 'center',
    justifyContent: 'center',
  },
  map: {
    flex: 1,
  },
  markerContainer: {
    alignItems: 'center',
  },
  marker: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: '#0d9488',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 3,
    borderColor: '#FFFFFF',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.3,
    shadowRadius: 3,
    elevation: 5,
  },
  markerText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '700',
  },
  mapBottomSheet: {
    backgroundColor: '#FFFFFF',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    paddingTop: 20,
    paddingHorizontal: 20,
    maxHeight: Dimensions.get('window').height * 0.4,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: -2 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 5,
  },
  mapBottomTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: '#222222',
    marginBottom: 16,
  },
  mapActivityList: {
    marginBottom: 20,
  },
  mapActivityItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  mapActivityTime: {
    width: 60,
  },
  mapActivityTimeText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#0d9488',
  },
  mapActivityContent: {
    flex: 1,
    marginLeft: 12,
  },
  mapActivityTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#222222',
    marginBottom: 2,
  },
  mapActivityDuration: {
    fontSize: 13,
    color: '#999999',
  },
  mapActivityImage: {
    width: 60,
    height: 60,
    borderRadius: 12,
    marginLeft: 12,
  },
  // Weather Card Styles - Map Modal içinde
  mapWeatherCard: {
    backgroundColor: '#FFFFFF',
    marginHorizontal: 20,
    marginTop: 12,
    borderRadius: 16,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 3,
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  mapWeatherHeader: {
    marginBottom: 12,
  },
  mapWeatherTitleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  mapWeatherTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#222222',
  },
  mapWeatherContent: {
    gap: 12,
  },
  mapWeatherTempRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingVertical: 16,
    backgroundColor: '#F0F9FB',
    borderRadius: 12,
    gap: 8,
  },
  mapWeatherTempItem: {
    flex: 1,
    alignItems: 'center',
  },
  mapWeatherTempValue: {
    fontSize: 28,
    fontWeight: '700',
    color: '#0d9488',
    marginBottom: 4,
  },
  mapWeatherTempLabel: {
    fontSize: 12,
    color: '#6b7280',
    fontWeight: '500',
  },
  mapWeatherDetails: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    gap: 12,
    paddingTop: 8,
  },
  mapWeatherDetailItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    flex: 1,
    justifyContent: 'center',
    backgroundColor: '#F9FAFB',
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 8,
  },
  mapWeatherDetailText: {
    fontSize: 13,
    color: '#6b7280',
    fontWeight: '500',
  },
});

