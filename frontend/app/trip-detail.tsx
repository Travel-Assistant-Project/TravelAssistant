import React, { useEffect, useState, useRef, useMemo } from 'react';
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
  Animated,
  PanResponder,
} from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';
import { useRouter, useLocalSearchParams } from 'expo-router';
import { IconSymbol } from '@/components/ui/icon-symbol';
import { api } from '@/lib/api';
import MapView, { Marker, Polyline, PROVIDER_GOOGLE } from 'react-native-maps';

const { width, height } = Dimensions.get('window');

interface Activity {
  title: string;
  description: string;
  reason: string;
  startTime: string;
  endTime: string;
  place: {
    id?: number;
    name: string;
    description?: string;
    city?: string;
    country?: string;
    imageUrls?: string[];
    googleRating?: number;
    latitude?: number;
    longitude?: number;
  } | null;
  // Transport fields from backend
  travelFromPreviousMode?: string;
  travelFromPreviousDurationMinutes?: number;
  travelFromPreviousDistanceMeters?: number;
  travelFromPreviousDetails?: any;
  travelFromPreviousPolyline?: string; // Encoded polyline
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
    icon?: string;
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

const BOTTOM_SHEET_MIN_HEIGHT = height * 0.35;
const BOTTOM_SHEET_MAX_HEIGHT = height * 0.8;

export default function TripDetailScreen() {
  const router = useRouter();
  const params = useLocalSearchParams();
  const itineraryId = params.itineraryId as string;
  const insets = useSafeAreaInsets();
  const mapRef = useRef<MapView>(null);

  const [tripData, setTripData] = useState<TripData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedDay, setSelectedDay] = useState(1);
  const [showMapModal, setShowMapModal] = useState(false);
  const [favoritePlaces, setFavoritePlaces] = useState<Set<number>>(new Set());

  // --- Map Modal Bottom Sheet Logic ---
  const panY = useRef(new Animated.Value(0)).current;
  const [sheetHeight, setSheetHeight] = useState(BOTTOM_SHEET_MIN_HEIGHT);
  
  const panResponder = useRef(
    PanResponder.create({
      onStartShouldSetPanResponder: () => true,
      onMoveShouldSetPanResponder: () => true,
      onPanResponderMove: (_, gestureState) => {
         // Simplified move logic
      },
      onPanResponderRelease: (_, gestureState) => {
        if (gestureState.dy > 50) {
          // Dragged down -> Minimize
           Animated.spring(panY, {
            toValue: 0,
            useNativeDriver: false,
          }).start();
          setSheetHeight(BOTTOM_SHEET_MIN_HEIGHT);
        } else if (gestureState.dy < -50) {
          // Dragged up -> Maximize
           Animated.spring(panY, {
            toValue: 0, 
            useNativeDriver: false,
          }).start();
          setSheetHeight(BOTTOM_SHEET_MAX_HEIGHT);
        }
      },
    })
  ).current;

  useEffect(() => {
    fetchTripDetails();
    fetchFavoritePlaces();
  }, []);

  useEffect(() => {
     // Refocus map when modal opens or day changes
     if (showMapModal && tripData) {
        setTimeout(() => {
            const region = getMapRegion();
            mapRef.current?.animateToRegion(region, 1000);
        }, 500); // Small delay for modal animation
     }
  }, [showMapModal, selectedDay, tripData, sheetHeight]);

  const fetchTripDetails = async () => {
    try {
      const response = await api.get(`/api/Routes/${itineraryId}`);
      setTripData(response.data);
      
      // Debug: Place ID'leri kontrol et
      response.data.days.forEach((day: any) => {
        day.activities.forEach((activity: any) => {
          if (activity.place) {
            console.log('Place:', activity.place.name, 'ID:', activity.place.id);
          }
        });
      });
      
      setIsLoading(false);
    } catch (error: any) {
      console.error('Error fetching trip details:', error);
      setIsLoading(false);
    }
  };

  const fetchFavoritePlaces = async () => {
    try {
      const response = await api.get('/api/Favorites/places/ids');
      setFavoritePlaces(new Set(response.data));
    } catch (error) {
      console.error('Error fetching favorite places:', error);
    }
  };

