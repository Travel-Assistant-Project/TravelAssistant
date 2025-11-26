// frontend/app/(tabs)/new-trip.tsx
import Slider from '@react-native-community/slider';
import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ScrollView,
  Modal,
  SafeAreaView,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useRouter } from 'expo-router';
import { api } from '@/lib/api';



export default function NewTripScreen() {
  const router = useRouter();

  const [destination, setDestination] = useState('');
  const [country, setCountry] = useState('');
  const [days, setDays] = useState('');
  const [selectedThemes, setSelectedThemes] = useState<string[]>([]);
  const [budget, setBudget] = useState('5000');
  const [activityLevel, setActivityLevel] = useState<'Relaxed' | 'Moderate' | 'Active' | null>(null);
  const [transport, setTransport] = useState<'Car' | 'Walking' | 'Public Transport' | null>(null);
  const [showDaysDropdown, setShowDaysDropdown] = useState(false);
  const [showCountryDropdown, setShowCountryDropdown] = useState(false);
  const [showCityDropdown, setShowCityDropdown] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [loadingStep, setLoadingStep] = useState(0);

  // Loading messages
  const loadingMessages = [
    'Checking location details...',
    'Processing data...',
    'Finding the best path...',
    'Preparing the map...',
  ];

  // Loading animation effect
  useEffect(() => {
    let interval: ReturnType<typeof setInterval> | undefined;
    if (isLoading) {
      setLoadingStep(0);
      interval = setInterval(() => {
        setLoadingStep((prev) => (prev + 1) % loadingMessages.length);
      }, 2000); // Her 2 saniyede bir mesaj değişir
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [isLoading, loadingMessages.length]);

  const themes = ['Design', 'Nature', 'History', 'Food'];
  const activityLevels: Array<'Relaxed' | 'Moderate' | 'Active'> = [
    'Relaxed',
    'Moderate',
    'Active',
  ];
  const transports: Array<'Car' | 'Walking' | 'Public Transport'> = [
    'Car',
    'Walking',
    'Public Transport',
  ];

  // Ülke listesi (popüler ülkeler)
  const countries = [
    'Turkey', 'United States', 'United Kingdom', 'France', 'Germany', 'Italy', 'Spain',
    'Greece', 'Netherlands', 'Belgium', 'Switzerland', 'Austria', 'Portugal', 'Ireland',
    'Japan', 'South Korea', 'China', 'Thailand', 'Singapore', 'Malaysia', 'Indonesia',
    'Australia', 'New Zealand', 'Canada', 'Mexico', 'Brazil', 'Argentina', 'Chile',
    'Egypt', 'Morocco', 'South Africa', 'Kenya', 'United Arab Emirates', 'Saudi Arabia',
    'India', 'Russia', 'Poland', 'Czech Republic', 'Hungary', 'Croatia', 'Norway',
    'Sweden', 'Denmark', 'Finland', 'Iceland'
  ].sort();

  // Ülkelere göre şehir mapping
  const citiesByCountry: { [key: string]: string[] } = {
    'Turkey': ['Istanbul', 'Ankara', 'Izmir', 'Antalya', 'Bodrum', 'Kaş', 'Fethiye', 'Cappadocia', 'Pamukkale', 'Marmaris', 'Alanya', 'Side'],
    'United States': ['New York', 'Los Angeles', 'San Francisco', 'Las Vegas', 'Miami', 'Chicago', 'Boston', 'Seattle', 'Washington DC', 'Orlando'],
    'United Kingdom': ['London', 'Edinburgh', 'Manchester', 'Liverpool', 'Oxford', 'Cambridge', 'Bath', 'York'],
    'France': ['Paris', 'Nice', 'Lyon', 'Marseille', 'Bordeaux', 'Cannes', 'Strasbourg', 'Toulouse'],
    'Germany': ['Berlin', 'Munich', 'Hamburg', 'Frankfurt', 'Cologne', 'Dresden', 'Heidelberg'],
    'Italy': ['Rome', 'Venice', 'Florence', 'Milan', 'Naples', 'Pisa', 'Verona', 'Bologna'],
    'Spain': ['Barcelona', 'Madrid', 'Seville', 'Valencia', 'Granada', 'Bilbao', 'Malaga'],
    'Greece': ['Athens', 'Santorini', 'Mykonos', 'Crete', 'Rhodes', 'Thessaloniki'],
    'Netherlands': ['Amsterdam', 'Rotterdam', 'The Hague', 'Utrecht', 'Eindhoven'],
    'Belgium': ['Brussels', 'Bruges', 'Antwerp', 'Ghent'],
    'Switzerland': ['Zurich', 'Geneva', 'Lucerne', 'Bern', 'Interlaken'],
    'Austria': ['Vienna', 'Salzburg', 'Innsbruck', 'Hallstatt'],
    'Portugal': ['Lisbon', 'Porto', 'Algarve', 'Sintra', 'Coimbra'],
    'Ireland': ['Dublin', 'Cork', 'Galway', 'Killarney'],
    'Japan': ['Tokyo', 'Kyoto', 'Osaka', 'Hiroshima', 'Nara', 'Fukuoka'],
    'South Korea': ['Seoul', 'Busan', 'Jeju', 'Incheon'],
    'China': ['Beijing', 'Shanghai', 'Hong Kong', 'Guangzhou', 'Xian'],
    'Thailand': ['Bangkok', 'Phuket', 'Chiang Mai', 'Pattaya', 'Krabi'],
    'Singapore': ['Singapore'],
    'Malaysia': ['Kuala Lumpur', 'Penang', 'Langkawi', 'Malacca'],
    'Indonesia': ['Bali', 'Jakarta', 'Yogyakarta', 'Lombok'],
    'Australia': ['Sydney', 'Melbourne', 'Brisbane', 'Perth', 'Adelaide'],
    'New Zealand': ['Auckland', 'Wellington', 'Queenstown', 'Christchurch'],
    'Canada': ['Toronto', 'Vancouver', 'Montreal', 'Quebec City', 'Ottawa'],
    'Mexico': ['Mexico City', 'Cancun', 'Playa del Carmen', 'Tulum', 'Guadalajara'],
    'Brazil': ['Rio de Janeiro', 'São Paulo', 'Salvador', 'Brasilia'],
    'Argentina': ['Buenos Aires', 'Mendoza', 'Bariloche', 'Cordoba'],
    'Chile': ['Santiago', 'Valparaiso', 'San Pedro de Atacama'],
    'Egypt': ['Cairo', 'Luxor', 'Aswan', 'Alexandria', 'Sharm El Sheikh'],
    'Morocco': ['Marrakech', 'Casablanca', 'Fes', 'Rabat', 'Tangier'],
    'South Africa': ['Cape Town', 'Johannesburg', 'Durban', 'Pretoria'],
    'Kenya': ['Nairobi', 'Mombasa', 'Masai Mara'],
    'United Arab Emirates': ['Dubai', 'Abu Dhabi', 'Sharjah'],
    'Saudi Arabia': ['Riyadh', 'Jeddah', 'Mecca', 'Medina'],
    'India': ['Mumbai', 'Delhi', 'Bangalore', 'Jaipur', 'Agra', 'Goa'],
    'Russia': ['Moscow', 'St Petersburg', 'Kazan', 'Sochi'],
    'Poland': ['Warsaw', 'Krakow', 'Gdansk', 'Wroclaw'],
    'Czech Republic': ['Prague', 'Brno', 'Cesky Krumlov'],
    'Hungary': ['Budapest', 'Debrecen', 'Eger'],
    'Croatia': ['Zagreb', 'Dubrovnik', 'Split', 'Pula'],
    'Norway': ['Oslo', 'Bergen', 'Tromso', 'Stavanger'],
    'Sweden': ['Stockholm', 'Gothenburg', 'Malmo', 'Uppsala'],
    'Denmark': ['Copenhagen', 'Aarhus', 'Odense'],
    'Finland': ['Helsinki', 'Rovaniemi', 'Tampere'],
    'Iceland': ['Reykjavik', 'Akureyri'],
  };

  // Seçilen ülkeye göre şehirleri filtrele
  const getAvailableCities = () => {
    if (!country) return [];
    return citiesByCountry[country] || [];
  };

  const toggleTheme = (theme: string) => {
    setSelectedThemes(prev =>
      prev.includes(theme) ? prev.filter(t => t !== theme) : [...prev, theme],
    );
  };

  const handlePlanTrip = async () => {
    console.log('=== handlePlanTrip STARTED ===');
    
    // Validation
    if (!destination.trim()) {
      Alert.alert('Error', 'Please enter a destination');
      return;
    }
    if (!country.trim()) {
      Alert.alert('Error', 'Please select a country');
      return;
    }
    if (!days) {
      Alert.alert('Error', 'Please select number of days');
      return;
    }
    if (selectedThemes.length === 0) {
      Alert.alert('Error', 'Please select at least one theme');
      return;
    }
    if (!activityLevel) {
      Alert.alert('Error', 'Please select activity level');
      return;
    }
    if (!transport) {
      Alert.alert('Error', 'Please select transport mode');
      return;
    }

    console.log('Validation passed, starting request...');
    setIsLoading(true);

    try {
      // Theme mapping: frontend -> backend
      const themeMap: { [key: string]: number } = {
        'Design': 1,
        'Nature': 1,
        'History': 3,
        'Food': 5,
      };
      
      // Budget mapping (₺ to level)
      const budgetValue = Number.parseInt(budget);
      let budgetLevel = 1; // Low
      if (budgetValue > 30000) budgetLevel = 3; // High
      else if (budgetValue > 10000) budgetLevel = 2; // Medium

      // Intensity mapping
      const intensityMap: { [key: string]: number } = {
        'Relaxed': 1,
        'Moderate': 2,
        'Active': 2,
      };

      // Transport mapping
      const transportMap: { [key: string]: number } = {
        'Car': 1,
        'Walking': 2,
        'Public Transport': 3,
      };

      // Backend isteği
      console.log('Sending request to backend:', {
        region: destination,
        days: Number.parseInt(days),
        theme: themeMap[selectedThemes[0]] || 1,
        budget: budgetLevel,
        intensity: intensityMap[activityLevel] || 1,
        transport: transportMap[transport] || 1,
      });

      const response = await api.post('/api/Routes/plan', {
        region: destination,
        days: Number.parseInt(days),
        theme: themeMap[selectedThemes[0]] || 1,
        budget: budgetLevel,
        intensity: intensityMap[activityLevel] || 1,
        transport: transportMap[transport] || 1,
      });

      console.log('Backend response:', response.data);
      
      const itineraryId = response.data.itineraryId;
      
      if (!itineraryId) {
        throw new Error('No itinerary ID received from backend');
      }

      // Trip detail sayfasına yönlendir
      router.push({
        pathname: '/trip-detail',
        params: {
          itineraryId: itineraryId.toString(),
        },
      });

    } catch (error: any) {
      console.error('Error creating trip:', error);
      console.error('Error response:', error.response?.data);
      console.error('Error code:', error.code);
      console.error('Error message:', error.message);
      
      let errorMessage = 'Failed to create trip. Please try again.';
      
      if (error.code === 'ECONNABORTED') {
        errorMessage = 'Request timeout. The AI is taking longer than expected. Please try again.';
      } else if (error.code === 'ERR_NETWORK') {
        errorMessage = 'Network error. Please check your connection and make sure the backend is running.';
      } else if (error.response?.status === 401) {
        errorMessage = 'Please login first to create a trip.';
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      Alert.alert('Error', errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <SafeAreaView style={styles.root}>
      <ScrollView contentContainerStyle={styles.scroll}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.headerTitle}>Plan Your Trip</Text>
          <Text style={styles.headerSubtitle}>Create your perfect trip itinerary</Text>
        </View>

        {/* Country */}
        <View style={styles.field}>
          <Text style={styles.label}>Country</Text>
          <TouchableOpacity
            style={styles.select}
            onPress={() => setShowCountryDropdown(true)}
          >
            <Text style={[styles.selectText, !country && styles.placeholderText]}>
              {country || 'Select country'}
            </Text>
            <Text style={styles.selectIcon}>˅</Text>
          </TouchableOpacity>
        </View>

        {/* City / Region */}
        <View style={styles.field}>
          <Text style={styles.label}>City / Region</Text>
          <TouchableOpacity
            style={[styles.select, !country && styles.selectDisabled]}
            onPress={() => {
              if (country) {
                setShowCityDropdown(true);
              } else {
                Alert.alert('Please select a country first');
              }
            }}
            disabled={!country}
          >
            <Text style={[styles.selectText, !destination && styles.placeholderText]}>
              {destination || (country ? 'Select city' : 'Select country first')}
            </Text>
            <Text style={styles.selectIcon}>˅</Text>
          </TouchableOpacity>
        </View>

        {/* Days */}
        <View style={styles.field}>
          <Text style={styles.label}>How many days?</Text>
          <TouchableOpacity
            style={styles.select}
            onPress={() => setShowDaysDropdown(true)}
          >
            <Text style={[styles.selectText, !days && styles.placeholderText]}>
              {days || 'Select days'}
            </Text>
            <Text style={styles.selectIcon}>˅</Text>
          </TouchableOpacity>
        </View>

        {/* Country Modal */}
        <Modal visible={showCountryDropdown} transparent animationType="fade">
          <View style={styles.modalOverlay}>
            <View style={styles.modalBoxLarge}>
              <View style={styles.modalHeader}>
                <Text style={styles.modalHeaderText}>Select Country</Text>
              </View>
              <ScrollView style={styles.modalScroll}>
                {countries.map(c => (
                  <TouchableOpacity
                    key={c}
                    style={styles.modalItem}
                    onPress={() => {
                      setCountry(c);
                      setDestination(''); // Ülke değiştiğinde şehri sıfırla
                      setShowCountryDropdown(false);
                    }}
                  >
                    <Text style={styles.modalItemText}>{c}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
              <TouchableOpacity
                style={styles.modalCancel}
                onPress={() => setShowCountryDropdown(false)}
              >
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
            </View>
          </View>
        </Modal>

        {/* City Modal */}
        <Modal visible={showCityDropdown} transparent animationType="fade">
          <View style={styles.modalOverlay}>
            <View style={styles.modalBoxLarge}>
              <View style={styles.modalHeader}>
                <Text style={styles.modalHeaderText}>Select City in {country}</Text>
              </View>
              <ScrollView style={styles.modalScroll}>
                {getAvailableCities().length > 0 ? (
                  getAvailableCities().map(c => (
                    <TouchableOpacity
                      key={c}
                      style={styles.modalItem}
                      onPress={() => {
                        setDestination(c);
                        setShowCityDropdown(false);
                      }}
                    >
                      <Text style={styles.modalItemText}>{c}</Text>
                    </TouchableOpacity>
                  ))
                ) : (
                  <View style={styles.modalItem}>
                    <Text style={[styles.modalItemText, { color: '#9ca3af' }]}>
                      No cities available for {country}
                    </Text>
                  </View>
                )}
              </ScrollView>
              <TouchableOpacity
                style={styles.modalCancel}
                onPress={() => setShowCityDropdown(false)}
              >
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
            </View>
          </View>
        </Modal>

        {/* Days Modal */}
        <Modal visible={showDaysDropdown} transparent animationType="fade">
          <View style={styles.modalOverlay}>
            <View style={styles.modalBoxLarge}>
              <View style={styles.modalHeader}>
                <Text style={styles.modalHeaderText}>Select Days</Text>
              </View>
              <ScrollView style={styles.modalScroll}>
                {Array.from({ length: 20 }, (_, i) => (i + 1).toString()).map(num => (
                  <TouchableOpacity
                    key={num}
                    style={styles.modalItem}
                    onPress={() => {
                      setDays(num);
                      setShowDaysDropdown(false);
                    }}
                  >
                    <Text style={styles.modalItemText}>{num} {num === '1' ? 'day' : 'days'}</Text>
                  </TouchableOpacity>
                ))}
              </ScrollView>
              <TouchableOpacity
                style={styles.modalCancel}
                onPress={() => setShowDaysDropdown(false)}
              >
                <Text style={styles.modalCancelText}>Cancel</Text>
              </TouchableOpacity>
            </View>
          </View>
        </Modal>

        {/* Themes */}
        <View style={styles.field}>
          <Text style={styles.label}>Select themes</Text>
          <View style={styles.chipRow}>
            {themes.map(theme => {
              const active = selectedThemes.includes(theme);
              return (
                <TouchableOpacity
                  key={theme}
                  onPress={() => toggleTheme(theme)}
                  style={[styles.chip, active && styles.chipActive]}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {theme}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

       {/* Budget */}
<View style={styles.field}>
  <Text style={styles.label}>Budget (₺)</Text>

  <View style={{ marginTop: 8 }}>
    <Slider
      minimumValue={0}
      maximumValue={100000}      // İstersen bunu artırabiliriz
      step={100}                // 100 TL artışlarla
      value={Number(budget)}
      onValueChange={(v) => setBudget(String(Math.floor(v)))}
      minimumTrackTintColor="#0d9488"
      maximumTrackTintColor="#a7f3d0"
      thumbTintColor="#0d9488"
    />

    <Text style={styles.budgetValue}>
      {budget} ₺
    </Text>
  </View>
</View>


        {/* Activity Level */}
        <View style={styles.field}>
          <Text style={styles.label}>Activity level</Text>
          <View style={styles.chipRow}>
            {activityLevels.map(level => {
              const active = activityLevel === level;
              return (
                <TouchableOpacity
                  key={level}
                  onPress={() => setActivityLevel(level)}
                  style={[styles.chipWide, active && styles.chipActive]}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {level}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

        {/* Transport */}
        <View style={styles.field}>
          <Text style={styles.label}>Transport</Text>
          <View style={styles.chipRow}>
            {transports.map(t => {
              const active = transport === t;
              return (
                <TouchableOpacity
                  key={t}
                  onPress={() => setTransport(t)}
                  style={[styles.chipWide, active && styles.chipActive]}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {t}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

        {/* Plan Trip Button */}
        <TouchableOpacity 
          style={[styles.planBtn, isLoading && styles.planBtnDisabled]} 
          onPress={handlePlanTrip}
          disabled={isLoading}
        >
          <Text style={styles.planBtnText}>Plan Trip</Text>
        </TouchableOpacity>
      </ScrollView>

      {/* Loading Modal */}
      <Modal
        visible={isLoading}
        transparent
        animationType="fade"
      >
        <View style={styles.loadingOverlay}>
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color="#0d9488" />
            <Text style={styles.loadingText}>{loadingMessages[loadingStep]}</Text>
            <View style={styles.loadingDots}>
              {loadingMessages.map((msg, index) => (
                <View
                  key={`dot-${msg}-${index}`}
                  style={[
                    styles.dot,
                    index === loadingStep && styles.dotActive,
                  ]}
                />
              ))}
            </View>
          </View>
        </View>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: '#f9fafb',
  },
  scroll: {
    paddingHorizontal: 20,
    paddingBottom: 40,
    paddingTop: 16, // Back yazısını aşağı çekiyor
  },
  header: {
    marginBottom: 32,
    paddingTop: 8,
  },
  headerTitle: {
    fontSize: 32,
    fontWeight: '800',
    color: '#0d9488',
    marginBottom: 4,
  },
  headerSubtitle: {
    fontSize: 16,
    color: '#6b7280',
    fontWeight: '400',
  },
  field: {
    marginBottom: 24,
  },
  label: {
    fontSize: 15,
    fontWeight: '600',
    color: '#374151',
    marginBottom: 10,
  },
  input: {
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
    backgroundColor: '#ffffff',
    color: '#111827',
    fontSize: 16,
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  select: {
    borderRadius: 12,
    paddingHorizontal: 16,
    paddingVertical: 14,
    backgroundColor: '#ffffff',
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  selectDisabled: {
    backgroundColor: '#f3f4f6',
    opacity: 0.6,
  },
  selectText: {
    fontSize: 16,
    color: '#111827',
  },
  selectIcon: {
    fontSize: 16,
    color: '#4b5563',
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  chip: {
    paddingHorizontal: 18,
    paddingVertical: 10,
    borderRadius: 20,
    backgroundColor: '#f0fdfa',
    borderWidth: 1.5,
    borderColor: '#99f6e4',
  },
  chipWide: {
    flex: 1,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 20,
    backgroundColor: '#f0fdfa',
    alignItems: 'center',
    borderWidth: 1.5,
    borderColor: '#99f6e4',
  },
  chipActive: {
    backgroundColor: '#0d9488',
    borderColor: '#0d9488',
  },
  chipText: {
    fontSize: 15,
    color: '#0f766e',
    fontWeight: '600',
  },
  chipTextActive: {
    color: '#ffffff',
  },
  planBtn: {
    marginTop: 24,
    borderRadius: 16,
    backgroundColor: '#0d9488',
    paddingVertical: 16,
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 10,
    shadowColor: '#0d9488',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 5,
  },
  planBtnDisabled: {
    opacity: 0.6,
  },
  planBtnText: {
    color: '#ffffff',
    fontWeight: '700',
    fontSize: 17,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(15, 23, 42, 0.4)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  modalBox: {
    width: 200,
    backgroundColor: '#ffffff',
    borderRadius: 16,
    paddingVertical: 8,
  },
  modalBoxLarge: {
    width: '80%',
    maxHeight: '70%',
    backgroundColor: '#ffffff',
    borderRadius: 16,
    overflow: 'hidden',
  },
  modalHeader: {
    paddingVertical: 16,
    paddingHorizontal: 20,
    backgroundColor: '#f9fafb',
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
  },
  modalHeaderText: {
    fontSize: 18,
    fontWeight: '700',
    color: '#0d9488',
    textAlign: 'center',
  },
  modalScroll: {
    maxHeight: 400,
  },
  modalItem: {
    paddingVertical: 14,
    paddingHorizontal: 20,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: '#f3f4f6',
  },
  modalItemText: {
    fontSize: 16,
    color: '#111827',
  },
  modalCancel: {
    paddingVertical: 14,
    borderTopWidth: 1,
    borderTopColor: '#e5e7eb',
    alignItems: 'center',
    backgroundColor: '#ffffff',
  },
  modalCancelText: {
    color: '#ef4444',
    fontWeight: '600',
    fontSize: 16,
  },
  budgetValue: {
    marginTop: 6,
    fontSize: 16,
    fontWeight: '600',
    color: '#0f766e',
    alignSelf: 'center',
  },
  placeholderText: {
    color: '#9ca3af',
  },
  loadingOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingContainer: {
    backgroundColor: '#ffffff',
    borderRadius: 20,
    padding: 40,
    alignItems: 'center',
    minWidth: 280,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 10,
  },
  loadingText: {
    marginTop: 20,
    fontSize: 16,
    color: '#4b5563',
    fontWeight: '500',
    textAlign: 'center',
  },
  loadingDots: {
    flexDirection: 'row',
    marginTop: 20,
    gap: 8,
  },
  dot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: '#d1d5db',
  },
  dotActive: {
    backgroundColor: '#0d9488',
  },
});
