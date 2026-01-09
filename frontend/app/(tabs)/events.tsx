import React, { useState, useEffect, useRef } from 'react';
import { 
  View, 
  Text, 
  StyleSheet, 
  ScrollView, 
  TextInput, 
  TouchableOpacity, 
  Alert,
  ActivityIndicator,
  Modal,
  Platform
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Image } from 'expo-image';
import DateTimePicker from '@react-native-community/datetimepicker';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { useRouter, useFocusEffect } from 'expo-router';
import { api } from '../../lib/api';

// Color constants based on app theme
const colors = {
  primary: '#5C9B9B',
  primaryDark: '#4A8080',
  primaryLight: '#7BB3B3',
  background: '#F7F9FC',
  backgroundCard: '#FFFFFF',
  text: '#1A202C',
  textSecondary: '#718096',
  border: '#E2E8F0',
  shadow: '#000000',
  success: '#38A169',
  error: '#E53E3E',
  warning: '#D69E2E',
  accent: '#FF6B6B',
  gradientStart: '#5C9B9B',
  gradientEnd: '#68D391'
};

interface EventVenue {
  name: string;
  address?: string;
  city?: string;
  country?: string;
  latitude?: number;
  longitude?: number;
}

interface EventPrice {
  min?: number;
  max?: number;
  currency: string;
}

interface Event {
  id: string;
  name: string;
  description?: string;
  startDate: string;
  endDate?: string;
  imageUrl?: string;
  venue?: EventVenue;
  priceRange?: EventPrice;
  eventType?: string;
  genre?: string;
  ticketUrl?: string;
  source: string;
}

interface EventSearchRequest {
  location: string;
  startDate: string;
  endDate: string;
  eventType?: string;
  radius?: number;
  limit?: number;
}

interface EventSearchResponse {
  events: Event[];
  totalCount: number;
  searchLocation: string;
  searchStartDate: string;
  searchEndDate: string;
}

const eventTypes = [
  { id: '', name: 'All Types' },
  { id: 'music', name: 'Music' },
  { id: 'sports', name: 'Sports' },
  { id: 'arts', name: 'Arts & Theatre' },
  { id: 'family', name: 'Family' },
  { id: 'film', name: 'Film' },
  { id: 'miscellaneous', name: 'Miscellaneous' }
];