  const togglePlaceFavorite = async (placeId: number) => {
    try {
      const isFavorite = favoritePlaces.has(placeId);
      
      if (isFavorite) {
        await api.delete(`/api/Favorites/places/${placeId}`);
        setFavoritePlaces(prev => {
          const newSet = new Set(prev);
          newSet.delete(placeId);
          return newSet;
        });
      } else {
        await api.post(`/api/Favorites/places/${placeId}`);
        setFavoritePlaces(prev => new Set(prev).add(placeId));
      }
    } catch (error) {
      console.error('Error toggling place favorite:', error);
    }
  };

  const getCurrentDayData = () => {
    if (!tripData) return null;
    return tripData.days.find(d => d.dayNumber === selectedDay);
  };

  const formatTime = (time: string) => time;

  // Google Polyline Decoder (Utility Function)
  const decodePolyline = (encoded: string) => {
    if (!encoded) return [];
    const poly = [];
    let index = 0, len = encoded.length;
    let lat = 0, lng = 0;

    while (index < len) {
      let b, shift = 0, result = 0;
      do {
        b = encoded.charCodeAt(index++) - 63;
        result |= (b & 0x1f) << shift;
        shift += 5;
      } while (b >= 0x20);
      const dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
      lat += dlat;

      shift = 0;
      result = 0;
      do {
        b = encoded.charCodeAt(index++) - 63;
        result |= (b & 0x1f) << shift;
        shift += 5;
      } while (b >= 0x20);
      const dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
      lng += dlng;

      poly.push({
        latitude: lat / 1e5,
        longitude: lng / 1e5,
      });
    }
    return poly;
  };

  // Helper to get midpoint of polyline for badge placement
  const getPolylineMidpoint = (points: any[]) => {
    if (!points || points.length < 2) return null;
    const midIndex = Math.floor(points.length / 2);
    return points[midIndex];
  };

  // Helper to extract transit line info (e.g. "E-10", "M4") from details
  const getTransitLineInfo = (details: any) => {
    if (!details || !Array.isArray(details)) return null;
    
    // Check for our simplified backend structure first (step.line)
    // If not found, try to look for nested transit_details (fallback)
    const lineNames = details
      .map((step: any) => {
          if (step.line) return step.line; // Simplified format
          if (step.transit_details?.line?.short_name) return step.transit_details.line.short_name; // Raw Google format
          if (step.transit_details?.line?.name) return step.transit_details.line.name;
          return null;
      })
      .filter(Boolean);
      
    const uniqueLines = [...new Set(lineNames)];
    
    if (uniqueLines.length === 0) return null;
    
    return uniqueLines.join(' > ');
  };