export default function EventsScreen() {
  const router = useRouter();
  
  // Bugünün tarihini al
  const getTodayDate = () => {
    const today = new Date();
    return today.toISOString().split('T')[0];
  };
  
  // Bir hafta sonraki tarihi al
  const getWeekLaterDate = () => {
    const today = new Date();
    const weekLater = new Date(today);
    weekLater.setDate(today.getDate() + 7);
    return weekLater.toISOString().split('T')[0];
  };

  const [location, setLocation] = useState(''); // Boş başlasın
  const [startDate, setStartDate] = useState(getTodayDate());
  const [endDate, setEndDate] = useState(getWeekLaterDate());
  const [eventType, setEventType] = useState('');
  const [showEventTypeModal, setShowEventTypeModal] = useState(false);
  const [showStartDatePicker, setShowStartDatePicker] = useState(false);
  const [showEndDatePicker, setShowEndDatePicker] = useState(false);
  const [events, setEvents] = useState<Event[]>([]);
  const [loading, setLoading] = useState(false);
  const [totalCount, setTotalCount] = useState(0);
  const [failedImages, setFailedImages] = useState<Set<string>>(new Set());
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  const scrollViewRef = useRef<ScrollView>(null);
  const eventsListRef = useRef<View>(null);

  useEffect(() => {
    checkAuthStatus();
  }, []);

  // Sayfa focus edildiğinde verileri temizle ve tarihleri sıfırla
  useFocusEffect(
    React.useCallback(() => {
      console.log('🔄 Events screen focused - resetting data');
      setEvents([]);
      setTotalCount(0);
      setFailedImages(new Set());
      setStartDate(getTodayDate());
      setEndDate(getWeekLaterDate());
      setEventType('');
      setLocation(''); // Location alanını da boşalt
    }, [])
  );

  const checkAuthStatus = async () => {
    try {
      const token = await AsyncStorage.getItem('accessToken');
      console.log('🔐 Auth check - Token exists:', token ? 'YES' : 'NO');
      setIsAuthenticated(!!token);
      
      if (!token) {
        Alert.alert(
          'Authentication Required',
          'Please login to search for events',
          [{ text: 'OK' }]
        );
      }
    } catch (error) {
      console.error('Error checking auth status:', error);
      setIsAuthenticated(false);
    }
  };

  const searchEvents = async () => {
    if (!isAuthenticated) {
      Alert.alert('Authentication Required', 'Please login first to search for events');
      return;
    }

    if (!location.trim()) {
      Alert.alert('Error', 'Please enter a location');
      return;
    }

    if (!startDate || !endDate) {
      Alert.alert('Error', 'Please enter both start and end dates');
      return;
    }

    // Tarih validasyonu - geçmiş tarih kontrolü
    const today = new Date();
    today.setHours(0, 0, 0, 0); // Bugünün başlangıcı
    
    const selectedStartDate = new Date(startDate);
    const selectedEndDate = new Date(endDate);
    
    if (selectedStartDate < today) {
      Alert.alert('Invalid Date', 'Start date cannot be in the past. Please select today or a future date.');
      return;
    }
    
    if (selectedEndDate < selectedStartDate) {
      Alert.alert('Invalid Date', 'End date must be after start date.');
      return;
    }

    // Maksimum 1 yıl kontrolü
    const oneYearLater = new Date(selectedStartDate);
    oneYearLater.setFullYear(oneYearLater.getFullYear() + 1);
    
    if (selectedEndDate > oneYearLater) {
      Alert.alert('Invalid Date', 'Event search period cannot exceed 1 year.');
      return;
    }

    setLoading(true);
    try {
      console.log('🔍 Starting event search...');
      console.log('📍 Location:', location.trim());
      console.log('📅 Date range:', startDate, 'to', endDate);
      
      const searchRequest: EventSearchRequest = {
        location: location.trim(),
        startDate: startDate + 'T00:00:00.000Z',
        endDate: endDate + 'T23:59:59.999Z',
        eventType: eventType || undefined,
        radius: 50,
        limit: 20
      };

      console.log('📤 Request payload:', searchRequest);
      
      const response = await api.post<EventSearchResponse>('/api/events/search', searchRequest);
      
      console.log('📥 API Response status:', response.status);
      console.log('📥 API Response data:', response.data);
      
      if (response.data) {
        console.log(`✅ Found ${response.data.events.length} events`);
        response.data.events.forEach((event, index) => {
          console.log(`Event ${index + 1}: ${event.name}, ImageURL: ${event.imageUrl}`);
        });
        setEvents(response.data.events);
        setTotalCount(response.data.totalCount);
        
        // Auto-scroll after search results are loaded
        setTimeout(() => {
          // Scroll to approximately where events start (after search form)
          scrollViewRef.current?.scrollTo({
            x: 0,
            y: 600, // Approximate position after search form
            animated: true
          });
        }, 500); // Small delay to ensure UI has updated and data is rendered
      }
    } catch (error: any) {
      console.error('❌ Error searching events:', error);
      console.error('❌ Error response:', error.response?.data);
      console.error('❌ Error status:', error.response?.status);
      
      let errorMessage = 'Failed to search events';
      
      if (error.response?.status === 401) {
        errorMessage = 'Authentication required. Please login again.';
        setIsAuthenticated(false);
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      }
      
      Alert.alert('Error', errorMessage);
      setEvents([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const formatPrice = (priceRange?: EventPrice) => {
    if (!priceRange) return '';
    
    const { min, max, currency } = priceRange;
    
    if (min && max) {
      return `${currency} ${min} - ${max}`;
    } else if (min) {
      return `From ${currency} ${min}`;
    } else if (max) {
      return `Up to ${currency} ${max}`;
    }
    
    return '';
  };

  const selectEventType = (typeId: string) => {
    setEventType(typeId);
    setShowEventTypeModal(false);
  };

  const onStartDateChange = (event: any, selectedDate?: Date) => {
    if (Platform.OS === 'android') {
      setShowStartDatePicker(false);
    }
    if (selectedDate && event.type !== 'dismissed') {
      const formattedDate = selectedDate.toISOString().split('T')[0];
      setStartDate(formattedDate);
      if (Platform.OS === 'ios') {
        setShowStartDatePicker(false);
      }
    }
  };

  const onEndDateChange = (event: any, selectedDate?: Date) => {
    if (Platform.OS === 'android') {
      setShowEndDatePicker(false);
    }
    if (selectedDate && event.type !== 'dismissed') {
      const formattedDate = selectedDate.toISOString().split('T')[0];
      setEndDate(formattedDate);
      if (Platform.OS === 'ios') {
        setShowEndDatePicker(false);
      }
    }
  };

  const formatDisplayDate = (dateString: string) => {
    const date = new Date(dateString);
    const day = date.getDate();
    const month = date.toLocaleDateString('tr-TR', { month: 'short' });
    const year = date.getFullYear();
    return `${day} ${month} ${year}`;
  };

  const getEventTypeColor = (eventType: string) => {
    switch (eventType.toLowerCase()) {
      case 'music': return '#FF6B6B';
      case 'sports': return '#4ECDC4'; 
      case 'arts':
      case 'theatre':
      case 'theater': return '#9B59B6';
      case 'family': return '#F39C12';
      case 'film': return '#34495E';
      case 'comedy': return '#E74C3C';
      case 'dance': return '#E91E63';
      case 'festival': return '#FF9800';
      default: return colors.primary;
    }
  };

  const getEventTypeIconSmall = (eventType: string) => {
    const iconColor = getEventTypeColor(eventType);
    const iconSize = 24;
    
    switch (eventType.toLowerCase()) {
      case 'music': return <Ionicons name="musical-notes" size={iconSize} color={iconColor} />;
      case 'sports': return <Ionicons name="basketball" size={iconSize} color={iconColor} />;
      case 'arts':
      case 'theatre':
      case 'theater': return <Ionicons name="ticket" size={iconSize} color={iconColor} />;
      case 'family': return <Ionicons name="people" size={iconSize} color={iconColor} />;
      case 'film': return <Ionicons name="videocam" size={iconSize} color={iconColor} />;
      case 'comedy': return <Ionicons name="happy" size={iconSize} color={iconColor} />;
      case 'dance': return <Ionicons name="body" size={iconSize} color={iconColor} />;
      case 'festival': return <Ionicons name="balloon" size={iconSize} color={iconColor} />;
      default: return <Ionicons name="calendar" size={iconSize} color={iconColor} />;
    }
  };

  return (
    <ScrollView ref={scrollViewRef} style={styles.container} showsVerticalScrollIndicator={false}>
      {/* Enhanced Header with Modern Design */}
      <View style={styles.header}>
        <View style={styles.gradientOverlay} />
        <View style={styles.headerContent}>
          <View style={styles.headerTopRow}>
            <TouchableOpacity 
              style={styles.backButton} 
              onPress={() => router.push('/(tabs)')}
              activeOpacity={0.8}
            >
              <Ionicons name="chevron-back" size={24} color={colors.primary} />
            </TouchableOpacity>
            <View style={styles.titleContainer}>
              <View style={styles.iconWrapper}>
                <Ionicons name="calendar" size={28} color="white" />
              </View>
              <Text style={styles.title}>Find Events</Text>
            </View>
          </View>
          <Text style={styles.subtitle}>Discover amazing concerts, sports, and shows worldwide</Text>
        </View>
      </View>

      {/* Enhanced Search Form */}
      <View style={styles.searchForm}>
        <View style={styles.formHeader}>
          <Ionicons name="search" size={24} color={colors.primary} />
          <Text style={styles.formTitle}>Search Events</Text>
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>
            <Ionicons name="location" size={16} color={colors.primary} /> Location
          </Text>
          <View style={styles.inputContainer}>
            <Ionicons name="location-outline" size={20} color={colors.textSecondary} />
            <TextInput
              style={styles.input}
              placeholder="Enter city or destination"
              value={location}
              onChangeText={setLocation}
              placeholderTextColor={colors.textSecondary}
            />
          </View>
        </View>

        <View style={styles.dateRow}>
          <View style={styles.dateGroup}>
            <Text style={styles.label}>
              <Ionicons name="calendar" size={16} color={colors.primary} /> Start Date
            </Text>
            <TouchableOpacity 
              style={styles.dateButton}
              onPress={() => setShowStartDatePicker(true)}
            >
              <View style={styles.dateButtonContent}>
                <Ionicons name="calendar-outline" size={20} color={colors.primary} />
                <Text style={styles.dateButtonText}>
                  {formatDisplayDate(startDate)}
                </Text>
              </View>
              <Ionicons name="chevron-down" size={16} color={colors.primary} />
            </TouchableOpacity>
          </View>

          <View style={styles.dateGroup}>
            <Text style={styles.label}>
              <Ionicons name="calendar" size={16} color={colors.primary} /> End Date
            </Text>
            <TouchableOpacity 
              style={styles.dateButton}
              onPress={() => setShowEndDatePicker(true)}
            >
              <View style={styles.dateButtonContent}>
                <Ionicons name="calendar-outline" size={20} color={colors.primary} />
                <Text style={styles.dateButtonText}>
                  {formatDisplayDate(endDate)}
                </Text>
              </View>
              <Ionicons name="chevron-down" size={16} color={colors.primary} />
            </TouchableOpacity>
          </View>
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>
            <Ionicons name="grid" size={16} color={colors.primary} /> Event Type
          </Text>
          <TouchableOpacity 
            style={styles.eventTypeButton}
            onPress={() => setShowEventTypeModal(true)}
          >
            <View style={styles.eventTypeButtonContent}>
              <Ionicons name="apps-outline" size={20} color={colors.primary} />
              <Text style={styles.eventTypeButtonText}>
                {eventTypes.find(t => t.id === eventType)?.name || 'All Types'}
              </Text>
            </View>
            <Ionicons name="chevron-down" size={16} color={colors.primary} />
          </TouchableOpacity>
        </View>

        <TouchableOpacity
          style={[
            styles.searchButton, 
            (loading || !isAuthenticated) && styles.searchButtonDisabled
          ]}
          onPress={searchEvents}
          disabled={loading || !isAuthenticated}
        >
          {loading ? (
            <ActivityIndicator color="white" />
          ) : (
            <>
              <Ionicons name="search" size={20} color="white" />
              <Text style={styles.searchButtonText}>
                {isAuthenticated ? 'Search Events' : 'Login Required'}
              </Text>
            </>
          )}
        </TouchableOpacity>
      </View>

      {/* Date Pickers - iOS Modal Style */}
      {showStartDatePicker && Platform.OS === 'ios' && (
        <Modal
          visible={showStartDatePicker}
          transparent={true}
          animationType="slide"
          onRequestClose={() => setShowStartDatePicker(false)}
        >
          <View style={styles.datePickerModalOverlay}>
            <View style={styles.datePickerModalContent}>
              <View style={styles.datePickerHeader}>
                <TouchableOpacity onPress={() => setShowStartDatePicker(false)}>
                  <Text style={styles.datePickerCancel}>Cancel</Text>
                </TouchableOpacity>
                <Text style={styles.datePickerTitle}>Select Start Date</Text>
                <TouchableOpacity onPress={() => setShowStartDatePicker(false)}>
                  <Text style={styles.datePickerDone}>Done</Text>
                </TouchableOpacity>
              </View>
              <DateTimePicker
                value={new Date(startDate)}
                mode="date"
                display="spinner"
                onChange={onStartDateChange}
                minimumDate={new Date()}
                style={styles.datePicker}
              />
            </View>
          </View>
        </Modal>
      )}

      {showEndDatePicker && Platform.OS === 'ios' && (
        <Modal
          visible={showEndDatePicker}
          transparent={true}
          animationType="slide"
          onRequestClose={() => setShowEndDatePicker(false)}
        >
          <View style={styles.datePickerModalOverlay}>
            <View style={styles.datePickerModalContent}>
              <View style={styles.datePickerHeader}>
                <TouchableOpacity onPress={() => setShowEndDatePicker(false)}>
                  <Text style={styles.datePickerCancel}>Cancel</Text>
                </TouchableOpacity>
                <Text style={styles.datePickerTitle}>Select End Date</Text>
                <TouchableOpacity onPress={() => setShowEndDatePicker(false)}>
                  <Text style={styles.datePickerDone}>Done</Text>
                </TouchableOpacity>
              </View>
              <DateTimePicker
                value={new Date(endDate)}
                mode="date"
                display="spinner"
                onChange={onEndDateChange}
                minimumDate={new Date(startDate)}
                style={styles.datePicker}
              />
            </View>
          </View>
        </Modal>
      )}

      {/* Android Date Pickers */}
      {showStartDatePicker && Platform.OS === 'android' && (
        <DateTimePicker
          value={new Date(startDate)}
          mode="date"
          display="default"
          onChange={onStartDateChange}
          minimumDate={new Date()}
        />
      )}

      {showEndDatePicker && Platform.OS === 'android' && (
        <DateTimePicker
          value={new Date(endDate)}
          mode="date"
          display="default"
          onChange={onEndDateChange}
          minimumDate={new Date(startDate)}
        />
      )}

      {totalCount > 0 && (
        <View style={styles.resultsHeader}>
          <Ionicons name="list" size={20} color={colors.primary} />
          <Text style={styles.resultsCount}>
            Found {totalCount} events in {location}
          </Text>
        </View>
      )}

      {/* No Events Found Message */}
      {!loading && events.length === 0 && totalCount === 0 && (
        <View style={styles.noEventsContainer}>
          <Ionicons name="calendar-outline" size={64} color={colors.textSecondary} />
          <Text style={styles.noEventsTitle}>No Events Found</Text>
          <Text style={styles.noEventsSubtitle}>
            No events were found for your search criteria. Try adjusting your location, dates, or event type.
          </Text>
        </View>
      )}

      <View ref={eventsListRef} style={styles.eventsList}>
        {events.map((event) => {
          return (
          <View key={event.id} style={styles.eventCard}>
            <View style={styles.eventContent}>
              <View style={styles.eventHeader}>
                <View style={styles.eventTitleSection}>
                  <View style={styles.eventTypeIconSmall}>
                    {getEventTypeIconSmall(event.eventType || 'miscellaneous')}
                  </View>
                  <Text style={styles.eventName} numberOfLines={2}>
                    {event.name}
                  </Text>
                </View>
                {event.eventType && (
                  <View style={styles.eventTypeTag}>
                    <Text style={styles.eventTypeText}>{event.eventType}</Text>
                  </View>
                )}
              </View>

              <View style={styles.eventDetails}>
                <View style={styles.eventDetailRow}>
                  <Ionicons name="calendar" size={16} color={colors.textSecondary} />
                  <Text style={styles.eventDetailText}>
                    {formatDate(event.startDate)}
                  </Text>
                </View>

                {event.venue && (
                  <View style={styles.eventDetailRow}>
                    <Ionicons name="location" size={16} color={colors.textSecondary} />
                    <Text style={styles.eventDetailText} numberOfLines={1}>
                      {event.venue.name}
                      {event.venue.city && `, ${event.venue.city}`}
                    </Text>
                  </View>
                )}

                {event.priceRange && (
                  <View style={styles.eventDetailRow}>
                    <Ionicons name="pricetag" size={16} color={colors.textSecondary} />
                    <Text style={styles.eventDetailText}>
                      {formatPrice(event.priceRange)}
                    </Text>
                  </View>
                )}
              </View>

              {event.description && (
                <Text style={styles.eventDescription} numberOfLines={3}>
                  {event.description}
                </Text>
              )}

              <View style={styles.eventFooter}>
                <Text style={styles.eventSource}>via {event.source}</Text>
                {event.ticketUrl && (
                  <TouchableOpacity style={styles.ticketButton}>
                    <Text style={styles.ticketButtonText}>Get Tickets</Text>
                    <Ionicons name="open-outline" size={16} color="white" />
                  </TouchableOpacity>
                )}
              </View>
            </View>
          </View>
        )})}
      </View>

      {/* Event Type Modal */}
      <Modal
        visible={showEventTypeModal}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setShowEventTypeModal(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Select Event Type</Text>
              <TouchableOpacity 
                onPress={() => setShowEventTypeModal(false)}
                style={styles.modalCloseButton}
              >
                <Ionicons name="close" size={24} color={colors.textSecondary} />
              </TouchableOpacity>
            </View>
            
            <ScrollView style={styles.eventTypeList}>
              {eventTypes.map((type) => (
                <TouchableOpacity
                  key={type.id}
                  style={[
                    styles.eventTypeItem,
                    eventType === type.id && styles.eventTypeItemSelected
                  ]}
                  onPress={() => selectEventType(type.id)}
                >
                  <Text style={[
                    styles.eventTypeItemText,
                    eventType === type.id && styles.eventTypeItemTextSelected
                  ]}>
                    {type.name}
                  </Text>
                  {eventType === type.id && (
                    <Ionicons name="checkmark" size={20} color={colors.primary} />
                  )}
                </TouchableOpacity>
              ))}
            </ScrollView>
          </View>
        </View>
      </Modal>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  iconWrapper: {
    marginRight: 12,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    padding: 10,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  header: {
    backgroundColor: colors.primary,
    paddingTop: 65,
    paddingBottom: 32,
    paddingHorizontal: 20,
    marginBottom: 8,
    shadowColor: colors.shadow,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.25,
    shadowRadius: 16,
    elevation: 12,
    position: 'relative',
    overflow: 'hidden',
  },
  gradientOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: `linear-gradient(135deg, ${colors.gradientStart} 0%, ${colors.gradientEnd} 100%)`,
    opacity: 0.1,
  },
  headerContent: {
    alignItems: 'flex-start',
    paddingHorizontal: 4,
  },
  headerTopRow: {
    flexDirection: 'row',
    alignItems: 'center',
    width: '100%',
    marginBottom: 8,
  },
  backButton: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: 'rgba(255, 255, 255, 0.9)',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 16,
    shadowColor: colors.shadow,
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 6,
    elevation: 4,
  },
  titleContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 16,
    flex: 1,
  },
  headerIcon: {
    marginRight: 0,
    backgroundColor: 'rgba(255, 255, 255, 0.2)',
    padding: 10,
    borderRadius: 16,
  },
  title: {
    fontSize: 28,
    fontWeight: '800',
    color: '#FFFFFF',
    marginBottom: 0,
    letterSpacing: -0.5,
  },
  subtitle: {
    fontSize: 16,
    color: 'rgba(255, 255, 255, 0.8)',
    lineHeight: 22,
    marginLeft: 0,
    paddingLeft: 0,
    fontWeight: '500',
  },
  searchForm: {
    backgroundColor: colors.backgroundCard,
    marginHorizontal: 16,
    borderRadius: 24,
    padding: 28,
    marginTop: -16,
    marginBottom: 20,
    shadowColor: colors.shadow,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.15,
    shadowRadius: 16,
    elevation: 8,
    borderWidth: 1,
    borderColor: 'rgba(27, 157, 157, 0.1)',
  },
  formHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    marginBottom: 20,
    paddingBottom: 16,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  formTitle: {
    fontSize: 22,
    fontWeight: '800',
    color: colors.text,
    letterSpacing: -0.3,
  },
  inputGroup: {
    marginBottom: 20,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
    marginBottom: 10,
    flexDirection: 'row',
    alignItems: 'center',
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 2,
    borderColor: colors.border,
    borderRadius: 16,
    paddingHorizontal: 16,
    paddingVertical: 14,
    backgroundColor: colors.background,
    gap: 12,
  },
  input: {
    flex: 1,
    fontSize: 16,
    color: colors.text,
  },
  dateRow: {
    flexDirection: 'row',
    gap: 16,
    marginBottom: 20,
  },
  dateGroup: {
    flex: 1,
    minWidth: 0, // This ensures proper text wrapping
  },
  dateButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderWidth: 2,
    borderColor: colors.border,
    borderRadius: 16,
    paddingHorizontal: 16,
    paddingVertical: 14,
    backgroundColor: colors.background,
    minHeight: 52,
  },
  dateButtonContent: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    flex: 1,
  },
  dateButtonText: {
    fontSize: 16,
    color: colors.text,
    fontWeight: '500',
    flex: 1,
    textAlign: 'left',
  },
  eventTypeButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderWidth: 2,
    borderColor: colors.border,
    borderRadius: 16,
    paddingHorizontal: 16,
    paddingVertical: 14,
    backgroundColor: colors.background,
  },
  eventTypeButtonContent: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  eventTypeButtonText: {
    fontSize: 16,
    color: colors.text,
    fontWeight: '500',
  },
  searchButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.primary,
    borderRadius: 18,
    paddingVertical: 20,
    paddingHorizontal: 24,
    gap: 12,
    marginTop: 12,
    shadowColor: colors.primary,
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.4,
    shadowRadius: 12,
    elevation: 8,
    transform: [{ scale: 1 }],
  },
  searchButtonDisabled: {
    backgroundColor: colors.textSecondary,
    shadowOpacity: 0,
    elevation: 0,
  },
  searchButtonText: {
    color: 'white',
    fontSize: 18,
    fontWeight: '800',
    letterSpacing: 0.5,
    textTransform: 'uppercase',
  },
  resultsHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginHorizontal: 20,
    marginBottom: 16,
    marginTop: 16,
  },
  resultsCount: {
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
  },
  eventsList: {
    paddingHorizontal: 16,
    paddingBottom: 20,
  },
  eventCard: {
    backgroundColor: colors.backgroundCard,
    borderRadius: 20,
    marginBottom: 20,
    overflow: 'hidden',
    shadowColor: colors.shadow,
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.12,
    shadowRadius: 16,
    elevation: 8,
    borderWidth: 1,
    borderColor: colors.border + '50',
  },
  placeholderText: {
    fontSize: 12,
    color: colors.textSecondary,
    marginTop: 8,
  },
  eventContent: {
    padding: 24,
  },
  eventHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    marginBottom: 16,
  },
  eventTitleSection: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
    marginRight: 12,
  },
  eventTypeIconSmall: {
    marginRight: 12,
    padding: 8,
    borderRadius: 8,
    backgroundColor: colors.background,
  },
  eventName: {
    fontSize: 22,
    fontWeight: '700',
    color: colors.text,
    flex: 1,
    lineHeight: 28,
  },
  eventTypeTag: {
    backgroundColor: colors.primary + '20',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
  },
  eventTypeText: {
    fontSize: 12,
    color: colors.primary,
    fontWeight: '600',
  },
  eventDetails: {
    marginBottom: 16,
    gap: 12,
  },
  eventDetailRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    backgroundColor: colors.background,
    padding: 12,
    borderRadius: 12,
  },
  eventDetailText: {
    fontSize: 15,
    color: colors.textSecondary,
    flex: 1,
    fontWeight: '500',
  },
  eventDescription: {
    fontSize: 14,
    color: colors.textSecondary,
    lineHeight: 20,
    marginBottom: 16,
    backgroundColor: colors.background,
    padding: 16,
    borderRadius: 12,
  },
  eventFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  eventSource: {
    fontSize: 12,
    color: colors.textSecondary,
    fontStyle: 'italic',
  },
  ticketButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    backgroundColor: colors.primary,
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 20,
  },
  ticketButtonText: {
    fontSize: 14,
    color: 'white',
    fontWeight: '600',
  },
  // Modal styles
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: 'white',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    maxHeight: '70%',
    shadowColor: colors.shadow,
    shadowOffset: { width: 0, height: -4 },
    shadowOpacity: 0.2,
    shadowRadius: 12,
    elevation: 8,
  },
  modalHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: 24,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: '700',
    color: colors.text,
  },
  modalCloseButton: {
    padding: 8,
    borderRadius: 20,
    backgroundColor: colors.background,
  },
  eventTypeList: {
    paddingHorizontal: 24,
    paddingBottom: 20,
  },
  eventTypeItem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 18,
    paddingHorizontal: 16,
    borderRadius: 12,
    marginVertical: 4,
  },
  eventTypeItemSelected: {
    backgroundColor: colors.primary + '15',
    borderWidth: 2,
    borderColor: colors.primary + '40',
  },
  eventTypeItemText: {
    fontSize: 16,
    color: colors.text,
    fontWeight: '500',
  },
  eventTypeItemTextSelected: {
    color: colors.primary,
    fontWeight: '600',
  },
  modalButton: {
    fontSize: 16,
    color: colors.primary,
    fontWeight: '500',
  },
  // Date Picker Modal styles
  datePickerModalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-end',
  },
  datePickerModalContent: {
    backgroundColor: 'white',
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    paddingBottom: 40,
  },
  datePickerHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: 20,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  datePickerTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
  },
  datePickerCancel: {
    fontSize: 16,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  datePickerDone: {
    fontSize: 16,
    color: colors.primary,
    fontWeight: '600',
  },
  datePicker: {
    backgroundColor: 'white',
  },
  // No Events Found styles
  noEventsContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
    paddingVertical: 64,
    marginHorizontal: 16,
    backgroundColor: colors.backgroundCard,
    borderRadius: 20,
    marginTop: 20,
    marginBottom: 20,
  },
  noEventsTitle: {
    fontSize: 24,
    fontWeight: '700',
    color: colors.text,
    marginTop: 20,
    marginBottom: 12,
    textAlign: 'center',
  },
  noEventsSubtitle: {
    fontSize: 16,
    color: colors.textSecondary,
    lineHeight: 24,
    textAlign: 'center',
    maxWidth: 300,
  },
});