  const getMapRegion = () => {
    const currentDay = getCurrentDayData();
    if (!currentDay?.activities || currentDay.activities.length === 0) {
      return {
        latitude: 36.201667,
        longitude: 29.645556,
        latitudeDelta: 0.15,
        longitudeDelta: 0.15,
      };
    }

    const activitiesWithCoords = currentDay.activities.filter(
      (a) => a.place?.latitude != null && a.place?.longitude != null
    );

    if (activitiesWithCoords.length === 0) {
      return {
        latitude: 36.201667,
        longitude: 29.645556,
        latitudeDelta: 0.15,
        longitudeDelta: 0.15,
      };
    }

    const latitudes = activitiesWithCoords.map((a) => a.place?.latitude ?? 0);
    const longitudes = activitiesWithCoords.map((a) => a.place?.longitude ?? 0);

    const minLat = Math.min(...latitudes);
    const maxLat = Math.max(...latitudes);
    const minLng = Math.min(...longitudes);
    const maxLng = Math.max(...longitudes);

    const centerLat = (minLat + maxLat) / 2;
    const centerLng = (minLng + maxLng) / 2;

    const latDelta = Math.max((maxLat - minLat) * 1.5, 0.02);
    const lngDelta = Math.max((maxLng - minLng) * 1.5, 0.02);
    
    // Shift center if sheet is maximized to keep markers visible above sheet
    const finalCenterLat = sheetHeight === BOTTOM_SHEET_MAX_HEIGHT ? centerLat - (latDelta * 0.25) : centerLat;

    return {
      latitude: finalCenterLat,
      longitude: centerLng,
      latitudeDelta: latDelta,
      longitudeDelta: lngDelta,
    };
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

        {/* Activities List (Original Design) */}
        <ScrollView 
          style={styles.activitiesList} 
          contentContainerStyle={styles.activitiesContent}
          showsVerticalScrollIndicator={false}
        >
          {currentDay?.activities.map((activity, index) => (
            <View key={index}>
                 {/* Transport Info (Frontend code restored/preserved if needed) */}
                 {(activity.travelFromPreviousMode === 'public_transport' || activity.travelFromPreviousMode === 'walking' || activity.travelFromPreviousMode === 'driving') && (
                  <View style={styles.transportContainer}>
                    <View style={styles.transportLine} />
                    <View style={styles.transportBadge}>
                       <IconSymbol 
                          name={
                              activity.travelFromPreviousMode === 'walking' ? 'figure.walk' : 
                              activity.travelFromPreviousMode === 'driving' ? 'car.fill' : 
                              'bus.fill'
                          } 
                          size={12} 
                          color="#666" 
                       />
                       <Text style={styles.transportInfoText}>
                          {activity.travelFromPreviousDurationMinutes ? `${activity.travelFromPreviousDurationMinutes} min` : ''}
                       </Text>
                    </View>
                  </View>
                 )}

                <TouchableOpacity 
                  style={styles.activityCard}
                  onPress={() => {
                    if (activity.place?.id) {
                      router.push(`/place-detail?id=${activity.place.id}`);
                    }
                  }}
                  activeOpacity={0.8}
                >
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
                    
                    {/* Favorite Button */}
                    {activity.place?.id && (
                      <TouchableOpacity
                        style={styles.favoritePlaceButton}
                        onPress={(e) => {
                          e.stopPropagation();
                          togglePlaceFavorite(activity.place!.id!);
                        }}
                      >
                        <IconSymbol
                          name={favoritePlaces.has(activity.place.id) ? 'heart.fill' : 'heart'}
                          size={20}
                          color={favoritePlaces.has(activity.place.id) ? '#dc2626' : '#ffffff'}
                        />
                      </TouchableOpacity>
                    )}
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
                </TouchableOpacity>
            </View>
          ))}
           <View style={{height: 40}} />
        </ScrollView>

      {/* Map Modal with Bottom Sheet */}
      <Modal
        visible={showMapModal}
        animationType="slide"
        onRequestClose={() => setShowMapModal(false)}
      >
        <View style={styles.mapModalContainer}>
            {/* Full Screen Map */}
            <MapView
                ref={mapRef}
                style={[styles.map, { height: height }]}
                provider={PROVIDER_GOOGLE}
                initialRegion={getMapRegion()}
            >
                {/* Activity Markers */}
                {currentDay?.activities
                .filter((activity) => activity.place?.latitude != null && activity.place?.longitude != null)
                .map((activity, index) => (
                    <Marker
                    key={index}
                    coordinate={{
                        latitude: activity.place?.latitude ?? 0,
                        longitude: activity.place?.longitude ?? 0,
                    }}
                    title={activity.place?.name || activity.title}
                    zIndex={2} // Ensure markers are above lines
                    >
                    <View style={styles.markerContainer}>
                        <View style={styles.marker}>
                        <Text style={styles.markerText}>{index + 1}</Text>
                        </View>
                    </View>
                    </Marker>
                ))}

                {/* Route Lines & Transport Badges */}
                {currentDay?.activities.map((activity, index) => {
                     if (index === 0) return null; // No previous activity for first one
                     
                     const prevActivity = currentDay.activities[index - 1];
                     
                     // Check if we have polyline data
                     if (activity.travelFromPreviousPolyline) {
                         const coords = decodePolyline(activity.travelFromPreviousPolyline);
                         const isWalking = activity.travelFromPreviousMode === 'walking';
                         const isTransit = activity.travelFromPreviousMode === 'public_transport';
                         const isDriving = activity.travelFromPreviousMode === 'driving';
                         
                         const strokeColor = isWalking ? '#666666' : isTransit ? '#1a73e8' : '#0d9488'; // Gray, Blue, Teal
                         const midpoint = getPolylineMidpoint(coords);

                         return (
                             <React.Fragment key={`route-${index}`}>
                                 <Polyline
                                    coordinates={coords}
                                    strokeColor={strokeColor}
                                    strokeWidth={4}
                                    lineDashPattern={isWalking ? [10, 10] : undefined} // Dotted line for walking
                                    zIndex={1}
                                 />
                                 
                                 {/* Transport Duration Badge on Route */}
                                 {midpoint && activity.travelFromPreviousDurationMinutes && (
                                     <Marker
                                        coordinate={midpoint}
                                        anchor={{ x: 0.5, y: 0.5 }}
                                        zIndex={3}
                                     >
                                         <View style={[styles.mapTransportBadge, { backgroundColor: strokeColor }]}>
                                             <IconSymbol 
                                                name={isWalking ? 'figure.walk' : isDriving ? 'car.fill' : 'bus.fill'} 
                                                size={10} 
                                                color="#FFF" 
                                             />
                                             <Text style={styles.mapTransportBadgeText}>
                                                 {isTransit && getTransitLineInfo(activity.travelFromPreviousDetails) 
                                                    ? `${getTransitLineInfo(activity.travelFromPreviousDetails)} (${activity.travelFromPreviousDurationMinutes} min)`
                                                    : `${activity.travelFromPreviousDurationMinutes} min`
                                                 }
                                             </Text>
                                         </View>
                                     </Marker>
                                 )}
                             </React.Fragment>
                         );
                     } else {
                         // Fallback to straight line if no polyline (e.g. very short distance or old data)
                         if (activity.place?.latitude != null && activity.place?.longitude != null && 
                             prevActivity.place?.latitude != null && prevActivity.place?.longitude != null) {
                              return (
                                 <Polyline
                                    key={`line-${index}`}
                                    coordinates={[
                                        { latitude: prevActivity.place.latitude, longitude: prevActivity.place.longitude },
                                        { latitude: activity.place.latitude, longitude: activity.place.longitude }
                                    ]}
                                    strokeColor="#0d9488"
                                    strokeWidth={3}
                                    lineDashPattern={[5, 5]} // Dotted for fallback
                                 />
                              );
                         }
                     }
                     return null;
                })}
        {/* Modal Header Overlay as a child of MapView so it renders above the native map and receives touches */}
        <View pointerEvents="box-none" style={styles.mapOverlayContainer}>
          <View pointerEvents="box-none" style={styles.modalHeaderOverlay}>
            <View style={styles.modalHeaderRow}>
              <View style={{ width: 48 }} />
              <Text style={styles.modalHeaderTitle}>Day {selectedDay} Map</Text>
              <TouchableOpacity 
                onPress={() => setShowMapModal(false)}
                style={styles.closeButton}
                hitSlop={{ top: 20, left: 20, right: 20, bottom: 20 }}
                accessibilityRole="button"
              >
                <IconSymbol name="xmark" size={26} color="#222" />
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </MapView>

            {/* Bottom Sheet in Modal */}
            <Animated.View 
                style={[
                    styles.bottomSheet, 
                    { height: sheetHeight, bottom: 0 } 
                ]}
            >
                {/* Drag Handle */}
                <View 
                    style={styles.dragHandleArea}
                    {...panResponder.panHandlers}
                >
                    <View style={styles.dragHandle} />
                </View>

                {/* Compact Weather Info */}
                {currentDay?.weatherInfo && (
                    <View style={styles.compactWeatherContainer}>
                        <View style={styles.weatherRow}>
                            <View style={styles.weatherMain}>
                                <IconSymbol name="cloud.sun.fill" size={20} color="#0d9488" />
                                <Text style={styles.weatherTemp}>
                                    {currentDay.weatherInfo.temp?.day ? Math.round(currentDay.weatherInfo.temp.day) : '--'}°
                                </Text>
                                <Text style={styles.weatherDesc}>
                                    {currentDay.weatherInfo.weather?.main || 'Clear'}
                                </Text>
                            </View>
                            <View style={styles.weatherDetails}>
                                <Text style={styles.weatherDetailText}>
                                    <IconSymbol name="drop.fill" size={12} color="#6b7280" /> {Math.round(currentDay.weatherInfo.humidity || 0)}%
                                </Text>
                                <Text style={styles.weatherDetailText}>
                                    <IconSymbol name="wind" size={12} color="#6b7280" /> {Math.round(currentDay.weatherInfo.windSpeed || 0)} km/h
                                </Text>
                            </View>
                        </View>
                    </View>
                )}

                {/* Content */}
                <View style={styles.sheetContent}>
                    <Text style={styles.sheetTitle}>Route Summary</Text>
                    
                    <ScrollView 
                        showsVerticalScrollIndicator={false}
                        contentContainerStyle={styles.activitiesScrollContent}
                    >
                        {currentDay?.activities.map((activity, index) => (
                            <View key={index} style={styles.mapActivityItem}>
                                <View style={styles.mapActivityTime}>
                                    <Text style={styles.mapActivityTimeText}>{activity.startTime}</Text>
                                    {/* Dot Line */}
                                    {index < (currentDay?.activities.length || 0) - 1 && (
                                        <View style={styles.mapTimeConnector} />
                                    )}
                                </View>
                                <View style={styles.mapActivityContent}>
                                    <Text style={styles.mapActivityTitle}>{activity.place?.name || activity.title}</Text>
                                    <Text style={styles.mapActivityDuration}>
                                    {activity.startTime} - {activity.endTime}
                                    </Text>
                                    
                                    {/* Transport Info in List */}
                                    {(activity.travelFromPreviousMode) && (
                                        <View style={styles.mapTransportInfo}>
                                            <IconSymbol 
                                                name={
                                                    activity.travelFromPreviousMode === 'walking' ? 'figure.walk' : 
                                                    activity.travelFromPreviousMode === 'driving' ? 'car.fill' : 
                                                    'bus.fill'
                                                } 
                                                size={12} 
                                                color="#666" 
                                            />
                                            <Text style={styles.mapTransportText}>
                                                {activity.travelFromPreviousDurationMinutes} min 
                                                {activity.travelFromPreviousMode === 'public_transport' && getTransitLineInfo(activity.travelFromPreviousDetails)
                                                    ? ` · ${getTransitLineInfo(activity.travelFromPreviousDetails)}`
                                                    : ` (${activity.travelFromPreviousMode === 'public_transport' ? 'Transit' : activity.travelFromPreviousMode})`
                                                }
                                            </Text>
                                        </View>
                                    )}
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
                        <View style={{ height: 40 }} />
                    </ScrollView>
                </View>
            </Animated.View>
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
  favoritePlaceButton: {
    position: 'absolute',
    top: 12,
    left: 12,
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: 'rgba(0, 0, 0, 0.4)',
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
    elevation: 5,
  },
  activityRating: {
    fontSize: 13,
    fontWeight: '600',
    color: '#222222',
  },
  activityInfo: {
    padding: 14,
  },
  activityTitleRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 6,
  },
  activityTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: '#222222',
    flex: 1,
    marginRight: 8,
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
  transportContainer: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: 12,
      marginLeft: 72, // Align with content, skipping time col
  },
  transportLine: {
      width: 2,
      height: 20,
      backgroundColor: '#E5E7EB',
      marginRight: 12,
  },
  transportBadge: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: '#F3F4F6',
      paddingHorizontal: 10,
      paddingVertical: 4,
      borderRadius: 12,
      gap: 6,
  },
  transportInfoText: {
      fontSize: 12,
      color: '#666',
  },
  
  // --- Map Modal Styles ---
  mapModalContainer: {
    flex: 1,
    backgroundColor: '#FFFFFF',
  },
  map: {
    width: '100%',
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
  },
  modalHeaderOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    zIndex: 10,
    backgroundColor: 'rgba(255,255,255,0.95)',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    paddingTop: 60,
  },
  mapOverlayContainer: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    zIndex: 9999,
    elevation: 30,
    pointerEvents: 'box-none',
  },
  modalHeaderRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingTop: 24,
    paddingBottom: 16,
    height: 80,
  },
  modalHeaderTitle: {
    fontSize: 17,
    fontWeight: '600',
    color: '#222',
    textAlign: 'center',
    flex: 1,
  },
  closeButton: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: '#F5F5F7',
    alignItems: 'center',
    justifyContent: 'center',
    marginLeft: 'auto', // Push to right if needed, but structure handles it
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  
  // Bottom Sheet Styles (Reused)
  bottomSheet: {
      position: 'absolute',
      left: 0,
      right: 0,
      backgroundColor: '#FFF',
      borderTopLeftRadius: 24,
      borderTopRightRadius: 24,
      shadowColor: '#000',
      shadowOffset: { width: 0, height: -4 },
      shadowOpacity: 0.15,
      shadowRadius: 8,
      elevation: 20,
      overflow: 'hidden',
  },
  dragHandleArea: {
      width: '100%',
      height: 30,
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: '#FFF',
  },
  dragHandle: {
      width: 40,
      height: 5,
      borderRadius: 3,
      backgroundColor: '#E5E7EB',
  },
  
  // Compact Weather
  compactWeatherContainer: {
      paddingHorizontal: 20,
      paddingBottom: 10,
      borderBottomWidth: 1,
      borderBottomColor: '#F3F4F6',
  },
  weatherRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      backgroundColor: '#F0F9FB',
      borderRadius: 12,
      paddingHorizontal: 12,
      paddingVertical: 8,
  },
  weatherMain: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 8,
  },
  weatherTemp: {
      fontSize: 18,
      fontWeight: '700',
      color: '#222',
  },
  weatherDesc: {
      fontSize: 14,
      color: '#666',
      textTransform: 'capitalize',
  },
  weatherDetails: {
      flexDirection: 'row',
      gap: 12,
  },
  weatherDetailText: {
      fontSize: 12,
      color: '#666',
  },

  // Sheet Content
  sheetContent: {
      flex: 1,
      paddingTop: 16,
  },
  sheetTitle: {
      fontSize: 18,
      fontWeight: '700',
      color: '#222',
      marginHorizontal: 20,
      marginBottom: 16,
  },
  activitiesScrollContent: {
      paddingHorizontal: 20,
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
    alignItems: 'center',
  },
  mapActivityTimeText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#0d9488',
    marginBottom: 4,
  },
  mapTimeConnector: {
      width: 2,
      height: 20,
      backgroundColor: '#E5E7EB',
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
    marginBottom: 4,
  },
  mapActivityImage: {
    width: 60,
    height: 60,
    borderRadius: 12,
    marginLeft: 12,
  },
  mapTransportInfo: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 4,
  },
  mapTransportText: {
      fontSize: 12,
      color: '#666',
  },
  mapTransportBadge: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 4,
      paddingHorizontal: 8,
      paddingVertical: 4,
      borderRadius: 12,
      borderWidth: 1,
      borderColor: '#FFF',
      shadowColor: '#000',
      shadowOffset: { width: 0, height: 1 },
      shadowOpacity: 0.2,
      shadowRadius: 2,
      elevation: 3,
  },
  mapTransportBadgeText: {
      fontSize: 10,
      color: '#FFF',
      fontWeight: '700',
  },
  
  // Markers
  markerContainer: {
      alignItems: 'center',
  },
  marker: {
      width: 30,
      height: 30,
      borderRadius: 15,
      backgroundColor: '#0d9488',
      alignItems: 'center',
      justifyContent: 'center',
      borderWidth: 2,
      borderColor: '#FFF',
      shadowColor: '#000',
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.3,
      shadowRadius: 3,
      elevation: 5,
  },
  markerText: {
      color: '#FFF',
      fontSize: 13,
      fontWeight: '700',
  },
});
